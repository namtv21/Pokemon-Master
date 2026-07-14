/// <summary>
/// Tính cách CỐ ĐỊNH của một Pokemon và bậc gắn kết (bond) suy từ FriendshipLevel.
/// Chỉ phục vụ TƯƠNG TÁC (AI companion + hiển thị) — KHÔNG ảnh hưởng cơ chế battle.
/// </summary>
public enum PokemonPersonality
{
    Playful,   // Tinh nghịch
    Brave,     // Dũng cảm
    Timid,     // Nhút nhát
    Proud,     // Kiêu hãnh
    Gentle,    // Điềm tĩnh
    Curious,   // Tò mò
    Lazy       // Lười biếng
}

public enum BondTier
{
    Stranger,     // Xa lạ
    Acquaintance, // Quen
    Friend,       // Bạn
    Companion,    // Đồng hành
    Soulmate      // Tri kỷ
}

public static class PokemonPersonalityUtil
{
    private static readonly System.Random rng = new System.Random();

    public static PokemonPersonality RandomPersonality()
    {
        var values = System.Enum.GetValues(typeof(PokemonPersonality));
        return (PokemonPersonality)values.GetValue(rng.Next(values.Length));
    }

    /// Nhãn tiếng Việt (có dấu) để hiển thị UI và gửi cho AI.
    public static string Label(PokemonPersonality personality)
    {
        switch (personality)
        {
            case PokemonPersonality.Playful: return "Tinh nghịch";
            case PokemonPersonality.Brave:   return "Dũng cảm";
            case PokemonPersonality.Timid:   return "Nhút nhát";
            case PokemonPersonality.Proud:   return "Kiêu hãnh";
            case PokemonPersonality.Gentle:  return "Điềm tĩnh";
            case PokemonPersonality.Curious: return "Tò mò";
            case PokemonPersonality.Lazy:    return "Lười biếng";
            default:                         return "Bình thường";
        }
    }

    public static BondTier TierOf(int friendshipLevel)
    {
        if (friendshipLevel >= 200) return BondTier.Soulmate;
        if (friendshipLevel >= 100) return BondTier.Companion;
        if (friendshipLevel >= 50)  return BondTier.Friend;
        if (friendshipLevel >= 20)  return BondTier.Acquaintance;
        return BondTier.Stranger;
    }

    public static string TierLabel(BondTier tier)
    {
        switch (tier)
        {
            case BondTier.Stranger:     return "Xa lạ";
            case BondTier.Acquaintance: return "Quen";
            case BondTier.Friend:       return "Bạn";
            case BondTier.Companion:    return "Đồng hành";
            case BondTier.Soulmate:     return "Tri kỷ";
            default:                    return "Xa lạ";
        }
    }
}
