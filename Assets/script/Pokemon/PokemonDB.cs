using System;
using System.Collections.Generic;
using UnityEngine;

public class PokemonDB : MonoBehaviour
{
    private static PokemonDB instance;

    public static PokemonDB Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<PokemonDB>(true);
                if (instance == null)
                {
                    var go = new GameObject("RuntimePokemonDB");
                    DontDestroyOnLoad(go);
                    instance = go.AddComponent<PokemonDB>();
                }

                instance.LoadAllPokemon();
            }

            return instance;
        }
        private set => instance = value;
    }

    private List<PokemonBase> pokemons;
    // Index tra theo tên đã chuẩn hóa — O(1) thay vì quét + chuẩn hóa cả danh sách mỗi lần gọi.
    private Dictionary<string, PokemonBase> pokemonsByName;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        LoadAllPokemon();
    }

    public IReadOnlyList<PokemonBase> GetAllPokemons()
    {
        LoadAllPokemon();
        return pokemons;
    }

    public PokemonBase GetPokemonByName(string name)
    {
        LoadAllPokemon();
        pokemonsByName.TryGetValue(NormalizePokemonName(name), out var pokemon);
        return pokemon;
    }

    private void LoadAllPokemon()
    {
        if (pokemons != null && pokemons.Count > 0)
            return;

        pokemons = new List<PokemonBase>(Resources.LoadAll<PokemonBase>("PokemonData"));

        // Index cả tên hiển thị lẫn tên asset (2 cách gọi đều dùng trong save/dialog).
        pokemonsByName = new Dictionary<string, PokemonBase>();
        foreach (var p in pokemons)
        {
            if (p == null) continue;
            string displayKey = NormalizePokemonName(p.Name);
            string assetKey   = NormalizePokemonName(p.name);
            if (!string.IsNullOrEmpty(displayKey)) pokemonsByName[displayKey] = p;
            if (!string.IsNullOrEmpty(assetKey))   pokemonsByName[assetKey]   = p;
        }
    }

    private static string NormalizePokemonName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = new System.Text.StringBuilder(value.Length);
        foreach (char c in value)
        {
            if (char.IsLetterOrDigit(c))
                normalized.Append(char.ToLowerInvariant(c));
        }

        return normalized.ToString();
    }
}
