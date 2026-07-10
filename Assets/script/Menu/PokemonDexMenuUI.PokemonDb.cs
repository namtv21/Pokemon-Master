using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Phần Pokemon Database của PokemonDexMenuUI (partial class):
// hiển thị danh sách Pokedex, chi tiết loài và dựng runtime UI.
public partial class PokemonDexMenuUI
{
    private void EnsurePokemonDbRuntimeUI()
    {
        if (pokemonDbLines != null && pokemonDbLines.Any(t => t != null))
            return;

        GameObject panel = null;
        if (tabPanels != null && tabPanels.Length > (int)DexTab.PokemonDb)
            panel = tabPanels[(int)DexTab.PokemonDb];
        else if (rootPanel != null)
            panel = rootPanel;
        else
            panel = gameObject;

        if (panel == null)
            return;

        var listRoot = panel.transform.Find("PokemonDbAutoList") as RectTransform;
        if (listRoot == null)
        {
            var listGo = new GameObject("PokemonDbAutoList", typeof(RectTransform), typeof(VerticalLayoutGroup));
            listRoot = listGo.GetComponent<RectTransform>();
            listRoot.SetParent(panel.transform, false);
            listRoot.anchorMin = new Vector2(0.02f, 0.08f);
            listRoot.anchorMax = new Vector2(0.48f, 0.92f);
            listRoot.offsetMin = Vector2.zero;
            listRoot.offsetMax = Vector2.zero;

            var layout = listGo.GetComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 6f;
            layout.padding = new RectOffset(6, 6, 6, 6);
        }

        var infoPanel = panel.transform.Find("PokemonDbAutoInfoPanel") as RectTransform;
        if (infoPanel == null)
        {
            var infoGo = new GameObject("PokemonDbAutoInfoPanel", typeof(RectTransform), typeof(Image));
            infoPanel = infoGo.GetComponent<RectTransform>();
            infoPanel.SetParent(panel.transform, false);
            infoPanel.anchorMin = new Vector2(0.52f, 0.08f);
            infoPanel.anchorMax = new Vector2(0.98f, 0.92f);
            infoPanel.offsetMin = Vector2.zero;
            infoPanel.offsetMax = Vector2.zero;

            var bg = infoGo.GetComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.2f);
        }

        if (pokemonDbDetailText == null)
        {
            var detailTf = infoPanel.Find("InfoText") as RectTransform;
            if (detailTf == null)
            {
                var detailGo = new GameObject("InfoText", typeof(RectTransform));
                detailTf = detailGo.GetComponent<RectTransform>();
                detailTf.SetParent(infoPanel, false);
                detailTf.anchorMin = Vector2.zero;
                detailTf.anchorMax = Vector2.one;
                detailTf.offsetMin = new Vector2(10f, 10f);
                detailTf.offsetMax = new Vector2(-10f, -10f);

                var tmp = detailGo.AddComponent<TextMeshProUGUI>();
                tmp.enableWordWrapping = true;
                tmp.alignment = TextAlignmentOptions.TopLeft;
                tmp.fontSize = 22f;
                tmp.color = normalColor;
                if (TMP_Settings.defaultFontAsset != null)
                    tmp.font = TMP_Settings.defaultFontAsset;
                pokemonDbDetailText = tmp;

                // Leave space for sprite preview above text.
                detailTf.offsetMin = new Vector2(10f, 10f);
                detailTf.offsetMax = new Vector2(-10f, -150f);
            }
            else
            {
                pokemonDbDetailText = detailTf.GetComponent<TextMeshProUGUI>();
                if (pokemonDbDetailText == null)
                    pokemonDbDetailText = detailTf.gameObject.AddComponent<TextMeshProUGUI>();
            }
        }

        if (pokemonDbDetailImage == null)
        {
            var imageTf = infoPanel.Find("InfoImage") as RectTransform;
            if (imageTf == null)
            {
                var imageGo = new GameObject("InfoImage", typeof(RectTransform), typeof(Image));
                imageTf = imageGo.GetComponent<RectTransform>();
                imageTf.SetParent(infoPanel, false);
                imageTf.anchorMin = new Vector2(0.5f, 1f);
                imageTf.anchorMax = new Vector2(0.5f, 1f);
                imageTf.pivot = new Vector2(0.5f, 1f);
                imageTf.anchoredPosition = new Vector2(0f, -10f);
                imageTf.sizeDelta = new Vector2(128f, 128f);
            }

            pokemonDbDetailImage = imageTf.GetComponent<Image>();
            if (pokemonDbDetailImage == null)
                pokemonDbDetailImage = imageTf.gameObject.AddComponent<Image>();

            pokemonDbDetailImage.preserveAspect = true;
        }

        autoPokemonDbTexts.Clear();
        int rowCount = PokemonRowsPerPage;
        for (int i = 0; i < rowCount; i++)
        {
            var rowName = $"Row_{i + 1:00}";
            var rowTf = listRoot.Find(rowName) as RectTransform;
            TextMeshProUGUI rowText;

            if (rowTf == null)
            {
                var rowGo = new GameObject(rowName, typeof(RectTransform));
                rowTf = rowGo.GetComponent<RectTransform>();
                rowTf.SetParent(listRoot, false);
                rowTf.sizeDelta = new Vector2(0f, 28f);

                rowText = rowGo.AddComponent<TextMeshProUGUI>();
                rowText.enableWordWrapping = false;
                rowText.overflowMode = TextOverflowModes.Ellipsis;
                rowText.alignment = TextAlignmentOptions.MidlineLeft;
                rowText.fontSize = 22f;
                rowText.color = normalColor;
                rowText.margin = new Vector4(28f, 0f, 0f, 0f);
                if (TMP_Settings.defaultFontAsset != null)
                    rowText.font = TMP_Settings.defaultFontAsset;
            }
            else
            {
                rowText = rowTf.GetComponent<TextMeshProUGUI>();
                if (rowText == null)
                    rowText = rowTf.gameObject.AddComponent<TextMeshProUGUI>();
                rowText.margin = new Vector4(28f, 0f, 0f, 0f);
            }

            autoPokemonDbTexts.Add(rowText);
        }
    }

    private void RefreshPokemonDb()
    {
        var lines = GetPokemonDbTexts();
        int visibleRows = Mathf.Min(PokemonRowsPerPage, Mathf.Max(1, lines.Count));
        EnsureDbSelectionVisible(visibleRows);

        for (int i = 0; i < lines.Count; i++)
        {
            var text = lines[i];

            if (i >= visibleRows)
            {
                text.text = string.Empty;
                text.color = normalColor;
                continue;
            }

            int dataIndex = dbScrollOffset + i;
            if (dataIndex < 0 || dataIndex >= pokemonDb.Count)
            {
                text.text = string.Empty;
                text.color = normalColor;
                continue;
            }

            var pokemon = pokemonDb[dataIndex];
            bool caught = PokedexManager.GetOrCreate().HasCaught(pokemon.Name);
            text.text = FormatDexEntry(dataIndex, pokemon);
            text.color = BuildRowColor(dataIndex == dbIndex, true);
            EnsureDexIndicator(text, caught);
        }

        if (pokemonDbDetailText == null) return;

        if (pokemonDb.Count == 0)
        {
            pokemonDbDetailText.text = "No Pokemon data found.";
            if (pokemonDbDetailImage != null)
            {
                pokemonDbDetailImage.sprite = null;
                pokemonDbDetailImage.enabled = false;
            }
            return;
        }

        var selected = pokemonDb[Mathf.Clamp(dbIndex, 0, pokemonDb.Count - 1)];
        if (pokemonDbDetailImage != null)
        {
            pokemonDbDetailImage.sprite = selected.FrontSprite;
            pokemonDbDetailImage.enabled = selected.FrontSprite != null;
        }

        string learnsetText = BuildLearnsetPreview(selected);
        string evolutionText = BuildEvolutionText(selected);
        string chainText = pokemonEvolutionPath.TryGetValue(selected, out var path) && !string.IsNullOrWhiteSpace(path)
            ? path
            : selected.Name;
        string locationText = selected.EncounterLocations != null && selected.EncounterLocations.Length > 0
            ? string.Join(", ", selected.EncounterLocations)
            : "Tiến hóa / Không gặp trong tự nhiên";

        pokemonDbDetailText.text =
            $"Name: {selected.Name}\n" +
            $"Type: {selected.Type1}/{selected.Type2}\n" +
            $"HP: {selected.MaxHp}  Atk: {selected.Attack}  Def: {selected.Defense}\n" +
            $"SpA: {selected.SpAttack}  SpD: {selected.SpDefense}  Spe: {selected.Speed}\n" +
            $"Chain: {chainText}\n" +
            $"Gặp tại: {locationText}\n" +
            $"Evolution: {evolutionText}\n" +
            $"Learnset:\n{learnsetText}";
    }

    private string BuildEvolutionText(PokemonBase pokemon)
    {
        if (pokemon == null)
            return "None";

        var options = pokemon.GetValidEvolutionOptions();
        if (options == null || options.Count == 0)
            return "None";

        return string.Join(", ", options
            .Where(option => option != null && option.EvolvesTo != null)
            .Select(option => $"Lv {Mathf.Max(1, option.EvolutionLevel)} -> {option.EvolvesTo.Name}"));
    }

    private string BuildLearnsetPreview(PokemonBase pokemon)
    {
        if (pokemon == null || pokemon.LearnableMoves == null || pokemon.LearnableMoves.Length == 0)
            return "(No learnset data)";

        var sortedMoves = pokemon.LearnableMoves
            .Where(lm => lm != null && lm.move != null)
            .OrderBy(lm => lm.level)
            .ToList();

        if (sortedMoves.Count == 0)
            return "(No learnset data)";

        const int maxRowsPerColumn = 10;
        const int maxTotalRows = maxRowsPerColumn * 2;
        int rows = Mathf.Min(maxTotalRows, sortedMoves.Count);

        var leftCol = new List<string>(maxRowsPerColumn);
        var rightCol = new List<string>(maxRowsPerColumn);

        for (int i = 0; i < rows; i++)
        {
            var lm = sortedMoves[i];
            string moveName = string.IsNullOrWhiteSpace(lm.move.MoveName) ? lm.move.name : lm.move.MoveName;
            string entry = $"Lv {Mathf.Max(1, lm.level):00} - {moveName}";
            if (i < maxRowsPerColumn)
                leftCol.Add(entry);
            else
                rightCol.Add(entry);
        }

        int rowCount = Mathf.Max(leftCol.Count, rightCol.Count);
        int leftWidth = 0;
        for (int i = 0; i < leftCol.Count; i++)
            leftWidth = Mathf.Max(leftWidth, leftCol[i].Length);

        var lines = new List<string>(rowCount + 1);
        for (int row = 0; row < rowCount; row++)
        {
            string left = row < leftCol.Count ? leftCol[row] : string.Empty;
            string right = row < rightCol.Count ? rightCol[row] : string.Empty;

            if (!string.IsNullOrEmpty(right))
                lines.Add($"{left.PadRight(leftWidth + 3)}{right}");
            else
                lines.Add(left);
        }

        if (sortedMoves.Count > maxTotalRows)
            lines.Add($"... +{sortedMoves.Count - maxTotalRows} more moves");

        return string.Join("\n", lines);
    }

    private string FormatDexEntry(int dataIndex, PokemonBase pokemon)
    {
        if (pokemon == null)
            return string.Empty;

        pokemonEvolutionDepth.TryGetValue(pokemon, out var depth);
        depth = Mathf.Clamp(depth, 0, 4);

        string indent = depth > 0 ? new string(' ', depth * 3) : string.Empty;
        string prefix = depth > 0 ? "-> " : string.Empty;
        return $"{indent}{prefix}No.{pokemon.Num:000}  {pokemon.Name}";
    }

    private void EnsureDexIndicator(TMP_Text lineText, bool caught)
    {
        if (lineText == null)
            return;

        var indicatorRoot = lineText.transform.Find("DexIndicator");
        if (indicatorRoot == null)
        {
            var iconGo = new GameObject("DexIndicator", typeof(RectTransform), typeof(Image));
            indicatorRoot = iconGo.transform;
            indicatorRoot.SetParent(lineText.transform, false);

            var rt = iconGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(4f, 0f);
            rt.sizeDelta = new Vector2(16f, 16f);

            var ring = iconGo.GetComponent<Image>();
            ring.sprite = GetDexCircleSprite();
            ring.type = Image.Type.Simple;
            ring.color = Color.black;
            ring.raycastTarget = false;

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(indicatorRoot, false);

            var fillRt = fillGo.GetComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0.5f, 0.5f);
            fillRt.anchorMax = new Vector2(0.5f, 0.5f);
            fillRt.pivot = new Vector2(0.5f, 0.5f);
            fillRt.anchoredPosition = Vector2.zero;
            fillRt.sizeDelta = new Vector2(9f, 9f);

            var fill = fillGo.GetComponent<Image>();
            fill.sprite = GetDexCircleSprite();
            fill.type = Image.Type.Simple;
            fill.color = Color.green;
            fill.raycastTarget = false;
        }

        var fillTransform = indicatorRoot.Find("Fill");
        if (fillTransform != null)
            fillTransform.gameObject.SetActive(caught);
    }

    private Sprite GetDexCircleSprite()
    {
        if (dexCircleSprite != null)
            return dexCircleSprite;

        const int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "DexCircleSpriteTexture",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        var pixels = new Color32[size * size];
        float center = (size - 1) * 0.5f;
        float radius = center - 0.5f;
        float radiusSqr = radius * radius;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                bool inside = dx * dx + dy * dy <= radiusSqr;
                pixels[y * size + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();

        dexCircleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);

        return dexCircleSprite;
    }

    private List<TMP_Text> GetPokemonDbTexts()
    {
        if (pokemonDbLines != null && pokemonDbLines.Any(t => t != null))
            return pokemonDbLines.Where(t => t != null).ToList();

        if (autoPokemonDbTexts.Count == 0)
        {
            EnsurePokemonDbRuntimeUI();
            ResolveAutoTextCollections();
        }

        return autoPokemonDbTexts;
    }
}
