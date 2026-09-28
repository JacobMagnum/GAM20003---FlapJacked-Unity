using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "Game";
    [SerializeField]
    [TextArea(6, 20)]
    private string controlsText =
        "WASD — Move\n" +
        "Mouse — Look\n" +
        "Left Click — Grab / drop (left hand)\n" +
        "Right Click — Grab / drop (right hand)\n" +
        "Throw Left / Throw Right — Throw held item\n" +
        "F — Place held item on a stove or bench\n" +
        "Click a stove knob, move the mouse to change heat, click again to let go";
    [SerializeField] private bool buildTmpMenuIfEmpty = true;
    [SerializeField] private Button playButton;
    [SerializeField] private Button controlsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private GameObject controlsPanel;

    private void Awake()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        EnsureEventSystem();

        if (buildTmpMenuIfEmpty && (playButton == null || quitButton == null || controlsButton == null))
        {
            BuildTmpMenu();
        }

        if (controlsPanel == null)
        {
            Transform parent = transform;
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                parent = canvas.transform;
            }
            BuildControlsPanel(parent);
        }

        if (playButton != null)
        {
            playButton.onClick.AddListener(PlayGame);
        }

        if (controlsButton != null)
        {
            controlsButton.onClick.AddListener(ShowControls);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitGame);
        }

        if (controlsPanel != null)
        {
            controlsPanel.SetActive(false);
        }
    }

    public void PlayGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void ShowControls()
    {
        if (controlsPanel == null)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            Transform parent = canvas != null ? canvas.transform : transform;
            BuildControlsPanel(parent);
        }

        if (controlsPanel != null)
        {
            controlsPanel.SetActive(true);
            controlsPanel.transform.SetAsLastSibling();
        }
    }

    public void HideControls()
    {
        if (controlsPanel != null)
        {
            controlsPanel.SetActive(false);
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    private static void EnsureEventSystem()
    {
        EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            GameObject eventObject = new GameObject("EventSystem");
            eventSystem = eventObject.AddComponent<EventSystem>();
        }

        if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
        {
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }
    }

    private void BuildTmpMenu()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("MenuCanvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
        }

        RectTransform root = CreateUiObject("MenuRoot", canvas.transform);
        StretchFull(root);
        Image background = root.gameObject.AddComponent<Image>();
        background.color = new Color(0.08f, 0.08f, 0.1f, 1f);

        RectTransform title = CreateUiObject("Title", root);
        title.sizeDelta = new Vector2(800f, 80f);
        title.anchoredPosition = new Vector2(0f, 220f);
        TextMeshProUGUI titleText = title.gameObject.AddComponent<TextMeshProUGUI>();
        titleText.text = "Kitchen";
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontSize = 72f;
        titleText.color = Color.white;

        if (playButton == null)
        {
            playButton = CreateTmpButton(root, "PlayButton", "Play", new Vector2(0f, 60f));
        }
        if (controlsButton == null)
        {
            controlsButton = CreateTmpButton(root, "ControlsButton", "Controls", new Vector2(0f, -20f));
        }
        if (quitButton == null)
        {
            quitButton = CreateTmpButton(root, "QuitButton", "Quit", new Vector2(0f, -100f));
        }
    }

    private void BuildControlsPanel(Transform parent)
    {
        if (parent == null)
        {
            return;
        }

        RectTransform panel = CreateUiObject("ControlsPanel", parent);
        panel.sizeDelta = new Vector2(720f, 520f);
        Image panelImage = panel.gameObject.AddComponent<Image>();
        panelImage.color = new Color(0.12f, 0.12f, 0.16f, 0.98f);
        controlsPanel = panel.gameObject;

        RectTransform heading = CreateUiObject("ControlsTitle", panel);
        heading.sizeDelta = new Vector2(640f, 50f);
        heading.anchoredPosition = new Vector2(0f, 210f);
        TextMeshProUGUI headingText = heading.gameObject.AddComponent<TextMeshProUGUI>();
        headingText.text = "Controls";
        headingText.alignment = TextAlignmentOptions.Center;
        headingText.fontSize = 40f;
        headingText.color = Color.white;

        RectTransform body = CreateUiObject("ControlsText", panel);
        body.sizeDelta = new Vector2(640f, 340f);
        body.anchoredPosition = new Vector2(0f, 10f);
        TextMeshProUGUI bodyText = body.gameObject.AddComponent<TextMeshProUGUI>();
        bodyText.text = controlsText;
        bodyText.alignment = TextAlignmentOptions.TopLeft;
        bodyText.fontSize = 26f;
        bodyText.color = Color.white;
        bodyText.enableWordWrapping = true;

        Button closeButton = CreateTmpButton(panel, "CloseControlsButton", "Back", new Vector2(0f, -210f));
        closeButton.onClick.AddListener(HideControls);
    }

    private static Button CreateTmpButton(Transform parent, string objectName, string label, Vector2 position)
    {
        RectTransform rect = CreateUiObject(objectName, parent);
        rect.sizeDelta = new Vector2(320f, 64f);
        rect.anchoredPosition = position;

        Image image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.18f, 0.18f, 0.22f, 1f);

        Button button = rect.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.28f, 0.28f, 0.34f, 1f);
        colors.pressedColor = new Color(0.12f, 0.12f, 0.16f, 1f);
        button.colors = colors;

        RectTransform textRect = CreateUiObject("Text", rect);
        StretchFull(textRect);
        TextMeshProUGUI tmp = textRect.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 32f;
        tmp.color = Color.white;

        return button;
    }

    private static RectTransform CreateUiObject(string objectName, Transform parent)
    {
        GameObject uiObject = new GameObject(objectName);
        uiObject.transform.SetParent(parent, false);
        RectTransform rect = uiObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        return rect;
    }

    private static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
