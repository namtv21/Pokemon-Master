using System.Collections.Generic;
using UnityEngine;

public class StorageSystem : MonoBehaviour
{
    private static StorageSystem _instance;
    public static StorageSystem Instance
    {
        get
        {
            if (_instance == null)
                _instance = Object.FindObjectOfType<StorageSystem>(true);
            return _instance;
        }
    }

    [SerializeField] private Transform storagePanelParent;
    [SerializeField] private StorageSlotUI storageSlotPrefab;
    [SerializeField] private PokemonInfoUI infoUI;

    public List<Pokemon> storedPokemons = new List<Pokemon>();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            DuplicateSystemRootUtility.DestroyDuplicate(this, _instance);
            return;
        }

        _instance = this;

        SaveLoadSystem.ApplyDeferredStorageDataIfAvailable(this);
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }
    private List<StorageSlotUI> slotUIs = new List<StorageSlotUI>();
    private int selectedIndex = 0;
    private bool isReplacing;
    private Pokemon pendingStoredPokemon;

    public void OpenStorage()
    {
        if (storagePanelParent == null || storageSlotPrefab == null || infoUI == null)
        {
            Debug.LogWarning("Storage UI references are missing.");
            return;
        }

        gameObject.SetActive(true);
        storagePanelParent.gameObject.SetActive(true);
        UiFx.PopIn(storagePanelParent.gameObject);
        isReplacing = false;
        pendingStoredPokemon = null;
        selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, storedPokemons.Count - 1));
        RefreshUI();
        if (storedPokemons.Count > 0)
        {
            infoUI.gameObject.SetActive(true);
            ShowInfo(selectedIndex);
        }
        else
        {
            infoUI.Hide();
            infoUI.gameObject.SetActive(false);
        }
    }

    public void CloseStorage()
    {
        if (this == null) return;

        isReplacing = false;
        pendingStoredPokemon = null;

        if (storagePanelParent != null)
            storagePanelParent.gameObject.SetActive(false);
        if (infoUI != null)
            infoUI.gameObject.SetActive(false);
        gameObject.SetActive(false);
        infoUI?.Hide();
    }

    private void FinishStorageSession()
    {
        CloseStorage();

        var gc = GameController.Instance;
        if (gc == null)
            return;

        if (gc.State == GameState.Storage)
        {
            gc.SetState(GameState.Overworld);
            return;
        }

        if (gc.State == GameState.Menu && MenuController.Instance != null)
        {
            MenuController.Instance.SetState(MenuState.Main);
            MenuController.Instance.OpenMainMenu();
        }
    }

    public void AddPokemon(Pokemon pokemon)
    {
        storedPokemons.Add(pokemon);
        PokedexManager.GetOrCreate().MarkCaught(pokemon);
        RefreshUI();
        // Chỉ hiện panel info khi màn kho ĐANG mở; tránh bật panel khi gửi vào kho ngầm
        // (vd bắt Pokemon lúc party đã đầy) — lúc đó chỉ cần toast thông báo là đủ.
        bool storageOpen = storagePanelParent != null && storagePanelParent.gameObject.activeSelf;
        if (infoUI != null && storageOpen)
            infoUI.gameObject.SetActive(true);
    }
    public void RemovePokemon(Pokemon pokemon)
    {
        storedPokemons.Remove(pokemon);
        RefreshUI();
        if (storedPokemons.Count == 0 && infoUI != null)
        {
            infoUI.Hide();
            infoUI.gameObject.SetActive(false);
        }
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
        if (storagePanelParent == null || storageSlotPrefab == null) return;

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

        if (infoUI != null && storedPokemons.Count == 0)
            infoUI.Hide();
    }

    public void HandleUpdate()
    {
        // Luôn cho phép bấm X để đóng, kể cả khi box rỗng.
        if (Input.GetKeyDown(KeyCode.X))
        {
            CloseStorage();
            FinishStorageSession();
            return;
        }

        if (isReplacing)
            return;

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
        else if (Input.GetKeyDown(KeyCode.Z))
        {
            var selectedPokemon = storedPokemons[selectedIndex];
            BeginWithdrawOrReplace(selectedPokemon);
        }

    }

    private void ShowNoti(string message, bool warning = false)
    {
        ToastNotificationManager.Instance?.Show(message, warning ? Color.yellow : Color.white);
    }

    private void BeginWithdrawOrReplace(Pokemon pokemon)
    {
        var playerParty = GameController.Instance.PlayerParty;
        var storage = StorageSystem.Instance;

        if (storage.storedPokemons.Contains(pokemon))
        {
            if (playerParty.Pokemons.Count < 6)
            {
                storage.RemovePokemon(pokemon);
                playerParty.AddPokemon(pokemon);
                ShowNoti($"{pokemon.Base.Name} has been retrieved from storage!");
                FinishStorageSession();
            }
            else
            {
                StartReplaceFlow(pokemon);
            }
        }
        else
        {
            ShowNoti($"{pokemon.Base.Name} is not in storage.");
        }
    }

    private void StartReplaceFlow(Pokemon storedPokemon)
    {
        if (storedPokemon == null)
            return;

        var partyMenu = PartyMenuUI.Instance != null ? PartyMenuUI.Instance : FindObjectOfType<PartyMenuUI>(true);
        if (partyMenu == null)
        {
            ShowNoti("Party menu is unavailable.", true);
            return;
        }

        pendingStoredPokemon = storedPokemon;
        isReplacing = true;

        partyMenu.Open(
            GameController.Instance.PlayerParty.Pokemons,
            PartyMenuMode.Selection,
            onSelected: (partyPokemon) =>
            {
                if (partyPokemon == null || pendingStoredPokemon == null)
                    return;

                var playerParty = GameController.Instance.PlayerParty;
                if (!playerParty.Pokemons.Contains(partyPokemon))
                    return;

                playerParty.RemovePokemon(partyPokemon);
                playerParty.AddPokemon(pendingStoredPokemon);

                RemovePokemon(pendingStoredPokemon);
                AddPokemon(partyPokemon);

                ShowNoti($"Moved {pendingStoredPokemon.Base.Name} and stored {partyPokemon.Base.Name}.");

                pendingStoredPokemon = null;
                isReplacing = false;
                partyMenu.Close();
                FinishStorageSession();
            },
            onCancel: () =>
            {
                pendingStoredPokemon = null;
                isReplacing = false;
                partyMenu.Close();
                ShowInfo(selectedIndex);
            },
            promptText: "Swap"
        );
    }
    private void ShowInfo(int index)
    {
        if (infoUI == null) return;
        if (index < 0 || index >= storedPokemons.Count) return;

        // highlight slot
        for (int i = 0; i < slotUIs.Count; i++)
            slotUIs[i].SetHighlight(i == index);

        // show info
        infoUI.Show(storedPokemons[index]);
    }

}
