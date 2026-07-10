using System;
using System.Collections.Generic;

[Serializable]
public class BaseStats
{
    public int hp;
    public int atk;
    public int def;
    public int spa;
    public int spd;
    public int spe;
}

[Serializable]
public class PokemonJson
{
    public int num;
    public string name;
    public string[] types;
    public BaseStats baseStats;

    // Nhận diện dạng phụ (mega/galar/alola…) để bỏ qua khi import.
    public string forme;        // vd "Mega", "Galar" — có giá trị = dạng phụ
    public string baseSpecies;  // vd "Venusaur" — có giá trị = dạng phụ của loài khác
}