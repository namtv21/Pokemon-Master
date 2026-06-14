using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EncounterZone : MonoBehaviour
{
    [SerializeField] private List<WildPokemon> possiblePokemon;
    [SerializeField, Range(0f, 100f)] private float battleRatePercent = 10f;

    public float BattleRatePercent => Mathf.Clamp(battleRatePercent, 0f, 100f);

    public Pokemon GetRandomPokemon()
    {
        if (possiblePokemon == null || possiblePokemon.Count == 0)
            return null;

        float roll = Random.value;
        float cumulative = 0f;
        foreach (var wp in possiblePokemon)
        {
            if (wp == null || wp.pokemon == null)
                continue;

            cumulative += wp.encounterRate;
            if (roll <= cumulative)
            {
                int level = Random.Range(wp.minLevel, wp.maxLevel + 1);
                return new Pokemon(wp.pokemon, level);
            }
        }

        // Rates don't sum to 1 — pick proportionally among valid entries as fallback
        float totalRate = 0f;
        for (int i = 0; i < possiblePokemon.Count; i++)
        {
            var wp = possiblePokemon[i];
            if (wp != null && wp.pokemon != null)
                totalRate += wp.encounterRate;
        }

        if (totalRate > 0f)
        {
            float fallbackRoll = Random.value * totalRate;
            float fallbackCumulative = 0f;
            for (int i = 0; i < possiblePokemon.Count; i++)
            {
                var wp = possiblePokemon[i];
                if (wp == null || wp.pokemon == null) continue;
                fallbackCumulative += wp.encounterRate;
                if (fallbackRoll <= fallbackCumulative)
                {
                    int level = Random.Range(wp.minLevel, wp.maxLevel + 1);
                    return new Pokemon(wp.pokemon, level);
                }
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
