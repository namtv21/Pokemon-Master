using System.Collections.Generic;

public class PartyHandler
{
    private List<Pokemon> pokemons;

    public PartyHandler(List<Pokemon> pokemons)
    {
        this.pokemons = pokemons;
    }

    public List<Pokemon> GetPokemons()
    {
        return pokemons;
    }

    public void SwitchPokemon(int indexA, int indexB)
    {
        if (indexA < 0 || indexA >= pokemons.Count) return;
        if (indexB < 0 || indexB >= pokemons.Count) return;

        var temp = pokemons[indexA];
        pokemons[indexA] = pokemons[indexB];
        pokemons[indexB] = temp;
    }
}
