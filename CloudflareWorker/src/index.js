/**
 * Cloudflare Worker — Pokemon Companion Proxy
 *
 * Biến môi trường (Settings > Variables and Secrets):
 *   CLAUDE_API_KEY  = sk-ant-api03-...
 *   GAME_TOKEN      = chuỗi bí mật khớp với Unity Inspector
 *
 * KV Namespace (Settings > Bindings > KV Namespace):
 *   GAME_DATA       = KV namespace chứa Pokemon/Item/Story data
 */
export default {
    async fetch(request, env) {
        try {
            return await handleRequest(request, env);
        } catch (err) {
            return new Response(JSON.stringify({ error: err.message, stack: err.stack }), {
                status: 500,
                headers: { "content-type": "application/json" },
            });
        }
    },
};

async function handleRequest(request, env) {
        if (request.method === "OPTIONS") {
            return new Response(null, {
                status: 204,
                headers: {
                    "Access-Control-Allow-Origin": "*",
                    "Access-Control-Allow-Methods": "POST, OPTIONS",
                    "Access-Control-Allow-Headers": "content-type, x-game-token",
                },
            });
        }

        if (request.method !== "POST")
            return new Response("Method " + request.method + " not allowed", { status: 405 });

        const gameToken = request.headers.get("x-game-token");
        if (!env.GAME_TOKEN || gameToken !== env.GAME_TOKEN)
            return new Response("Unauthorized", { status: 401 });

        const body = JSON.parse(await request.text());
        const { gameState, messages, model, max_tokens } = body;

        // --- Fetch static data từ KV (upload 1 lần, dùng mãi) ---
        const companionKey = "pokemon_" + (gameState.companionName || "pikachu").toLowerCase().replace(" ", "_");

        const [pokemonData, storyLore, allItems] = await Promise.all([
            getKVJson(env.GAME_DATA, companionKey),
            getKVJson(env.GAME_DATA, "story_lore"),
            getKVJson(env.GAME_DATA, "all_items"),
        ]);

        // --- Build system prompt ---
        const staticPrompt  = buildStaticPrompt(gameState.companionName, pokemonData, storyLore, allItems);
        const dynamicPrompt = buildDynamicPrompt(gameState);

        // Inject system prompt vào đầu messages (key này không cho dùng system field)
        const systemContent = staticPrompt + "\n\n" + dynamicPrompt;
        const firstMsg = messages[0];
        const messagesWithSystem = [
            { role: firstMsg.role, content: systemContent + "\n\n---\n" + firstMsg.content },
            ...messages.slice(1),
        ];

        const anthropicRes = await fetch("https://api.anthropic.com/v1/messages", {
            method: "POST",
            headers: {
                "x-api-key": env.CLAUDE_API_KEY,
                "anthropic-version": "2023-06-01",
                "content-type": "application/json",
            },
            body: JSON.stringify({
                model:      model || "claude-haiku-4-5-20251001",
                max_tokens: max_tokens || 250,
                messages:   messagesWithSystem,
            }),
        });

        const responseText = await anthropicRes.text();
        if (anthropicRes.status !== 200) {
            console.error(`Anthropic error ${anthropicRes.status}: ${responseText}`);
        }
        return new Response(responseText, {
            status: anthropicRes.status,
            headers: { "content-type": "application/json" },
        });
}

// Strip BOM + parse JSON từ KV (Cloudflare không tự strip BOM)
async function getKVJson(kv, key) {
    if (!kv) return null;
    const raw = await kv.get(key);
    if (!raw) return null;
    const clean = raw.charCodeAt(0) === 0xFEFF ? raw.slice(1) : raw;
    try { return JSON.parse(clean); } catch { return null; }
}

// --- Static prompt: data từ KV (Pokemon stats, learnset, story lore, items) ---
function buildStaticPrompt(companionName, pokemonData, storyLore, allItems) {
    const parts = [];

    parts.push(`Bạn là ${companionName}, Pokemon đồng hành trung thành của người chơi Red trong thế giới Pokemon.`);
    parts.push(`Trả lời bằng tiếng Việt, ngắn gọn 1-2 câu, hơi ngơ ngác như Pokemon thật. Thỉnh thoảng xen tiếng Pokemon (${companionName}~, Pika~...). Không cần format tên nhân vật.`);

    if (pokemonData) {
        const learnsetSummary = (pokemonData.learnset || [])
            .map(l => `Lv${l.level}:${l.move}(${l.type},${l.power > 0 ? l.power + "pw" : "status"})`)
            .join(", ");

        parts.push(`\n=== THÔNG TIN ${companionName.toUpperCase()} ===`);
        parts.push(`Type: ${pokemonData.type1}${pokemonData.type2 ? "/" + pokemonData.type2 : ""}`);
        parts.push(`Base stats: HP ${pokemonData.hp}, ATK ${pokemonData.attack}, DEF ${pokemonData.defense}, SPD ${pokemonData.speed}`);
        if (learnsetSummary) parts.push(`Learnset: ${learnsetSummary}`);
    }

    if (storyLore) {
        parts.push(`\n=== BỐI CẢNH THẾ GIỚI ===`);
        if (storyLore.world)      parts.push(storyLore.world);
        if (storyLore.characters) {
            parts.push("Nhân vật:");
            for (const [name, desc] of Object.entries(storyLore.characters))
                parts.push(`- ${name}: ${desc}`);
        }
        if (storyLore.gyms) {
            parts.push("Phòng Tập:");
            for (const [name, desc] of Object.entries(storyLore.gyms))
                parts.push(`- ${name}: ${desc}`);
        }
    }

    if (allItems && allItems.length > 0) {
        const itemSummary = allItems
            .map(it => `${it.name}(${it.type}${it.healAmount > 0 ? ",heal+" + it.healAmount : ""})`)
            .join(", ");
        parts.push(`\n=== VẬT PHẨM ===\n${itemSummary}`);
    }

    return parts.join("\n");
}

// --- Dynamic prompt: runtime state từ Unity (flags, level, HP, moves) ---
function buildDynamicPrompt(gs) {
    if (!gs) return "";

    const hpRatio = gs.companionMaxHp > 0 ? gs.companionHp / gs.companionMaxHp : 1;
    const hpDesc  = hpRatio <= 0    ? "đang ngất xỉu"
                  : hpRatio <= 0.25 ? "HP rất thấp, kiệt sức"
                  : hpRatio <= 0.5  ? "HP thấp, hơi mệt"
                  : hpRatio < 1     ? "HP khá ổn"
                  : "HP đầy, năng lượng tràn trề";

    const flags = (gs.activeFlags || "").split(",").filter(Boolean);
    const storyDesc = describeStory(flags);

    const intimacyDesc = gs.intimacy <= 0  ? "mới gặp nhau"
                       : gs.intimacy <= 5  ? "đang quen dần"
                       : gs.intimacy <= 10 ? "bạn tốt"
                       : "tri kỷ thân thiết";

    const lines = [
        `\n=== TRẠNG THÁI HIỆN TẠI (theo save file) ===`,
        `Level: ${gs.companionLevel} | ${hpDesc} (${gs.companionHp}/${gs.companionMaxHp} HP)`,
        `Thân mật: ${gs.intimacy} (${intimacyDesc})`,
        `Chiêu hiện có: ${gs.currentMoves || "chưa có"}`,
        `Vị trí: ${gs.currentLocation || "không rõ"}`,
        `Hành trình: ${storyDesc}`,
    ];

    return lines.join("\n");
}

function describeStory(flags) {
    if (flags.includes("Champion"))               return "đã trở thành Champion, hoàn thành hành trình";
    if (flags.includes("AfterFireGym"))           return "đã chinh phục cả ba Phòng Tập, sắp thách thức Champion";
    if (flags.includes("OutCave"))                return "vừa ra khỏi hang, lấy lại WaterBadge từ Team Rocket";
    if (flags.includes("InCave"))                 return "đang trong hang tối, Team Rocket đã đánh cắp WaterBadge";
    if (flags.includes("AfterWaterGym"))          return "đã qua WaterGym, đang tiến tới Mountain";
    if (flags.includes("MeetTeamRocket"))         return "vừa gặp Team Rocket nguy hiểm";
    if (flags.includes("AfterGrassGym"))          return "đã qua GrassGym, hướng tới WaterGym";
    if (flags.includes("MeetBlue"))               return "vừa gặp Blue, cô ấy rất thân thiện";
    if (flags.includes("MeetGreen"))              return "vừa gặp Green, tên kiêu ngạo";
    if (flags.includes("StarterChosen"))          return "vừa chọn Pokemon đầu tiên, tiến ra Road01";
    if (flags.includes("FirstMainQuestAccepted")) return "bắt đầu hành trình, nhận nhiệm vụ đầu tiên";
    return "đang ở giai đoạn đầu cuộc phiêu lưu";
}
