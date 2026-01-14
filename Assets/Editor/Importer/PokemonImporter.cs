using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

public class PokemonImporter : EditorWindow
{
    private string jsonFilePath = "Assets/Game/Resources/PokemonsBase/pokemon.json";
    private string outputFolder = "Assets/Game/Resources/PokemonData/";

    [MenuItem("Tools/Import Pokemon From JSON")]
    public static void ShowWindow()
    {
        GetWindow<PokemonImporter>("Pokemon Importer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Pokemon JSON Importer", EditorStyles.boldLabel);

        jsonFilePath = EditorGUILayout.TextField("JSON File Path", jsonFilePath);
        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);

        if (GUILayout.Button("Import Pokémon"))
        {
            ImportPokemon();
        }
    }

    private void ImportPokemon()
    {
        if (!File.Exists(jsonFilePath))
        {
            Debug.LogError($"Không tìm thấy file JSON tại: {jsonFilePath}");
            return;
        }

        string json = File.ReadAllText(jsonFilePath);
        var pokemonDict = JsonConvert.DeserializeObject<Dictionary<string, PokemonJson>>(json);

        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        foreach (var kv in pokemonDict)
        {
            string key = kv.Key;
            PokemonJson data = kv.Value;

            PokemonBase asset = ScriptableObject.CreateInstance<PokemonBase>();
            asset.LoadFromJson(data);

            string assetPath = Path.Combine(outputFolder, $"{key}.asset");
            AssetDatabase.CreateAsset(asset, assetPath);
            Debug.Log($"✅ Tạo Pokémon asset: {key}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("🎉 Hoàn tất import Pokémon!");
    }
}