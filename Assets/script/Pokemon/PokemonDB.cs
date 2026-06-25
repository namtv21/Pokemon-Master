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
        string normalizedName = NormalizePokemonName(name);

        var pokemon = pokemons.Find(p =>
            string.Equals(NormalizePokemonName(p?.Name), normalizedName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(NormalizePokemonName(p?.name), normalizedName, StringComparison.OrdinalIgnoreCase));

        return pokemon;
    }

    private void LoadAllPokemon()
    {
        if (pokemons != null && pokemons.Count > 0)
            return;

        pokemons = new List<PokemonBase>(Resources.LoadAll<PokemonBase>("PokemonData"));
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
