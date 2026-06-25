using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// Tools > Setup Pokemon Data
/// Tự động gán frontSprite, backSprite (theo tên) và encounterLocations cho tất cả PokemonBase asset.
public static class SetupPokemonData
{
    private const string PokemonDataPath = "Assets/Game/Resources/PokemonData";
    private const string FrontSpritePath = "Assets/Game/Resources/Sprites/Front";
    private const string BackSpritePath  = "Assets/Game/Resources/Sprites/Back";

    // ── Bảng địa điểm gặp ──────────────────────────────────────────
    // key = tên file asset (lowercase, không có .asset)
    // value = danh sách scene name nơi gặp được
    private static readonly Dictionary<string, string[]> EncounterMap = new()
    {
        // Road01
        { "weedle",    new[] { "Road01" } },
        { "kakuna",    new[] { "Road01" } },
        { "spearow",   new[] { "Road01" } },
        { "doduo",     new[] { "Road01" } },
        { "caterpie",  new[] { "Road01" } },
        { "metapod",   new[] { "Road01" } },
        { "pidgey",    new[] { "Road01" } },
        { "rattata",   new[] { "Road01" } },
        { "abra",      new[] { "Road01" } },
        { "drowzee",   new[] { "Road01" } },
        { "farfetchd", new[] { "Road01" } },
        { "eevee",     new[] { "Road01" } },
        { "magikarp",  new[] { "Road01" } },
        { "dratini",   new[] { "Road01", "Road03" } },

        // Road02
        { "poliwag",   new[] { "Road02" } },
        { "squirtle",  new[] { "Road02" } },
        { "vaporeon",  new[] { "Road02" } },
        { "bulbasaur", new[] { "Road02" } },
        { "exeggcute", new[] { "Road02" } },
        { "bellsprout",new[] { "Road02" } },
        { "oddish",    new[] { "Road02" } },
        { "paras",     new[] { "Road02" } },
        { "venonat",   new[] { "Road02" } },
        { "nidoranf",  new[] { "Road02" } },
        { "nidoranm",  new[] { "Road02" } },
        { "tangela",   new[] { "Road02" } },
        { "koffing",   new[] { "Road02" } },
        { "grimer",    new[] { "Road02" } },
        { "ekans",     new[] { "Road02" } },
        { "zubat",     new[] { "Road02" } },

        // WaterTown
        { "horsea",    new[] { "WaterTown" } },
        { "shellder",  new[] { "WaterTown" } },
        { "seel",      new[] { "WaterTown" } },
        { "tentacool", new[] { "WaterTown" } },
        { "goldeen",   new[] { "WaterTown" } },
        { "staryu",    new[] { "WaterTown" } },
        { "krabby",    new[] { "WaterTown" } },

        // Road03
        { "slowpoke",  new[] { "Road03" } },
        { "psyduck",   new[] { "Road03" } },
        { "growlithe", new[] { "Road03" } },
        { "vulpix",    new[] { "Road03" } },
        { "ponyta",    new[] { "Road03" } },
        { "magmar",    new[] { "Road03" } },
        { "meowth",    new[] { "Road03" } },
        { "tauros",    new[] { "Road03" } },
        { "lickitung", new[] { "Road03" } },
        { "flareon",   new[] { "Road03" } },

        // Mountain
        { "magnemite", new[] { "Mountain" } },
        { "voltorb",   new[] { "Mountain" } },
        { "electabuzz",new[] { "Mountain" } },
        { "jolteon",   new[] { "Mountain" } },
        { "hitmonlee", new[] { "Mountain" } },
        { "hitmonchan",new[] { "Mountain" } },
        { "mankey",    new[] { "Mountain" } },
        { "machop",    new[] { "Mountain" } },
        { "kabuto",    new[] { "Mountain" } },
        { "omanyte",   new[] { "Mountain" } },

        // Cave
        { "geodude",   new[] { "Cave" } },
        { "onix",      new[] { "Cave" } },
        { "diglett",   new[] { "Cave" } },
        { "cubone",    new[] { "Cave" } },
        { "sandshrew", new[] { "Cave" } },

        // Road04
        { "clefairy",  new[] { "Road04" } },
        { "jigglypuff",new[] { "Road04" } },
        { "chansey",   new[] { "Road04" } },
        { "gastly",    new[] { "Road04" } },
        { "jynx",      new[] { "Road04" } },
        { "mrmime",    new[] { "Road04" } },

        // Overworld (gặp trực tiếp trên map, không phải trong grass)
        { "articuno",  new[] { "Overworld" } },
        { "moltres",   new[] { "Overworld" } },
        { "zapdos",    new[] { "Overworld" } },
        { "mew",       new[] { "Overworld" } },
        { "mewtwo",    new[] { "Overworld" } },
        { "rhyhorn",   new[] { "Overworld" } },
        { "aerodactyl",new[] { "Overworld" } },
        { "scyther",   new[] { "Overworld" } },
        { "pinsir",    new[] { "Overworld" } },
        { "snorlax",   new[] { "Overworld" } },
        { "kangaskhan",new[] { "Overworld" } },
        { "raichu",    new[] { "Overworld" } },
        { "charmander",new[] { "Overworld" } },
    };

    // ── Entry point ─────────────────────────────────────────────────

    [MenuItem("Tools/Setup Pokemon Data (Sprites + Locations)")]
    public static void Run()
    {
        string[] guids = AssetDatabase.FindAssets("t:PokemonBase", new[] { PokemonDataPath });
        int updated = 0, skippedSprite = 0;

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var pokemon = AssetDatabase.LoadAssetAtPath<PokemonBase>(assetPath);
            if (pokemon == null) continue;

            string assetName = Path.GetFileNameWithoutExtension(assetPath); // e.g. "abra"
            string spriteName = assetName.ToUpper();                        // e.g. "ABRA"

            var so = new SerializedObject(pokemon);

            // ── Gán sprite theo tên ───────────────────────────────
            string frontPath = $"{FrontSpritePath}/{spriteName}.png";
            string backPath  = $"{BackSpritePath}/{spriteName}.png";

            var frontSprite = AssetDatabase.LoadAssetAtPath<Sprite>(frontPath);
            var backSprite  = AssetDatabase.LoadAssetAtPath<Sprite>(backPath);

            if (frontSprite != null)
                so.FindProperty("frontSprite").objectReferenceValue = frontSprite;
            else
            {
                Debug.LogWarning($"[SetupPokemon] Không tìm thấy front sprite: {frontPath}");
                skippedSprite++;
            }

            if (backSprite != null)
                so.FindProperty("backSprite").objectReferenceValue = backSprite;
            else
                Debug.LogWarning($"[SetupPokemon] Không tìm thấy back sprite: {backPath}");

            // ── Gán encounter locations ───────────────────────────
            var locProp = so.FindProperty("encounterLocations");
            if (EncounterMap.TryGetValue(assetName, out string[] locs))
            {
                locProp.arraySize = locs.Length;
                for (int i = 0; i < locs.Length; i++)
                    locProp.GetArrayElementAtIndex(i).stringValue = locs[i];
            }
            else
            {
                // Pokemon chỉ nhận qua tiến hóa — không gán location
                locProp.arraySize = 0;
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(pokemon);
            updated++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Setup Pokemon Data",
            $"Hoàn tất!\n\n" +
            $"Đã cập nhật: {updated} Pokemon\n" +
            $"Thiếu sprite: {skippedSprite} (xem Console)\n\n" +
            $"Pokemon nhận qua tiến hóa sẽ không có encounterLocations.",
            "OK");

        Debug.Log($"[SetupPokemon] Done. Updated={updated}, MissingSprites={skippedSprite}");
    }

    // ── Giảm số thứ tự Pokemon từ 133 trở đi xuống 1 (sau khi xóa Ditto #132) ──

    [MenuItem("Tools/Renumber Pokemon After Ditto (133→132, 134→133, ...)")]
    public static void RenumberAfterDitto()
    {
        if (!EditorUtility.DisplayDialog(
            "Xác nhận renumber",
            "Thao tác này sẽ giảm num của mọi Pokemon có num >= 133 xuống 1.\n\n" +
            "Ví dụ: Eevee 133→132, Vaporeon 134→133, ..., Mew 151→150.\n\n" +
            "Tiếp tục?",
            "OK", "Hủy"))
            return;

        string[] guids = AssetDatabase.FindAssets("t:PokemonBase", new[] { PokemonDataPath });
        int updated = 0;

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var pokemon = AssetDatabase.LoadAssetAtPath<PokemonBase>(assetPath);
            if (pokemon == null) continue;

            var so = new SerializedObject(pokemon);
            var numProp = so.FindProperty("num");
            if (numProp == null) continue;

            if (numProp.intValue >= 133)
            {
                numProp.intValue -= 1;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(pokemon);
                updated++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Renumber Pokemon",
            $"Hoàn tất!\nĐã cập nhật {updated} Pokemon (num 133+ giảm 1).",
            "OK");

        Debug.Log($"[SetupPokemon] Renumber done. Updated={updated} assets.");
    }
}
