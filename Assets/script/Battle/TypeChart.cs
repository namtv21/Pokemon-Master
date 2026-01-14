using UnityEngine;

public static class TypeChart
{
    // Bảng hiệu quả: [attacker][defender]
    // Bao gồm cả None ở vị trí đầu tiên (index 0)
    private static readonly float[,] chart = new float[,]
    {
        //             NONE NOR FIR WAT ELE GRA ICE FIG POI GRO FLY PSY BUG ROC GHO DRA DAR STE FAI
        /* None     */ {1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f},
        /* Normal   */ {1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 0.5f, 0.5f, 0f, 1f, 1f, 0.5f, 1f},
        /* Fire     */ {1f, 1f, 0.5f, 0.5f, 1f, 2f, 2f, 1f, 1f, 1f, 1f, 1f, 2f, 0.5f, 1f, 0.5f, 1f, 2f, 1f},
        /* Water    */ {1f, 1f, 2f, 0.5f, 1f, 0.5f, 1f, 1f, 1f, 2f, 1f, 1f, 1f, 2f, 1f, 0.5f, 1f, 1f, 1f},
        /* Electric */ {1f, 1f, 1f, 2f, 0.5f, 0.5f, 1f, 1f, 1f, 0f, 2f, 1f, 1f, 1f, 1f, 0.5f, 1f, 1f, 1f},
        /* Grass    */ {1f, 1f, 0.5f, 2f, 1f, 0.5f, 1f, 1f, 0.5f, 2f, 0.5f, 1f, 0.5f, 2f, 1f, 0.5f, 1f, 0.5f, 1f},
        /* Ice      */ {1f, 1f, 0.5f, 0.5f, 1f, 2f, 0.5f, 1f, 1f, 2f, 2f, 1f, 1f, 1f, 1f, 2f, 1f, 0.5f, 1f},
        /* Fighting */ {1f, 2f, 1f, 1f, 1f, 1f, 2f, 1f, 0.5f, 1f, 0.5f, 0.5f, 0.5f, 2f, 0f, 1f, 2f, 2f, 0.5f},
        /* Poison   */ {1f, 1f, 1f, 1f, 1f, 2f, 1f, 0.5f, 0.5f, 0.5f, 1f, 1f, 1f, 0.5f, 1f, 1f, 1f, 0.5f, 2f},
        /* Ground   */ {1f, 1f, 2f, 1f, 2f, 0.5f, 1f, 1f, 2f, 1f, 0f, 1f, 0.5f, 2f, 1f, 1f, 1f, 2f, 1f},
        /* Flying   */ {1f, 1f, 1f, 1f, 0.5f, 2f, 1f, 2f, 1f, 0f, 1f, 1f, 2f, 0.5f, 1f, 1f, 1f, 0.5f, 1f},
        /* Psychic  */ {1f, 1f, 1f, 1f, 1f, 1f, 1f, 2f, 2f, 1f, 1f, 1f, 0.5f, 1f, 1f, 1f, 0f, 0.5f, 1f},
        /* Bug      */ {1f, 1f, 0.5f, 1f, 1f, 2f, 1f, 0.5f, 0.5f, 0.5f, 0.5f, 1f, 2f, 1f, 0.5f, 1f, 2f, 0.5f, 0.5f},
        /* Rock     */ {1f, 1f, 2f, 1f, 1f, 1f, 2f, 0.5f, 1f, 0.5f, 2f, 1f, 1f, 1f, 1f, 1f, 1f, 0.5f, 1f},
        /* Ghost    */ {1f, 0f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 2f, 1f, 2f, 1f, 1f, 1f, 1f},
        /* Dragon   */ {1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 2f, 1f, 0.5f, 0f},
        /* Dark     */ {1f, 1f, 1f, 1f, 1f, 1f, 1f, 0.5f, 1f, 1f, 1f, 1f, 2f, 1f, 1f, 1f, 0.5f, 1f, 2f},
        /* Steel    */ {1f, 1f, 0.5f, 0.5f, 0.5f, 1f, 2f, 1f, 1f, 1f, 1f, 1f, 1f, 2f, 1f, 1f, 1f, 0.5f, 2f},
        /* Fairy    */ {1f, 1f, 0.5f, 1f, 1f, 1f, 1f, 2f, 0.5f, 1f, 1f, 1f, 1f, 1f, 1f, 2f, 0.5f, 2f, 1f},
    };


    public static float GetEffectiveness(PokemonType attackType, PokemonType defenseType)
    {
        //Debug.Log($"AttackType={attackType}({(int)attackType}), DefenseType={defenseType}({(int)defenseType})");
        if (attackType == PokemonType.None || defenseType == PokemonType.None)
            return 1f;

        int row = (int)attackType;
        int col = (int)defenseType;
        return chart[row, col];
    }

    public static float GetEffectiveness(PokemonType attackType, PokemonType defenseType1, PokemonType defenseType2)
    {
        //Debug.Log($"AttackType={attackType}({(int)attackType}), DefenseType1={defenseType1}({(int)defenseType1}), DefenseType2={defenseType2}({(int)defenseType2})");
        float eff1 = GetEffectiveness(attackType, defenseType1);
        float eff2 = defenseType2 != PokemonType.None ? GetEffectiveness(attackType, defenseType2) : 1f;
        return eff1 * eff2;
    }
}