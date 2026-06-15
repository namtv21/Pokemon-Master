using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools > Export Game Data to KV
/// Xuất toàn bộ Pokemon/Move/Item data ra JSON để upload lên Cloudflare KV
/// </summary>
public static class ExportGameDataToKV
{
    private static readonly string OutputDir = "KVData";

    [MenuItem("Tools/Export Game Data to KV")]
    public static void Export()
    {
        Directory.CreateDirectory(OutputDir);

        ExportAllPokemon();
        ExportAllItems();
        WriteStoryLore();
        WriteUploadScript();

        AssetDatabase.Refresh();
        Debug.Log($"[KV Export] Xong! File nằm trong thư mục '{OutputDir}/'");
        EditorUtility.DisplayDialog("Export thành công",
            $"Đã xuất data vào thư mục '{OutputDir}/'.\n\nChạy file upload_to_kv.bat để upload lên Cloudflare.", "OK");
    }

    // --- Pokemon ---

    private static void ExportAllPokemon()
    {
        var allPokemon = Resources.LoadAll<PokemonBase>("PokemonData");
        var allMoves = Resources.LoadAll<MoveBase>("MoveData");

        // Index moves để tra nhanh
        var moveDict = new Dictionary<string, MoveBase>();
        foreach (var m in allMoves)
            if (m != null) moveDict[m.MoveName] = m;

        var list = new List<object>();
        foreach (var p in allPokemon)
        {
            if (p == null) continue;

            var learnset = new List<object>();
            if (p.LearnableMoves != null)
            {
                foreach (var lm in p.LearnableMoves)
                {
                    if (lm?.move == null) continue;
                    learnset.Add(new {
                        level = lm.level,
                        move  = lm.move.MoveName,
                        type  = lm.move.Type.ToString(),
                        power = lm.move.Power,
                        pp    = lm.move.PP
                    });
                }
            }

            list.Add(new {
                name      = p.Name,
                type1     = p.Type1.ToString(),
                type2     = p.Type2 == PokemonType.None ? null : p.Type2.ToString(),
                hp        = p.MaxHp,
                attack    = p.Attack,
                defense   = p.Defense,
                spAttack  = p.SpAttack,
                spDefense = p.SpDefense,
                speed     = p.Speed,
                learnset  = learnset
            });

            // Mỗi Pokemon 1 file KV riêng (tra nhanh theo tên companion)
            string key = p.Name.ToLower().Replace(" ", "_");
            WriteJson($"{OutputDir}/pokemon_{key}.json", new {
                name      = p.Name,
                type1     = p.Type1.ToString(),
                type2     = p.Type2 == PokemonType.None ? null : p.Type2.ToString(),
                hp        = p.MaxHp,
                attack    = p.Attack,
                defense   = p.Defense,
                spAttack  = p.SpAttack,
                spDefense = p.SpDefense,
                speed     = p.Speed,
                learnset  = learnset
            });
        }

        // File tổng (backup)
        WriteJson($"{OutputDir}/all_pokemon.json", list);
        Debug.Log($"[KV Export] {allPokemon.Length} Pokemon");
    }

    // --- Items ---

    private static void ExportAllItems()
    {
        var allItems = Resources.LoadAll<ItemBase>("Item");
        var list = new List<object>();

        foreach (var item in allItems)
        {
            if (item == null) continue;
            list.Add(new {
                name        = item.itemName,
                description = item.description,
                type        = item.itemType.ToString(),
                healAmount  = item.healAmount,
                healToFull  = item.healToFull,
                isRevive    = item.isRevive,
                revivePct   = item.revivePercent,
                price       = item.price
            });
        }

        WriteJson($"{OutputDir}/all_items.json", list);
        Debug.Log($"[KV Export] {allItems.Length} Items");
    }

    // --- Story lore (viết tay, chỉnh theo game) ---

    private static void WriteStoryLore()
    {
        var lore = new {
            world = "Thế giới Pokemon, người chơi đóng vai Red — huấn luyện viên Pokemon trẻ đầy tiềm năng.",
            characters = new {
                Red         = "Nhân vật chính. Chăm chỉ, quyết tâm, yêu Pokemon.",
                Green       = "Đối thủ kiêu ngạo hay khiêu khích Red, nhưng thực ra không xấu.",
                Blue        = "Cô gái thân thiện, bạn đồng hành tốt bụng và hay giúp đỡ Red.",
                TeamRocket  = "Tổ chức tội phạm lợi dụng Pokemon cho mục đích xấu. Đã đánh cắp WaterBadge trong hang động."
            },
            gyms = new {
                GrassGym = "Phòng Tập Thảo Nguyên — Pokemon loại Grass. Badge đầu tiên.",
                WaterGym = "Phòng Tập Nước — Pokemon loại Water. Badge thứ hai, bị Team Rocket đánh cắp.",
                FireGym  = "Phòng Tập Lửa — Pokemon loại Fire. Badge thứ ba."
            },
            locations = new {
                Road01  = "Con đường từ làng khởi đầu đến GrassGym.",
                Road02  = "Con đường từ GrassGym đến WaterGym.",
                Cave    = "Hang động — nơi Team Rocket ẩn náu và đánh cắp WaterBadge.",
                Mountain= "Núi — con đường đến FireGym sau khi ra khỏi hang."
            }
        };

        WriteJson($"{OutputDir}/story_lore.json", lore);
        Debug.Log("[KV Export] story_lore.json");
    }

    // --- Script upload tự động ---

    private static void WriteUploadScript()
    {
        var sb = new StringBuilder();
        sb.AppendLine("@echo off");
        sb.AppendLine("REM =====================================================");
        sb.AppendLine("REM  HUONG DAN:");
        sb.AppendLine("REM  1. Vao Cloudflare Dashboard -> Workers & Pages -> KV");
        sb.AppendLine("REM  2. Click vao \"GAME_DATA\" namespace");
        sb.AppendLine("REM  3. Copy ID tu URL (dang: /kv/namespaces/abc123...)");
        sb.AppendLine("REM  4. Dan vao dong NAMESPACE_ID ben duoi");
        sb.AppendLine("REM  5. Chay file nay (double-click hoac cmd)");
        sb.AppendLine("REM  NOTE: Can chay \"wrangler login\" truoc neu chua dang nhap");
        sb.AppendLine("REM =====================================================");
        sb.AppendLine();
        sb.AppendLine("set NAMESPACE_ID=PASTE_YOUR_NAMESPACE_ID_HERE");
        sb.AppendLine();
        sb.AppendLine("if \"%NAMESPACE_ID%\"==\"PASTE_YOUR_NAMESPACE_ID_HERE\" (");
        sb.AppendLine("    echo [LOI] Ban chua dien NAMESPACE_ID!");
        sb.AppendLine("    echo Mo file nay bang Notepad va sua dong \"set NAMESPACE_ID=...\"");
        sb.AppendLine("    pause");
        sb.AppendLine("    exit /b 1");
        sb.AppendLine(")");
        sb.AppendLine();
        sb.AppendLine("echo Uploading game data to Cloudflare KV (namespace: %NAMESPACE_ID%)...");
        sb.AppendLine("echo.");
        sb.AppendLine();

        sb.AppendLine("wrangler kv key put --namespace-id=%NAMESPACE_ID% \"story_lore\" --path=story_lore.json");
        sb.AppendLine("wrangler kv key put --namespace-id=%NAMESPACE_ID% \"all_items\" --path=all_items.json");
        sb.AppendLine("wrangler kv key put --namespace-id=%NAMESPACE_ID% \"all_pokemon\" --path=all_pokemon.json");

        string[] pokemonFiles = Directory.GetFiles(OutputDir, "pokemon_*.json");
        foreach (var f in pokemonFiles)
        {
            string filename = Path.GetFileName(f);
            string key = Path.GetFileNameWithoutExtension(f);
            sb.AppendLine($"wrangler kv key put --namespace-id=%NAMESPACE_ID% \"{key}\" --path={filename}");
        }

        sb.AppendLine();
        sb.AppendLine("echo.");
        sb.AppendLine("echo Done! Kiem tra KV tren Cloudflare Dashboard.");
        sb.AppendLine("pause");

        File.WriteAllText($"{OutputDir}/upload_to_kv.bat", sb.ToString());
        Debug.Log("[KV Export] upload_to_kv.bat");
    }

    // --- Helper ---

    private static void WriteJson(string path, object data)
    {
        string json = JsonUtility.ToJson(new Wrapper { data = JsonUtility.ToJson(data) });
        // Dùng Newtonsoft nếu có, còn không dùng cách đơn giản
        File.WriteAllText(path, SimpleSerialize(data), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    // Serialize đơn giản bằng reflection (không cần Newtonsoft)
    private static string SimpleSerialize(object obj)
    {
        if (obj == null) return "null";
        if (obj is string s) return $"\"{EscapeJson(s)}\"";
        if (obj is bool b) return b ? "true" : "false";
        if (obj is int i) return i.ToString();
        if (obj is float f) return f.ToString("G");

        var type = obj.GetType();

        if (type.IsArray)
        {
            var arr = (System.Array)obj;
            var items = new List<string>();
            foreach (var item in arr) items.Add(SimpleSerialize(item));
            return "[" + string.Join(",", items) + "]";
        }

        if (obj is System.Collections.IList list)
        {
            var items = new List<string>();
            foreach (var item in list) items.Add(SimpleSerialize(item));
            return "[" + string.Join(",", items) + "]";
        }

        // Anonymous type / class
        var props = type.GetProperties();
        var fields = type.GetFields();
        var parts = new List<string>();

        foreach (var p in props)
        {
            var val = p.GetValue(obj);
            if (val == null && p.PropertyType == typeof(string)) continue;
            parts.Add($"\"{p.Name}\":{SimpleSerialize(val)}");
        }
        foreach (var fi in fields)
        {
            var val = fi.GetValue(obj);
            if (val == null && fi.FieldType == typeof(string)) continue;
            parts.Add($"\"{fi.Name}\":{SimpleSerialize(val)}");
        }

        return "{" + string.Join(",", parts) + "}";
    }

    private static string EscapeJson(string s)
        => s?.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r") ?? "";

    [System.Serializable] private class Wrapper { public string data; }
}
