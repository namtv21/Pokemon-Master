using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CheatGiveByNameInput : MonoBehaviour, Interactable
{
    [Header("Input UI")]
    [SerializeField] private string hintText = "Nhap ten item/pokemon, co the kem so o cuoi. Vi du: potion 5, abra 10";
    [SerializeField] private Color panelColor = new Color(0f, 0f, 0f, 0.75f);

    private GameObject panel;
    private TMP_InputField inputField;
    private TMP_Text hintLabel;
    private bool isOpen;

    public void Interact()
    {
        OpenInput();
    }

    public void OpenInput()
    {
        EnsureUi();
        if (panel == null || inputField == null)
            return;

        panel.SetActive(true);
        isOpen = true;

        if (hintLabel != null)
            hintLabel.text = hintText;

        inputField.text = string.Empty;
        inputField.ActivateInputField();
        inputField.Select();

        if (GameController.Instance != null)
            GameController.Instance.SetState(GameState.NPCInteraction);
    }

    private void Update()
    {
        if (!isOpen)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseInput();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            SubmitInput();
    }

    private void SubmitInput()
    {
        if (inputField == null)
            return;

        string raw = (inputField.text ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            ShowWarning("Hãy nhập tên item hoặc pokemon.");
            return;
        }

        if (TryExecuteCommand(raw))
            CloseInput();
    }

    private bool TryExecuteCommand(string raw)
    {
        string[] parts = raw.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return false;

        if (parts.Length >= 2 && int.TryParse(parts[^1], out int trailingNumber))
        {
            string nameWithoutAmount = string.Join(" ", parts.Take(parts.Length - 1));

            if (IsMoneyKeyword(nameWithoutAmount))
                return TryGiveMoney(trailingNumber);

            if (TryGiveItem(nameWithoutAmount, trailingNumber))
                return true;

            if (TryGivePokemon(nameWithoutAmount, trailingNumber))
                return true;
        }

        string cmd = TextKeyUtility.NormalizeLoose(parts[0]);

        if (cmd == "money" || cmd == "cash" || cmd == "dong" || cmd == "yen")
            return GiveMoneyFromParts(parts, 1);

        if (cmd == "item")
            return GiveItemFromParts(parts, 1);

        if (cmd == "pokemon" || cmd == "poke")
            return GivePokemonFromParts(parts, 1);

        if (TryGiveItem(raw, 1))
            return true;

        if (TryGivePokemon(raw, 5))
            return true;

        ShowWarning($"Không tìm thấy item/pokemon: {raw}");
        return false;
    }

    private bool GiveMoneyFromParts(string[] parts, int startIndex)
    {
        if (parts.Length <= startIndex || !int.TryParse(parts[startIndex], out int amount))
        {
            ShowWarning("Câu lệnh money: money <so_tien>");
            return false;
        }

        return TryGiveMoney(amount);
    }

    private bool GiveItemFromParts(string[] parts, int startIndex)
    {
        if (parts.Length <= startIndex)
        {
            ShowWarning("Câu lệnh item: item <tên> [số_lượng]");
            return false;
        }

        int count = 1;
        int last = parts.Length - 1;
        if (last > startIndex && int.TryParse(parts[last], out int parsedCount))
        {
            count = Mathf.Max(1, parsedCount);
            last -= 1;
        }

        string name = string.Join(" ", parts.Skip(startIndex).Take(last - startIndex + 1));
        return TryGiveItem(name, count);
    }

    private bool GivePokemonFromParts(string[] parts, int startIndex)
    {
        if (parts.Length <= startIndex)
        {
            ShowWarning("Câu lệnh pokemon: pokemon <tên> [level]");
            return false;
        }

        int level = 5;
        int last = parts.Length - 1;
        if (last > startIndex && int.TryParse(parts[last], out int parsedLevel))
        {
            level = Mathf.Clamp(parsedLevel, 1, 100);
            last -= 1;
        }

        string name = string.Join(" ", parts.Skip(startIndex).Take(last - startIndex + 1));
        return TryGivePokemon(name, level);
    }

    private bool TryGiveItem(string itemName, int count)
    {
        if (Inventory.Instance == null)
        {
            ShowWarning("Inventory chưa sẵn sàng.");
            return false;
        }

        ItemBase item = FindItemByName(itemName);
        if (item == null)
            return false;

        Inventory.Instance.AddItem(item, Mathf.Max(1, count));
        ToastNotificationManager.Instance?.Show($"Đã nhận {item.itemName} x{Mathf.Max(1, count)}");
        return true;
    }

    private bool TryGivePokemon(string pokemonName, int level)
    {
        if (PlayerParty.Instance == null)
        {
            ShowWarning("PlayerParty chưa sẵn sàng.");
            return false;
        }

        PokemonBase pokemonBase = FindPokemonByName(pokemonName);
        if (pokemonBase == null)
            return false;

        if (PlayerParty.Instance.Pokemons.Count >= 6 && StorageSystem.Instance == null)
        {
            ShowWarning("Party đã đủ 6 và StorageSystem chưa sẵn sàng.");
            return false;
        }

        var pokemon = new Pokemon(pokemonBase, Mathf.Clamp(level, 1, 100));
        PlayerParty.Instance.AddPokemon(pokemon);
        ToastNotificationManager.Instance?.Show($"Đã nhận {pokemonBase.Name} Lv.{Mathf.Clamp(level, 1, 100)}");
        return true;
    }

    private bool TryGiveMoney(int amount)
    {
        if (Inventory.Instance == null)
        {
            ShowWarning("Inventory chưa sẵn sàng.");
            return false;
        }

        amount = Mathf.Max(0, amount);
        if (amount <= 0)
        {
            ShowWarning("Số tiền phải lớn hơn 0.");
            return false;
        }

        Inventory.Instance.AddMoney(amount);
        ToastNotificationManager.Instance?.Show($"Đã nhận {amount} Đồng");
        return true;
    }

    private bool IsMoneyKeyword(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        string normalized = TextKeyUtility.NormalizeLoose(value);
        return normalized == "money" || normalized == "cash" || normalized == "dong" || normalized == "yen";
    }

    private ItemBase FindItemByName(string itemName)
    {
        if (string.IsNullOrWhiteSpace(itemName))
            return null;

        ItemBase fromInventory = Inventory.Instance != null ? Inventory.Instance.FindItemByName(itemName) : null;
        if (fromInventory != null)
            return fromInventory;

        string normalized = TextKeyUtility.NormalizeLoose(itemName);
        var allItems = Resources.LoadAll<ItemBase>(string.Empty);
        return allItems.FirstOrDefault(item => item != null &&
            (TextKeyUtility.NormalizeLoose(item.itemName) == normalized || TextKeyUtility.NormalizeLoose(item.name) == normalized));
    }

    private PokemonBase FindPokemonByName(string pokemonName)
    {
        if (string.IsNullOrWhiteSpace(pokemonName))
            return null;

        var db = PokemonDB.Instance;
        PokemonBase fromDb = db != null ? db.GetPokemonByName(pokemonName) : null;
        if (fromDb != null)
            return fromDb;

        string normalized = TextKeyUtility.NormalizeLoose(pokemonName);
        var allPokemons = Resources.LoadAll<PokemonBase>("PokemonData");
        return allPokemons.FirstOrDefault(p => p != null &&
            (TextKeyUtility.NormalizeLoose(p.Name) == normalized || TextKeyUtility.NormalizeLoose(p.name) == normalized));
    }

    private void CloseInput()
    {
        isOpen = false;
        if (panel != null)
            panel.SetActive(false);

        if (GameController.Instance != null)
            GameController.Instance.SetState(GameState.Overworld);
    }

    private void ShowWarning(string message)
    {
        ToastNotificationManager.Instance?.Show(message, Color.yellow);
        if (hintLabel != null)
            hintLabel.text = message;
    }

    private void EnsureUi()
    {
        if (panel != null && inputField != null)
            return;

        Canvas canvas = FindObjectOfType<Canvas>(true);
        if (canvas == null)
        {
            var canvasGo = new GameObject("CheatGiveInputCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
        }

        panel = new GameObject("CheatGiveInputPanel", typeof(RectTransform), typeof(Image));
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.SetParent(canvas.transform, false);
        panelRect.anchorMin = new Vector2(0.15f, 0.88f);
        panelRect.anchorMax = new Vector2(0.85f, 0.98f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = panelColor;

        var hintGo = new GameObject("Hint", typeof(RectTransform), typeof(TextMeshProUGUI));
        var hintRect = hintGo.GetComponent<RectTransform>();
        hintRect.SetParent(panelRect, false);
        hintRect.anchorMin = new Vector2(0.02f, 0.52f);
        hintRect.anchorMax = new Vector2(0.98f, 0.98f);
        hintRect.offsetMin = Vector2.zero;
        hintRect.offsetMax = Vector2.zero;
        hintLabel = hintGo.GetComponent<TextMeshProUGUI>();
        hintLabel.fontSize = 22;
        hintLabel.color = Color.white;
        hintLabel.alignment = TextAlignmentOptions.Left;
        if (TMP_Settings.defaultFontAsset != null)
            hintLabel.font = TMP_Settings.defaultFontAsset;

        var inputRoot = new GameObject("Input", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        var inputRect = inputRoot.GetComponent<RectTransform>();
        inputRect.SetParent(panelRect, false);
        inputRect.anchorMin = new Vector2(0.02f, 0.05f);
        inputRect.anchorMax = new Vector2(0.98f, 0.45f);
        inputRect.offsetMin = Vector2.zero;
        inputRect.offsetMax = Vector2.zero;
        inputRoot.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);

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
        textComponent.fontSize = 28;
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
        placeholderText.fontSize = 24;
        placeholderText.text = "Vi du: potion 5  |  abra 10  |  pokeball";
        placeholderText.color = new Color(1f, 1f, 1f, 0.45f);
        placeholderText.alignment = TextAlignmentOptions.Left;
        if (TMP_Settings.defaultFontAsset != null)
            placeholderText.font = TMP_Settings.defaultFontAsset;

        inputField = inputRoot.GetComponent<TMP_InputField>();
        inputField.textViewport = textAreaRect;
        inputField.textComponent = textComponent;
        inputField.placeholder = placeholderText;
        inputField.lineType = TMP_InputField.LineType.SingleLine;

        panel.SetActive(false);
    }
}
