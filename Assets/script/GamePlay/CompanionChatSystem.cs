using UnityEngine;

public class CompanionChatSystem : MonoBehaviour
{
    public static CompanionChatSystem Instance { get; private set; }

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

    public void AddIntimacy(int amount = 1)
    {
        GetCompanion()?.AddFriendship(amount);
    }

    // 0 = mệt/fainted, 1 = bình thường, 2 = vui (HP đầy)
    public int GetMoodIndex(Pokemon companion)
    {
        if (companion == null || companion.CurrentHp <= 0) return 0;
        float hp = (float)companion.CurrentHp / companion.MaxHp;
        if (hp < 0.5f) return 0;
        if (hp < 1f) return 1;
        return 2;
    }

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
        if (hp <= 0f)
            return $"{name}: Pi... (cần nghỉ ngơi.. Pi.)";
        if (hp <= 0.25f)
            return $"{name}: Pi... ka... (Đến Healing center là lựa chọn không tồi...)";
        if (hp <= 0.5f)
            return $"{name}: Pika pika... (Có lẽ cần dùng potion)";
        if (hp >= 1f)
            return $"{name}: PIKACHU! (Trạng thái hoàn hảo! Cũng chiến đấu nào!)";
        return $"{name}: Pikachu! (Tiếp tục nào)";
    }

    private string GetNextStepResponse(string name, StoryFlags flags)
    {
        if (flags.AfterFireGym)
            return $"{name}: Pi PIKA chu! (Chúng ta đã chinh phục cả ba Phòng Tập! Đến lúc thách thức Champion rồi!)";
        if (flags.AfterWaterGym)
            return $"{name}: Pika chu! (Phòng Tập Lửa đang chờ! Hãy chuẩn bị kỹ trước khi vào nhé.)";
        if (flags.AfterGrassGym)
            return $"{name}: Pikachu! (Tiếp theo là Phòng Tập Nước. Chúng ta cần cẩn thận với nước đấy!)";
        if (flags.FirstMainQuestAccepted)
            return $"{name}: Pika... (Nhiệm vụ vẫn đang chờ. Hãy hoàn thành đi rồi mình cùng tiến xa hơn!)";
        if (flags.PrologueDone)
            return $"{name}: Pi pi! (Thách thức Phòng Tập Thảo Nguyên đi! Đó là bước đầu tiên của chúng ta!)";
        return $"{name}: Pikachu... (Hãy khám phá thế giới, gặp gỡ nhiều Pokemon và trở nên mạnh hơn!)";
    }

    private string GetCharacterThoughtResponse(string name, string character, StoryFlags flags)
    {
        if (character == "Green")
        {
            if (flags.AfterFireGym)
                return $"{name}: Pika... (Có thể thấy Green đối xử khá tốt với Pokemon, tại sao anh ta luôn độc miệng nhỉ?)";
            if (flags.AfterWaterGym)
                return $"{name}: Pi ka... (Green đúng là tên khó ưa!)";
            if (flags.FirstMainQuestAccepted)
                return $"{name}: Pi ka? (Green là ai?)";
            return $"{name}: Pikachu! (Tên kiêu ngạo! Hừ!)";
        }
        if (character == "Blue")
        {
            if (flags.AfterFireGym)
                return $"{name}: PIKA! (Blue đã trở nên mạnh hơn nhiều. Cuộc đối đầu cuối cùng sẽ rất gay cấn!)";
            if (flags.AfterGrassGym)
                return $"{name}: Pi pi... (Blue luôn xuất hiện đúng lúc chúng ta muốn nghỉ ngơi nhỉ...)";
            return $"{name}: Pikachu! (Blue trông kiêu ngạo vậy thôi, nhưng tôi nghĩ anh ấy cũng quan tâm đến Pokemon đấy.)";
        }
        return $"{name}: Pika pika?";
    }

    private string GetEnemyThoughtResponse(string name, StoryFlags flags)
    {
        if (flags.AfterFireGym)
            return $"{name}: PIKA PIKA! (Team Rocket thật nguy hiểm... nhưng chúng ta sẽ không bao giờ bỏ cuộc!)";
        if (flags.AfterGrassGym)
            return $"{name}: Pi ka! (Team Rocket đang trở nên táo bạo hơn. Chúng ta phải mạnh hơn họ!)";
        if (flags.PrologueDone)
            return $"{name}: Pika... (Tôi nghe nói về Team Rocket... Họ thật đáng sợ. Nhưng có bạn bên cạnh, tôi không sợ!)";
        return $"{name}: Pi pi? (Kẻ xấu ư? Tôi sẽ bảo vệ bạn dù thế nào đi nữa! Hãy tin tưởng tôi!)";
    }

}
