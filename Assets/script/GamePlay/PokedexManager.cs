using System;
using System.Collections.Generic;
using UnityEngine;

public class PokedexManager : MonoBehaviour
{
    public static PokedexManager Instance { get; private set; }

    private readonly HashSet<string> seenIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> caughtIds = new(StringComparer.OrdinalIgnoreCase);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static PokedexManager GetOrCreate()
    {
        if (Instance != null)
            return Instance;

        var existing = FindObjectOfType<PokedexManager>();
        if (existing != null)
            return existing;

        var go = new GameObject("PokedexManager");
        return go.AddComponent<PokedexManager>();
    }

    public bool HasSeen(string pokemonName) => !string.IsNullOrWhiteSpace(pokemonName) && seenIds.Contains(pokemonName);
    public bool HasCaught(string pokemonName) => !string.IsNullOrWhiteSpace(pokemonName) && caughtIds.Contains(pokemonName);

    public void MarkSeen(PokemonBase pokemonBase)
    {
        if (pokemonBase == null || string.IsNullOrWhiteSpace(pokemonBase.Name)) return;
        seenIds.Add(pokemonBase.Name);
    }

    public void MarkCaught(PokemonBase pokemonBase)
    {
        if (pokemonBase == null || string.IsNullOrWhiteSpace(pokemonBase.Name)) return;
        seenIds.Add(pokemonBase.Name);
        caughtIds.Add(pokemonBase.Name);
    }

    public void MarkCaught(Pokemon pokemon)
    {
        if (pokemon == null) return;
        MarkCaught(pokemon.Base);
    }

    public void RebuildFromOwnedPokemon(IEnumerable<Pokemon> party, IEnumerable<Pokemon> storage)
    {
        if (party != null)
        {
            foreach (var p in party)
                MarkCaught(p);
        }

        if (storage != null)
        {
            foreach (var p in storage)
                MarkCaught(p);
        }
    }

    public PokedexSaveData ExportData()
    {
        return new PokedexSaveData
        {
            seenPokemonIds = new List<string>(seenIds),
            caughtPokemonIds = new List<string>(caughtIds)
        };
    }

    public void ImportData(PokedexSaveData data)
    {
        seenIds.Clear();
        caughtIds.Clear();

        if (data == null)
            return;

        if (data.seenPokemonIds != null)
        {
            foreach (var id in data.seenPokemonIds)
            {
                if (!string.IsNullOrWhiteSpace(id))
                    seenIds.Add(id);
            }
        }

        if (data.caughtPokemonIds != null)
        {
            foreach (var id in data.caughtPokemonIds)
            {
                if (string.IsNullOrWhiteSpace(id)) continue;
                caughtIds.Add(id);
                seenIds.Add(id);
            }
        }
    }
}
