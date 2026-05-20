using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

public class SaveLoadSystem : MonoBehaviour
{
    [SerializeField] private PlayerParty playerParty;
    [SerializeField] private StorageSystem storageSystem;
    [SerializeField] private Inventory inventory;

    // Dữ liệu tạm để áp sau khi scene load
    public static SaveData pendingLoadData;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetPendingLoadData()
    {
        pendingLoadData = null;
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
            storyMainSequenceIndex = StoryFlags.Instance != null ? StoryFlags.Instance.MainStorySequenceIndex : 0,
            storyMainStepIndex = StoryFlags.Instance != null ? StoryFlags.Instance.MainStoryStepIndex : 0,
            questSnapshot = QuestManager.Instance != null ? QuestManager.Instance.ExportSaveSnapshot() : null,
            pokedex = PokedexManager.GetOrCreate().ExportData()
        };

        // Save NPC states
        data.npcStates = new List<NPCStateSaveData>();
        var allNpcs = FindObjectsOfType<NPC>(true);
        foreach (var npc in allNpcs)
        {
            if (string.IsNullOrWhiteSpace(npc.NPCId)) continue;
            data.npcStates.Add(new NPCStateSaveData { npcId = npc.NPCId, canBattle = npc.CanBattle });
        }

        foreach (var p in playerParty.Pokemons)
            data.partyPokemons.Add(new PokemonData(p));

        foreach (var sp in storageSystem.GetStoredPokemons())
            data.storagePokemons.Add(new PokemonData(sp));

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(GetSavePath(slotName), json);

        Debug.Log($"Game Saved to {GetSavePath(slotName)}");
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

        if (!string.IsNullOrWhiteSpace(data.sceneName) && !string.Equals(SceneManager.GetActiveScene().name, data.sceneName))
        {
            pendingLoadData = data;
            SceneManager.LoadScene(data.sceneName);
            return;
        }

        ApplyData(data);
        Debug.Log($"Game Loaded from {path}");
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

        // Chuyển sang scene đã lưu
        if (!string.IsNullOrWhiteSpace(data.sceneName))
            SceneManager.LoadScene(data.sceneName);
        else
            Debug.LogWarning("Save file has no scene name. Pending data will be applied in current scene.");

        Debug.Log($"Scene switched to {data.sceneName}, waiting to apply save data...");
    }

    // ---------------- ÁP DỮ LIỆU SAU KHI SCENE LOAD ----------------
    public static void ApplyLoadedData()
    {
        if (pendingLoadData == null) return;

        ApplyData(pendingLoadData);
        pendingLoadData = null;
    }

    // ---------------- HÀM ÁP DỮ LIỆU CHUNG ----------------
    private static void ApplyData(SaveData data)
    {
        var playerParty = FindObjectOfType<PlayerParty>();
        var storageSystem = FindObjectOfType<StorageSystem>();
        var inventory = FindObjectOfType<Inventory>();
        var player = FindObjectOfType<PlayerController>();

        if (playerParty != null)
        {
            playerParty.Pokemons.Clear();
            foreach (var pd in data.partyPokemons ?? new List<PokemonData>())
                playerParty.Pokemons.Add(new Pokemon(pd));
        }

        if (storageSystem != null)
        {
            var storageList = storageSystem.GetStoredPokemons();
            storageList.Clear();
            foreach (var sd in data.storagePokemons ?? new List<PokemonData>())
                storageList.Add(new Pokemon(sd));
            storageSystem.RefreshUIAfterLoad();
        }

        if (inventory != null)
            inventory.SetMoney(data.money);

        if (player != null)
            player.transform.position = new Vector3(data.playerX, data.playerY, data.playerZ);

        var storyFlags = StoryFlags.GetOrCreate();
        storyFlags.PrologueDone = data.storyPrologueDone;
        storyFlags.FirstMainQuestAccepted = data.storyFirstMainQuestAccepted;
        storyFlags.StarterChosen = data.storyStarterChosen;
        storyFlags.StarterPokemonId = data.storyStarterPokemonId;

        var savedSequenceIndex = Mathf.Max(0, data.storyMainSequenceIndex);
        var savedStepIndex = Mathf.Max(0, data.storyMainStepIndex);
        var currentSequenceIndex = storyFlags.MainStorySequenceIndex;
        var currentStepIndex = storyFlags.MainStoryStepIndex;

        var shouldApplyStoryProgress = savedSequenceIndex > currentSequenceIndex ||
                                       (savedSequenceIndex == currentSequenceIndex && savedStepIndex >= currentStepIndex);

        if (shouldApplyStoryProgress)
        {
            storyFlags.MainStorySequenceIndex = savedSequenceIndex;
            storyFlags.MainStoryStepIndex = savedStepIndex;
        }
        else
        {
            Debug.Log($"[SaveLoadSystem] Ignoring regressive story progress from save: current seqIdx={currentSequenceIndex}, stepIdx={currentStepIndex}, saved seqIdx={savedSequenceIndex}, stepIdx={savedStepIndex}");
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
        if (data.npcStates != null)
        {
            var allNpcs = Object.FindObjectsOfType<NPC>(true);
            var map = new Dictionary<string, NPC>();
            foreach (var n in allNpcs)
                if (!string.IsNullOrWhiteSpace(n.NPCId)) map[n.NPCId] = n;

            foreach (var ns in data.npcStates)
            {
                if (ns == null || string.IsNullOrWhiteSpace(ns.npcId)) continue;
                if (map.TryGetValue(ns.npcId, out var npc))
                {
                    npc.CanBattle = ns.canBattle;
                }
            }
        }
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
