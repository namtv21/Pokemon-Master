using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TrainerParty : MonoBehaviour
{
    [Header("Trainer Pokemon")]
    [SerializeField] private List<PokemonSaveData> pokemons;
    [SerializeField] private bool evolvePokemonByLevel = false;

    [System.Serializable]
    public class PokemonSaveData
    {
        public PokemonBase baseData;
        public int level;
    }

    public List<Pokemon> GetTrainerPokemons()
    {
        var result = new List<Pokemon>();
        if (pokemons == null)
            return result;

        foreach (var data in pokemons)
        {
            if (data == null || data.baseData == null)
                continue;

            int level = Mathf.Max(1, data.level);
            var resolvedBase = evolvePokemonByLevel
                ? ResolveEvolutionForLevel(data.baseData, level)
                : data.baseData;

            result.Add(new Pokemon(resolvedBase, level));
        }

        return result;
    }

    public bool ApplyStarterOverride(string starterPokemonId, bool useCounterStarter)
    {
        var nextStarterId = GetRivalStarterId(starterPokemonId, useCounterStarter);
        if (string.IsNullOrWhiteSpace(nextStarterId))
            return false;

        var pokemonBase = ResolvePokemonBase(nextStarterId);
        if (pokemonBase == null)
            return false;

        if (pokemons == null)
            pokemons = new List<PokemonSaveData>();

        if (pokemons.Count == 0)
            pokemons.Add(new PokemonSaveData());

        pokemons[0].baseData = pokemonBase;
        if (pokemons[0].level <= 0)
            pokemons[0].level = 5;

        return true;
    }

    private static string GetRivalStarterId(string starterPokemonId, bool useCounterStarter)
    {
        string normalized = TextKeyUtility.NormalizeKey(starterPokemonId);
        string[] cycle = { "bulbasaur", "charmander", "squirtle" };

        int index = System.Array.FindIndex(cycle, id => id == normalized);
        if (index < 0)
            return string.Empty;

        int offset = useCounterStarter ? 1 : 2;
        return cycle[(index + offset) % cycle.Length];
    }

    private static PokemonBase ResolvePokemonBase(string resourceId)
    {
        if (string.IsNullOrWhiteSpace(resourceId))
            return null;

        var loaded = Resources.Load<PokemonBase>($"PokemonData/{TextKeyUtility.NormalizeResourceId(resourceId)}");
        if (loaded != null)
            return loaded;

        var db = PokemonDB.Instance != null ? PokemonDB.Instance : Object.FindObjectOfType<PokemonDB>(true);
        if (db == null)
            return null;

        return db.GetAllPokemons().FirstOrDefault(p =>
            p != null &&
            string.Equals(
                TextKeyUtility.NormalizeKey(p.name),
                TextKeyUtility.NormalizeKey(resourceId),
                System.StringComparison.OrdinalIgnoreCase));
    }

    private static PokemonBase ResolveEvolutionForLevel(PokemonBase baseData, int level)
    {
        var current = baseData;
        int guard = 0;

        while (current != null &&
               current.Evolvable &&
               current.EvolvesTo != null &&
               level >= current.EvolutionLevel &&
               guard++ < 10)
        {
            current = current.EvolvesTo;
        }

        return current != null ? current : baseData;
    }

    public Pokemon GetFirstPokemon()
    {
        var party = GetTrainerPokemons();
        return party.Count > 0 ? party[0] : null;
    }

    public Pokemon GetNextHealthyPokemon()
    {
        var party = GetTrainerPokemons();
        foreach (var pokemon in party)
        {
            if (!pokemon.IsFainted)
                return pokemon;
        }

        return null;
    }

    public bool HasHealthyPokemon()
    {
        var party = GetTrainerPokemons();
        foreach (var pokemon in party)
        {
            if (!pokemon.IsFainted)
                return true;
        }

        return false;
    }

    public List<Pokemon> GetPokemons()
    {
        return GetTrainerPokemons();
    }

    public void SetTeamFromResourceSpecs(
        System.Collections.Generic.IReadOnlyList<string> resourceIds,
        System.Collections.Generic.IReadOnlyList<int> levels)
    {
        pokemons = new List<PokemonSaveData>();
        if (resourceIds == null)
            return;

        for (int i = 0; i < resourceIds.Count; i++)
        {
            var resId = resourceIds[i];
            if (string.IsNullOrWhiteSpace(resId))
            {
                Debug.LogWarning($"[TrainerParty] Story trainer team entry {i} is empty and was skipped.");
                continue;
            }

            var baseData = ResolvePokemonBase(resId);
            if (baseData == null)
            {
                Debug.LogWarning($"[TrainerParty] Could not resolve story trainer Pokemon '{resId}' and skipped it.");
                continue;
            }

            int lvl = 5;
            if (levels != null && i < levels.Count)
                lvl = Mathf.Max(1, levels[i]);

            pokemons.Add(new PokemonSaveData { baseData = baseData, level = lvl });
        }

        Debug.Log($"[TrainerParty] Story trainer team applied with {pokemons.Count} Pokemon(s).");
    }
}
