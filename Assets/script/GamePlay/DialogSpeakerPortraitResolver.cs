using UnityEngine;

public static class DialogSpeakerPortraitResolver
{
    public static Sprite Resolve(string speakerName, Sprite explicitPortrait = null)
    {
        if (explicitPortrait != null)
            return explicitPortrait;

        if (string.IsNullOrWhiteSpace(speakerName))
            return null;

        if (IsPlayerSpeaker(speakerName))
            return PlayerController.Instance != null ? PlayerController.Instance.Portrait : null;

        var npcPortrait = ResolveNpcPortrait(speakerName);
        if (npcPortrait != null)
            return npcPortrait;

        return ResolvePokemonPortrait(speakerName);
    }

    private static bool IsPlayerSpeaker(string speakerName)
    {
        var normalized = NormalizeSpeakerName(speakerName);
        return normalized.IndexOf("player", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static Sprite ResolveNpcPortrait(string speakerName)
    {
        var normalized = NormalizeSpeakerName(speakerName);
        var npcs = Object.FindObjectsOfType<NPC>(true);

        for (int i = 0; i < npcs.Length; i++)
        {
            var npc = npcs[i];
            if (npc == null)
                continue;

            if (MatchesSpeaker(speakerName, npc.NPCId) || MatchesSpeaker(speakerName, npc.npcName) || MatchesSpeaker(normalized, npc.npcName))
                return npc.Portrait;
        }

        return null;
    }

    private static Sprite ResolvePokemonPortrait(string speakerName)
    {
        var normalized = NormalizeSpeakerName(speakerName);

        var pokemonDb = PokemonDB.Instance;
        if (pokemonDb == null)
            return null;

        var pokemon = pokemonDb.GetPokemonByName(normalized);
        if (pokemon != null)
            return pokemon.FrontSprite;

        if (!MatchesSpeaker(normalized, speakerName))
            pokemon = pokemonDb.GetPokemonByName(speakerName);

        return pokemon != null ? pokemon.FrontSprite : null;
    }

    private static bool MatchesSpeaker(string left, string right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
            return false;

        return string.Equals(NormalizeSpeakerName(left), NormalizeSpeakerName(right), System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(left.Trim(), right.Trim(), System.StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeSpeakerName(string speakerName)
    {
        if (string.IsNullOrWhiteSpace(speakerName))
            return string.Empty;

        var text = speakerName.Trim();
        var bracketIndex = text.IndexOf('(');
        if (bracketIndex >= 0)
            text = text.Substring(0, bracketIndex).Trim();

        return text;
    }
}