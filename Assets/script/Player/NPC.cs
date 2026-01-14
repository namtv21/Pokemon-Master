using System.Collections.Generic;
using UnityEngine;

public enum NPCType
{
    NPC,
    Healer,
    Shopkeeper,
    StorageKeeper
}

public class NPC : MonoBehaviour, Interactable
{
    [Header("Thông tin NPC")]
    public string npcName;
    [TextArea] public string introDialog;
    public TrainerParty Party;
    public NPCType npcType = NPCType.NPC;

    [Header("Shop Items (chỉ dùng cho Shopkeeper)")]
    public List<ItemBase> shopItems = new List<ItemBase>();

    [Header("Hội thoại")]
    [SerializeField] private Dialog dialog;   // gán asset Dialog trong Inspector

    // 👉 Khi người chơi tương tác (nhấn Z)
    public void Interact()
    {
        DialogManager.Instance.OnDialogFinished += AfterDialog;

        if (dialog != null)
            DialogManager.Instance.ShowDialog(dialog);
        else
            DialogManager.Instance.ShowDialog(introDialog);
    }

    // 👉 Sau khi hội thoại xong thì mở menu lựa chọn
    private void AfterDialog()
    {
        DialogManager.Instance.OnDialogFinished -= AfterDialog;
        GameController.Instance.SetState(GameState.NPCInteraction);
        OptionUI.Instance.ShowOptions(this);
    }

    // 👉 Các hành động NPC có thể làm
    public void HealPlayerPokemon()
    {
        GameController.Instance.HealAllPlayerPokemon();

        DialogManager.Instance.OnDialogFinished += () =>
        {
            OptionUI.Instance.ShowOptions(this);
        };
        DialogManager.Instance.ShowDialog("Your Pokémon have been healed!");
    }

    public void OpenShop()
    {
        // Nếu NPC là Shopkeeper thì mở shop với danh sách riêng hoặc fallback về defaultItems
        if (npcType == NPCType.Shopkeeper)
        {
            ShopUI.Instance.Open(shopItems);
        }
        else
        {
            Debug.LogWarning($"{npcName} is not a shopkeeper!");
        }
    }

    public void SendPokemonToStorage(Pokemon pokemon)
    {
        var playerParty = GameController.Instance.PlayerParty;
        var storage = GameController.Instance.StorageSystem;

        if (playerParty.Pokemons.Contains(pokemon) && playerParty.Pokemons.Count > 1)
        {
            playerParty.RemovePokemon(pokemon);
            storage.AddPokemon(pokemon);

            DialogManager.Instance.ClearOnDialogFinished();
            DialogManager.Instance.OnDialogFinished += () => { OptionUI.Instance.ShowOptions(this); };
            DialogManager.Instance.ShowDialog($"{pokemon.Base.Name} was sent to storage!");
        }
        else if (playerParty.Pokemons.Count <= 1)
        {
            DialogManager.Instance.ClearOnDialogFinished();
            DialogManager.Instance.OnDialogFinished += () =>
            {
                OptionUI.Instance.ShowOptions(this);
            };
            DialogManager.Instance.ShowDialog("You cannot send your last Pokémon to storage!");
        }
        else
        {
            DialogManager.Instance.ClearOnDialogFinished();
            DialogManager.Instance.OnDialogFinished += () => { OptionUI.Instance.ShowOptions(this); };
            DialogManager.Instance.ShowDialog($"{pokemon.Base.Name} is not in your party.");
        }

    }

    public string ShowPokemon()
    {
        List<Pokemon> pokemons = Party.GetPokemons();
        if (pokemons == null || pokemons.Count == 0)
            return $"{npcName} has no Pokémon.";

        string info = $"{npcName}'s Pokémon:\n";

        for (int i = 0; i < pokemons.Count; i++)
        {
            var p = pokemons[i];
            info += $"{p.Base.Name} (Lv {p.Level})";

            if (i % 3 != 2 && i != pokemons.Count - 1)
                info += ", ";

            if (i % 3 == 2 || i == pokemons.Count - 1)
                info += "\n";
        }

        return info;
    }

    public void StartBattle()
    {
        GameController.Instance.StartTrainerBattle(this);
    }
}