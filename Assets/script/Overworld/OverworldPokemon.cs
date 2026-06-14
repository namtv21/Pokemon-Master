using UnityEngine;

public class OverworldPokemon : MonoBehaviour, Interactable
{
    [Header("Identity")]
    [SerializeField] private string encounterId;

    [Header("Pokemon")]
    [SerializeField] private PokemonBase pokemonBase;
    [SerializeField] private int level = 5;
    [SerializeField] private bool allowRun = true;

    private bool isCaptured;

    public string EncounterId => GetEffectiveEncounterId();
    public bool IsCaptured => isCaptured;

    private void Start()
    {
        string effectiveEncounterId = GetEffectiveEncounterId();
        bool captured = SaveLoadSystem.IsRuntimeCapturedOverworldPokemon(effectiveEncounterId);

        if (!captured)
        {
            var pending = SaveLoadSystem.pendingLoadData;
            if (pending != null && pending.capturedOverworldPokemonIds != null)
                captured = pending.capturedOverworldPokemonIds.Contains(effectiveEncounterId);
        }

        ApplyCapturedState(captured);
    }

    public void Interact()
    {
        if (isCaptured)
            return;

        if (pokemonBase == null || GameController.Instance == null)
            return;

        GameController.Instance.StartOverworldPokemonBattle(this, allowRun);
    }

    public Pokemon CreateBattlePokemon()
    {
        if (pokemonBase == null)
            return null;

        return new Pokemon(pokemonBase, Mathf.Max(1, level));
    }

    public void HandleBattleFinished(bool captured)
    {
        if (!captured)
            return;

        MarkCaptured();
    }

    public void ApplyCapturedState(bool captured)
    {
        isCaptured = captured;

        if (captured)
            gameObject.SetActive(false);
    }

    private void MarkCaptured()
    {
        if (isCaptured)
            return;

        isCaptured = true;

        SaveLoadSystem.RegisterRuntimeCapturedOverworldPokemon(GetEffectiveEncounterId());

        gameObject.SetActive(false);
    }

    private string GetEffectiveEncounterId()
    {
        if (!string.IsNullOrWhiteSpace(encounterId))
            return encounterId.Trim();

        Vector3 pos = transform.position;
        return $"{gameObject.scene.name}:{gameObject.name}:{pos.x:0.###}:{pos.y:0.###}:{pos.z:0.###}";
    }
}
