using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EncounterZone : MonoBehaviour
{
    public List<WildPokemon> possiblePokemon;

    public Pokemon GetRandomPokemon()
    {
        float roll = Random.value;
        float cumulative = 0f;
        foreach (var wp in possiblePokemon)
        {
            cumulative += wp.encounterRate;
            if (roll <= cumulative)
            {
                int level = Random.Range(wp.minLevel, wp.maxLevel + 1);
                return new Pokemon(wp.pokemon, level);
            }
        }
        return null;
    }
}

[System.Serializable]
public class WildPokemon
{
    public PokemonBase pokemon;
    public int minLevel;
    public int maxLevel;
    public float encounterRate;
}
