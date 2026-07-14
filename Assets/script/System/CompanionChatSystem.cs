using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class CompanionChatSystem : MonoBehaviour
{
    public static CompanionChatSystem Instance { get; private set; }

    [SerializeField] private string workerUrl;
    [SerializeField] private string gameToken;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public bool IsOnline => Application.internetReachability != NetworkReachability.NotReachable;

    public Pokemon GetCompanion()
    {
        var party = PlayerParty.Instance;
        return (party != null && party.Pokemons.Count > 0) ? party.Pokemons[0] : null;
    }

    public int IntimacyLevel => GetCompanion()?.FriendshipLevel ?? 0;

    public void AddIntimacy(int amount = 1) => GetCompanion()?.AddFriendship(amount);

    // 0 = mệt/fainted, 1 = bình thường, 2 = vui (HP đầy)
    public int GetMoodIndex(Pokemon companion)
    {
        if (companion == null || companion.CurrentHp <= 0) return 0;
        float hp = (float)companion.CurrentHp / companion.MaxHp;
        if (hp < 0.5f) return 0;
        if (hp < 1f) return 1;
        return 2;
    }

    // --- Request / Response types ---

    // Dynamic context — chỉ gửi runtime state, KHÔNG gửi static data (đã có trên KV)
    [System.Serializable]
    private class DynamicContext
    {
        public string companionName;
        public int    companionLevel;
        public int    companionHp;
        public int    companionMaxHp;
        public int    intimacy;
        public string currentMoves;     // "Thunderbolt, Quick Attack, Iron Tail"
        public string activeFlags;      // "AfterGrassGym,MeetGreen,InCave"
        public string currentLocation;
        public string personality;      // "Tinh nghịch"
        public string bondTier;         // "Bạn"
        public int    bondPoints;       // = friendship
        public string mood;             // "vui vẻ, phấn khởi"
    }

    [System.Serializable]
    private class ApiRequest
    {
        public string         model      = "claude-haiku-4-5-20251001";
        public int            max_tokens = 250;
        public DynamicContext gameState;
        public ApiMessage[]   messages;
    }

    [System.Serializable]
    private class ApiMessage
    {
        public string role;
        public string content;
    }

    [System.Serializable]
    private class ApiResponse
    {
        public ApiContent[] content;

        [System.Serializable]
        public class ApiContent
        {
            public string type;
            public string text;
        }
    }

    // --- Build dynamic context từ game state ---

    private DynamicContext BuildDynamicContext(Pokemon companion, StoryFlags flags)
    {
        // Gom tên các move hiện tại
        var moveNames = new List<string>();
        if (companion?.Moves != null)
            foreach (var m in companion.Moves)
                if (m?.Base != null) moveNames.Add(m.Base.MoveName);

        // Gom các flags đang active
        var activeFlags = new List<string>();
        if (flags.PrologueDone)           activeFlags.Add("PrologueDone");
        if (flags.FirstMainQuestAccepted) activeFlags.Add("FirstMainQuestAccepted");
        if (flags.StarterChosen)          activeFlags.Add("StarterChosen");
        if (flags.MeetGreen)              activeFlags.Add("MeetGreen");
        if (flags.MeetBlue)               activeFlags.Add("MeetBlue");
        if (flags.AfterGrassGym)          activeFlags.Add("AfterGrassGym");
        if (flags.MeetTeamRocket)         activeFlags.Add("MeetTeamRocket");
        if (flags.AfterWaterGym)          activeFlags.Add("AfterWaterGym");
        if (flags.InCave)                 activeFlags.Add("InCave");
        if (flags.OutCave)                activeFlags.Add("OutCave");
        if (flags.AfterFireGym)           activeFlags.Add("AfterFireGym");
        if (flags.Champion)               activeFlags.Add("Champion");

        return new DynamicContext
        {
            companionName    = companion?.Base?.Name ?? "Pikachu",
            companionLevel   = companion?.Level ?? 1,
            companionHp      = companion?.CurrentHp ?? 0,
            companionMaxHp   = companion?.MaxHp ?? 1,
            intimacy         = companion?.FriendshipLevel ?? 0,
            currentMoves     = string.Join(", ", moveNames),
            activeFlags      = string.Join(",", activeFlags),
            currentLocation  = GetCurrentLocation(),
            personality      = companion != null ? PokemonPersonalityUtil.Label(companion.Personality) : "Bình thường",
            bondTier         = PokemonPersonalityUtil.TierLabel(PokemonPersonalityUtil.TierOf(companion?.FriendshipLevel ?? 0)),
            bondPoints       = companion?.FriendshipLevel ?? 0,
            mood             = GetMoodLabel(companion)
        };
    }

    // --- Tâm trạng & tương tác (chỉ interaction, không đụng battle) ---

    private static float lastInteractionTime = -999f;

    // Tâm trạng đa yếu tố: HP + trạng thái + bond + vừa được vỗ về.
    public string GetMoodLabel(Pokemon companion)
    {
        if (companion == null) return "không rõ";
        if (companion.CurrentHp <= 0) return "kiệt sức";
        if (companion.Status != StatusEffect.None) return "khó chịu";

        if (Time.time - lastInteractionTime < 6f) return "vui vẻ, vừa được vỗ về";

        float hp = (float)companion.CurrentHp / companion.MaxHp;
        var tier = PokemonPersonalityUtil.TierOf(companion.FriendshipLevel);
        if (hp < 0.35f) return "mệt mỏi";
        if (hp >= 1f && tier >= BondTier.Friend) return "vui vẻ, phấn khởi";
        if (tier >= BondTier.Companion) return "thoải mái, tin tưởng";
        return "bình thường";
    }

    // Chuỗi tóm tắt hiển thị trên UI: "Tinh nghịch · Bạn (55) · vui vẻ"
    public string GetStatusSummary(Pokemon companion)
    {
        if (companion == null) return "";
        var tier = PokemonPersonalityUtil.TierOf(companion.FriendshipLevel);
        return $"{PokemonPersonalityUtil.Label(companion.Personality)} · {PokemonPersonalityUtil.TierLabel(tier)} ({companion.FriendshipLevel}) · {GetMoodLabel(companion)}";
    }

    // Vuốt ve: chỉ tương tác + đổi tâm trạng, KHÔNG tăng bond (bond chỉ lên qua số trận đấu).
    public string PetCompanion(Pokemon companion)
    {
        if (companion == null) return "...";
        lastInteractionTime = Time.time;   // boost mood tạm thời, không đụng friendship
        return GetPetResponse(companion);
    }

    private string GetPetResponse(Pokemon companion)
    {
        string name = companion.Base?.Name ?? "Pokemon";
        switch (companion.Personality)
        {
            case PokemonPersonality.Playful: return $"{name}: {name}~! (nhảy cẫng lên vui vẻ)";
            case PokemonPersonality.Brave:   return $"{name}: Hừm! (tỏ ra ngầu nhưng vẫn thích)";
            case PokemonPersonality.Timid:   return $"{name}: ...{name}? (rụt rè rồi dựa vào bạn)";
            case PokemonPersonality.Proud:   return $"{name}: Hmph~ (giả vờ không quan tâm, nhưng đuôi vẫy)";
            case PokemonPersonality.Gentle:  return $"{name}: {name}... (nhắm mắt thoải mái)";
            case PokemonPersonality.Curious: return $"{name}: {name}? {name}! (tò mò nghiêng đầu)";
            case PokemonPersonality.Lazy:    return $"{name}: ...zzz~ (lười biếng nhưng hài lòng)";
            default:                         return $"{name}: {name}~";
        }
    }

    public static string GetCurrentLocation()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        return scene switch
        {
            "Town01"    => "Town 01",
            "GrassTown" => "Grass Town",
            "WaterTown" => "Water Town",
            "FireTown"  => "Fire Town",
            "Road01"    => "Road 01",
            "Road02"    => "Road 02",
            "Road03"    => "Road 03",
            "Road04"    => "Road 04",
            "Cave"      => "Cave",
            "Mountain"  => "Mountain",
            "ChampionMeet"  => "Champion's Meeting",
            "CM_studio"  => "Champion's Studio",
            _           => scene
        };
    }

    // --- API call ---

    private const int MaxInputChars = 200;

    // Làm sạch input người chơi: bỏ ký tự điều khiển, cắt độ dài (defense-in-depth phía client).
    private static string SanitizeUserInput(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return string.Empty;
        var sb = new System.Text.StringBuilder(raw.Length);
        foreach (char c in raw)
        {
            if (char.IsControl(c) && c != ' ') continue;   // bỏ ký tự điều khiển
            sb.Append(c);
        }
        string cleaned = sb.ToString().Trim();
        if (cleaned.Length > MaxInputChars) cleaned = cleaned.Substring(0, MaxInputChars);
        return cleaned;
    }

    // target = null → mặc định chat với Pokemon đầu đội; truyền vào để chat với con bất kỳ trong party.
    public IEnumerator SendMessageToCompanion(string userMessage, System.Action<string> onComplete, Pokemon target = null)
    {
        if (string.IsNullOrEmpty(workerUrl))
        {
            onComplete?.Invoke("(Chưa cấu hình Worker URL — đặt trong Inspector của CompanionChatSystem)");
            yield break;
        }

        userMessage = SanitizeUserInput(userMessage);
        if (string.IsNullOrEmpty(userMessage))
        {
            onComplete?.Invoke("(Tin nhắn trống hoặc không hợp lệ)");
            yield break;
        }

        var companion = target ?? GetCompanion();
        var flags     = StoryFlags.GetOrCreate();

        var requestObj = new ApiRequest
        {
            gameState = BuildDynamicContext(companion, flags),
            messages  = new[] { new ApiMessage { role = "user", content = userMessage } }
        };

        string json = JsonUtility.ToJson(requestObj);

        string resultText;
        using (var webRequest = new UnityWebRequest(workerUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            webRequest.uploadHandler            = new UploadHandlerRaw(bodyRaw);
            webRequest.uploadHandler.contentType = "application/json";
            webRequest.downloadHandler          = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("x-game-token", gameToken);
            webRequest.timeout = 20;   // chống treo UI nếu server/mạng không phản hồi

            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<ApiResponse>(webRequest.downloadHandler.text);
                resultText = (response != null && response.content != null && response.content.Length > 0)
                    ? response.content[0].text
                    : "(Không có phản hồi)";
            }
            else
            {
                string err = webRequest.responseCode > 0
                    ? $"Lỗi {webRequest.responseCode}: {webRequest.downloadHandler?.text ?? webRequest.error}"
                    : $"Lỗi kết nối: {webRequest.error}";
                resultText = $"({err})";
            }
        }

        onComplete?.Invoke(resultText);
    }

    // --- Offline responses ---

    public string GetOfflineResponse(Pokemon companion, int topicIndex)
    {
        string name = companion?.Base?.Name ?? "Pikachu";
        var flags = StoryFlags.GetOrCreate();

        return topicIndex switch
        {
            0 => GetMoodResponse(companion, name),
            1 => GetNextStepResponse(name, flags),
            2 => GetEnemyThoughtResponse(name, flags),
            3 => GetCharacterThoughtResponse(name, "Green", flags),
            4 => GetCharacterThoughtResponse(name, "Blue", flags),
            _ => $"{name}: ?"
        };
    }

    private string GetMoodResponse(Pokemon companion, string name)
    {
        if (companion == null) return $"{name}: Pikachu!";
        float hp = (float)companion.CurrentHp / companion.MaxHp;
        if (hp <= 0f)   return $"{name}: cần nghỉ ngơi..";
        if (hp <= 0.5f)  return $"{name}: Cần hồi phục rồi..";
        if (hp >= 1f)    return $"{name}: Trạng thái hoàn hảo!";
        return $"{name}: I can do this all day!";
    }

    // LƯU Ý: cờ truyện chỉ bật không tắt → PHẢI check mốc MỚI NHẤT trước.
    // Thứ tự cốt truyện: FirstMainQuestAccepted → StarterChosen → AfterGrassGym →
    // AfterWaterGym → InCave → OutCave → AfterFireGym → Champion.
    private string GetNextStepResponse(string name, StoryFlags flags)
    {
        if (flags.Champion)               return $"{name}: Chúng ta là nhà vô địch rồi! Giờ đi khám phá tự do thôi~";
        if (flags.AfterFireGym)           return $"{name}: Đến lúc thách thức Champion rồi!";
        if (flags.OutCave)                return $"{name}: Lấy lại được huy hiệu rồi! Tiếp theo: FireGym!";
        if (flags.InCave)                 return $"{name}: Chúng ta cần tìm tên đã cướp huy hiệu trong hang động này";
        if (flags.AfterWaterGym)          return $"{name}: FireGym cần đi qua Mountain và Cave bên trái WaterTown";
        if (flags.AfterGrassGym)          return $"{name}: Tiếp theo là WaterGym. Nơi đó ở phía nam của GrassTown!";
        if (flags.StarterChosen)          return $"{name}: Điểm đến đầu tiên: GrassGym";
        if (flags.FirstMainQuestAccepted) return $"{name}: Bắt đầu cuộc hành trình thôi! Mà tiến sĩ Oke đang đợi cậu đấy!";
        return $"{name}: Khám phá thế giới nào!";
    }

    private string GetCharacterThoughtResponse(string name, string character, StoryFlags flags)
    {
        if (character == "Green")
        {
            if (flags.AfterFireGym)         return $"{name}: Green có lẽ cũng không quá tệ?";
            if (flags.MeetGreen)        return $"{name}: Green đúng là tên khó ưa!";
            if (flags.FirstMainQuestAccepted) return $"{name}: Green là ai?";
            return $"{name}: Tên kiêu ngạo! Hừ!";
        }
        if (character == "Blue")
        {
            if (flags.AfterFireGym)  return $"{name}: Blue đã trở nên mạnh hơn nhiều!";
            if (flags.MeetBlue) return $"{name}:Blue luôn chia sẻ đồ ăn của cô ấy cho tôi <3";
            return $"{name}:  Blue, đồ ăn, thích!";
        }
        return $"{name}: ?";
    }

    private string GetEnemyThoughtResponse(string name, StoryFlags flags)
    {
        if (flags.OutCave)  return $"{name}: Có lẽ việc này sẽ khiến họ im lặng một thời gian!";
        if (flags.MeetTeamRocket) return $"{name}: Team Rocket cần được dạy cho một bài học!";
        if (flags.FirstMainQuestAccepted)  return $"{name}: Team Rocket? chưa từng nghe qua";
        return $"{name}: Team Rocket thật khó ưa";
    }
}
