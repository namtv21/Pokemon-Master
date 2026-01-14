using UnityEngine;
using System.Collections.Generic;

public class PokemonDB : MonoBehaviour
{
    public static PokemonDB Instance { get; private set; }
    private List<PokemonBase> pokemons;

    void Awake()
    {
        Instance = this;
        // Tự động load tất cả PokemonBase trong Resources/Pokemon
        pokemons = new List<PokemonBase>(Resources.LoadAll<PokemonBase>("PokemonData"));
        //Debug.Log($"PokemonDB loaded {pokemons.Count} Pokémon");
    }

    public PokemonBase GetPokemonByName(string name)
    {
        var pokemon = pokemons.Find(p => 
        string.Equals(p.name, name, System.StringComparison.OrdinalIgnoreCase));

        if (pokemon == null)
            Debug.LogWarning($"PokemonDB: Không tìm thấy Pokémon '{name}'");
        return pokemon;
    }
}