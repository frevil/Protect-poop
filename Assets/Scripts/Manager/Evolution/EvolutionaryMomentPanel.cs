using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Manager.Evolution
{
    public class EvolutionaryMomentPanel : MonoBehaviour
    {
        private const float PanelWidth = 900f;
        private const float PanelHeight = 460f;

        private Canvas _canvas;
        private RectTransform _optionsContainer;
        private readonly List<GameObject> _spawnedOptions = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            var existing = FindObjectOfType<EvolutionaryMomentPanel>();
            if (existing != null) return;

            EnsureEventSystem();

            var panelObj = new GameObject("EvolutionaryMomentPanel");
            DontDestroyOnLoad(panelObj);
            panelObj.AddComponent<EvolutionaryMomentPanel>();
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
            Hide();
        }

        private void OnEnable()
        {
            EvolutionaryMomentSystem.EvolutionaryMomentStarted += Show;
            EvolutionaryMomentSystem.EvolutionaryMomentEnded += Hide;
        }

        private void OnDisable()
        {
            EvolutionaryMomentSystem.EvolutionaryMomentStarted -= Show;
            EvolutionaryMomentSystem.EvolutionaryMomentEnded -= Hide;
        }

        private void BuildUI()
        {
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 1000;

            gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            gameObject.AddComponent<GraphicRaycaster>();

            var dimmer = CreateUIObject("Dimmer", _canvas.transform);
            var dimmerImage = dimmer.AddComponent<Image>();
            dimmerImage.color = new Color(0f, 0f, 0f, 0.6f);
            StretchToParent(dimmer.GetComponent<RectTransform>());

            var panel = CreateUIObject("Panel", dimmer.transform);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;

            var panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.11f, 0.11f, 0.15f, 0.96f);

            var panelLayout = panel.AddComponent<VerticalLayoutGroup>();
            panelLayout.spacing = 16f;
            panelLayout.padding = new RectOffset(24, 24, 24, 24);
            panelLayout.childControlHeight = true;
            panelLayout.childControlWidth = true;
            panelLayout.childForceExpandHeight = false;
            panelLayout.childForceExpandWidth = true;

            var titleObj = CreateUIObject("Title", panel.transform);
            var title = titleObj.AddComponent<Text>();
            title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            title.alignment = TextAnchor.MiddleCenter;
            title.fontSize = 32;
            title.color = Color.white;
            title.text = "进化时刻";

            var subtitleObj = CreateUIObject("Subtitle", panel.transform);
            var subtitle = subtitleObj.AddComponent<Text>();
            subtitle.font = title.font;
            subtitle.alignment = TextAnchor.MiddleCenter;
            subtitle.fontSize = 18;
            subtitle.color = new Color(0.88f, 0.88f, 0.88f, 1f);
            subtitle.text = "请选择一个进化方向";

            var containerObj = CreateUIObject("OptionsContainer", panel.transform);
            _optionsContainer = containerObj.GetComponent<RectTransform>();
            var containerLayout = containerObj.AddComponent<HorizontalLayoutGroup>();
            containerLayout.spacing = 16f;
            containerLayout.padding = new RectOffset(0, 0, 12, 0);
            containerLayout.childControlHeight = true;
            containerLayout.childControlWidth = true;
            containerLayout.childForceExpandWidth = true;
            containerLayout.childForceExpandHeight = true;

            var optionsFitter = containerObj.AddComponent<ContentSizeFitter>();
            optionsFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var hintObj = CreateUIObject("Hint", panel.transform);
            var hint = hintObj.AddComponent<Text>();
            hint.font = title.font;
            hint.alignment = TextAnchor.MiddleCenter;
            hint.fontSize = 14;
            hint.color = new Color(0.72f, 0.72f, 0.72f, 1f);
            hint.text = "游戏已暂停，点击一个选项继续";
        }

        private void Show(IReadOnlyList<EvolutionaryMomentOption> options)
        {
            ClearOptions();

            for (var i = 0; i < options.Count; i++)
            {
                CreateOptionButton(options[i], i);
            }

            _canvas.enabled = true;
        }

        private void Hide()
        {
            _canvas.enabled = false;
            ClearOptions();
        }

        private void CreateOptionButton(EvolutionaryMomentOption option, int index)
        {
            var buttonObj = CreateUIObject($"Option_{index}", _optionsContainer);
            _spawnedOptions.Add(buttonObj);

            var image = buttonObj.AddComponent<Image>();
            image.color = new Color(0.2f, 0.24f, 0.3f, 0.95f);

            var button = buttonObj.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = new Color(0.28f, 0.34f, 0.43f, 1f);
            colors.pressedColor = new Color(0.16f, 0.2f, 0.27f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
            button.colors = colors;
            button.targetGraphic = image;
            button.onClick.AddListener(() => EvolutionaryMomentSystem.ChooseOption(index));

            var layoutElement = buttonObj.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 260f;
            layoutElement.minHeight = 260f;

            var contentObj = CreateUIObject("Content", buttonObj.transform);
            var contentRect = contentObj.GetComponent<RectTransform>();
            StretchToParent(contentRect, 14f);

            var contentLayout = contentObj.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 10f;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            var titleObj = CreateUIObject("Title", contentObj.transform);
            var title = titleObj.AddComponent<Text>();
            title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            title.fontSize = 24;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = Color.white;
            title.text = option.title;

            var descObj = CreateUIObject("Description", contentObj.transform);
            var description = descObj.AddComponent<Text>();
            description.font = title.font;
            description.fontSize = 18;
            description.alignment = TextAnchor.UpperLeft;
            description.color = new Color(0.85f, 0.85f, 0.9f, 1f);
            description.horizontalOverflow = HorizontalWrapMode.Wrap;
            description.verticalOverflow = VerticalWrapMode.Overflow;
            description.text = option.description;

            var descLayout = descObj.AddComponent<LayoutElement>();
            descLayout.flexibleHeight = 1f;
        }

        private void ClearOptions()
        {
            for (var i = 0; i < _spawnedOptions.Count; i++)
            {
                if (_spawnedOptions[i] != null)
                {
                    Destroy(_spawnedOptions[i]);
                }
            }

            _spawnedOptions.Clear();
        }

        private static GameObject CreateUIObject(string objectName, Transform parent)
        {
            var obj = new GameObject(objectName, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private static void StretchToParent(RectTransform rectTransform, float padding = 0f)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(padding, padding);
            rectTransform.offsetMax = new Vector2(-padding, -padding);
        }
    }
}
