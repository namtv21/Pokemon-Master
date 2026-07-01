using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Phần Move Database của PokemonDexMenuUI (partial class):
// định dạng dòng/chi tiết move và thanh tìm kiếm move.
public partial class PokemonDexMenuUI
{
    private string FormatMoveEntry(int dataIndex, MoveBase move)
    {
        if (move == null)
            return string.Empty;

        string moveName = string.IsNullOrWhiteSpace(move.MoveName) ? move.name : move.MoveName;
        return $"No.{dataIndex + 1:000}  {moveName}";
    }

    private string FormatMoveHeader(MoveBase move, int index)
    {
        if (move == null)
            return string.Empty;

        string moveName = string.IsNullOrWhiteSpace(move.MoveName) ? move.name : move.MoveName;
        return $"No.{index + 1:000}  {moveName}";
    }

    private string BuildMoveDetailText(MoveBase move)
    {
        if (move == null)
            return string.Empty;

        string statusText = string.IsNullOrWhiteSpace(move.StatusEffect) ? "None" : move.StatusEffect;
        string drainText = move.DrainRatio > 0f ? $"{move.DrainRatio:P0}" : "None";
        string typeText = move.Type.ToString();

        return
            $"Type: {typeText}\n" +
            $"Category: {move.Category}\n" +
            $"Power: {move.Power}\n" +
            $"Accuracy: {move.Accuracy}\n" +
            $"PP: {move.PP}\n" +
            $"Learn Priority: {move.LearnPriority}\n" +
            $"Target: {move.Target}\n" +
            $"Drain: {drainText}\n" +
            $"Status: {statusText}\n" +
            $"Learnable in battle data";
    }

    private List<TMP_Text> GetMoveDbTexts()
    {
        if (moveDbLines != null && moveDbLines.Any(t => t != null))
            return moveDbLines.Where(t => t != null).ToList();

        if (autoMoveDbTexts.Count == 0)
            EnsureMoveRuntimeUI();

        return autoMoveDbTexts;
    }

    private void HandleMoveSearchInput()
    {
        if (moveSearchInput == null)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            moveSearchInput.DeactivateInputField();
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
            if (moveSearchHint != null)
                moveSearchHint.text = "Nhấn Enter để tìm move";
            return;
        }
    }

    private void EnsureMoveSearchUi(RectTransform infoPanel)
    {
        if (infoPanel == null || moveSearchPanel != null)
            return;

        var searchGo = new GameObject("MoveSearchBar", typeof(RectTransform), typeof(Image));
        var searchRect = searchGo.GetComponent<RectTransform>();
        searchRect.SetParent(infoPanel, false);
        searchRect.anchorMin = new Vector2(0.02f, 0.86f);
        searchRect.anchorMax = new Vector2(0.98f, 0.98f);
        searchRect.offsetMin = Vector2.zero;
        searchRect.offsetMax = Vector2.zero;
        searchGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.35f);
        moveSearchPanel = searchGo;

        var hintGo = new GameObject("Hint", typeof(RectTransform), typeof(TextMeshProUGUI));
        var hintRect = hintGo.GetComponent<RectTransform>();
        hintRect.SetParent(searchRect, false);
        hintRect.anchorMin = new Vector2(0.03f, 0.55f);
        hintRect.anchorMax = new Vector2(0.97f, 0.97f);
        hintRect.offsetMin = Vector2.zero;
        hintRect.offsetMax = Vector2.zero;
        moveSearchHint = hintGo.GetComponent<TextMeshProUGUI>();
        moveSearchHint.fontSize = 20f;
        moveSearchHint.alignment = TextAlignmentOptions.MidlineLeft;
        moveSearchHint.color = Color.white;
        moveSearchHint.text = "Nhấn Enter để tìm move";
        if (TMP_Settings.defaultFontAsset != null)
            moveSearchHint.font = TMP_Settings.defaultFontAsset;

        var inputGo = new GameObject("Input", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        var inputRect = inputGo.GetComponent<RectTransform>();
        inputRect.SetParent(searchRect, false);
        inputRect.anchorMin = new Vector2(0.03f, 0.08f);
        inputRect.anchorMax = new Vector2(0.97f, 0.48f);
        inputRect.offsetMin = Vector2.zero;
        inputRect.offsetMax = Vector2.zero;
        inputGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);

        var textArea = new GameObject("Text Area", typeof(RectTransform));
        var textAreaRect = textArea.GetComponent<RectTransform>();
        textAreaRect.SetParent(inputRect, false);
        textAreaRect.anchorMin = Vector2.zero;
        textAreaRect.anchorMax = Vector2.one;
        textAreaRect.offsetMin = new Vector2(12f, 6f);
        textAreaRect.offsetMax = new Vector2(-12f, -6f);

        var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        var textRect = textGo.GetComponent<RectTransform>();
        textRect.SetParent(textAreaRect, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        var textComponent = textGo.GetComponent<TextMeshProUGUI>();
        textComponent.fontSize = 24;
        textComponent.color = Color.white;
        textComponent.alignment = TextAlignmentOptions.Left;
        if (TMP_Settings.defaultFontAsset != null)
            textComponent.font = TMP_Settings.defaultFontAsset;

        var placeholderGo = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
        var placeholderRect = placeholderGo.GetComponent<RectTransform>();
        placeholderRect.SetParent(textAreaRect, false);
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = Vector2.zero;
        placeholderRect.offsetMax = Vector2.zero;
        var placeholderText = placeholderGo.GetComponent<TextMeshProUGUI>();
        placeholderText.fontSize = 22;
        placeholderText.text = "Nhập tên Move";
        placeholderText.color = new Color(1f, 1f, 1f, 0.45f);
        placeholderText.alignment = TextAlignmentOptions.Left;
        if (TMP_Settings.defaultFontAsset != null)
            placeholderText.font = TMP_Settings.defaultFontAsset;

        moveSearchInput = inputGo.GetComponent<TMP_InputField>();
        moveSearchInput.textViewport = textAreaRect;
        moveSearchInput.textComponent = textComponent;
        moveSearchInput.placeholder = placeholderText;
        moveSearchInput.lineType = TMP_InputField.LineType.SingleLine;
        moveSearchInput.onValueChanged.AddListener(HandleMoveSearchChanged);
    }

    private int FindMoveIndex(string query)
    {
        if (moveDb.Count == 0)
            return -1;

        string normalized = TextKeyUtility.NormalizeLoose(query);
        if (string.IsNullOrWhiteSpace(normalized))
            return -1;

        int fallbackIndex = -1;
        for (int i = 0; i < moveDb.Count; i++)
        {
            var move = moveDb[i];
            if (move == null)
                continue;

            string moveName = TextKeyUtility.NormalizeLoose(move.MoveName);
            string rawName = TextKeyUtility.NormalizeLoose(move.name);

            if (moveName.StartsWith(normalized) || rawName.StartsWith(normalized))
                return i;

            if (fallbackIndex < 0 && (moveName.Contains(normalized) || rawName.Contains(normalized)))
                fallbackIndex = i;
        }

        return fallbackIndex;
    }
    private Color GetMoveTypeColor(PokemonType type)
    {
        int index = (int)type;
        if (moveTypeColors != null && index >= 0 && index < moveTypeColors.Length)
            return moveTypeColors[index];

        switch (type)
        {
            case PokemonType.Fire: return new Color(0.95f, 0.35f, 0.25f);
            case PokemonType.Water: return new Color(0.25f, 0.55f, 0.95f);
            case PokemonType.Grass: return new Color(0.25f, 0.8f, 0.35f);
            case PokemonType.Electric: return new Color(0.95f, 0.85f, 0.25f);
            case PokemonType.Psychic: return new Color(0.85f, 0.35f, 0.85f);
            case PokemonType.Ice: return new Color(0.4f, 0.85f, 0.95f);
            case PokemonType.Fighting: return new Color(0.8f, 0.35f, 0.2f);
            case PokemonType.Poison: return new Color(0.65f, 0.35f, 0.8f);
            case PokemonType.Ground: return new Color(0.75f, 0.55f, 0.25f);
            case PokemonType.Flying: return new Color(0.6f, 0.7f, 0.95f);
            case PokemonType.Bug: return new Color(0.55f, 0.75f, 0.2f);
            case PokemonType.Rock: return new Color(0.65f, 0.55f, 0.35f);
            case PokemonType.Ghost: return new Color(0.45f, 0.35f, 0.7f);
            case PokemonType.Dragon: return new Color(0.35f, 0.45f, 0.9f);
            case PokemonType.Dark: return new Color(0.4f, 0.35f, 0.45f);
            case PokemonType.Steel: return new Color(0.6f, 0.65f, 0.75f);
            case PokemonType.Fairy: return new Color(0.95f, 0.55f, 0.8f);
            case PokemonType.Normal:
            default:
                return new Color(0.85f, 0.85f, 0.85f);
        }
    }

    private bool IsMoveSearchFocused()
    {
        return moveSearchInput != null && moveSearchInput.isFocused;
    }

    private void FocusMoveSearch()
    {
        if (moveSearchInput == null)
            return;

        moveSearchInput.ActivateInputField();
        // Place caret at end and clear selection so Enter won't highlight whole text
        try
        {
            int len = moveSearchInput.text != null ? moveSearchInput.text.Length : 0;
            moveSearchInput.caretPosition = len;
            moveSearchInput.selectionAnchorPosition = len;
            moveSearchInput.selectionFocusPosition = len;
        }
        catch
        {
            // Some TMP versions may not expose selection properties; fall back to Select()
            moveSearchInput.Select();
        }

        if (moveSearchHint != null)
            moveSearchHint.text = "Nhập tên move để tìm kiếm (Nhấn Esc để thoát)";
    }

    private void HandleMoveSearchChanged(string query)
    {
        if (!IsMoveSearchFocused())
            return;

        string normalized = TextKeyUtility.NormalizeLoose(query);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            if (moveSearchHint != null)
                moveSearchHint.text = "Nhập tên move để tìm kiếm";
            return;
        }

        int foundIndex = FindMoveIndex(query);
        if (foundIndex < 0)
        {
            if (moveSearchHint != null)
                moveSearchHint.text = $"Không tìm thấy: {query}";
            return;
        }

        moveIndex = foundIndex;
        moveScrollOffset = Mathf.Clamp(moveIndex, 0, Mathf.Max(0, moveDb.Count - PokemonRowsPerPage));
        string foundName = null;
        var foundMove = moveDb[moveIndex];
        if (foundMove != null)
            foundName = string.IsNullOrWhiteSpace(foundMove.MoveName) ? foundMove.name : foundMove.MoveName;
        if (moveSearchHint != null)
            moveSearchHint.text = foundName != null ? $"Đã tìm: {foundName} — Nhấn Esc để thoát" : $"Không tìm thấy move — Nhấn Esc để thoát";
        RefreshAll();
    }

    private void EnsureMoveTypeIndicator(TMP_Text lineText, MoveBase move)
    {
        if (lineText == null || move == null)
            return;

        string moveName = string.IsNullOrWhiteSpace(move.MoveName) ? move.name : move.MoveName;
        lineText.text = $"No.{moveDb.IndexOf(move) + 1:000}  {moveName}";
    }

    private void EnsureMoveRuntimeUI()
    {
        if (moveDbLines != null && moveDbLines.Any(t => t != null) && moveDbDetailText != null && moveDbHeaderText != null && moveSearchInput != null)
            return;

        GameObject panel = null;
        if (tabPanels != null && tabPanels.Length > (int)DexTab.MoveDb)
            panel = tabPanels[(int)DexTab.MoveDb];
        else if (rootPanel != null)
            panel = rootPanel;
        else
            panel = gameObject;

        if (panel == null)
            return;

        var listRoot = panel.transform.Find("MoveDbAutoList") as RectTransform;
        if (listRoot == null)
        {
            var listGo = new GameObject("MoveDbAutoList", typeof(RectTransform), typeof(VerticalLayoutGroup));
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

        var infoPanel = panel.transform.Find("MoveDbAutoInfoPanel") as RectTransform;
        if (infoPanel == null)
        {
            var infoGo = new GameObject("MoveDbAutoInfoPanel", typeof(RectTransform), typeof(Image));
            infoPanel = infoGo.GetComponent<RectTransform>();
            infoPanel.SetParent(panel.transform, false);
            infoPanel.anchorMin = new Vector2(0.52f, 0.08f);
            infoPanel.anchorMax = new Vector2(0.98f, 0.92f);
            infoPanel.offsetMin = Vector2.zero;
            infoPanel.offsetMax = Vector2.zero;

            var bg = infoGo.GetComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.2f);
        }

        if (moveDbDetailText == null)
        {
            var detailTf = infoPanel.Find("MoveInfoText") as RectTransform;
            if (detailTf == null)
            {
                var detailGo = new GameObject("MoveInfoText", typeof(RectTransform));
                detailTf = detailGo.GetComponent<RectTransform>();
                detailTf.SetParent(infoPanel, false);
                detailTf.anchorMin = new Vector2(0.02f, 0.04f);
                detailTf.anchorMax = new Vector2(0.98f, 0.68f);
                detailTf.offsetMin = Vector2.zero;
                detailTf.offsetMax = Vector2.zero;

                var tmp = detailGo.AddComponent<TextMeshProUGUI>();
                tmp.enableWordWrapping = true;
                tmp.alignment = TextAlignmentOptions.TopLeft;
                tmp.fontSize = 22f;
                tmp.color = normalColor;
                if (TMP_Settings.defaultFontAsset != null)
                    tmp.font = TMP_Settings.defaultFontAsset;
                moveDbDetailText = tmp;
            }
            else
            {
                moveDbDetailText = detailTf.GetComponent<TextMeshProUGUI>();
                if (moveDbDetailText == null)
                    moveDbDetailText = detailTf.gameObject.AddComponent<TextMeshProUGUI>();
            }
        }

        if (moveDbHeaderText == null)
        {
            var headerTf = infoPanel.Find("MoveHeaderText") as RectTransform;
            if (headerTf == null)
            {
                var headerGo = new GameObject("MoveHeaderText", typeof(RectTransform));
                headerTf = headerGo.GetComponent<RectTransform>();
                headerTf.SetParent(infoPanel, false);
                headerTf.anchorMin = new Vector2(0.02f, 0.72f);
                headerTf.anchorMax = new Vector2(0.98f, 0.82f);
                headerTf.offsetMin = Vector2.zero;
                headerTf.offsetMax = Vector2.zero;

                var tmp = headerGo.AddComponent<TextMeshProUGUI>();
                tmp.enableWordWrapping = false;
                tmp.overflowMode = TextOverflowModes.Ellipsis;
                tmp.alignment = TextAlignmentOptions.MidlineLeft;
                tmp.fontSize = 24f;
                tmp.color = normalColor;
                if (TMP_Settings.defaultFontAsset != null)
                    tmp.font = TMP_Settings.defaultFontAsset;
                moveDbHeaderText = tmp;
            }
            else
            {
                moveDbHeaderText = headerTf.GetComponent<TextMeshProUGUI>();
                if (moveDbHeaderText == null)
                    moveDbHeaderText = headerTf.gameObject.AddComponent<TextMeshProUGUI>();
            }
        }

        EnsureMoveSearchUi(infoPanel);

        autoMoveDbTexts.Clear();
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

            autoMoveDbTexts.Add(rowText);
        }
    }

    private void EnsureMoveSelectionVisible(int visibleRows)
    {
        if (visibleRows <= 0 || moveDb.Count <= 0)
        {
            moveScrollOffset = 0;
            return;
        }

        moveScrollOffset = Mathf.Clamp(moveScrollOffset, 0, Mathf.Max(0, moveDb.Count - visibleRows));

        if (moveIndex < moveScrollOffset)
            moveScrollOffset = moveIndex;
        else if (moveIndex >= moveScrollOffset + visibleRows)
            moveScrollOffset = moveIndex - visibleRows + 1;
    }

    private void RefreshMoveDb()
    {
        var lines = GetMoveDbTexts();
        int visibleRows = Mathf.Min(PokemonRowsPerPage, Mathf.Max(1, lines.Count));
        moveIndex = ClampIndex(moveIndex, moveDb.Count);
        EnsureMoveSelectionVisible(visibleRows);

        for (int i = 0; i < lines.Count; i++)
        {
            var text = lines[i];

            if (i >= visibleRows)
            {
                text.text = string.Empty;
                text.color = normalColor;
                continue;
            }

            int dataIndex = moveScrollOffset + i;
            if (dataIndex < 0 || dataIndex >= moveDb.Count)
            {
                text.text = string.Empty;
                text.color = normalColor;
                continue;
            }

            var move = moveDb[dataIndex];
            text.text = FormatMoveEntry(dataIndex, move);
            text.color = BuildRowColor(dataIndex == moveIndex, true);
            EnsureMoveTypeIndicator(text, move);
        }

        if (moveDbDetailText == null)
            return;

        if (moveDb.Count == 0)
        {
            moveDbDetailText.text = "No move data found.";
            return;
        }

        var selected = moveDb[Mathf.Clamp(moveIndex, 0, moveDb.Count - 1)];
        if (moveDbHeaderText != null)
            moveDbHeaderText.text = FormatMoveHeader(selected, moveIndex);

        moveDbDetailText.text = BuildMoveDetailText(selected);
    }
}
