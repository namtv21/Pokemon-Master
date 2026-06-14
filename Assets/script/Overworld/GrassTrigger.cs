using UnityEngine;

public class GrassTrigger : MonoBehaviour
{
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private EncounterZone encounterZone;
    [SerializeField] private bool useEncounterZoneBattleRate = true;
    [SerializeField, Range(0f, 100f)] private float battleRatePercent = 10f;

    private bool playerInGrass = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (IsPlayer(collision))
        {
            playerInGrass = true;
            Debug.Log("Player entered grass area.");
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (IsPlayer(collision))
            playerInGrass = false;
    }

    public void TryEncounter()
    {
        if (!playerInGrass)
            return;

        float resolvedRate = ResolveBattleRatePercent();
        if (Random.Range(0f, 100f) > resolvedRate)
            return;

        var wildPokemon = encounterZone != null ? encounterZone.GetRandomPokemon() : null;
        if (wildPokemon == null)
            return;

        Debug.Log($"Wild Pokemon appeared! Rate={resolvedRate:0.##}%");
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
