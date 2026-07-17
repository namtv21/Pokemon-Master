using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

public class SaveLoadSystem : MonoBehaviour
{
    private const float SaveToastHoldTime = 1.6f;
    private const float LoadFadeDuration = 0.2f;
    private const int MaxLoadApplyFrames = 120;

    [SerializeField] private PlayerParty playerParty;
    [SerializeField] private StorageSystem storageSystem;
    [SerializeField] private Inventory inventory;

    // Dữ liệu tạm để áp sau khi scene load
    public static SaveData pendingLoadData;
    private static bool pendingFadeRevealAfterLoad;
    private static List<Pokemon> deferredStoragePokemons;
    private static bool isApplyingPendingLoad;

    public static bool IsLoadInProgress => pendingLoadData != null || isApplyingPendingLoad;
    public static event Action<bool> OnPendingLoadFinished;

    private sealed class ResolvedItemStack
    {
        public ItemBase Item;
        public int Count;
        public int StoredExp;
    }

    // Runtime set of triggered one-shot IDs (persists across scene loads in memory)
    private static HashSet<string> runtimeTriggeredIds;
    private static HashSet<string> runtimeCapturedOverworldPokemonIds;
    private static HashSet<string> runtimeBadgeIds;
    private static Dictionary<string, bool> runtimeNpcBattleStates;
    private static Dictionary<string, NPCStateSaveData> runtimeNpcTransformStates;

    public static void RegisterRuntimeTriggered(string triggerId)
    {
        if (string.IsNullOrWhiteSpace(triggerId)) return;
        runtimeTriggeredIds ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        runtimeTriggeredIds.Add(triggerId);
    }

    public static bool IsRuntimeTriggered(string triggerId)
    {
        if (string.IsNullOrWhiteSpace(triggerId)) return false;
        return runtimeTriggeredIds != null && runtimeTriggeredIds.Contains(triggerId);
    }

    public static void RegisterRuntimeCapturedOverworldPokemon(string encounterId)
    {
        if (string.IsNullOrWhiteSpace(encounterId)) return;
        if (runtimeCapturedOverworldPokemonIds == null)
            runtimeCapturedOverworldPokemonIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        runtimeCapturedOverworldPokemonIds.Add(encounterId);
    }

    public static bool IsRuntimeCapturedOverworldPokemon(string encounterId)
    {
        if (string.IsNullOrWhiteSpace(encounterId)) return false;
        return runtimeCapturedOverworldPokemonIds != null && runtimeCapturedOverworldPokemonIds.Contains(encounterId);
    }

    public static bool RegisterRuntimeBadge(string badgeId)
    {
        if (string.IsNullOrWhiteSpace(badgeId))
            return false;

        runtimeBadgeIds ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return runtimeBadgeIds.Add(badgeId.Trim());
    }

    public static bool HasRuntimeBadge(string badgeId)
    {
        return !string.IsNullOrWhiteSpace(badgeId) &&
               runtimeBadgeIds != null &&
               runtimeBadgeIds.Contains(badgeId.Trim());
    }

    public static string BuildNpcStateKey(string sceneName, string npcId, Vector3 originPosition)
    {
        if (string.IsNullOrWhiteSpace(npcId))
            return string.Empty;

        return $"{sceneName}|{npcId}|{originPosition.x:F3}|{originPosition.y:F3}|{originPosition.z:F3}";
    }

    public static void RegisterRuntimeNpcBattleState(string stateKey, bool canBattle)
    {
        if (string.IsNullOrWhiteSpace(stateKey))
            return;

        if (runtimeNpcBattleStates == null)
            runtimeNpcBattleStates = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        runtimeNpcBattleStates[stateKey] = canBattle;
    }

    public static bool TryGetRuntimeNpcBattleState(string stateKey, out bool canBattle)
    {
        canBattle = false;
        return !string.IsNullOrWhiteSpace(stateKey) &&
               runtimeNpcBattleStates != null &&
               runtimeNpcBattleStates.TryGetValue(stateKey, out canBattle);
    }

    public static void RegisterRuntimeNpcTransformState(string stateKey, string npcId, string sceneName, Vector3 position, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(stateKey) || string.IsNullOrWhiteSpace(npcId))
            return;

        if (runtimeNpcTransformStates == null)
            runtimeNpcTransformStates = new Dictionary<string, NPCStateSaveData>(StringComparer.OrdinalIgnoreCase);

        runtimeNpcTransformStates[stateKey] = new NPCStateSaveData
        {
            stateKey = stateKey,
            npcId = npcId,
            sceneName = sceneName,
            posX = position.x,
            posY = position.y,
            posZ = position.z,
            isActive = isActive
        };
    }

    public static bool TryGetRuntimeNpcTransformState(string stateKey, out NPCStateSaveData state)
    {
        state = null;
        return !string.IsNullOrWhiteSpace(stateKey) &&
               runtimeNpcTransformStates != null &&
               runtimeNpcTransformStates.TryGetValue(stateKey, out state);
    }

    public static bool HasPendingLoadData()
    {
        return pendingLoadData != null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetPendingLoadData()
    {
        pendingLoadData = null;
        pendingFadeRevealAfterLoad = false;
        deferredStoragePokemons = null;
        isApplyingPendingLoad = false;
        OnPendingLoadFinished = null;
        runtimeTriggeredIds = new HashSet<string>();
        runtimeCapturedOverworldPokemonIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        runtimeBadgeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        runtimeNpcBattleStates = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        runtimeNpcTransformStates = new Dictionary<string, NPCStateSaveData>(StringComparer.OrdinalIgnoreCase);
    }

    public static string GetSavePath(string slotName)
    {
        return Path.Combine(Application.persistentDataPath, NormalizeSlotName(slotName) + ".json");
    }

    public static string GetExistingSavePath(string slotName)
    {
        string preferredPath = GetSavePath(slotName);
        if (File.Exists(preferredPath))
            return preferredPath;

        string legacyPath = GetLegacySavePath(slotName);
        return File.Exists(legacyPath) ? legacyPath : preferredPath;
    }

    public static bool TryReadSaveData(string slotName, out SaveData data, out string path)
    {
        data = null;
        path = GetExistingSavePath(slotName);
        if (!File.Exists(path))
            return false;

        try
        {
            data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
            if (data == null)
            {
                Debug.LogError($"[SaveLoad] Save file is invalid: {path}");
                return false;
            }

            if (data.schemaVersion > SaveData.CurrentSchemaVersion)
            {
                Debug.LogError($"[SaveLoad] Save schema {data.schemaVersion} is newer than supported schema {SaveData.CurrentSchemaVersion}.");
                data = null;
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveLoad] Failed to read save file '{path}': {ex.Message}");
            data = null;
            return false;
        }
    }

    private static string GetLegacySavePath(string slotName)
    {
        string executableFolder = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        return Path.Combine(executableFolder, NormalizeSlotName(slotName) + ".json");
    }

    private static string NormalizeSlotName(string slotName)
    {
        if (string.IsNullOrWhiteSpace(slotName))
            throw new ArgumentException("Save slot name cannot be empty.", nameof(slotName));

        string normalized = slotName.Trim();
        if (normalized.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new ArgumentException($"Save slot name '{slotName}' contains invalid characters.", nameof(slotName));

        return normalized;
    }

    // ---------------- SAVE ----------------
    public void Save(string slotName)
    {
        if (playerParty == null) playerParty = FindObjectOfType<PlayerParty>();
        if (storageSystem == null) storageSystem = FindObjectOfType<StorageSystem>();
        if (inventory == null) inventory = FindObjectOfType<Inventory>();
        var player = FindObjectOfType<PlayerController>();

        var data = new SaveData
        {
            schemaVersion = SaveData.CurrentSchemaVersion,
            gameVersion = Application.version,
            partyPokemons = new List<PokemonData>(),
            storagePokemons = new List<PokemonData>(),
            money = inventory != null ? inventory.Money : 0,
            sceneName = SceneManager.GetActiveScene().name,
            playerX = player != null ? player.transform.position.x : 0f,
            playerY = player != null ? player.transform.position.y : 0f,
            playerZ = player != null ? player.transform.position.z : 0f,
            storyPrologueDone = StoryFlags.Instance != null && StoryFlags.Instance.PrologueDone,
            storyFirstMainQuestAccepted = StoryFlags.Instance != null && StoryFlags.Instance.FirstMainQuestAccepted,
            storyStarterChosen = StoryFlags.Instance != null && StoryFlags.Instance.StarterChosen,
            storyStarterPokemonId = StoryFlags.Instance != null ? StoryFlags.Instance.StarterPokemonId : string.Empty,
            storyInCave = StoryFlags.Instance != null && StoryFlags.Instance.InCave,
            storyAfterGrassGym = StoryFlags.Instance != null && StoryFlags.Instance.AfterGrassGym,
            storyAfterWaterGym = StoryFlags.Instance != null && StoryFlags.Instance.AfterWaterGym,
            storyAfterFireGym = StoryFlags.Instance != null && StoryFlags.Instance.AfterFireGym,
            storyMeetGreen = StoryFlags.Instance != null && StoryFlags.Instance.MeetGreen,
            storyMeetBlue = StoryFlags.Instance != null && StoryFlags.Instance.MeetBlue,
            storyMeetTeamRocket = StoryFlags.Instance != null && StoryFlags.Instance.MeetTeamRocket,
            storyOutCave = StoryFlags.Instance != null && StoryFlags.Instance.OutCave,
            storyChampion = StoryFlags.Instance != null && StoryFlags.Instance.Champion,
            storyMainSequenceIndex = StoryFlags.Instance != null ? StoryFlags.Instance.MainStorySequenceIndex : 0,
            storyMainStepIndex = StoryFlags.Instance != null ? StoryFlags.Instance.MainStoryStepIndex : 0,
            questSnapshot = QuestManager.Instance != null ? QuestManager.Instance.ExportSaveSnapshot() : null,
            pokedex = PokedexManager.GetOrCreate().ExportData(),
            inventoryItems = new List<ItemStackSaveData>(),
            badgeIds = new List<string>()
        };

        // Save NPC states
        data.npcStates = new List<NPCStateSaveData>();
        var allNpcs = FindObjectsOfType<NPC>(true);
        foreach (var npc in allNpcs)
        {
            if (string.IsNullOrWhiteSpace(npc.NPCId)) continue;
            string stateKey = npc.RuntimeStateKey;
            if (string.IsNullOrWhiteSpace(stateKey)) continue;
            RegisterRuntimeNpcBattleState(stateKey, npc.CanBattle);
            RegisterRuntimeNpcTransformState(stateKey, npc.NPCId, npc.gameObject.scene.name, npc.transform.position, npc.gameObject.activeSelf);
            data.npcStates.Add(new NPCStateSaveData
            {
                stateKey = stateKey,
                npcId = npc.NPCId,
                canBattle = npc.CanBattle,
                sceneName = npc.gameObject.scene.name,
                posX = npc.transform.position.x,
                posY = npc.transform.position.y,
                posZ = npc.transform.position.z,
                isActive = npc.gameObject.activeSelf
            });
        }

        if (runtimeNpcBattleStates != null)
        {
            foreach (var kvp in runtimeNpcBattleStates)
            {
                if (string.IsNullOrWhiteSpace(kvp.Key))
                    continue;

                bool alreadySaved = false;
                for (int i = 0; i < data.npcStates.Count; i++)
                {
                    if (string.Equals(data.npcStates[i].stateKey, kvp.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        data.npcStates[i].canBattle = kvp.Value;
                        alreadySaved = true;
                        break;
                    }
                }

                if (!alreadySaved)
                    data.npcStates.Add(new NPCStateSaveData { stateKey = kvp.Key, canBattle = kvp.Value });
            }
        }

        if (runtimeNpcTransformStates != null)
        {
            foreach (var kvp in runtimeNpcTransformStates)
            {
                if (string.IsNullOrWhiteSpace(kvp.Key) || kvp.Value == null)
                    continue;

                bool alreadySaved = false;
                for (int i = 0; i < data.npcStates.Count; i++)
                {
                    if (string.Equals(data.npcStates[i].stateKey, kvp.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        data.npcStates[i].stateKey = kvp.Value.stateKey;
                        data.npcStates[i].npcId = kvp.Value.npcId;
                        data.npcStates[i].sceneName = kvp.Value.sceneName;
                        data.npcStates[i].posX = kvp.Value.posX;
                        data.npcStates[i].posY = kvp.Value.posY;
                        data.npcStates[i].posZ = kvp.Value.posZ;
                        data.npcStates[i].isActive = kvp.Value.isActive;
                        alreadySaved = true;
                        break;
                    }
                }

                if (!alreadySaved)
                    data.npcStates.Add(new NPCStateSaveData
                    {
                        stateKey = kvp.Value.stateKey,
                        npcId = kvp.Value.npcId,
                        sceneName = kvp.Value.sceneName,
                        posX = kvp.Value.posX,
                        posY = kvp.Value.posY,
                        posZ = kvp.Value.posZ,
                        isActive = kvp.Value.isActive
                    });
            }
        }

        foreach (var p in playerParty.Pokemons)
            data.partyPokemons.Add(new PokemonData(p));

        if (storageSystem != null)
        {
            foreach (var sp in storageSystem.GetStoredPokemons())
                data.storagePokemons.Add(new PokemonData(sp));
        }
        else if (deferredStoragePokemons != null)
        {
            foreach (var sp in deferredStoragePokemons)
                data.storagePokemons.Add(new PokemonData(sp));
        }

        if (inventory != null)
        {
            foreach (var slot in inventory.GetSlots())
            {
                if (slot == null || slot.item == null || slot.count <= 0)
                    continue;

                data.inventoryItems.Add(new ItemStackSaveData
                {
                    itemName = slot.item.itemName,
                    count = slot.count,
                    storedExp = slot.storedExp
                });
            }
        }

        // Save one-shot story triggers that have been triggered (including runtime-registered ones)
        data.triggeredTriggers = new List<string>();
        var allTriggers = FindObjectsOfType<MainStoryTrigger>(true);
        foreach (var t in allTriggers)
        {
            if (t == null) continue;
            if (t.IsOneShot && t.HasTriggered && !string.IsNullOrWhiteSpace(t.TriggerId))
                data.triggeredTriggers.Add(t.TriggerId);
        }

        // Include any runtime-registered triggers (e.g., triggered since last save)
        foreach (var rt in runtimeTriggeredIds)
            if (!data.triggeredTriggers.Contains(rt))
                data.triggeredTriggers.Add(rt);

        data.capturedOverworldPokemonIds = new List<string>();
        foreach (var capturedId in runtimeCapturedOverworldPokemonIds)
        {
            if (!string.IsNullOrWhiteSpace(capturedId) && !data.capturedOverworldPokemonIds.Contains(capturedId))
                data.capturedOverworldPokemonIds.Add(capturedId);
        }

        runtimeBadgeIds ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var badgeId in runtimeBadgeIds)
        {
            if (!string.IsNullOrWhiteSpace(badgeId))
                data.badgeIds.Add(badgeId);
        }

        string path = GetSavePath(slotName);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(path, json);

        Debug.Log($"Game Saved to {path}");
        ToastNotificationManager.Instance?.Show($"Saved to {slotName}.", Color.white, SaveToastHoldTime);
    }

    // ---------------- LOAD (chỉ dùng trong game, không đổi scene) ----------------
    public void Load(string slotName)
    {
        if (!TryReadSaveData(slotName, out SaveData data, out string path))
        {
            Debug.LogWarning($"No save file found at {path}");
            return;
        }

        GameController.Instance?.PrepareForSaveLoad();
        BeginLoadFade();
        pendingLoadData = data;

        if (!string.IsNullOrWhiteSpace(data.sceneName) && !string.Equals(SceneManager.GetActiveScene().name, data.sceneName))
        {
            SceneManager.LoadScene(data.sceneName);
            return;
        }

        StartCoroutine(ApplyLoadedDataWhenReady());
    }

    // ---------------- LOAD FROM MENU (chuyển scene, áp dữ liệu sau) ----------------
    public void LoadFromMenu(string slotName)
    {
        if (!TryReadSaveData(slotName, out SaveData data, out string path))
        {
            Debug.LogWarning($"No save file found at {path}");
            return;
        }

        // Lưu tạm dữ liệu
        GameController.Instance?.PrepareForSaveLoad();
        pendingLoadData = data;

        // The loaded scene needs global systems during Awake/OnEnable. Creating
        // SystemRoot from sceneLoaded is too late for a cold load from Main Menu.
        if (BootstrapLoader.EnsureSystemRoot() == null)
        {
            Debug.LogError("[SaveLoad] Cannot load because SystemRoot could not be initialized.");
            pendingLoadData = null;
            return;
        }

        BeginLoadFade();

        // Chuyển sang scene đã lưu
        if (!string.IsNullOrWhiteSpace(data.sceneName))
            SceneManager.LoadScene(data.sceneName);
        else
        {
            Debug.LogWarning("Save file has no scene name. Pending data will be applied in current scene.");
            StartCoroutine(ApplyLoadedDataWhenReady());
        }

        Debug.Log($"Scene switched to {data.sceneName}, waiting to apply save data...");
    }

    // ---------------- ÁP DỮ LIỆU SAU KHI SCENE LOAD ----------------
    public static void ApplyLoadedData()
    {
        if (pendingLoadData == null || isApplyingPendingLoad)
            return;

        if (TryApplyData(pendingLoadData, out bool retryable))
        {
            FinishPendingLoad(true);
        }
        else if (!retryable)
        {
            FinishPendingLoad(false);
        }
    }

    public static IEnumerator ApplyLoadedDataWhenReady()
    {
        if (isApplyingPendingLoad)
        {
            while (isApplyingPendingLoad)
                yield return null;
            yield break;
        }

        if (pendingLoadData == null)
            yield break;

        isApplyingPendingLoad = true;
        bool applied = false;
        bool retryable = true;
        int attempts = 0;

        while (pendingLoadData != null && attempts < MaxLoadApplyFrames)
        {
            applied = TryApplyData(pendingLoadData, out retryable, logFailure: false);
            if (applied || !retryable)
                break;

            attempts++;
            yield return null;
        }

        if (!applied && retryable && pendingLoadData != null)
            TryApplyData(pendingLoadData, out _, logFailure: true);

        FinishPendingLoad(applied);
    }

    private static void FinishPendingLoad(bool success)
    {
        pendingLoadData = null;
        isApplyingPendingLoad = false;
        CompleteLoadFade();

        if (success)
        {
            Debug.Log("[SaveLoad] Pending save data applied successfully.");
        }
        else
        {
            Debug.LogError("[SaveLoad] Load was cancelled because the save could not be restored completely. Current runtime data was kept.");
            PlayerParty.Instance?.EnsureDefaultPokemonIfEmpty();
            ToastNotificationManager.Instance?.Show("Load failed: save data is incomplete.", Color.red);
        }

        OnPendingLoadFinished?.Invoke(success);
    }

    // ---------------- HÀM ÁP DỮ LIỆU CHUNG ----------------
    private static bool TryApplyData(SaveData data, out bool retryable, bool logFailure = true)
    {
        retryable = false;
        if (data == null)
        {
            if (logFailure)
                Debug.LogError("[SaveLoad] Cannot apply null save data.");
            return false;
        }

        var playerParty = FindObjectOfType<PlayerParty>(true);
        var storageSystem = FindObjectOfType<StorageSystem>(true) ?? StorageSystem.Instance;
        var inventory = FindObjectOfType<Inventory>(true);
        var player = FindObjectOfType<PlayerController>(true);

        bool requiresStorage = data?.storagePokemons != null && data.storagePokemons.Count > 0;
        if (playerParty == null || inventory == null)
        {
            retryable = true;
            if (logFailure)
                Debug.LogWarning("[SaveLoad] Load deferred because scene systems are not ready yet.");
            return false;
        }

        if (PokemonDB.Instance == null || MoveDB.Instance == null)
        {
            retryable = true;
            if (logFailure)
                Debug.LogWarning("[SaveLoad] Load deferred because PokemonDB or MoveDB is not ready yet.");
            return false;
        }

        var loadedParty = new List<Pokemon>();
        foreach (var pd in data.partyPokemons ?? new List<PokemonData>())
        {
            if (!TryRestorePokemon(pd, "party", out var pokemon))
                return false;

            loadedParty.Add(pokemon);
        }

        int expectedPartyCount = data.partyPokemons?.Count ?? 0;
        if (loadedParty.Count != expectedPartyCount)
        {
            Debug.LogError($"[SaveLoad] Party restore was incomplete ({loadedParty.Count}/{expectedPartyCount}).");
            return false;
        }

        var loadedStorage = new List<Pokemon>();
        foreach (var sd in data.storagePokemons ?? new List<PokemonData>())
        {
            if (!TryRestorePokemon(sd, "storage", out var pokemon))
                return false;

            loadedStorage.Add(pokemon);
        }

        int expectedStorageCount = data.storagePokemons?.Count ?? 0;
        if (loadedStorage.Count != expectedStorageCount)
        {
            Debug.LogError($"[SaveLoad] Storage restore was incomplete ({loadedStorage.Count}/{expectedStorageCount}).");
            return false;
        }

        var resolvedItems = new List<ResolvedItemStack>();
        foreach (var itemStack in data.inventoryItems ?? new List<ItemStackSaveData>())
        {
            if (itemStack == null || string.IsNullOrWhiteSpace(itemStack.itemName) || itemStack.count <= 0)
            {
                Debug.LogError("[SaveLoad] Inventory contains an invalid item stack.");
                return false;
            }

            var itemBase = inventory.FindItemByName(itemStack.itemName);
            if (itemBase == null)
            {
                Debug.LogError($"[SaveLoad] Item '{itemStack.itemName}' could not be resolved.");
                return false;
            }

            resolvedItems.Add(new ResolvedItemStack
            {
                Item = itemBase,
                Count = itemStack.count,
                StoredExp = itemStack.storedExp
            });
        }

        // Commit only after every Pokemon and item has been reconstructed successfully.
        playerParty.Pokemons.Clear();
        playerParty.Pokemons.AddRange(loadedParty);

        if (storageSystem != null)
        {
            var storageList = storageSystem.GetStoredPokemons();
            storageList.Clear();
            storageList.AddRange(loadedStorage);
            storageSystem.RefreshUIAfterLoad();
            deferredStoragePokemons = null;
        }
        else
        {
            deferredStoragePokemons = requiresStorage ? loadedStorage : null;
        }

        inventory.SetMoney(data.money);
        inventory.ClearItems();
        foreach (var itemStack in resolvedItems)
        {
            if (itemStack.Item.isExperienceBottle)
            {
                inventory.AddItem(itemStack.Item, 1);
                int bottleExp = Mathf.Max(0, itemStack.StoredExp);
                if (bottleExp > 0)
                    inventory.AddExperienceBottleExp(bottleExp);
            }
            else
            {
                inventory.AddItem(itemStack.Item, itemStack.Count);
            }
        }

        if (player != null)
            player.transform.position = new Vector3(data.playerX, data.playerY, data.playerZ);

        var storyFlags = StoryFlags.GetOrCreate();
        storyFlags.PrologueDone = data.storyPrologueDone;
        storyFlags.FirstMainQuestAccepted = data.storyFirstMainQuestAccepted;
        storyFlags.StarterChosen = data.storyStarterChosen;
        storyFlags.StarterPokemonId = data.storyStarterPokemonId;
        storyFlags.InCave = data.storyInCave;
        storyFlags.AfterGrassGym = data.storyAfterGrassGym;
        storyFlags.AfterWaterGym = data.storyAfterWaterGym;
        storyFlags.AfterFireGym = data.storyAfterFireGym;
        storyFlags.MeetGreen = data.storyMeetGreen;
        storyFlags.MeetBlue = data.storyMeetBlue;
        storyFlags.MeetTeamRocket = data.storyMeetTeamRocket;
        storyFlags.OutCave = data.storyOutCave;
        storyFlags.Champion = data.storyChampion;

        storyFlags.MainStorySequenceIndex = Mathf.Max(0, data.storyMainSequenceIndex);
        storyFlags.MainStoryStepIndex = Mathf.Max(0, data.storyMainStepIndex);

        runtimeBadgeIds ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        runtimeBadgeIds.Clear();
        if (data.badgeIds != null)
        {
            foreach (var badgeId in data.badgeIds)
                RegisterRuntimeBadge(badgeId);
        }

        if (QuestManager.Instance != null && data.questSnapshot != null)
            QuestManager.Instance.ImportSaveSnapshot(data.questSnapshot);

        var pokedex = PokedexManager.GetOrCreate();
        pokedex.ImportData(data.pokedex);
        bool missingPokedex = data.pokedex == null ||
                              ((data.pokedex.seenPokemonIds == null || data.pokedex.seenPokemonIds.Count == 0) &&
                               (data.pokedex.caughtPokemonIds == null || data.pokedex.caughtPokemonIds.Count == 0));

        if (missingPokedex && playerParty != null)
        {
            pokedex.RebuildFromOwnedPokemon(playerParty.Pokemons, storageSystem != null ? storageSystem.GetStoredPokemons() : null);
        }

        // Apply NPC states
        if (runtimeNpcBattleStates == null)
            runtimeNpcBattleStates = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        else
            runtimeNpcBattleStates.Clear();
        if (runtimeNpcTransformStates == null)
            runtimeNpcTransformStates = new Dictionary<string, NPCStateSaveData>(StringComparer.OrdinalIgnoreCase);
        else
            runtimeNpcTransformStates.Clear();

        if (data.npcStates != null)
        {
            var allNpcs = UnityEngine.Object.FindObjectsOfType<NPC>(true);
            var map = new Dictionary<string, NPC>(StringComparer.OrdinalIgnoreCase);
            foreach (var n in allNpcs)
                if (!string.IsNullOrWhiteSpace(n.RuntimeStateKey)) map[n.RuntimeStateKey] = n;

            foreach (var ns in data.npcStates)
            {
                if (ns == null)
                    continue;

                string stateKey = !string.IsNullOrWhiteSpace(ns.stateKey)
                    ? ns.stateKey
                    : BuildNpcStateKey(ns.sceneName, ns.npcId, new Vector3(ns.posX, ns.posY, ns.posZ));
                string npcId = !string.IsNullOrWhiteSpace(ns.npcId)
                    ? ns.npcId
                    : ExtractNpcIdFromStateKey(stateKey);

                if (string.IsNullOrWhiteSpace(stateKey) || string.IsNullOrWhiteSpace(npcId))
                    continue;

                runtimeNpcBattleStates[stateKey] = ns.canBattle;
                runtimeNpcTransformStates[stateKey] = new NPCStateSaveData
                {
                    stateKey = stateKey,
                    npcId = npcId,
                    canBattle = ns.canBattle,
                    sceneName = ns.sceneName,
                    posX = ns.posX,
                    posY = ns.posY,
                    posZ = ns.posZ,
                    isActive = ns.isActive
                };
                if (map.TryGetValue(stateKey, out var npc))
                {
                    npc.CanBattle = ns.canBattle;
                    bool sameScene = string.IsNullOrWhiteSpace(ns.sceneName) ||
                                     string.Equals(ns.sceneName, npc.gameObject.scene.name, StringComparison.OrdinalIgnoreCase);
                    if (sameScene)
                    {
                        npc.transform.position = new Vector3(ns.posX, ns.posY, ns.posZ);
                        if (npc.gameObject.activeSelf != ns.isActive)
                            npc.gameObject.SetActive(ns.isActive);
                    }
                }
            }
        }

        // Loading replaces the runtime trigger snapshot so an older save can roll story state back correctly.
        runtimeTriggeredIds ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        runtimeTriggeredIds.Clear();
        if (data.triggeredTriggers != null)
            foreach (var id in data.triggeredTriggers)
                if (!string.IsNullOrWhiteSpace(id)) runtimeTriggeredIds.Add(id);

        var allTriggers = UnityEngine.Object.FindObjectsOfType<MainStoryTrigger>(true);
        foreach (var trigger in allTriggers)
        {
            if (trigger == null || string.IsNullOrWhiteSpace(trigger.TriggerId))
                continue;

            trigger.ApplyTriggeredState(runtimeTriggeredIds.Contains(trigger.TriggerId));
        }

        if (runtimeCapturedOverworldPokemonIds == null)
            runtimeCapturedOverworldPokemonIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        else
            runtimeCapturedOverworldPokemonIds.Clear();

        if (data.capturedOverworldPokemonIds != null)
        {
            foreach (var id in data.capturedOverworldPokemonIds)
            {
                if (!string.IsNullOrWhiteSpace(id))
                    runtimeCapturedOverworldPokemonIds.Add(id);
            }
        }

        var allOverworldPokemon = UnityEngine.Object.FindObjectsOfType<OverworldPokemon>(true);
        foreach (var overworldPokemon in allOverworldPokemon)
        {
            if (overworldPokemon == null)
                continue;

            bool captured = !string.IsNullOrWhiteSpace(overworldPokemon.EncounterId) &&
                            runtimeCapturedOverworldPokemonIds.Contains(overworldPokemon.EncounterId);
            overworldPokemon.ApplyCapturedState(captured);
        }

        return true;
    }

    private static string ExtractNpcIdFromStateKey(string stateKey)
    {
        if (string.IsNullOrWhiteSpace(stateKey))
            return string.Empty;

        string[] parts = stateKey.Split('|');
        return parts.Length >= 2 ? parts[1] : string.Empty;
    }

    private static bool TryRestorePokemon(PokemonData data, string destination, out Pokemon pokemon)
    {
        pokemon = null;
        if (data == null || (string.IsNullOrWhiteSpace(data.resourceId) && string.IsNullOrWhiteSpace(data.name)))
        {
            Debug.LogError($"[SaveLoad] {destination} contains invalid Pokemon data.");
            return false;
        }

        try
        {
            pokemon = new Pokemon(data);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveLoad] Failed to restore {destination} Pokemon '{data.name}': {ex}");
            return false;
        }

        int expectedMoveCount = 0;
        if (data.moves != null)
        {
            foreach (var moveName in data.moves)
            {
                if (!string.IsNullOrWhiteSpace(moveName))
                    expectedMoveCount++;
            }
        }

        if (pokemon.Moves.Count != expectedMoveCount)
        {
            Debug.LogError($"[SaveLoad] Moves for {destination} Pokemon '{data.name}' were incomplete ({pokemon.Moves.Count}/{expectedMoveCount}).");
            pokemon = null;
            return false;
        }

        return true;
    }

    private static void BeginLoadFade()
    {
        pendingFadeRevealAfterLoad = true;
        var fadeController = GetOrCreateSceneFadeController();
        fadeController?.SetImmediate(1f);
    }

    private static void CompleteLoadFade()
    {
        if (!pendingFadeRevealAfterLoad)
            return;

        pendingFadeRevealAfterLoad = false;
        var fadeController = GetOrCreateSceneFadeController();
        if (fadeController != null)
            fadeController.StartCoroutine(fadeController.Fade(0f, LoadFadeDuration));
    }

    private static SceneFadeController GetOrCreateSceneFadeController()
    {
        if (SceneFadeController.Instance != null)
            return SceneFadeController.Instance;

        var existing = FindObjectOfType<SceneFadeController>(true);
        if (existing != null)
            return existing;

        var go = new GameObject("SceneFadeController");
        DontDestroyOnLoad(go);
        return go.AddComponent<SceneFadeController>();
    }

    public static void ApplyDeferredStorageDataIfAvailable(StorageSystem storageSystem)
    {
        if (storageSystem == null || deferredStoragePokemons == null)
            return;

        var storageList = storageSystem.GetStoredPokemons();
        storageList.Clear();
        storageList.AddRange(deferredStoragePokemons);
        storageSystem.RefreshUIAfterLoad();
        deferredStoragePokemons = null;
    }

    // ---------------- LẤY DANH SÁCH FILE SAVE ----------------
    public List<string> GetAllSaveFiles()
    {
        var saveFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddSaveFilesFromDirectory(Application.persistentDataPath, saveFiles);
        AddSaveFilesFromDirectory(Path.GetFullPath(Path.Combine(Application.dataPath, "..")), saveFiles);
        return new List<string>(saveFiles);
    }

    private static void AddSaveFilesFromDirectory(string directory, HashSet<string> saveFiles)
    {
        if (!Directory.Exists(directory))
            return;

        foreach (var file in Directory.GetFiles(directory, "SaveFile*.json"))
            saveFiles.Add(Path.GetFileNameWithoutExtension(file));
    }
}
