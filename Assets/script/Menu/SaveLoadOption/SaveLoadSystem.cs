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
            playerZ = player != null ? player.transform.position.z : 0f
        };

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
        SceneManager.LoadScene(data.sceneName);

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
        
        SceneManager.LoadScene(data.sceneName);
        if (playerParty != null)
        {
            playerParty.Pokemons.Clear();
            foreach (var pd in data.partyPokemons)
                playerParty.Pokemons.Add(new Pokemon(pd));
        }

        if (storageSystem != null)
        {
            var storageList = storageSystem.GetStoredPokemons();
            storageList.Clear();
            foreach (var sd in data.storagePokemons)
                storageList.Add(new Pokemon(sd));
            storageSystem.RefreshUIAfterLoad();
        }

        if (inventory != null)
            inventory.SetMoney(data.money);

        if (player != null)
            player.transform.position = new Vector3(data.playerX, data.playerY, data.playerZ);
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
