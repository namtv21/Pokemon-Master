using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

public class LearnsetFolderImporter : EditorWindow
{
    private string learnsetFolder = "Assets/Game/Resources/PokemonsBase/learnsets";
    private string moveAssetFolder = "Assets/Game/Resources/MoveData/";
    private string pokemonAssetFolder = "Assets/Game/Resources/PokemonData/";

    [MenuItem("Tools/Import Learnsets From Folder")]
    public static void ShowWindow()
    {
        GetWindow<LearnsetFolderImporter>("Learnset Folder Importer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Import Learnsets From Folder", EditorStyles.boldLabel);
        learnsetFolder = EditorGUILayout.TextField("Learnset Folder", learnsetFolder);
        moveAssetFolder = EditorGUILayout.TextField("Move Asset Folder", moveAssetFolder);
        pokemonAssetFolder = EditorGUILayout.TextField("Pokemon Asset Folder", pokemonAssetFolder);

        if (GUILayout.Button("Import All Learnsets"))
        {
            ImportAllLearnsets();
        }
    }

    private void ImportAllLearnsets()
    {
        string[] files = Directory.GetFiles(learnsetFolder, "*.json");

        foreach (string file in files)
        {
            string json = File.ReadAllText(file);
            LearnsetFile data = JsonConvert.DeserializeObject<LearnsetFile>(json);
            string pokemonName = data.pokemon;

            string pokemonPath = Path.Combine(pokemonAssetFolder, $"{pokemonName}.asset");
            PokemonBase pokemon = AssetDatabase.LoadAssetAtPath<PokemonBase>(pokemonPath);
            if (pokemon == null)
            {
                Debug.LogWarning($"❌ Không tìm thấy Pokémon asset: {pokemonName}");
                continue;
            }

            Dictionary<string, int> moveLevelDict = new Dictionary<string, int>();
            foreach (var kv in data.learnset)
            {
                string moveName = kv.Key;
                List<string> sources = kv.Value;

                foreach (string source in sources)
                {
                    if (source.Length >= 2 && source[1] == 'L') // ví dụ "8L5"
                    {
                        if (int.TryParse(source.Substring(2), out int level))
                        {
                            if (!moveLevelDict.ContainsKey(moveName) || level < moveLevelDict[moveName])
                            {
                                moveLevelDict[moveName] = level; // lưu level thấp nhất
                            }
                        }
                    }
                }
            }

            List<LearnableMove> learnableMoves = new List<LearnableMove>();

            foreach (var kv in moveLevelDict)
            {
                string moveName = kv.Key;
                int level = kv.Value;

                string movePath = Path.Combine(moveAssetFolder, $"{moveName}.asset");
                MoveBase move = AssetDatabase.LoadAssetAtPath<MoveBase>(movePath);
                if (move != null)
                {
                    learnableMoves.Add(new LearnableMove
                    {
                        move = move,
                        level = level
                    });
                }
                else
                {
                    Debug.LogWarning($"⚠️ Không tìm thấy Move asset: {moveName}");
                }
            }

            // Sắp xếp theo level
            learnableMoves.Sort((a, b) => a.level.CompareTo(b.level));
            // Gán vào Pokémon
            SerializedObject so = new SerializedObject(pokemon);
            SerializedProperty prop = so.FindProperty("learnableMoves");
            so.Update();

            prop.arraySize = learnableMoves.Count;
            for (int i = 0; i < learnableMoves.Count; i++)
            {
                prop.GetArrayElementAtIndex(i).FindPropertyRelative("move").objectReferenceValue = learnableMoves[i].move;
                prop.GetArrayElementAtIndex(i).FindPropertyRelative("level").intValue = learnableMoves[i].level;
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(pokemon);
            Debug.Log($"✅ Gán learnset theo level cho: {pokemonName}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("🎉 Hoàn tất import tất cả learnset!");
    }
}