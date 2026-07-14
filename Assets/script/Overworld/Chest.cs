using System.Collections;
using UnityEngine;

public class Chest : MonoBehaviour, Interactable
{
    [Header("Reward")]
    [SerializeField] private ItemBase itemReward;
    [SerializeField] private int moneyReward = 0;
    [SerializeField] private PokemonBase pokemonReward;
    [SerializeField] private int pokemonLevel = 5;
    [SerializeField] private bool givesBadge = false;
    [SerializeField] private string badgeId;

    [Header("Visual")]
    [SerializeField] private Sprite Sprite;

    private bool opened = false;
    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = gameObject.AddComponent<SpriteRenderer>();

        // Auto-add collider to block movement
        var collider = GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = false;
        }
    }

    public void Interact()
    {
        if (opened)
        {
            DialogManager.Instance?.ShowDialog("It's empty.");
            return;
        }

        opened = true;

        if (givesBadge && !string.IsNullOrWhiteSpace(badgeId))
        {
            SaveLoadSystem.RegisterRuntimeBadge(badgeId);

            DialogManager.Instance?.ShowDialog($"You found a badge: {badgeId}!");
        }
        else if (itemReward != null)
        {
            Inventory.Instance?.AddItem(itemReward, 1);
            DialogManager.Instance?.ShowDialog($"You found {itemReward.name}!");
        }
        else if (pokemonReward != null)
        {
            var party = PlayerParty.Instance;
            if (party == null)
            {
                DialogManager.Instance?.ShowDialog("You found a Pokemon, but your party is not ready.");
                return;
            }

            bool sentToStorage = party.Pokemons.Count >= 6;
            var rewardPokemon = new Pokemon(pokemonReward, Mathf.Max(1, pokemonLevel));
            party.AddPokemon(rewardPokemon);

            string locationText = sentToStorage ? " It was sent to storage." : string.Empty;
            DialogManager.Instance?.ShowDialog($"You found {pokemonReward.Name}!{locationText}");
        }
        else if (moneyReward > 0)
        {
            Inventory.Instance?.AddMoney(moneyReward);
            DialogManager.Instance?.ShowDialog($"You found {moneyReward} Dong!");
        }
        else
        {
            DialogManager.Instance?.ShowDialog("The chest is empty.");
        }
    }
}
