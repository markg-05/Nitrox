using NitroxClient.GameLogic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NitroxClient.MonoBehaviours.Gui.InGame;

public sealed class FabricatorBatchSelector : MonoBehaviour
{
    private static readonly Color PANEL_COLOR = new(0.015f, 0.075f, 0.105f, 0.96f);
    private static readonly Color NORMAL_COLOR = new(0.02f, 0.25f, 0.33f, 0.96f);
    private static readonly Color HIGHLIGHT_COLOR = new(0.03f, 0.55f, 0.68f, 1f);
    private static readonly Color PRESSED_COLOR = new(0.015f, 0.72f, 0.84f, 1f);
    private static readonly Color DISABLED_COLOR = new(0.06f, 0.12f, 0.14f, 0.72f);
    private static readonly Color TEXT_COLOR = new(0.78f, 0.96f, 1f, 1f);

    private readonly FabricatorQuantitySelection selection = new();

    private uGUI_CraftingMenu menu;
    private Fabricator fabricator;
    private uGUI_CraftingMenu.Node node;
    private GameObject root;
    private Button decrementButton;
    private Button incrementButton;
    private Button fabricateButton;
    private TextMeshProUGUI quantityText;
    private TextMeshProUGUI failureText;
    private float nextRefreshTime;
    private int openedFrame;

    public bool IsOpen => root && root.activeSelf;

    public void Show(uGUI_CraftingMenu craftingMenu, Fabricator selectedFabricator, uGUI_CraftingMenu.Node selectedNode)
    {
        menu = craftingMenu;
        fabricator = selectedFabricator;
        node = selectedNode;

        if (!root)
        {
            BuildView(craftingMenu.canvasGroup.transform.parent);
        }

        root.transform.SetAsLastSibling();
        root.SetActive(true);
        openedFrame = Time.frameCount;
        nextRefreshTime = 0f;

        FabricatorBatchValidation validation = FabricatorBatchManager.GetValidation(node.techType);
        selection.Reset(validation.Maximum);
        Refresh(validation);

        menu.SetInteractable(false);
        if (GamepadInputModule.current != null)
        {
            GamepadInputModule.current.SetCurrentGrid(null);
        }
        SelectPreferredButton();
    }

    public void Hide(bool restoreCraftingSelection)
    {
        if (!IsOpen)
        {
            return;
        }

        root.SetActive(false);
        if (!restoreCraftingSelection || !menu)
        {
            return;
        }

        menu.SetInteractable(true);
        if (GamepadInputModule.current != null)
        {
            GamepadInputModule.current.SetCurrentGrid(menu);
        }
        if (node != null && node.icon)
        {
            menu.NavigationSelect(node.icon);
        }
    }

    private void Update()
    {
        if (!IsOpen)
        {
            return;
        }

        if (Time.unscaledTime >= nextRefreshTime)
        {
            Refresh(FabricatorBatchManager.GetValidation(node.techType));
            nextRefreshTime = Time.unscaledTime + 0.2f;
        }

        if (Time.frameCount != openedFrame && GameInput.GetButtonDown(GameInput.Button.UICancel))
        {
            Hide(true);
        }
    }

    private void Increment()
    {
        selection.Increment();
        Refresh(FabricatorBatchManager.GetValidation(node.techType));
    }

    private void Decrement()
    {
        selection.Decrement();
        Refresh(FabricatorBatchManager.GetValidation(node.techType));
    }

    private void Fabricate()
    {
        FabricatorBatchValidation validation = FabricatorBatchManager.GetValidation(node.techType);
        selection.SetMaximum(validation.Maximum);
        Refresh(validation);
        if (!selection.CanFabricate)
        {
            FabricatorBatchManager.ShowFailure(validation.Failure);
            return;
        }

        int quantity = selection.Quantity;
        Hide(true);
        FabricatorBatchManager.StartBatch(menu, fabricator, node, quantity);
    }

    private void Refresh(FabricatorBatchValidation validation)
    {
        selection.SetMaximum(validation.Maximum);
        quantityText.text = $"×{selection.Quantity}";
        decrementButton.interactable = selection.CanDecrement;
        incrementButton.interactable = selection.CanIncrement;
        fabricateButton.interactable = selection.CanFabricate;
        failureText.text = validation.Maximum == 0 ? GetFailureText(validation.Failure) : string.Empty;
    }

    private static string GetFailureText(FabricatorBatchFailure failure) => failure switch
    {
        FabricatorBatchFailure.InventoryFull => Language.main.Get("InventoryFull"),
        FabricatorBatchFailure.MissingIngredients => Language.main.Get("DontHaveNeededIngredients"),
        _ => string.Empty
    };

    private void SelectPreferredButton()
    {
        if (EventSystem.current == null || EventSystem.current.alreadySelecting)
        {
            return;
        }
        EventSystem.current.SetSelectedGameObject((selection.CanIncrement ? incrementButton : fabricateButton).gameObject);
    }

    private void BuildView(Transform parent)
    {
        root = new GameObject("NitroxFabricatorBatchSelector", typeof(RectTransform), typeof(Image));
        RectTransform rootTransform = root.GetComponent<RectTransform>();
        rootTransform.SetParent(parent, false);
        rootTransform.anchorMin = rootTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rootTransform.pivot = new Vector2(0.5f, 0.5f);
        rootTransform.anchoredPosition = new Vector2(0f, -185f);
        rootTransform.sizeDelta = new Vector2(520f, 112f);
        root.GetComponent<Image>().color = PANEL_COLOR;

        GameObject row = new("Controls", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        RectTransform rowTransform = row.GetComponent<RectTransform>();
        rowTransform.SetParent(rootTransform, false);
        rowTransform.anchorMin = new Vector2(0f, 1f);
        rowTransform.anchorMax = new Vector2(1f, 1f);
        rowTransform.pivot = new Vector2(0.5f, 1f);
        rowTransform.anchoredPosition = new Vector2(0f, -10f);
        rowTransform.sizeDelta = new Vector2(-20f, 58f);
        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlHeight = true;
        layout.childControlWidth = false;
        layout.childForceExpandHeight = true;
        layout.childForceExpandWidth = false;

        decrementButton = CreateButton(rowTransform, "Decrease", "−", 72f, Decrement);
        quantityText = CreateText(rowTransform, "Quantity", "×1", 92f, 25f);
        incrementButton = CreateButton(rowTransform, "Increase", "+", 72f, Increment);
        fabricateButton = CreateButton(rowTransform, "Fabricate", Language.main.Get("Craft"), 220f, Fabricate);

        failureText = CreateText(rootTransform, "Failure", string.Empty, 500f, 16f);
        RectTransform failureTransform = failureText.rectTransform;
        failureTransform.anchorMin = failureTransform.anchorMax = new Vector2(0.5f, 0f);
        failureTransform.pivot = new Vector2(0.5f, 0f);
        failureTransform.anchoredPosition = new Vector2(0f, 8f);
        failureText.color = new Color(1f, 0.56f, 0.32f, 1f);

        SetNavigation(decrementButton, fabricateButton, incrementButton);
        SetNavigation(incrementButton, decrementButton, fabricateButton);
        SetNavigation(fabricateButton, incrementButton, decrementButton);
        root.SetActive(false);
    }

    private static Button CreateButton(Transform parent, string name, string label, float width, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = new(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObject.transform.SetParent(parent, false);
        buttonObject.GetComponent<LayoutElement>().preferredWidth = width;

        Image image = buttonObject.GetComponent<Image>();
        image.color = Color.white;
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = NORMAL_COLOR;
        colors.highlightedColor = HIGHLIGHT_COLOR;
        colors.selectedColor = HIGHLIGHT_COLOR;
        colors.pressedColor = PRESSED_COLOR;
        colors.disabledColor = DISABLED_COLOR;
        colors.colorMultiplier = 1f;
        button.colors = colors;
        button.onClick.AddListener(onClick);

        TextMeshProUGUI text = CreateText(buttonObject.transform, "Label", label, width, 24f);
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = Vector2.zero;
        text.rectTransform.offsetMax = Vector2.zero;
        return button;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, string value, float width, float fontSize)
    {
        GameObject textObject = new(name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        textObject.transform.SetParent(parent, false);
        textObject.GetComponent<LayoutElement>().preferredWidth = width;
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = TEXT_COLOR;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
        return text;
    }

    private static void SetNavigation(Button button, Selectable left, Selectable right)
    {
        Navigation navigation = button.navigation;
        navigation.mode = Navigation.Mode.Explicit;
        navigation.selectOnLeft = left;
        navigation.selectOnRight = right;
        button.navigation = navigation;
    }
}
