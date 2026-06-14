using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

public class SaveLoadSystem : MonoBehaviour
{
    private const float SaveToastHoldTime = 1.6f;
    private const float LoadFadeDuration = 0.2f;

    [SerializeField] private PlayerParty playerParty;
    [SerializeField] private StorageSystem storageSystem;
    [SerializeField] private Inventory inventory;

    // Dữ liệu tạm để áp sau khi scene load
    public static SaveData pendingLoadData;
    private static bool pendingFadeRevealAfterLoad;
    private static List<Pokemon> deferredStoragePokemons;

    // Runtime set of triggered one-shot IDs (persists across scene loads in memory)
    private static HashSet<string> runtimeTriggeredIds;
    private static HashSet<string> runtimeCapturedOverworldPokemonIds;
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
        runtimeTriggeredIds = new HashSet<string>();
        runtimeCapturedOverworldPokemonIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        runtimeNpcBattleStates = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        runtimeNpcTransformStates = new Dictionary<string, NPCStateSaveData>(StringComparer.OrdinalIgnoreCase);
    }

    private string GetSavePath(string slotName)
    {
        string exeFolder = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        return Path.Combine(exeFolder, slotName + ".json");
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
            storyMainSequenceIndex = StoryFlags.Instance != null ? StoryFlags.Instance.MainStorySequenceIndex : 0,
            storyMainStepIndex = StoryFlags.Instance != null ? StoryFlags.Instance.MainStoryStepIndex : 0,
            questSnapshot = QuestManager.Instance != null ? QuestManager.Instance.ExportSaveSnapshot() : null,
            pokedex = PokedexManager.GetOrCreate().ExportData(),
            inventoryItems = new List<ItemStackSaveData>()
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

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(GetSavePath(slotName), json);

        Debug.Log($"Game Saved to {GetSavePath(slotName)}");
        ToastNotificationManager.Instance?.Show($"Saved to {slotName}.", Color.white, SaveToastHoldTime);
    }

    // ---------------- LOAD (chỉ dùng trong game, không đổi scene) ----------------
    public void Load(string slotName)
    {
        string path = GetSavePath(slotName);
        if (!File.Exists(path))
        {
            Debug.LogWarning($"No save file found at {path}");
            return;
        }

        string json = File.ReadAllText(path);
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        BeginLoadFade();

        if (!string.IsNullOrWhiteSpace(data.sceneName) && !string.Equals(SceneManager.GetActiveScene().name, data.sceneName))
        {
            pendingLoadData = data;
            SceneManager.LoadScene(data.sceneName);
            return;
        }

        if (TryApplyData(data))
        {
            CompleteLoadFade();
            Debug.Log($"Game Loaded from {path}");
        }
        else
            Debug.LogWarning("[SaveLoad] Could not apply save data immediately.");
    }

    // ---------------- LOAD FROM MENU (chuyển scene, áp dữ liệu sau) ----------------
    public void LoadFromMenu(string slotName)
    {
        string path = GetSavePath(slotName);
        if (!File.Exists(path))
        {
            Debug.LogWarning($"No save file found at {path}");
            return;
        }

        string json = File.ReadAllText(path);
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        // Lưu tạm dữ liệu
        pendingLoadData = data;
        BeginLoadFade();

        // Chuyển sang scene đã lưu
        if (!string.IsNullOrWhiteSpace(data.sceneName))
            SceneManager.LoadScene(data.sceneName);
        else
        {
            Debug.LogWarning("Save file has no scene name. Pending data will be applied in current scene.");
            if (TryApplyData(pendingLoadData))
            {
                pendingLoadData = null;
                CompleteLoadFade();
            }
        }

        Debug.Log($"Scene switched to {data.sceneName}, waiting to apply save data...");
    }

    // ---------------- ÁP DỮ LIỆU SAU KHI SCENE LOAD ----------------
    public static void ApplyLoadedData()
    {
        if (pendingLoadData == null) return;

        if (TryApplyData(pendingLoadData))
        {
            pendingLoadData = null;
            CompleteLoadFade();
        }
    }

    // ---------------- HÀM ÁP DỮ LIỆU CHUNG ----------------
    private static bool TryApplyData(SaveData data)
    {
        var playerParty = FindObjectOfType<PlayerParty>(true);
        var storageSystem = FindObjectOfType<StorageSystem>(true) ?? StorageSystem.Instance;
        var inventory = FindObjectOfType<Inventory>(true);
        var player = FindObjectOfType<PlayerController>(true);

        bool requiresStorage = data?.storagePokemons != null && data.storagePokemons.Count > 0;
        if (playerParty == null || inventory == null)
        {
            Debug.LogWarning("[SaveLoad] Load deferred because scene systems are not ready yet.");
            return false;
        }

        if (PokemonDB.Instance == null || MoveDB.Instance == null)
        {
            Debug.LogWarning("[SaveLoad] Load deferred because PokemonDB or MoveDB is not ready yet.");
            return false;
        }

        var loadedParty = new List<Pokemon>();
        foreach (var pd in data.partyPokemons ?? new List<PokemonData>())
        {
            if (pd == null || string.IsNullOrWhiteSpace(pd.name))
                continue;

            try
            {
                loadedParty.Add(new Pokemon(pd));
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SaveLoad] Failed to restore party Pokemon '{pd.name}': {ex}");
            }
        }

        int expectedPartyCount = data?.partyPokemons?.Count ?? 0;
        if (expectedPartyCount > 0 && loadedParty.Count == 0)
        {
            Debug.LogWarning("[SaveLoad] Load deferred because party Pokemon could not be reconstructed yet.");
            return false;
        }

        var loadedStorage = new List<Pokemon>();
        foreach (var sd in data.storagePokemons ?? new List<PokemonData>())
        {
            if (sd == null || string.IsNullOrWhiteSpace(sd.name))
                continue;

            try
            {
                loadedStorage.Add(new Pokemon(sd));
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SaveLoad] Failed to restore storage Pokemon '{sd.name}': {ex}");
            }
        }

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
        else if (requiresStorage)
        {
            deferredStoragePokemons = loadedStorage;
        }

        if (inventory != null)
        {
            inventory.SetMoney(data.money);
            inventory.ClearItems();
            foreach (var itemStack in data.inventoryItems ?? new List<ItemStackSaveData>())
            {
                if (itemStack == null || string.IsNullOrWhiteSpace(itemStack.itemName) || itemStack.count <= 0)
                    continue;

                var itemBase = inventory.FindItemByName(itemStack.itemName);
                if (itemBase != null)
                {
                    if (itemBase.isExperienceBottle)
                    {
                        inventory.AddItem(itemBase, 1);
                        var bottleExp = Mathf.Max(0, itemStack.storedExp);
                        if (bottleExp > 0)
                            inventory.AddExperienceBottleExp(bottleExp);
                    }
                    else
                    {
                        inventory.AddItem(itemBase, itemStack.count);
                    }
                }
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

        storyFlags.MainStorySequenceIndex = Mathf.Max(0, data.storyMainSequenceIndex);
        storyFlags.MainStoryStepIndex = Mathf.Max(0, data.storyMainStepIndex);

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
        if (runtimeNpcTransformStates == null)
            runtimeNpcTransformStates = new Dictionary<string, NPCStateSaveData>(StringComparer.OrdinalIgnoreCase);

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

                if (string.IsNullOrWhiteSpace(stateKey) || string.IsNullOrWhiteSpace(ns.npcId))
                    continue;

                runtimeNpcBattleStates[stateKey] = ns.canBattle;
                runtimeNpcTransformStates[stateKey] = new NPCStateSaveData
                {
                    stateKey = stateKey,
                    npcId = ns.npcId,
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

        // Apply saved triggered one-shot story triggers: ensure visuals hidden immediately
        var triggeredToApply = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (data.triggeredTriggers != null)
            foreach (var id in data.triggeredTriggers)
                if (!string.IsNullOrWhiteSpace(id)) triggeredToApply.Add(id);

        // Merge runtime-registered triggers (they should also hide visuals)
        foreach (var rt in runtimeTriggeredIds)
            if (!string.IsNullOrWhiteSpace(rt)) triggeredToApply.Add(rt);

        if (triggeredToApply.Count > 0)
        {
            var allTriggers = UnityEngine.Object.FindObjectsOfType<MainStoryTrigger>(true);
            var map = new Dictionary<string, MainStoryTrigger>(StringComparer.OrdinalIgnoreCase);
            foreach (var tr in allTriggers)
                if (!string.IsNullOrWhiteSpace(tr.TriggerId)) map[tr.TriggerId] = tr;

            foreach (var id in triggeredToApply)
            {
                if (map.TryGetValue(id, out var trg))
                {
                    trg.ApplyTriggeredState(true);
                }
            }
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
        string exeFolder = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        var files = Directory.GetFiles(exeFolder, "SaveFile*.json");
        List<string> saveFiles = new List<string>();

        foreach (var f in files)
            saveFiles.Add(Path.GetFileNameWithoutExtension(f));

        return saveFiles;
    }
}
