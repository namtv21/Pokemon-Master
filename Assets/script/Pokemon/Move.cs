using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Move
{
    public MoveBase Base { get; private set; }
    public int PP { get; private set; }

    public Move(MoveBase moveBase)
    {
        if (moveBase == null)
        {
            Debug.LogError("MoveBase is null when creating Move!");
            Base = null;
            PP = 0;
            return;
        }

        Base = moveBase;
        PP = moveBase.PP;
    }
    public void UseMove()
    {
        PP = Mathf.Max(PP - 1, 0);
    }

    public void ResetPP()
    {
        PP = Base.PP;
    }

}
