/// <summary>
/// Fixed personality for a Pokemon instance and bond tier derived from FriendshipLevel.
/// Used only for companion interaction/display, not battle mechanics.
/// </summary>
public enum PokemonPersonality
{
    Playful,
    Brave,
    Timid,
    Proud,
    Gentle,
    Loyal,
    Curious,
    Lazy
}

public enum BondTier
{
    Stranger,
    Acquaintance,
    Friend,
    Companion,
    Soulmate
}

public static class PokemonPersonalityUtil
{
    private static readonly System.Random rng = new System.Random();

    public static PokemonPersonality RandomPersonality()
    {
        var values = System.Enum.GetValues(typeof(PokemonPersonality));
        return (PokemonPersonality)values.GetValue(rng.Next(values.Length));
    }

    public static string Label(PokemonPersonality personality)
    {
        switch (personality)
        {
            case PokemonPersonality.Playful: return "Tinh nghich";
            case PokemonPersonality.Brave:   return "Dung cam";
            case PokemonPersonality.Timid:   return "Nhut nhat";
            case PokemonPersonality.Proud:   return "Kieu hanh";
            case PokemonPersonality.Gentle:  return "Diem tinh";
            case PokemonPersonality.Loyal:   return "Trung thanh";
            case PokemonPersonality.Curious: return "To mo";
            case PokemonPersonality.Lazy:    return "Luoi bieng";
            default:                         return "Binh thuong";
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
            case BondTier.Stranger:     return "Xa la";
            case BondTier.Acquaintance: return "Quen";
            case BondTier.Friend:       return "Ban";
            case BondTier.Companion:    return "Dong hanh";
            case BondTier.Soulmate:     return "Tri ky";
            default:                    return "Xa la";
        }
    }
}
