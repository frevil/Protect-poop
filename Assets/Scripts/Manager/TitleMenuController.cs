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

            var content = CreateUIObject("Content", root.transform);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.sizeDelta = new Vector2(420f, 520f);

            var layout = content.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 14f;
            layout.padding = new RectOffset(20, 20, 20, 20);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            CreateTitle(content.transform, font);
            CreateMenuButton(content.transform, "新游戏", StartGame, font);
            CreateMenuButton(content.transform, "加载游戏", ShowDevelopingToast, font);
            CreateMenuButton(content.transform, "设置", ShowDevelopingToast, font);
            CreateMenuButton(content.transform, "退出游戏", QuitGame, font);

            var spacer = CreateUIObject("Spacer", content.transform);
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
        }

        private void ShowMenu()
        {
            _canvas.enabled = !UnitManager.IsGameRunning();
        }

        private void StartGame()
        {
            UnitManager.StartNewGame();
            _canvas.enabled = false;
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
