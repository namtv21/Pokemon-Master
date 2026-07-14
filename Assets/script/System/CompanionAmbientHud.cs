using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// HUD companion trên overworld: chân dung (theo mood) + bong bóng thoại ambient.
/// - KHÔNG chặn gameplay: không đụng GameState/DialogManager, chỉ hiển thị rồi tự ẩn.
/// - Offline-first: câu thoại dựng sẵn theo tính cách/mood/cờ truyện; AI chỉ dùng
///   thỉnh thoảng cho bình luận vào scene (throttle chặt, có race-timeout + fallback).
/// - Tự khởi động (không cần đặt vào scene/prefab), tự dựng UI, sống xuyên scene.
/// - Hệ thống khác có thể chủ động đẩy thoại qua CompanionAmbientHud.Say(text).
/// </summary>
public class CompanionAmbientHud : MonoBehaviour
{
    public static CompanionAmbientHud Instance { get; private set; }

    // ===== Cấu hình (hằng số để không phụ thuộc Inspector — object tạo lúc runtime) =====
    private const float PollInterval          = 2f;    // nhịp kiểm tra trigger
    private const float GlobalCooldown        = 25f;   // khoảng cách tối thiểu giữa 2 bong bóng
    private const float SceneCommentDelay     = 1.5f;  // chờ ổn định sau khi vào scene
    private const float SceneCooldown         = 60f;
    private const float HpLowCooldown         = 90f;
    private const float StoryCooldown         = 45f;
    private const float IdleStillSeconds      = 40f;   // đứng yên bao lâu thì tính là idle
    private const float IdleCooldown          = 120f;
    private const float RandomCooldown        = 150f;
    private const float AiAmbientMinGap       = 180f;  // tối thiểu 3 phút giữa 2 lần gọi AI ambient
    private const float AiAmbientChance       = 0.25f; // xác suất dùng AI khi vào scene (nếu đủ điều kiện)
    private const float AiRaceTimeout         = 6f;    // AI trả lời chậm hơn 6s → dùng câu offline
    private const int   MaxBubbleChars        = 160;

    private const string PrefHudEnabled  = "CompanionHudEnabled";
    private const string PrefHudAi       = "CompanionHudAiAmbient";

    /// Bật/tắt toàn bộ HUD (lưu PlayerPrefs — nối vào menu Setting sau nếu muốn).
    public static bool HudEnabled
    {
        get => PlayerPrefs.GetInt(PrefHudEnabled, 1) == 1;
        set { PlayerPrefs.SetInt(PrefHudEnabled, value ? 1 : 0); PlayerPrefs.Save(); }
    }

    /// Cho phép thỉnh thoảng dùng AI cho bình luận vào scene (mặc định bật, đã throttle chặt).
    public static bool AiAmbientEnabled
    {
        get => PlayerPrefs.GetInt(PrefHudAi, 1) == 1;
        set { PlayerPrefs.SetInt(PrefHudAi, value ? 1 : 0); PlayerPrefs.Save(); }
    }

    // ===== Bootstrap =====

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetInstance() => Instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("CompanionAmbientHud");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<CompanionAmbientHud>();
    }

    // ===== UI refs (tự dựng) =====
    private Canvas canvas;
    private GameObject hudRoot;
    private Image portraitImage;
    private TextMeshProUGUI nameLabel;
    private CanvasGroup bubbleGroup;
    private TextMeshProUGUI bubbleText;
    private RectTransform portraitRect;
    private Sprite[] pikachuPortraitFrames;
    private bool pikachuFramesLoadAttempted;
    private bool pikachuPortraitActive;
    private bool pikachuWasMoving;
    private int pikachuRunFrame;
    private float nextPikachuRunFrameAt;
    private float nextPikachuIdleVariantAt;
    private float pikachuIdleVariantUntil;
    private bool uiBuilt;

    // ===== Trạng thái trigger =====
    private float pollTimer;
    private float lastBubbleTime = -999f;
    private float lastSceneCommentTime = -999f;
    private float lastHpLowTime = -999f;
    private float lastStoryTime = -999f;
    private float lastIdleTime = -999f;
    private float lastRandomTime = -999f;
    private float lastAiAmbientTime = -999f;

    private bool greeted;
    private bool wasLowHp;
    private int cachedBondTier = -1;
    private string cachedFlagsSnapshot;
    private Vector3 lastPlayerPos;
    private float stillSince;

    // Theo dõi sự kiện companion/party
    private GameState prevGameState = GameState.Overworld;
    private Pokemon lastCompanion;
    private PokemonBase lastCompanionBase;
    private int lastCompanionLevel;
    private int lastPartyCount = -1;
    private bool healPending;

    private Coroutine bubbleRoutine;
    private Coroutine sceneCommentRoutine;

    private void OnEnable()  => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ===== API công khai cho hệ thống khác (story/quest/battle-end...) =====

    /// Đẩy một câu thoại companion lên HUD (không chặn game).
    /// important = true → bỏ qua global cooldown (dành cho story/quest đẩy thoại chủ động).
    public static void Say(string text, bool important = false)
    {
        if (Instance == null || string.IsNullOrWhiteSpace(text)) return;
        if (!Instance.ShouldShow()) return;
        Instance.ShowBubble(text, bypassGlobalCooldown: important);
    }

    // ===== Vòng đời =====

    private bool wasVisible;

    private void Update()
    {
        // Vừa từ Battle về Overworld → companion phản ứng theo kết quả trận
        var gc = GameController.Instance;
        var gcState = gc != null ? gc.State : GameState.Overworld;
        if (prevGameState == GameState.Battle && gcState == GameState.Overworld && gc != null)
            StartCoroutine(BattleEndReaction(gc.LastBattleOutcome));
        prevGameState = gcState;

        bool visible = ShouldShow();

        // Vừa hiện lại (thoát dialog/menu/battle): reset đồng hồ idle — người chơi "đứng yên"
        // trong lúc thoại/menu không được tính là idle, tránh companion nói ngay lập tức.
        if (visible && !wasVisible)
        {
            stillSince = Time.time;
            var player = PlayerController.Instance;
            if (player != null) lastPlayerPos = player.transform.position;
        }
        wasVisible = visible;

        if (uiBuilt && hudRoot != null && hudRoot.activeSelf != visible)
        {
            hudRoot.SetActive(visible);
            if (!visible && bubbleRoutine != null)
            {
                StopCoroutine(bubbleRoutine);
                bubbleRoutine = null;
                if (bubbleGroup != null) bubbleGroup.alpha = 0f;
            }
        }

        if (!visible) return;
        if (!uiBuilt) BuildUi();
        if (hudRoot != null && !hudRoot.activeSelf)
            hudRoot.SetActive(true);

        UpdatePikachuPortrait();

        pollTimer += Time.deltaTime;
        if (pollTimer >= PollInterval)
        {
            pollTimer = 0f;
            Poll();
        }
    }

    private bool ShouldShow()
    {
        if (!HudEnabled) return false;
        var gc = GameController.Instance;
        if (gc == null || gc.State != GameState.Overworld) return false;
        var chat = CompanionChatSystem.Instance;
        if (chat == null || chat.GetCompanion() == null) return false;
        return true;
    }

    // ===== Poll: cập nhật chân dung + kiểm tra trigger =====

    private void Poll()
    {
        var chat = CompanionChatSystem.Instance;
        var companion = chat != null ? chat.GetCompanion() : null;
        if (companion == null) return;

        RefreshPortrait(companion, chat);

        // 0) Chào lần đầu trong phiên
        if (!greeted)
        {
            greeted = true;
            ShowBubble(PickLine(GreetingLines, companion));
            return;
        }

        float now = Time.time;
        float hpRatio = companion.MaxHp > 0 ? (float)companion.CurrentHp / companion.MaxHp : 1f;
        var party = PlayerParty.Instance;
        int partyCount = party != null ? party.Pokemons.Count : 0;

        // ===== Sự kiện companion/party (mỗi poll tối đa 1 bong bóng, tracker luôn được đồng bộ) =====

        // a) Đổi con dẫn đầu → con mới chào
        if (companion != lastCompanion)
        {
            bool hadPrevious = lastCompanion != null;
            lastCompanion = companion;
            lastCompanionBase = companion.Base;
            lastCompanionLevel = companion.Level;
            healPending = false;
            if (hadPrevious)
            {
                lastPartyCount = partyCount;
                ShowBubble(NewLeadLine(companion));
                return;
            }
        }

        // b) Tiến hóa (cùng cá thể, Base đổi) — khoảnh khắc lớn, bỏ qua cooldown
        if (companion.Base != lastCompanionBase)
        {
            lastCompanionBase = companion.Base;
            lastCompanionLevel = companion.Level;
            lastPartyCount = partyCount;
            ShowBubble(EvolvedLine(companion), bypassGlobalCooldown: true);
            return;
        }

        // c) Lên cấp ngoài trận (vd dùng bình EXP)
        if (companion.Level > lastCompanionLevel)
        {
            lastCompanionLevel = companion.Level;
            lastPartyCount = partyCount;
            ShowBubble(LevelUpLine(companion));
            return;
        }

        // d) Có thành viên mới vào đội → companion bình luận
        if (lastPartyCount >= 0 && partyCount > lastPartyCount && party != null)
        {
            lastPartyCount = partyCount;
            var newcomer = party.Pokemons[partyCount - 1];
            if (newcomer != null && newcomer != companion)
            {
                ShowBubble(NewMemberLine(companion, newcomer));
                return;
            }
        }
        lastPartyCount = partyCount;

        // e) Được chữa khỏi sau khi nguy kịch → cảm ơn
        if (hpRatio < 0.35f && hpRatio > 0f)
            healPending = true;
        else if (healPending && hpRatio >= 0.95f)
        {
            healPending = false;
            ShowBubble(HealedLine(companion));
            return;
        }

        // ===== Các trigger nền (như cũ) =====

        // 1) Lên bậc bond (quan trọng — bỏ qua global cooldown)
        int tier = (int)companion.BondTier;
        if (cachedBondTier < 0) cachedBondTier = tier;
        else if (tier > cachedBondTier)
        {
            cachedBondTier = tier;
            ShowBubble(BondUpLine(companion), bypassGlobalCooldown: true);
            return;
        }
        cachedBondTier = tier;

        // 2) HP thấp (một lần mỗi "đợt": reset khi hồi trên 60%)
        if (hpRatio > 0.6f) wasLowHp = false;
        if (!wasLowHp && hpRatio > 0f && hpRatio < 0.35f && now - lastHpLowTime > HpLowCooldown)
        {
            wasLowHp = true;
            lastHpLowTime = now;
            ShowBubble(PickLine(HpLowLines, companion));
            return;
        }

        // 3) Cờ truyện thay đổi → companion bình luận bước tiếp theo (tái dùng logic offline sẵn có)
        string flagsNow = SnapshotFlags();
        if (cachedFlagsSnapshot == null) cachedFlagsSnapshot = flagsNow;
        else if (flagsNow != cachedFlagsSnapshot && now - lastStoryTime > StoryCooldown)
        {
            cachedFlagsSnapshot = flagsNow;
            lastStoryTime = now;
            ShowBubble(chat.GetOfflineResponse(companion, 1));   // "Tiếp theo nên làm gì" — đã bám cờ truyện
            return;
        }
        cachedFlagsSnapshot = flagsNow;

        // 4) Idle: người chơi đứng yên lâu
        var player = PlayerController.Instance;
        if (player != null)
        {
            if ((player.transform.position - lastPlayerPos).sqrMagnitude > 0.01f)
            {
                lastPlayerPos = player.transform.position;
                stillSince = now;
            }
            else if (now - stillSince > IdleStillSeconds && now - lastIdleTime > IdleCooldown)
            {
                lastIdleTime = now;
                stillSince = now;
                ShowBubble(PickLine(IdleLines, companion));
                return;
            }
        }

        // 5) Tán gẫu ngẫu nhiên (hiếm)
        if (now - lastRandomTime > RandomCooldown && Random.value < 0.2f)
        {
            lastRandomTime = now;
            ShowBubble(PickLine(ChatterLines, companion));
        }
    }

    // ===== Trigger vào scene =====

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;   // bỏ qua BattleScene (additive)
        if (sceneCommentRoutine != null) StopCoroutine(sceneCommentRoutine);
        sceneCommentRoutine = StartCoroutine(SceneCommentAfterDelay());
    }

    private IEnumerator SceneCommentAfterDelay()
    {
        yield return new WaitForSeconds(SceneCommentDelay);

        if (!ShouldShow()) yield break;
        if (Time.time - lastSceneCommentTime < SceneCooldown) yield break;

        var chat = CompanionChatSystem.Instance;
        var companion = chat.GetCompanion();
        lastSceneCommentTime = Time.time;

        string offline = SceneLine(companion);

        // Thỉnh thoảng dùng AI cho câu vào scene (online + đủ giãn cách + trúng xác suất)
        bool tryAi = AiAmbientEnabled && chat.IsOnline &&
                     Time.time - lastAiAmbientTime > AiAmbientMinGap &&
                     Random.value < AiAmbientChance;

        if (!tryAi)
        {
            ShowBubble(offline);
            yield break;
        }

        lastAiAmbientTime = Time.time;
        string aiResult = null;
        bool done = false;
        string loc = CompanionChatSystem.GetCurrentLocation();
        StartCoroutine(chat.SendMessageToCompanion(
            $"(Chúng ta vừa đặt chân đến {loc}. Hãy thốt lên MỘT câu ngắn dưới 20 từ về nơi này, đúng tính cách của bạn.)",
            r => { aiResult = r; done = true; }));

        float waited = 0f;
        while (!done && waited < AiRaceTimeout) { waited += Time.deltaTime; yield return null; }

        // AI kịp và hợp lệ → dùng; không thì rơi về câu offline (câu lỗi luôn bắt đầu bằng "(")
        if (done && !string.IsNullOrWhiteSpace(aiResult) && !aiResult.StartsWith("("))
            ShowBubble(aiResult);
        else
            ShowBubble(offline);
    }

    // ===== Hiển thị bong bóng =====

    private void ShowBubble(string text, bool bypassGlobalCooldown = false)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        if (!bypassGlobalCooldown && Time.time - lastBubbleTime < GlobalCooldown) return;
        if (!uiBuilt) BuildUi();

        lastBubbleTime = Time.time;
        if (text.Length > MaxBubbleChars) text = text.Substring(0, MaxBubbleChars - 1) + "…";

        if (bubbleRoutine != null) StopCoroutine(bubbleRoutine);
        bubbleRoutine = StartCoroutine(BubbleRoutine(text));
    }

    private IEnumerator BubbleRoutine(string text)
    {
        bubbleText.text = text;
        float showTime = 2.8f + text.Length * 0.04f;

        // Chân dung "nhún" một nhịp khi nói
        StartCoroutine(PortraitBounce());

        // Fade in
        for (float t = 0f; t < 0.18f; t += Time.deltaTime)
        {
            bubbleGroup.alpha = t / 0.18f;
            yield return null;
        }
        bubbleGroup.alpha = 1f;

        yield return new WaitForSeconds(showTime);

        // Fade out
        for (float t = 0f; t < 0.3f; t += Time.deltaTime)
        {
            bubbleGroup.alpha = 1f - t / 0.3f;
            yield return null;
        }
        bubbleGroup.alpha = 0f;
        bubbleRoutine = null;
    }

    private IEnumerator PortraitBounce()
    {
        if (portraitRect == null) yield break;
        Vector3 baseScale = Vector3.one;
        for (float t = 0f; t < 0.12f; t += Time.deltaTime)
        {
            portraitRect.localScale = Vector3.Lerp(baseScale, baseScale * 1.12f, t / 0.12f);
            yield return null;
        }
        for (float t = 0f; t < 0.12f; t += Time.deltaTime)
        {
            portraitRect.localScale = Vector3.Lerp(baseScale * 1.12f, baseScale, t / 0.12f);
            yield return null;
        }
        portraitRect.localScale = baseScale;
    }

    private void RefreshPortrait(Pokemon companion, CompanionChatSystem chat)
    {
        if (portraitImage != null && companion.Base != null)
        {
            if (!pikachuPortraitActive)
                portraitImage.sprite = companion.Base.FrontSprite;
            // Ngất/mệt → xám; bình thường → nguyên màu
            int mood = chat.GetMoodIndex(companion);
            portraitImage.color = mood == 0 ? new Color(0.55f, 0.55f, 0.55f, 1f) : Color.white;
        }

        if (nameLabel != null && companion.Base != null)
            nameLabel.text = $"{companion.Base.Name} · {PokemonPersonalityUtil.TierLabel(companion.BondTier)}";
    }

    // ===== Nội dung thoại (offline, theo tính cách) =====

    private void UpdatePikachuPortrait()
    {
        if (portraitImage == null)
            return;

        var chat = CompanionChatSystem.Instance;
        var companion = chat != null ? chat.GetCompanion() : null;
        bool isPikachu = companion != null && companion.Base != null &&
                         string.Equals(companion.Base.Name, "Pikachu", System.StringComparison.OrdinalIgnoreCase);

        if (!isPikachu)
        {
            if (pikachuPortraitActive && companion != null && companion.Base != null)
                portraitImage.sprite = companion.Base.FrontSprite;
            pikachuPortraitActive = false;
            pikachuWasMoving = false;
            return;
        }

        if (!pikachuPortraitActive)
        {
            pikachuPortraitActive = true;
            nextPikachuIdleVariantAt = Time.unscaledTime + Random.Range(3f, 7f);
        }

        EnsurePikachuPortraitFrames();
        if (pikachuPortraitFrames == null || pikachuPortraitFrames.Length < 4)
        {
            portraitImage.sprite = companion.Base.FrontSprite;
            return;
        }

        var player = PlayerController.Instance;
        bool moving = player != null && player.isMoving;
        float now = Time.unscaledTime;

        if (moving)
        {
            if (!pikachuWasMoving)
            {
                pikachuRunFrame = 0;
                nextPikachuRunFrameAt = now;
            }

            if (now >= nextPikachuRunFrameAt)
            {
                portraitImage.sprite = pikachuPortraitFrames[2 + pikachuRunFrame];
                pikachuRunFrame = 1 - pikachuRunFrame;
                nextPikachuRunFrameAt = now + 0.14f;
            }
        }
        else
        {
            if (pikachuWasMoving)
            {
                portraitImage.sprite = pikachuPortraitFrames[0];
                nextPikachuIdleVariantAt = now + Random.Range(3f, 7f);
                pikachuIdleVariantUntil = 0f;
            }

            if (now >= nextPikachuIdleVariantAt)
            {
                pikachuIdleVariantUntil = now + 0.7f;
                nextPikachuIdleVariantAt = pikachuIdleVariantUntil + Random.Range(3f, 7f);
            }

            portraitImage.sprite = now < pikachuIdleVariantUntil
                ? pikachuPortraitFrames[1]
                : pikachuPortraitFrames[0];
        }

        pikachuWasMoving = moving;
    }

    private void EnsurePikachuPortraitFrames()
    {
        if (pikachuFramesLoadAttempted)
            return;

        pikachuFramesLoadAttempted = true;
        var texture = Resources.Load<Texture2D>("Companion/Pikachu/pikachu_hud_sheet");
        if (texture == null)
        {
            var importedSprite = Resources.Load<Sprite>("Companion/Pikachu/pikachu_hud_sheet");
            texture = importedSprite != null ? importedSprite.texture : null;
        }
        if (texture == null)
        {
            Debug.LogWarning("[CompanionAmbientHud] Missing Pikachu HUD sheet in Resources.");
            return;
        }

        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        float halfWidth = texture.width * 0.5f;
        float halfHeight = texture.height * 0.5f;
        pikachuPortraitFrames = new[]
        {
            CreatePikachuFrame(texture, new Rect(0f, halfHeight, halfWidth, halfHeight), "Pikachu_Idle"),
            CreatePikachuFrame(texture, new Rect(halfWidth, halfHeight, halfWidth, halfHeight), "Pikachu_IdleVariant"),
            CreatePikachuFrame(texture, new Rect(0f, 0f, halfWidth, halfHeight), "Pikachu_RunA"),
            CreatePikachuFrame(texture, new Rect(halfWidth, 0f, halfWidth, halfHeight), "Pikachu_RunB")
        };
    }

    private static Sprite CreatePikachuFrame(Texture2D texture, Rect rect, string frameName)
    {
        var sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect);
        sprite.name = frameName;
        return sprite;
    }

    private static string PickLine(Dictionary<PokemonPersonality, string[]> table, Pokemon c)
    {
        string name = c.Base != null ? c.Base.Name : "Pokemon";
        if (table.TryGetValue(c.Personality, out var lines) && lines.Length > 0)
            return string.Format(lines[Random.Range(0, lines.Length)], name);
        return $"{name}~!";
    }

    private string SceneLine(Pokemon c)
    {
        string name = c.Base != null ? c.Base.Name : "Pokemon";
        string loc = CompanionChatSystem.GetCurrentLocation();
        switch (c.Personality)
        {
            case PokemonPersonality.Brave:   return $"{name}: {loc} à… có đối thủ mạnh nào ở đây không nhỉ!";
            case PokemonPersonality.Timid:   return $"{name}: {loc}… n-nơi này lạ quá, đi gần mình nhé…";
            case PokemonPersonality.Playful: return $"{name}: Ồ, {loc}! Chơi ở đây chắc vui lắm~";
            case PokemonPersonality.Proud:   return $"{name}: Hừm, {loc}. Cũng tạm được đấy.";
            case PokemonPersonality.Gentle:  return $"{name}: {loc} yên bình thật… dễ chịu ghê.";
            case PokemonPersonality.Curious: return $"{name}: {loc}?! Có gì hay ở đây ta, khám phá thôi!";
            case PokemonPersonality.Lazy:    return $"{name}: Tới {loc} rồi hả… đi chậm thôi mà~";
            default:                         return $"{name}: Đến {loc} rồi!";
        }
    }

    // ===== Phản ứng kết thúc trận =====

    private IEnumerator BattleEndReaction(BattleOutcome outcome)
    {
        yield return new WaitForSeconds(1.1f);
        if (!ShouldShow()) yield break;

        var chat = CompanionChatSystem.Instance;
        var companion = chat != null ? chat.GetCompanion() : null;
        if (companion == null) yield break;

        // Đồng bộ tracker trước — tiến hóa/lên cấp TRONG trận không bắn thêm bong bóng riêng
        lastCompanion = companion;
        lastCompanionBase = companion.Base;
        lastCompanionLevel = companion.Level;
        var party = PlayerParty.Instance;
        lastPartyCount = party != null ? party.Pokemons.Count : lastPartyCount;

        string line = BattleOutcomeLine(companion, outcome);
        if (!string.IsNullOrEmpty(line))
            ShowBubble(line, bypassGlobalCooldown: true);
    }

    private string BattleOutcomeLine(Pokemon c, BattleOutcome outcome)
    {
        string name = c.Base != null ? c.Base.Name : "Pokemon";
        switch (outcome)
        {
            case BattleOutcome.Win:
                switch (c.Personality)
                {
                    case PokemonPersonality.Brave:   return $"{name}: Thắng rồi! Trận sau đâu, mình chưa đã!";
                    case PokemonPersonality.Proud:   return $"{name}: Hmph, kết quả hiển nhiên thôi.";
                    case PokemonPersonality.Playful: return $"{name}: Thắng thắng thắng~! Ăn mừng đi!";
                    case PokemonPersonality.Timid:   return $"{name}: M-mình... thắng thật à? Hehe...";
                    case PokemonPersonality.Lazy:    return $"{name}: Thắng rồi... giờ ngủ được chưa...";
                    case PokemonPersonality.Curious: return $"{name}: Thắng rồi! Mà đối thủ vừa nãy là loài gì nhỉ?";
                    default:                         return $"{name}: Chúng ta làm được rồi!";
                }
            case BattleOutcome.Lose:
                switch (c.Personality)
                {
                    case PokemonPersonality.Brave:   return $"{name}: Thua keo này ta bày keo khác!";
                    case PokemonPersonality.Proud:   return $"{name}: ...Không có lần sau như vậy đâu.";
                    case PokemonPersonality.Timid:   return $"{name}: Xin lỗi... mình yếu quá...";
                    default:                         return $"{name}: Đừng buồn, nghỉ ngơi rồi thử lại nhé.";
                }
            case BattleOutcome.Capture:
                switch (c.Personality)
                {
                    case PokemonPersonality.Proud:   return $"{name}: Thêm thành viên? Miễn đừng chậm chân là được.";
                    case PokemonPersonality.Playful: return $"{name}: Bạn mới bạn mới~! Chơi chung không?";
                    case PokemonPersonality.Timid:   return $"{name}: Bạn mới... trông có hiền không nhỉ...";
                    default:                         return $"{name}: Chào mừng thành viên mới!";
                }
            case BattleOutcome.Escape:
                return $"{name}: Chạy... cũng là một chiến thuật!";
            default:
                return null;
        }
    }

    // ===== Thoại sự kiện companion/party =====

    private string NewLeadLine(Pokemon c)
    {
        string name = c.Base != null ? c.Base.Name : "Pokemon";
        switch (c.Personality)
        {
            case PokemonPersonality.Brave:   return $"{name}: Đến lượt mình dẫn đường! Cứ để đó!";
            case PokemonPersonality.Proud:   return $"{name}: Cuối cùng cũng chọn đúng người.";
            case PokemonPersonality.Playful: return $"{name}: Yay~ được đi đầu rồi! Đi đâu đây?";
            case PokemonPersonality.Timid:   return $"{name}: M-mình đi đầu á...? Được rồi, mình cố...";
            case PokemonPersonality.Lazy:    return $"{name}: Hở, mình dẫn đường...? Đi chậm thôi nha...";
            case PokemonPersonality.Curious: return $"{name}: Mình đi đầu hả? Để mình đánh hơi đường!";
            default:                         return $"{name}: Từ giờ mình đồng hành với cậu nhé!";
        }
    }

    private string EvolvedLine(Pokemon c)
    {
        string name = c.Base != null ? c.Base.Name : "Pokemon";
        switch (c.Personality)
        {
            case PokemonPersonality.Proud:   return $"{name}: Nhìn đi! Đây mới là dáng vẻ xứng đáng với mình!";
            case PokemonPersonality.Playful: return $"{name}: Oa mình to lên rồi!! Nhìn nè nhìn nè~";
            case PokemonPersonality.Timid:   return $"{name}: Mình... khang khác... cậu vẫn nhận ra mình chứ?";
            default:                         return $"{name}: Mình đã tiến hóa! Cảm ơn cậu đã đồng hành!";
        }
    }

    private string LevelUpLine(Pokemon c)
    {
        string name = c.Base != null ? c.Base.Name : "Pokemon";
        switch (c.Personality)
        {
            case PokemonPersonality.Brave: return $"{name}: Mạnh lên rồi! Thử ngay một trận đi!";
            case PokemonPersonality.Lazy:  return $"{name}: Lên cấp mà chẳng phải làm gì... thích thật~";
            default:                       return $"{name}: Mình thấy khỏe hơn hẳn rồi đó!";
        }
    }

    private string NewMemberLine(Pokemon c, Pokemon newcomer)
    {
        string name = c.Base != null ? c.Base.Name : "Pokemon";
        string other = newcomer.Base != null ? newcomer.Base.Name : "bạn mới";
        switch (c.Personality)
        {
            case PokemonPersonality.Proud:   return $"{name}: {other} hả... để xem có theo kịp mình không.";
            case PokemonPersonality.Playful: return $"{name}: {other} ơi chơi với mình không~?";
            case PokemonPersonality.Timid:   return $"{name}: {other}... trông cũng thân thiện ha...";
            case PokemonPersonality.Curious: return $"{name}: Ồ, {other}! Mình có nhiều câu muốn hỏi lắm!";
            default:                         return $"{name}: Chào {other}, gia nhập đội vui vẻ nhé!";
        }
    }

    private string HealedLine(Pokemon c)
    {
        string name = c.Base != null ? c.Base.Name : "Pokemon";
        switch (c.Personality)
        {
            case PokemonPersonality.Proud: return $"{name}: ...Cảm ơn. Đừng hiểu lầm, mình vẫn ổn mà!";
            case PokemonPersonality.Brave: return $"{name}: Hồi phục hoàn toàn! Chiến tiếp thôi!";
            case PokemonPersonality.Lazy:  return $"{name}: Khỏe re~ nhưng vẫn muốn nằm thêm tí...";
            default:                       return $"{name}: Khỏe lại rồi! Cảm ơn cậu nhé!";
        }
    }

    private string BondUpLine(Pokemon c)
    {
        string name = c.Base != null ? c.Base.Name : "Pokemon";
        string tier = PokemonPersonalityUtil.TierLabel(c.BondTier);
        switch (c.Personality)
        {
            case PokemonPersonality.Proud: return $"{name}: Hmph… được đấy. Giờ chúng ta là \"{tier}\" rồi. Đừng có tự mãn!";
            case PokemonPersonality.Timid: return $"{name}: M-mình cảm thấy… tin cậu hơn rồi. ({tier}!)";
            case PokemonPersonality.Brave: return $"{name}: Cùng nhau chiến đấu nhiều thật đáng giá — \"{tier}\" rồi đó!";
            default:                       return $"{name}: Mình thấy gần gũi với cậu hơn rồi! (Đã trở thành \"{tier}\")";
        }
    }

    private static readonly Dictionary<PokemonPersonality, string[]> GreetingLines = new()
    {
        { PokemonPersonality.Brave,   new[] { "{0}: Hôm nay đánh trận nào thì gọi mình nhé!", "{0}: Sẵn sàng chiến đấu bất cứ lúc nào!" } },
        { PokemonPersonality.Timid,   new[] { "{0}: H-hôm nay… mình sẽ cố gắng…", "{0}: Đừng đi nhanh quá nhé…" } },
        { PokemonPersonality.Playful, new[] { "{0}: Đi chơi thôi đi chơi thôi~!", "{0}: Hôm nay có gì vui không?!" } },
        { PokemonPersonality.Proud,   new[] { "{0}: Hừm, dậy sớm đấy. Khá khen.", "{0}: Đi thôi, đừng làm mình chờ." } },
        { PokemonPersonality.Gentle,  new[] { "{0}: Chào buổi sáng… hôm nay trời đẹp nhỉ.", "{0}: Đi cùng nhau chậm rãi thôi nhé." } },
        { PokemonPersonality.Curious, new[] { "{0}: Hôm nay khám phá chỗ nào mới không?!", "{0}: Mình đánh hơi thấy điều thú vị đâu đây~" } },
        { PokemonPersonality.Lazy,    new[] { "{0}: Ngáp… đi đâu cũng được, miễn có ăn…", "{0}: Cõng mình đi được không…" } },
    };

    private static readonly Dictionary<PokemonPersonality, string[]> HpLowLines = new()
    {
        { PokemonPersonality.Brave,   new[] { "{0}: Vết thương này… chưa là gì đâu! Nhưng nghỉ chút cũng được…", "{0}: Mình vẫn chiến được! …chắc vậy." } },
        { PokemonPersonality.Timid,   new[] { "{0}: Đau quá… mình muốn về Pokemon Center…", "{0}: Hu hu… mình mệt lắm rồi…" } },
        { PokemonPersonality.Playful, new[] { "{0}: Ui da… hết muốn đùa nổi rồi…", "{0}: Cho mình nghỉ tí được không…" } },
        { PokemonPersonality.Proud,   new[] { "{0}: Đừng nhìn! Mình KHÔNG sao cả… (thở dốc)", "{0}: Hmph… chỉ là vết xước." } },
        { PokemonPersonality.Gentle,  new[] { "{0}: Mình hơi mệt… ghé Pokemon Center nhé?", "{0}: Xin lỗi… mình cần nghỉ một lát." } },
        { PokemonPersonality.Curious, new[] { "{0}: Đau thật đấy… nhưng ai đánh mình nhỉ, mạnh ghê…", "{0}: Mình cần hồi phục để còn khám phá tiếp…" } },
        { PokemonPersonality.Lazy,    new[] { "{0}: Đau… giờ có cớ để nằm rồi…", "{0}: Mình xin phép… nằm tại chỗ…" } },
    };

    private static readonly Dictionary<PokemonPersonality, string[]> IdleLines = new()
    {
        { PokemonPersonality.Brave,   new[] { "{0}: Đứng mãi thế này chân mình cứng lại bây giờ!", "{0}: Đi tìm đối thủ đi mà~" } },
        { PokemonPersonality.Timid,   new[] { "{0}: Ưm… cậu đang suy nghĩ gì à…?", "{0}: Đứng yên thế này… cũng an toàn ha…" } },
        { PokemonPersonality.Playful, new[] { "{0}: Chán quáaaa~ chơi trò gì đi!", "{0}: Đố cậu bắt được mình đấy~!" } },
        { PokemonPersonality.Proud,   new[] { "{0}: …Cậu định đứng đây tới bao giờ?", "{0}: Thời gian của mình quý lắm đấy nhé." } },
        { PokemonPersonality.Gentle,  new[] { "{0}: Đứng ngắm cảnh thế này cũng thích nhỉ…", "{0}: Không cần vội đâu, từ từ thôi." } },
        { PokemonPersonality.Curious, new[] { "{0}: Ê nhìn kìa! …à không, nhầm.", "{0}: Trong lúc chờ, mình đi ngửi mấy bụi cỏ nhé?" } },
        { PokemonPersonality.Lazy,    new[] { "{0}: Zzz… hở? Chưa đi à… tốt…", "{0}: Đứng yên là sở trường của mình đó." } },
    };

    private static readonly Dictionary<PokemonPersonality, string[]> ChatterLines = new()
    {
        { PokemonPersonality.Brave,   new[] { "{0}: Mình mơ thấy hạ gục một con rồng đấy!", "{0}: Cậu thấy mình dạo này mạnh lên không?" } },
        { PokemonPersonality.Timid,   new[] { "{0}: Đi cạnh cậu… mình đỡ sợ hơn nhiều…", "{0}: Nếu gặp Pokemon dữ… cậu che cho mình nhé…" } },
        { PokemonPersonality.Playful, new[] { "{0}: Lêu lêu bắt được rồi~ à nhầm, chưa gì hết!", "{0}: Kể chuyện cười đi! Mình kể trước: Pika… quên rồi!" } },
        { PokemonPersonality.Proud,   new[] { "{0}: Nói trước, hôm nay mình tỏa sáng đấy.", "{0}: Người ngoài nhìn vào chắc ghen tị với cậu lắm — đi cùng mình mà." } },
        { PokemonPersonality.Gentle,  new[] { "{0}: Gió hôm nay dễ chịu ghê…", "{0}: Cảm ơn vì luôn chăm sóc mình nhé." } },
        { PokemonPersonality.Curious, new[] { "{0}: Cậu có bao giờ thắc mắc trong Pokeball trông thế nào không?", "{0}: Hình như phía trước có gì đó hay lắm!" } },
        { PokemonPersonality.Lazy,    new[] { "{0}: Nghĩ đến việc phải đi tiếp là mình buồn ngủ…", "{0}: Ước gì có ai cõng…" } },
    };

    // ===== Snapshot cờ truyện (phát hiện thay đổi) =====

    private string SnapshotFlags()
    {
        var f = StoryFlags.Instance;
        if (f == null) return cachedFlagsSnapshot ?? "";
        return $"{f.PrologueDone}{f.FirstMainQuestAccepted}{f.StarterChosen}{f.MeetGreen}{f.MeetBlue}" +
               $"{f.AfterGrassGym}{f.MeetTeamRocket}{f.AfterWaterGym}{f.InCave}{f.OutCave}{f.AfterFireGym}{f.Champion}";
    }

    // ===== Tự dựng UI =====

    private void BuildUi()
    {
        if (uiBuilt) return;
        uiBuilt = true;

        // Canvas riêng — không đụng canvas của scene
        var canvasGo = new GameObject("CompanionHudCanvas");
        canvasGo.transform.SetParent(transform, false);
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;   // dưới toast/thông báo hệ thống, trên map

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        hudRoot = new GameObject("HudRoot");
        hudRoot.transform.SetParent(canvasGo.transform, false);
        var rootRect = hudRoot.AddComponent<RectTransform>();
        rootRect.anchorMin = rootRect.anchorMax = new Vector2(0f, 0f);   // góc trái-dưới
        rootRect.pivot = new Vector2(0f, 0f);
        rootRect.anchoredPosition = new Vector2(24f, 24f);
        rootRect.sizeDelta = new Vector2(640f, 320f);

        // --- Khung chân dung ---
        var frame = CreateRect(hudRoot.transform, "PortraitFrame", new Vector2(0f, 0f), new Vector2(170f, 170f));
        var frameImg = frame.gameObject.AddComponent<Image>();
        frameImg.color = new Color(0.06f, 0.08f, 0.12f, 0.82f);

        var portrait = CreateRect(frame, "Portrait", new Vector2(12f, 16f), new Vector2(146f, 146f));
        portraitImage = portrait.gameObject.AddComponent<Image>();
        portraitImage.preserveAspect = true;
        portraitRect = portrait;

        // --- Nhãn tên · bond ---
        var label = CreateRect(hudRoot.transform, "NameLabel", new Vector2(0f, -26f), new Vector2(280f, 26f));
        nameLabel = label.gameObject.AddComponent<TextMeshProUGUI>();
        nameLabel.font = TMP_Settings.defaultFontAsset;
        nameLabel.fontSize = 19f;
        nameLabel.color = new Color(1f, 1f, 1f, 0.9f);
        nameLabel.alignment = TextAlignmentOptions.TopLeft;
        nameLabel.raycastTarget = false;

        // --- Bong bóng thoại ---
        var bubble = CreateRect(hudRoot.transform, "Bubble", new Vector2(184f, 14f), new Vector2(430f, 118f));
        var bubbleImg = bubble.gameObject.AddComponent<Image>();
        bubbleImg.color = new Color(0.06f, 0.08f, 0.12f, 0.88f);
        bubbleGroup = bubble.gameObject.AddComponent<CanvasGroup>();
        bubbleGroup.alpha = 0f;
        bubbleGroup.interactable = false;
        bubbleGroup.blocksRaycasts = false;

        var textRect = CreateRect(bubble, "Text", new Vector2(14f, 10f), new Vector2(402f, 98f));
        bubbleText = textRect.gameObject.AddComponent<TextMeshProUGUI>();
        bubbleText.font = TMP_Settings.defaultFontAsset;
        bubbleText.fontSize = 25f;
        bubbleText.color = Color.white;
        bubbleText.alignment = TextAlignmentOptions.TopLeft;
        bubbleText.enableWordWrapping = true;
        bubbleText.overflowMode = TextOverflowModes.Ellipsis;
        bubbleText.raycastTarget = false;

        hudRoot.SetActive(false);
    }

    private static RectTransform CreateRect(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        return rect;
    }
}
