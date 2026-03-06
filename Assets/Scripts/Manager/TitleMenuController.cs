using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Manager
{
    public class TitleMenuController : MonoBehaviour
    {
        private const string DevelopingText = "功能开发中";

        private Canvas _canvas;
        private Text _toastText;
        private GameObject _menuContent;
        private GameObject _companionSelectPanel;
        private GameObject _resultPanel;
        private Text _resultText;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            var existing = FindObjectOfType<TitleMenuController>();
            if (existing != null) return;

            EnsureEventSystem();

            var menuObj = new GameObject("TitleMenuController");
            DontDestroyOnLoad(menuObj);
            menuObj.AddComponent<TitleMenuController>();
        }

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            var eventSystemObj = new GameObject("EventSystem");
            DontDestroyOnLoad(eventSystemObj);
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();
        }

        private void Awake()
        {
            BuildUI();
            ShowMenu();
        }

        private void OnEnable()
        {
            UnitManager.GameEnded += HandleGameEnded;
        }

        private void OnDisable()
        {
            UnitManager.GameEnded -= HandleGameEnded;
        }

        private void BuildUI()
        {
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 900;

            gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            gameObject.AddComponent<GraphicRaycaster>();

            var root = CreateUIObject("Root", _canvas.transform);
            StretchToParent(root.GetComponent<RectTransform>());

            var background = CreateUIObject("Background", root.transform);
            var backgroundImage = background.AddComponent<Image>();
            backgroundImage.color = new Color(0.05f, 0.08f, 0.12f, 1f);
            StretchToParent(background.GetComponent<RectTransform>());

            _menuContent = CreateUIObject("Content", root.transform);
            var contentRect = _menuContent.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.sizeDelta = new Vector2(420f, 520f);

            var layout = _menuContent.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 14f;
            layout.padding = new RectOffset(20, 20, 20, 20);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            CreateTitle(_menuContent.transform, font);
            CreateMenuButton(_menuContent.transform, "新游戏", StartGame, font);
            CreateMenuButton(_menuContent.transform, "加载游戏", ShowDevelopingToast, font);
            CreateMenuButton(_menuContent.transform, "设置", ShowDevelopingToast, font);
            CreateMenuButton(_menuContent.transform, "退出游戏", QuitGame, font);

            var spacer = CreateUIObject("Spacer", _menuContent.transform);
            spacer.AddComponent<LayoutElement>().flexibleHeight = 1f;

            var toast = CreateUIObject("Toast", root.transform);
            var toastRect = toast.GetComponent<RectTransform>();
            toastRect.anchorMin = new Vector2(0.5f, 0.18f);
            toastRect.anchorMax = new Vector2(0.5f, 0.18f);
            toastRect.pivot = new Vector2(0.5f, 0.5f);
            toastRect.sizeDelta = new Vector2(280f, 54f);

            var toastBg = toast.AddComponent<Image>();
            toastBg.color = new Color(0f, 0f, 0f, 0.75f);

            _toastText = CreateUIObject("ToastText", toast.transform).AddComponent<Text>();
            _toastText.font = font;
            _toastText.text = DevelopingText;
            _toastText.alignment = TextAnchor.MiddleCenter;
            _toastText.color = Color.white;
            _toastText.fontSize = 24;
            StretchToParent(_toastText.GetComponent<RectTransform>());

            toast.SetActive(false);

            _companionSelectPanel = BuildCompanionSelectPanel(root.transform, font);
            _resultPanel = BuildResultPanel(root.transform, font);
        }

        private void ShowMenu()
        {
            _canvas.enabled = !UnitManager.IsGameRunning();
            _menuContent.SetActive(true);
            _companionSelectPanel.SetActive(false);
            _resultPanel.SetActive(false);
        }

        private void StartGame()
        {
            UnitManager.PrepareNewGame();
            _menuContent.SetActive(false);
            _resultPanel.SetActive(false);
            _companionSelectPanel.SetActive(true);
            _canvas.enabled = true;
        }

        private GameObject BuildCompanionSelectPanel(Transform root, Font font)
        {
            var panel = CreateUIObject("CompanionSelectPanel", root);
            StretchToParent(panel.GetComponent<RectTransform>());

            var blocker = panel.AddComponent<Image>();
            blocker.color = new Color(0f, 0f, 0f, 0.65f);

            var content = CreateUIObject("CompanionContent", panel.transform);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.sizeDelta = new Vector2(880f, 320f);

            var vLayout = content.AddComponent<VerticalLayoutGroup>();
            vLayout.spacing = 20f;
            vLayout.padding = new RectOffset(20, 20, 20, 20);
            vLayout.childControlHeight = true;
            vLayout.childControlWidth = true;
            vLayout.childForceExpandHeight = false;
            vLayout.childForceExpandWidth = true;

            var title = CreateUIObject("Title", content.transform).AddComponent<Text>();
            title.font = font;
            title.fontSize = 34;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = Color.white;
            title.text = "选择你的初始伙伴";
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 58f;

            var row = CreateUIObject("CompanionRow", content.transform);
            var hLayout = row.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 16f;
            hLayout.childControlHeight = true;
            hLayout.childControlWidth = true;
            hLayout.childForceExpandHeight = true;
            hLayout.childForceExpandWidth = true;
            row.AddComponent<LayoutElement>().preferredHeight = 180f;

            CreateCompanionButton(row.transform, "青蛙\n均衡型", font, InitialCompanionType.Frog);
            CreateCompanionButton(row.transform, "东方明蛛\n远程型", font, InitialCompanionType.Spider);
            CreateCompanionButton(row.transform, "独立游蜴\n突击型", font, InitialCompanionType.Lizard);

            panel.SetActive(false);
            return panel;
        }

        private GameObject BuildResultPanel(Transform root, Font font)
        {
            var panel = CreateUIObject("ResultPanel", root);
            StretchToParent(panel.GetComponent<RectTransform>());

            var blocker = panel.AddComponent<Image>();
            blocker.color = new Color(0f, 0f, 0f, 0.7f);

            var content = CreateUIObject("ResultContent", panel.transform);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.sizeDelta = new Vector2(760f, 260f);

            var bg = content.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.12f, 0.16f, 0.95f);

            var vLayout = content.AddComponent<VerticalLayoutGroup>();
            vLayout.spacing = 18f;
            vLayout.padding = new RectOffset(30, 30, 30, 30);
            vLayout.childControlHeight = true;
            vLayout.childControlWidth = true;
            vLayout.childForceExpandHeight = false;
            vLayout.childForceExpandWidth = true;

            _resultText = CreateUIObject("ResultText", content.transform).AddComponent<Text>();
            _resultText.font = font;
            _resultText.fontSize = 40;
            _resultText.alignment = TextAnchor.MiddleCenter;
            _resultText.color = Color.white;
            _resultText.gameObject.AddComponent<LayoutElement>().preferredHeight = 120f;

            CreateMenuButton(content.transform, "返回标题", BackToTitle, font);

            panel.SetActive(false);
            return panel;
        }

        private void CreateCompanionButton(Transform parent, string buttonText, Font font, InitialCompanionType companionType)
        {
            var buttonObj = CreateUIObject($"Companion_{companionType}", parent);
            var image = buttonObj.AddComponent<Image>();
            image.color = new Color(0.2f, 0.28f, 0.38f, 1f);

            var button = buttonObj.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => SelectCompanion(companionType));

            var colors = button.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = new Color(0.26f, 0.36f, 0.48f, 1f);
            colors.pressedColor = new Color(0.15f, 0.22f, 0.32f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;

            var text = CreateUIObject("Text", buttonObj.transform).AddComponent<Text>();
            text.font = font;
            text.text = buttonText;
            text.fontSize = 28;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            StretchToParent(text.GetComponent<RectTransform>());
        }

        private void SelectCompanion(InitialCompanionType companionType)
        {
            UnitManager.StartNewGameWithInitialCompanion(companionType);
            _companionSelectPanel.SetActive(false);
            _canvas.enabled = false;
        }

        private void HandleGameEnded(bool _, string message)
        {
            _resultText.text = message;
            _resultPanel.SetActive(true);
            _companionSelectPanel.SetActive(false);
            _menuContent.SetActive(false);
            _canvas.enabled = true;
        }

        private void BackToTitle()
        {
            UnitManager.ShutdownGame();
            ShowMenu();
        }

        private void ShowDevelopingToast()
        {
            StartCoroutine(ShowToastCoroutine());
        }

        private IEnumerator ShowToastCoroutine()
        {
            var toastRoot = _toastText.transform.parent.gameObject;
            toastRoot.SetActive(true);
            yield return new WaitForSeconds(0.5f);
            toastRoot.SetActive(false);
        }

        private void QuitGame()
        {
            UnitManager.ShutdownGame();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static void CreateTitle(Transform parent, Font font)
        {
            var title = CreateUIObject("Title", parent).AddComponent<Text>();
            title.font = font;
            title.text = "Protect Poop";
            title.fontSize = 56;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = new Color(0.94f, 0.95f, 0.98f, 1f);

            var layout = title.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 120f;
        }

        private static void CreateMenuButton(Transform parent, string buttonText, UnityEngine.Events.UnityAction action, Font font)
        {
            var buttonObj = CreateUIObject($"Button_{buttonText}", parent);
            var image = buttonObj.AddComponent<Image>();
            image.color = new Color(0.2f, 0.28f, 0.38f, 1f);

            var button = buttonObj.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);

            var colors = button.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = new Color(0.26f, 0.36f, 0.48f, 1f);
            colors.pressedColor = new Color(0.15f, 0.22f, 0.32f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;

            var layout = buttonObj.AddComponent<LayoutElement>();
            layout.preferredHeight = 72f;

            var text = CreateUIObject("Text", buttonObj.transform).AddComponent<Text>();
            text.font = font;
            text.text = buttonText;
            text.fontSize = 30;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            StretchToParent(text.GetComponent<RectTransform>());
        }

        private static GameObject CreateUIObject(string objectName, Transform parent)
        {
            var obj = new GameObject(objectName, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private static void StretchToParent(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
