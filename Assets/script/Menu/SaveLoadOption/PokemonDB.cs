using UnityEngine;
using System.Collections.Generic;

public class PokemonDB : MonoBehaviour
{
    public static PokemonDB Instance { get; private set; }
    private List<PokemonBase> pokemons;

    void Awake()
    {
        if (Instance != null && Instance != this)
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
        var pokemon = pokemons.Find(p => 
        string.Equals(p.name, name, System.StringComparison.OrdinalIgnoreCase));

        if (pokemon == null)
            Debug.LogWarning($"PokemonDB: Không tìm thấy Pokémon '{name}'");
        return pokemon;
    }

    private void LoadAllPokemon()
    {
        if (pokemons != null && pokemons.Count > 0)
            return;

        // Tự động load tất cả PokemonBase trong Resources/PokemonData
        pokemons = new List<PokemonBase>(Resources.LoadAll<PokemonBase>("PokemonData"));
    }
}