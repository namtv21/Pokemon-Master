using System.Collections;
using UnityEngine;

public class OverworldPokemon : MonoBehaviour, Interactable
{
    [Header("Identity")]
    [SerializeField] private string encounterId;

    [Header("Pokemon")]
    [SerializeField] private PokemonBase pokemonBase;
    [SerializeField] private int level = 5;
    [SerializeField] private bool allowRun = true;

    [Header("Idle Bob")]
    [SerializeField] private bool idleBob = true;
    [SerializeField] private float bobAmplitude = 0.05f;
    [SerializeField] private float bobPeriod = 1.4f;

    private bool isCaptured;
    private string cachedEncounterId;   // cache TRƯỚC khi nhún — id suy từ vị trí, không được lệch
    private Vector3 basePosition;
    private Coroutine bobRoutine;

    public string EncounterId => GetEffectiveEncounterId();
    public bool IsCaptured => isCaptured;

    private void Awake()
    {
        cachedEncounterId = ComputeEncounterId();
        basePosition = transform.position;
    }

    private void OnEnable()
    {
        if (idleBob)
            bobRoutine = StartCoroutine(BobRoutine());
    }

    private void OnDisable()
    {
        if (bobRoutine != null)
        {
            StopCoroutine(bobRoutine);
            bobRoutine = null;
        }
        transform.position = basePosition;
    }

    // Nhún sin nhẹ, lệch pha ngẫu nhiên để các con không nhún đồng loạt.
    private IEnumerator BobRoutine()
    {
        float phase = Random.value * Mathf.PI * 2f;
        while (true)
        {
            float y = Mathf.Sin(Time.time * Mathf.PI * 2f / Mathf.Max(0.2f, bobPeriod) + phase) * bobAmplitude;
            transform.position = basePosition + new Vector3(0f, y, 0f);
            yield return null;
        }
    }

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
        if (string.IsNullOrEmpty(cachedEncounterId))
            cachedEncounterId = ComputeEncounterId();
        return cachedEncounterId;
    }

    private string ComputeEncounterId()
    {
        if (!string.IsNullOrWhiteSpace(encounterId))
            return encounterId.Trim();

        Vector3 pos = transform.position;
        return $"{gameObject.scene.name}:{gameObject.name}:{pos.x:0.###}:{pos.y:0.###}:{pos.z:0.###}";
    }
}
