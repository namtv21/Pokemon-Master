using System;
using System.Collections.Generic;

[Serializable]
public class PokemonData
{
    public string name;
    public int level;
    public int currentHP;
    public int maxHP;
    public int exp;
    public List<string> moves;

    public int attack;
    public int defense;
    public int spAttack;
    public int spDefense;
    public int speed;

    public PokemonData() { }

    public PokemonData(Pokemon p)
    {
        name = p.Base.Name;
        level = p.Level;
        currentHP = p.CurrentHp;
        maxHP = p.MaxHp;
        exp = p.Exp;

        moves = new List<string>();
        foreach (var m in p.Moves)
            moves.Add(m.Base.MoveName);

        attack = p.Attack;
        defense = p.Defense;
        spAttack = p.SpAttack;
        spDefense = p.SpDefense;
        speed = p.Speed;
    }
}
