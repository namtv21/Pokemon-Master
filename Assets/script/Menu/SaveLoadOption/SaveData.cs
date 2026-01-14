using System.Collections.Generic;


[System.Serializable]
public class SaveData
{
    public List<PokemonData> partyPokemons;
    public List<PokemonData> storagePokemons;
    public int money;
    public string sceneName;
    public float playerX, playerY, playerZ;
}
