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

    private void Awake()
    {
        Instance = this;
        LoadAllMoves();
    }

    public MoveBase GetMoveByName(string name)
    {
        LoadAllMoves();

        var move = moves.Find(m =>
            string.Equals(NormalizeMoveName(m?.MoveName), NormalizeMoveName(name), StringComparison.OrdinalIgnoreCase) ||
            string.Equals(NormalizeMoveName(m?.name), NormalizeMoveName(name), StringComparison.OrdinalIgnoreCase));

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
