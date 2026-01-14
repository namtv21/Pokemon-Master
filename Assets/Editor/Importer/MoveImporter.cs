using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

public class MoveImporter : EditorWindow
{
    private string jsonFilePath = "Assets/Game/Resources/PokemonsBase/moves.json";
    private string outputFolder = "Assets/Game/Resources/MoveData/";

    [MenuItem("Tools/Import Moves From JSON")]
    public static void ShowWindow()
    {
        GetWindow<MoveImporter>("Move Importer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Move JSON Importer", EditorStyles.boldLabel);

        jsonFilePath = EditorGUILayout.TextField("JSON File Path", jsonFilePath);
        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);

        if (GUILayout.Button("Import Moves"))
        {
            ImportMoves();
        }
    }

    private void ImportMoves()
    {
        if (!File.Exists(jsonFilePath))
        {
            Debug.LogError($"❌ Không tìm thấy file JSON tại: {jsonFilePath}");
            return;
        }

        string json = File.ReadAllText(jsonFilePath);
        var moveDict = JsonConvert.DeserializeObject<Dictionary<string, MoveJson>>(json);

        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        foreach (var kv in moveDict)
        {
            string moveKey = kv.Key;
            MoveJson moveData = kv.Value;

            // Tạo Move asset
            MoveBase moveAsset = ScriptableObject.CreateInstance<MoveBase>();
            moveAsset.LoadFromJson(moveData);

            string assetPath = Path.Combine(outputFolder, $"{moveKey}.asset");
            AssetDatabase.CreateAsset(moveAsset, assetPath);
            Debug.Log($"✅ Tạo Move asset: {moveKey} (Target: {moveAsset.Target})");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("🎉 Hoàn tất import chiêu thức!");
    }
}