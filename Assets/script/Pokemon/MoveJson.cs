using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[Serializable]
public class MoveJson
{
    public int num;
    public string name;
    public PokemonType type;
    public int basePower;
    [JsonConverter(typeof(AccuracyConverter))]
    public int accuracy;
    public int pp;
    public string category;
    public int priority;
    public string target;

    public int[] drain; // ví dụ: [1, 2] → hồi 50% sát thương
    public Dictionary<string, int> boosts; // ví dụ: { "attack": 1 }
    public string status; // ví dụ: "Poison", "Paralyze", hoặc null
}