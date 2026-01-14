using UnityEngine;
using System.Collections.Generic;
using System;

public class MoveDB : MonoBehaviour
{
    public static MoveDB Instance { get; private set; }
    private List<MoveBase> moves;

    void Awake()
    {
        Instance = this;
        // Tự động load tất cả MoveBase trong Resources/MoveData
        moves = new List<MoveBase>(Resources.LoadAll<MoveBase>("MoveData"));
        //Debug.Log($"MoveDB loaded {moves.Count} moves");
    }

    public MoveBase GetMoveByName(string name)
    {
        var move = moves.Find(m =>
    string.Equals(
        m.name.Replace(" ", "").ToLower(),
        name.Replace(" ", "").ToLower(),
        StringComparison.OrdinalIgnoreCase));

        if (move == null)
            Debug.LogWarning($"MoveDB: Không tìm thấy move '{name}'");
        return move;
    }
}