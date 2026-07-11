using System.Collections.Generic;
using UnityEngine;

public class GrassTrigger : MonoBehaviour
{
    // Registry tĩnh: tránh FindObjectsOfType sau mỗi bước đi (rất tốn khi scene đông object).
    private static readonly List<GrassTrigger> active = new List<GrassTrigger>();
    public static IReadOnlyList<GrassTrigger> Active => active;

    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private EncounterZone encounterZone;
    [SerializeField] private bool useEncounterZoneBattleRate = true;
    [SerializeField, Range(0f, 100f)] private float battleRatePercent = 10f;
    [Tooltip("TẮT cho vùng encounter không phải cỏ (hang động, mặt nước...): không hiện bụi cỏ che chân, không bắn lá — encounter vẫn hoạt động bình thường.")]
    [SerializeField] private bool isVisualGrass = true;

    private bool playerInGrass = false;

    private void OnEnable()  => active.Add(this);
    private void OnDisable() => active.Remove(this);

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (IsPlayer(collision))
        {
            playerInGrass = true;
            if (isVisualGrass)
                PlayerGrassOverlay.NotifyGrass(true);   // bật bụi cỏ che chân player
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (IsPlayer(collision))
        {
            playerInGrass = false;
            if (isVisualGrass)
                PlayerGrassOverlay.NotifyGrass(false);
        }
    }

    public void TryEncounter()
    {
        if (!playerInGrass)
            return;

        // Rẽ cỏ: lá bắn nhẹ dưới chân mỗi bước — chỉ với vùng thật sự là cỏ
        if (isVisualGrass)
        {
            var player = PlayerController.Instance;
            if (player != null)
                GrassLeafFx.Spawn(player.transform.position + Vector3.down * 0.25f);
        }

        float resolvedRate = ResolveBattleRatePercent();
        if (Random.Range(0f, 100f) > resolvedRate)
            return;

        var wildPokemon = encounterZone != null ? encounterZone.GetRandomPokemon() : null;
        if (wildPokemon == null)
            return;

        GameController.Instance.StartWildBattle(wildPokemon);
    }

    private bool IsPlayer(Collider2D collider)
    {
        return ((1 << collider.gameObject.layer) & playerLayer) != 0;
    }

    private float ResolveBattleRatePercent()
    {
        if (useEncounterZoneBattleRate && encounterZone != null)
            return encounterZone.BattleRatePercent;

        return Mathf.Clamp(battleRatePercent, 0f, 100f);
    }
}
