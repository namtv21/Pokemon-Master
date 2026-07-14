using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Nhà Pokemon: party sống trong một căn phòng (UI panel) — đi lại, né nội thất,
/// nhịp sinh hoạt theo tính cách, và TƯƠNG TÁC:
///   [←][→] chọn Pokemon · [Z] vuốt ve · [C] chat · [X] thoát.
/// Hai Pokemon lại gần nhau sẽ trao đổi emote theo cặp tính cách.
/// Ở trong nhà tăng bond CHẬM và CÓ TRẦN (đường chính vẫn là chiến đấu).
/// </summary>
public class PokemonHomeUI : MonoBehaviour
{
    [Header("Tham chiếu")]
    [Tooltip("Vùng Pokemon được phép đi lại (RectTransform con của panel, chừa viền tường).")]
    [SerializeField] private RectTransform roomArea;

    [Header("Agent")]
    [SerializeField] private float agentSize = 110f;

    [Header("Emote khi gặp nhau")]
    [SerializeField] private float meetDistance = 125f;
    [SerializeField] private float meetCooldown = 14f;

    [Header("Bond (tăng chậm, có trần)")]
    [Tooltip("Số giây ở trong nhà để mỗi Pokemon +1 friendship.")]
    [SerializeField] private float secondsPerBondPoint = 180f;
    [Tooltip("Trần friendship cộng từ nhà cho MỖI Pokemon trong một phiên chơi.")]
    [SerializeField] private int bondCapPerSession = 3;

    public bool IsOpen { get; private set; }

    private readonly List<HomeAgent> agents = new List<HomeAgent>();
    private readonly List<Rect> obstacleRects = new List<Rect>();          // trong không gian roomArea
    private readonly Dictionary<int, float> meetCooldowns = new Dictionary<int, float>();
    private Action onClose;
    private float bondTimer;
    private float meetScanTimer;

    // Chọn Pokemon bằng ↑↓ — highlight trực tiếp sprite trong phòng,
    // thanh hint dưới đáy hiện tên + tính cách con đang chọn.
    private int selectedIndex;
    private RectTransform hintBar;
    private TextMeshProUGUI hintBarText;

    // Trần bond theo phiên chơi (reset khi khởi động game)
    private static readonly Dictionary<Pokemon, int> sessionBondGained = new Dictionary<Pokemon, int>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetSession() => sessionBondGained.Clear();

    public void Open(Action onCloseCallback)
    {
        if (roomArea == null)
        {
            ToastNotificationManager.Instance?.Show("Nhà Pokemon chưa được dựng (thiếu RoomArea).", Color.yellow);
            onCloseCallback?.Invoke();
            return;
        }

        onClose = onCloseCallback;
        gameObject.SetActive(true);
        UiFx.PopIn(gameObject);
        IsOpen = true;
        bondTimer = 0f;
        meetCooldowns.Clear();

        CollectObstacles();
        SpawnAgents();
        EnsureHintBar();

        selectedIndex = 0;
        RefreshSelection();
    }

    public void Close()
    {
        IsOpen = false;
        ClearAgents();
        gameObject.SetActive(false);
    }

    public void HandleUpdate()
    {
        if (!IsOpen)
            return;

        if (Input.GetKeyDown(KeyCode.X))
        {
            var callback = onClose;
            onClose = null;
            Close();
            callback?.Invoke();
            return;
        }

        if (agents.Count == 0)
            return;

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            selectedIndex = (selectedIndex + 1) % agents.Count;
            RefreshSelection();
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            selectedIndex = (selectedIndex - 1 + agents.Count) % agents.Count;
            RefreshSelection();
        }
        else if (Input.GetKeyDown(KeyCode.Z))
        {
            PetSelected();
        }
        else if (Input.GetKeyDown(KeyCode.C))
        {
            var agent = SelectedAgent();
            if (agent != null && agent.Pokemon != null)
                MenuController.Instance?.OpenChatFromHome(agent.Pokemon);
        }
    }

    private HomeAgent SelectedAgent()
    {
        return (selectedIndex >= 0 && selectedIndex < agents.Count) ? agents[selectedIndex] : null;
    }

    // Highlight sprite con đang chọn + cập nhật thanh hint (tên + tính cách)
    private void RefreshSelection()
    {
        for (int i = 0; i < agents.Count; i++)
        {
            if (agents[i] != null)
                agents[i].SetHighlighted(i == selectedIndex);
        }

        var selected = SelectedAgent();
        if (hintBarText != null && selected != null && selected.Pokemon != null)
        {
            string name = selected.Pokemon.Base != null ? selected.Pokemon.Base.Name : "?";
            string personality = PokemonPersonalityUtil.Label(selected.Pokemon.Personality);
            hintBarText.text =
                $"<color=#FFD34D>> {name}</color> <size=75%>({personality})</size>    " +
                "<color=#FFD34D>[↑][↓]</color> Chọn   <color=#FFD34D>[Z]</color> Vuốt ve   " +
                "<color=#FFD34D>[C]</color> Chat   <color=#FFD34D>[X]</color> Thoát";
        }
    }

    // Vuốt ve: đổi tâm trạng + phản ứng theo tính cách (bond KHÔNG đổi từ vuốt ve)
    private void PetSelected()
    {
        var agent = SelectedAgent();
        var chat = CompanionChatSystem.Instance;
        if (agent == null || agent.Pokemon == null || chat == null)
            return;

        chat.PetCompanion(agent.Pokemon); // Keep mood and interaction time updates.

        agent.Hop();
        agent.Say(BuildThreeCryReaction(agent.Pokemon), 3f);
    }

    private static string BuildThreeCryReaction(Pokemon pokemon)
    {
        string name = pokemon != null && pokemon.Base != null ? pokemon.Base.Name : "Pokemon";
        string cry = string.Equals(name, "Pikachu", StringComparison.OrdinalIgnoreCase) ? "Pika" : name;
        return $"{cry}! {cry}! {cry}!";
    }

    // ===== Bond tick + emote khi gặp nhau =====

    private void Update()
    {
        if (!IsOpen)
            return;

        TickBond();
        ScanMeetings();
    }

    private void TickBond()
    {
        bondTimer += Time.deltaTime;
        if (bondTimer < secondsPerBondPoint)
            return;

        bondTimer = 0f;
        foreach (var agent in agents)
        {
            var pokemon = agent != null ? agent.Pokemon : null;
            if (pokemon == null) continue;

            sessionBondGained.TryGetValue(pokemon, out int gained);
            if (gained >= bondCapPerSession) continue;

            pokemon.AddFriendship(1);
            sessionBondGained[pokemon] = gained + 1;
        }
    }

    // Hai Pokemon lại gần nhau → trao đổi emote theo cặp tính cách (có cooldown từng cặp).
    private void ScanMeetings()
    {
        meetScanTimer += Time.deltaTime;
        if (meetScanTimer < 0.6f)
            return;
        meetScanTimer = 0f;

        float now = Time.time;
        for (int i = 0; i < agents.Count; i++)
        {
            for (int j = i + 1; j < agents.Count; j++)
            {
                var a = agents[i];
                var b = agents[j];
                if (a == null || b == null) continue;

                if ((a.AnchoredPosition - b.AnchoredPosition).sqrMagnitude > meetDistance * meetDistance)
                    continue;

                int pairKey = i * 100 + j;
                if (meetCooldowns.TryGetValue(pairKey, out float readyAt) && now < readyAt)
                    continue;
                meetCooldowns[pairKey] = now + meetCooldown;

                GetMeetingEmotes(a.Pokemon, b.Pokemon, out string ea, out string eb);
                a.Emote(ea);
                b.Emote(eb);
            }
        }
    }

    // Emote khi gặp nhau: mỗi tính cách có "chữ ký" riêng (luôn đa dạng),
    // vài cặp đặc biệt được override. Ký hiệu ASCII an toàn với mọi font.
    private static void GetMeetingEmotes(Pokemon pa, Pokemon pb, out string ea, out string eb)
    {
        var a = pa != null ? pa.Personality : PokemonPersonality.Gentle;
        var b = pb != null ? pb.Personality : PokemonPersonality.Gentle;

        // — Cặp đặc biệt —
        if (a == PokemonPersonality.Proud && b == PokemonPersonality.Proud)
        { ea = ">:("; eb = ">:("; return; }                                   // hai kiêu hãnh nghênh nhau

        if ((a == PokemonPersonality.Gentle && b == PokemonPersonality.Timid) ||
            (a == PokemonPersonality.Timid && b == PokemonPersonality.Gentle))
        { ea = "<3"; eb = "<3"; return; }                                     // dịu dàng che chở nhút nhát

        if (a == PokemonPersonality.Playful && b == PokemonPersonality.Proud)
        { ea = "~"; eb = ">:("; return; }                                     // tinh nghịch trêu kiêu hãnh
        if (a == PokemonPersonality.Proud && b == PokemonPersonality.Playful)
        { ea = ">:("; eb = "~"; return; }

        // — Mặc định: mỗi con thể hiện đúng chất mình —
        ea = SignatureEmote(a);
        eb = SignatureEmote(b);
    }

    private static string SignatureEmote(PokemonPersonality p)
    {
        switch (p)
        {
            case PokemonPersonality.Playful: return "~~";     // vui nhộn
            case PokemonPersonality.Brave:   return "!!";    // hăng hái
            case PokemonPersonality.Timid:   return "...";   // rụt rè
            case PokemonPersonality.Proud:   return "-_-";   // lạnh lùng
            case PokemonPersonality.Gentle:  return "<3";    // trìu mến
            case PokemonPersonality.Curious: return "??";     // tò mò
            case PokemonPersonality.Lazy:    return "zZ";    // buồn ngủ
            default:                         return "!";
        }
    }

    // ===== Thanh hint nổi bật (đáy giữa màn hình) =====

    private void EnsureHintBar()
    {
        if (hintBar != null)
            return;

        // Khung ngoài màu vàng → viền nổi bật
        var barGo = new GameObject("HintBar", typeof(RectTransform));
        barGo.transform.SetParent(transform, false);
        hintBar = (RectTransform)barGo.transform;
        hintBar.anchorMin = new Vector2(0.5f, 0f);
        hintBar.anchorMax = new Vector2(0.5f, 0f);
        hintBar.pivot = new Vector2(0.5f, 0f);
        hintBar.anchoredPosition = new Vector2(0f, 16f);
        hintBar.sizeDelta = new Vector2(980f, 62f);

        var border = barGo.AddComponent<UnityEngine.UI.Image>();
        border.color = new Color(1f, 0.83f, 0.3f, 0.95f);
        border.raycastTarget = false;

        // Nền tối bên trong (chừa 3px làm viền)
        var innerGo = new GameObject("Inner", typeof(RectTransform));
        innerGo.transform.SetParent(barGo.transform, false);
        var innerRt = (RectTransform)innerGo.transform;
        innerRt.anchorMin = Vector2.zero;
        innerRt.anchorMax = Vector2.one;
        innerRt.offsetMin = new Vector2(3f, 3f);
        innerRt.offsetMax = new Vector2(-3f, -3f);

        var innerBg = innerGo.AddComponent<UnityEngine.UI.Image>();
        innerBg.color = new Color(0.07f, 0.08f, 0.12f, 0.96f);
        innerBg.raycastTarget = false;

        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(innerGo.transform, false);
        var textRt = (RectTransform)textGo.transform;
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(16f, 2f);
        textRt.offsetMax = new Vector2(-16f, -2f);

        hintBarText = textGo.AddComponent<TextMeshProUGUI>();
        hintBarText.font = TMP_Settings.defaultFontAsset;
        hintBarText.fontSize = 27f;
        hintBarText.alignment = TextAlignmentOptions.Center;
        hintBarText.color = Color.white;
        hintBarText.raycastTarget = false;
    }

    // ===== Spawn / dọn =====

    private void SpawnAgents()
    {
        ClearAgents();

        var party = PlayerParty.Instance;
        if (party == null) return;

        foreach (var pokemon in party.Pokemons)
        {
            if (pokemon == null) continue;

            // Phải tạo kèm RectTransform — GameObject trần chỉ có Transform thường,
            // ép kiểu trong HomeAgent.Init sẽ nổ InvalidCast.
            var go = new GameObject($"Home_{pokemon.Base?.Name ?? "Pokemon"}", typeof(RectTransform));
            go.transform.SetParent(roomArea, false);

            var agent = go.AddComponent<HomeAgent>();
            Vector2 start = TryGetWanderTarget(Vector2.zero, 9999f, false, out var pos) ? pos : Vector2.zero;
            agent.Init(this, pokemon, start, agentSize);
            agents.Add(agent);
        }
    }

    private void ClearAgents()
    {
        foreach (var agent in agents)
            if (agent != null)
                Destroy(agent.gameObject);
        agents.Clear();
    }

    // ===== Vật cản & tìm điểm đến =====

    // Đổi RectTransform nội thất về không gian roomArea → Rect để kiểm tra nhanh.
    private void CollectObstacles()
    {
        obstacleRects.Clear();
        foreach (var obstacle in GetComponentsInChildren<HomeObstacle>(true))
        {
            var ort = (RectTransform)obstacle.transform;
            var corners = new Vector3[4];
            ort.GetWorldCorners(corners);
            Vector2 min = roomArea.InverseTransformPoint(corners[0]);
            Vector2 max = roomArea.InverseTransformPoint(corners[2]);
            obstacleRects.Add(Rect.MinMaxRect(min.x, min.y, max.x, max.y));
        }
    }

    /// Tìm điểm đến hợp lệ: trong phòng, không đè nội thất, đường đi thẳng không xuyên đồ.
    public bool TryGetWanderTarget(Vector2 from, float maxDistance, bool preferCenter, out Vector2 result)
    {
        var room = roomArea.rect;
        float half = agentSize * 0.5f;

        for (int attempt = 0; attempt < 14; attempt++)
        {
            var candidate = new Vector2(
                UnityEngine.Random.Range(room.xMin + half, room.xMax - half),
                UnityEngine.Random.Range(room.yMin + half, room.yMax - half));

            if (preferCenter)
                candidate = Vector2.Lerp(candidate, room.center, 0.55f);

            if (maxDistance < 9000f)
                candidate = from + Vector2.ClampMagnitude(candidate - from, maxDistance);

            if (!IsFree(candidate, half)) continue;
            if (!IsPathClear(from, candidate, half)) continue;

            result = candidate;
            return true;
        }

        result = from;
        return false;
    }

    private bool IsFree(Vector2 pos, float half)
    {
        // Chân Pokemon chiếm nửa dưới sprite — chỉ chặn theo vùng chân cho tự nhiên
        var footRect = new Rect(pos.x - half * 0.6f, pos.y - half, half * 1.2f, half * 0.8f);
        for (int i = 0; i < obstacleRects.Count; i++)
        {
            if (obstacleRects[i].Overlaps(footRect))
                return false;
        }
        return true;
    }

    private bool IsPathClear(Vector2 from, Vector2 to, float half)
    {
        float dist = Vector2.Distance(from, to);
        int steps = Mathf.Max(2, Mathf.CeilToInt(dist / 25f));
        for (int i = 1; i < steps; i++)
        {
            if (!IsFree(Vector2.Lerp(from, to, (float)i / steps), half))
                return false;
        }
        return true;
    }

}
