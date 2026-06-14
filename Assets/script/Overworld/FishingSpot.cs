using System.Collections;
using UnityEngine;

// Attach to a water area (parent with collider). Implements Interactable so player can press Z to fish.
public class FishingSpot : MonoBehaviour, Interactable
{
    [Header("Fishing settings")]
    [SerializeField] private float minWaitSeconds = 1.5f;
    [SerializeField] private float maxWaitSeconds = 4.0f;
    [SerializeField, Range(0f, 1f)] private float catchChance = 0.7f;
    [SerializeField] private EncounterZone encounterZone;

    [Header("Animation")]
    [SerializeField] private string fishingStateName = "Fishing";
    [SerializeField] private string idleStateName = "Idle";

    private bool isFishing = false;

    public void Interact()
    {
        if (isFishing) return;
        StartCoroutine(FishRoutine());
    }

    private IEnumerator FishRoutine()
    {
        isFishing = true;

        // Block player movement by switching state to Dialog (keeps other systems consistent)
        var gc = GameController.Instance;
        if (gc != null) gc.SetState(GameState.Dialog);

        // Notify player
        ToastNotificationManager.Instance?.Show("Bắt đầu câu cá...");

        // Play idle/fishing pose by zeroing Speed so animator holds facing
        var player = PlayerController.Instance;
        if (player != null && player.animator != null)
        {
            var facing = player.GetFacingDirection();
            player.animator.SetFloat("Horizontal", facing.x);
            player.animator.SetFloat("Vertical", facing.y);
            player.animator.SetFloat("Speed", 0f);

            if (!string.IsNullOrWhiteSpace(fishingStateName))
                player.animator.Play(fishingStateName, 0, 0f);
        }

        float wait = Random.Range(minWaitSeconds, maxWaitSeconds);
        yield return new WaitForSeconds(wait);

        bool caught = Random.value <= catchChance;
        if (caught)
        {
            if (encounterZone != null)
            {
                var wildPokemon = encounterZone.GetRandomPokemon();
                if (wildPokemon != null && gc != null)
                {
                    ToastNotificationManager.Instance?.Show("Có cá cắn câu!");
                    gc.StartWildBattle(wildPokemon);
                }
                else
                {
                    ToastNotificationManager.Instance?.Show("Nhưng không câu được gì...");
                }
            }
            else
            {
                ToastNotificationManager.Instance?.Show("Nhưng không câu được gì...");
            }
        }
        else
        {
            ToastNotificationManager.Instance?.Show("Không bắt được gì...");
        }

        bool battleStarted = gc != null && gc.State == GameState.Battle;

        // Restore player control only if fishing ended without entering battle.
        if (player != null && player.animator != null)
        {
            player.animator.SetFloat("Speed", 0f);
            if (!string.IsNullOrWhiteSpace(idleStateName))
                player.animator.Play(idleStateName, 0, 0f);
        }

        if (gc != null && !battleStarted && gc.State == GameState.Dialog)
            gc.SetState(GameState.Overworld);

        isFishing = false;
    }
}
