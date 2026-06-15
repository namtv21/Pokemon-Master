using UnityEngine;
using UnityEditor;
using TMPro;

public static class FontSetup
{
    private const string OrangeKidPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/Orange kid SDF.asset";
    private const string FallbackPath  = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset";

    [MenuItem("Tools/Fix Font Fallback Chain")]
    public static void FixFontFallbackChain()
    {
        var orangeKid = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OrangeKidPath);
        var fallback  = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackPath);

        if (orangeKid == null) { Debug.LogError("Không tìm thấy Orange kid SDF.asset"); return; }
        if (fallback  == null) { Debug.LogError("Không tìm thấy LiberationSans SDF - Fallback.asset"); return; }

        // Tăng atlas size của Fallback lên 2048x2048 để chứa đủ ký tự tiếng Việt
        if (fallback.atlasWidth < 2048 || fallback.atlasHeight < 2048)
        {
            var so = new SerializedObject(fallback);
            so.FindProperty("m_AtlasWidth").intValue  = 2048;
            so.FindProperty("m_AtlasHeight").intValue = 2048;
            so.ApplyModifiedProperties();
            fallback.ClearFontAssetData(setAtlasSizeToZero: false);
            EditorUtility.SetDirty(fallback);
            Debug.Log("[FontSetup] Đã tăng Fallback atlas lên 2048x2048 và clear data để regenerate.");
        }

        // Set fallback chain cho Orange kid: dùng LiberationSans SDF - Fallback
        if (orangeKid.fallbackFontAssetTable == null)
            orangeKid.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();

        if (!orangeKid.fallbackFontAssetTable.Contains(fallback))
        {
            orangeKid.fallbackFontAssetTable.Clear();
            orangeKid.fallbackFontAssetTable.Add(fallback);
            EditorUtility.SetDirty(orangeKid);
            Debug.Log("[FontSetup] Đã thêm LiberationSans SDF - Fallback vào Orange kid SDF fallback chain.");
        }
        else
        {
            Debug.Log("[FontSetup] Fallback chain đã đúng rồi.");
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[FontSetup] Hoàn tất. Fallback: Orange kid SDF → LiberationSans SDF - Fallback (2048x2048 Dynamic).");
    }
}
