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
            currentLocation  = GetCurrentLocation()
        };
    }

    private string GetCurrentLocation()
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
            "GrassGym"  => "Grass Gym",
            "WaterGym"  => "Water Gym",
            "FireGym"   => "Fire Gym",
            _           => scene
        };
    }

    // --- API call ---

    public IEnumerator SendMessageToCompanion(string userMessage, System.Action<string> onComplete)
    {
        if (string.IsNullOrEmpty(workerUrl))
        {
            onComplete?.Invoke("(Chưa cấu hình Worker URL — đặt trong Inspector của CompanionChatSystem)");
            yield break;
        }

        var companion = GetCompanion();
        var flags     = StoryFlags.GetOrCreate();

        var requestObj = new ApiRequest
        {
            gameState = BuildDynamicContext(companion, flags),
            messages  = new[] { new ApiMessage { role = "user", content = userMessage } }
        };

        string json = JsonUtility.ToJson(requestObj);

        var webRequest = new UnityWebRequest(workerUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        webRequest.uploadHandler            = new UploadHandlerRaw(bodyRaw);
        webRequest.uploadHandler.contentType = "application/json";
        webRequest.downloadHandler          = new DownloadHandlerBuffer();
        webRequest.SetRequestHeader("x-game-token", gameToken);

        yield return webRequest.SendWebRequest();

        string resultText;
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

        webRequest.Dispose();
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
            _ => $"{name}: Pika pika!"
        };
    }

    private string GetMoodResponse(Pokemon companion, string name)
    {
        if (companion == null) return $"{name}: Pikachu!";
        float hp = (float)companion.CurrentHp / companion.MaxHp;
        if (hp <= 0f)   return $"{name}: Pi... (cần nghỉ ngơi.. Pi.)";
        if (hp <= 0.5f)  return $"{name}: Pika pika... (Cần hồi phục rồi..)";
        if (hp >= 1f)    return $"{name}: PIKACHU! (Trạng thái hoàn hảo! một ngàn vôn sẵn sàng!)";
        return $"{name}: Pikachu! (I can do this all day! - Một vị đội trưởng đã nói thế đấy Red)";
    }

    private string GetNextStepResponse(string name, StoryFlags flags)
    {
        if (flags.AfterFireGym)           return $"{name}: Pi PIKA chu! (Đến lúc thách thức Champion rồi!)";
        if (flags.InCave)                 return $"{name}: Pika... (Chúng ta cần tìm tên đã cướp huy hiệu trong hang động này)";
        if (flags.AfterWaterGym)          return $"{name}: Pika chu! (FireGym caanfd đi qua Mountain và Cave bên trái WaterTown)";
        if (flags.AfterGrassGym)          return $"{name}: Pikachu! (Tiếp theo là WaterGym. Nơi đó ở phía nam của GrassTown!)";
        if (flags.FirstMainQuestAccepted) return $"{name}: Pika... (Bắt đầu cuộc hành trình thôi! Mà tiến sĩ Oke đang đợi cậu đấy!)";
        if (flags.StarterChosen)           return $"{name}: Pi pi! (Điếm đến đầu tiên: GrassGym)";
        return $"{name}: Pikachu... (Khám phá thế giới nào!)";
    }

    private string GetCharacterThoughtResponse(string name, string character, StoryFlags flags)
    {
        if (character == "Green")
        {
            if (flags.AfterFireGym)         return $"{name}: Pika... (Green có lẽ cũng không quá tệ?)";
            if (flags.MeetGreen)        return $"{name}: Pi ka... (Green đúng là tên khó ưa!)";
            if (flags.FirstMainQuestAccepted) return $"{name}: Pi ka? (Green là ai?)";
            return $"{name}: Pikachu! (Tên kiêu ngạo! Hừ!)";
        }
        if (character == "Blue")
        {
            if (flags.AfterFireGym)  return $"{name}: PIKA! (Blue đã trở nên mạnh hơn nhiều!)";
            if (flags.MeetBlue) return $"{name}: Pi pi... (Blue luôn chia sẻ đồ ăn của cô ấy cho tôi <3)";
            return $"{name}: Pikachu! (Blue, đồ ăn, thích!)";
        }
        return $"{name}: Pika pika?";
    }

    private string GetEnemyThoughtResponse(string name, StoryFlags flags)
    {
        if (flags.OutCave)  return $"{name}: PIKA PIKA! (Có lẽ việc này sẽ khiến họ im lặng một thời gian!)";
        if (flags.MeetTeamRocket) return $"{name}: Pi ka! (Team Rocket cần được dạy cho một bài học!)";
        if (flags.FirstMainQuestAccepted)  return $"{name}: Pika? (Team Rocket? chưa từng nghe qua)";
        return $"{name}: Pi pi! (Team Rocket thật khó ưa)";
    }
}
