using System;
using System.Collections.Generic;
using UnityEngine;

public class MoveDB : MonoBehaviour
{
    private static MoveDB instance;

    public static MoveDB Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<MoveDB>(true);
                if (instance == null)
                {
                    var go = new GameObject("RuntimeMoveDB");
                    DontDestroyOnLoad(go);
                    instance = go.AddComponent<MoveDB>();
                }
            }

            return instance;
        }
        private set => instance = value;
    }

    private List<MoveBase> moves;
    // Index tra theo tên đã chuẩn hóa — O(1); quan trọng khi load save resolve từng chiêu theo tên.
    private Dictionary<string, MoveBase> movesByName;

    private void Awake()
    {
        Instance = this;
        LoadAllMoves();
    }

    public MoveBase GetMoveByName(string name)
    {
        LoadAllMoves();

        movesByName.TryGetValue(NormalizeMoveName(name), out var move);
        if (move == null)
            Debug.LogWarning($"MoveDB: Move not found: '{name}'");

        return move;
    }

    public IReadOnlyList<MoveBase> GetAllMoves()
    {
        LoadAllMoves();
        return moves;
    }

    private void LoadAllMoves()
    {
        if (moves != null && moves.Count > 0)
            return;

        moves = new List<MoveBase>(Resources.LoadAll<MoveBase>("MoveData"));

        movesByName = new Dictionary<string, MoveBase>();
        foreach (var m in moves)
        {
            if (m == null) continue;
            string displayKey = NormalizeMoveName(m.MoveName);
            string assetKey   = NormalizeMoveName(m.name);
            if (!string.IsNullOrEmpty(displayKey)) movesByName[displayKey] = m;
            if (!string.IsNullOrEmpty(assetKey))   movesByName[assetKey]   = m;
        }
    }

    private static string NormalizeMoveName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = new System.Text.StringBuilder(value.Length);
        foreach (char c in value)
        {
            if (char.IsLetterOrDigit(c))
                normalized.Append(char.ToLowerInvariant(c));
        }

        return normalized.ToString();
    }
}
