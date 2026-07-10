using System;
using System.Collections.Generic;

[Serializable]
public class PokemonData
{
    public string name;
    public string resourceId;
    public int level;
    public int currentHP;
    public int maxHP;
    public int exp;
    public List<string> moves;
    public List<int> movePPs;

    public int battleParticipationCount;
    public int friendshipLevel;
    public int personality = -1;   // -1 = chưa gán (save cũ) → sẽ gán ngẫu nhiên khi load

    public int attack;
    public int defense;
    public int spAttack;
    public int spDefense;
    public int speed;

    public PokemonData() { }

    public PokemonData(Pokemon p)
    {
        name = p.Base.Name;
        resourceId = p.Base.name;
        level = p.Level;
        currentHP = p.CurrentHp;
        maxHP = p.MaxHp;
        exp = p.Exp;

        moves = new List<string>();
        movePPs = new List<int>();
        foreach (var m in p.Moves)
        {
            moves.Add(m.Base.MoveName);
            movePPs.Add(m.PP);
        }

        attack = p.Attack;
        defense = p.Defense;
        spAttack = p.SpAttack;
        spDefense = p.SpDefense;
        speed = p.Speed;

        battleParticipationCount = p.BattleParticipationCount;
        friendshipLevel = p.FriendshipLevel;
        personality = (int)p.Personality;
    }
}
