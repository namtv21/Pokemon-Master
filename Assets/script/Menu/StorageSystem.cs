using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class StorageSystem : MonoBehaviour
{
    [SerializeField] private Transform storagePanelParent;
    [SerializeField] private StorageSlotUI storageSlotPrefab;
    [SerializeField] private PokemonInfoUI infoUI;

    public List<Pokemon> storedPokemons = new List<Pokemon>();
    private List<StorageSlotUI> slotUIs = new List<StorageSlotUI>();
    private int selectedIndex = 0;

    public void OpenStorage()
    {
        gameObject.SetActive(true);
        RefreshUI();
        if (storedPokemons.Count > 0)
            ShowInfo(selectedIndex);
    }

    public void CloseStorage()
    {
        gameObject.SetActive(false);
        infoUI.Hide();
    }

    public void AddPokemon(Pokemon pokemon)
    {
        storedPokemons.Add(pokemon);
        RefreshUI();
    }
    public void RemovePokemon(Pokemon pokemon)
    {
        storedPokemons.Remove(pokemon);
        RefreshUI();
    }

    public List<Pokemon> GetStoredPokemons()
    {
        return storedPokemons;
    }

    public void RefreshUIAfterLoad() 
    { 
        RefreshUI(); 
    }

    private void RefreshUI()
    {
        foreach (Transform child in storagePanelParent)
            Destroy(child.gameObject);
        slotUIs.Clear();

        for (int i = 0; i < storedPokemons.Count; i++)
        {
            var slot = Instantiate(storageSlotPrefab, storagePanelParent);
            slot.SetData(storedPokemons[i]);
            slotUIs.Add(slot);
        }
        selectedIndex = 0;
    }

    public void HandleUpdate()
    {
        if (storedPokemons.Count == 0) return;

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            selectedIndex = (selectedIndex + 1) % storedPokemons.Count;
            ShowInfo(selectedIndex);
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            selectedIndex = (selectedIndex - 1 + storedPokemons.Count) % storedPokemons.Count;
            ShowInfo(selectedIndex);
        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            CloseStorage();
        }
        else if (Input.GetKeyDown(KeyCode.Z))
        {
            var selectedPokemon = storedPokemons[selectedIndex];
            RetrievePokemonFromStorage(selectedPokemon);
            // Sau khi rút, cập nhật UI
            RefreshUI();
            if (storedPokemons.Count > 0)
                ShowInfo(selectedIndex % storedPokemons.Count);
            else
                infoUI.Hide();
        }

    }
    public void RetrievePokemonFromStorage(Pokemon pokemon)
    {
        var playerParty = GameController.Instance.PlayerParty;
        var storage = GameController.Instance.StorageSystem;

        if (storage.storedPokemons.Contains(pokemon))
        {
            if (playerParty.Pokemons.Count < 6)
            {
                storage.RemovePokemon(pokemon);
                playerParty.AddPokemon(pokemon);
                
                DialogManager.Instance.ClearOnDialogFinished();
                DialogManager.Instance.OnDialogFinished += () => { GameController.Instance.SetState(GameState.Overworld); };
                DialogManager.Instance.ShowDialog($"{pokemon.Base.Name} was retrieved from storage!");
                CloseStorage();
            }
            else
            {
                DialogManager.Instance.ClearOnDialogFinished();
                DialogManager.Instance.OnDialogFinished += () => { GameController.Instance.SetState(GameState.Overworld); };
                DialogManager.Instance.ShowDialog("Your party is full!");
                CloseStorage();
            }
        }
        else
        {
            DialogManager.Instance.ShowDialog($"{pokemon.Base.Name} is not in storage.");
        }
    }
    private void ShowInfo(int index)
    {
        // highlight slot
        for (int i = 0; i < slotUIs.Count; i++)
            slotUIs[i].SetHighlight(i == index);

        // show info
        infoUI.Show(storedPokemons[index]);
    }

}
