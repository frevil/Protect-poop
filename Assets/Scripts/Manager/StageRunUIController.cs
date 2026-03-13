using UnityEngine;
using UnityEngine.UI;

namespace Manager
{
    public class StageRunUIController : MonoBehaviour
    {
        private Canvas _canvas;
        private Text _nutritionText;
        private Text _stageText;
        private GameObject _settlementPanel;
        private Text _settlementText;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            var existing = FindObjectOfType<StageRunUIController>();
            if (existing != null) return;

            var uiObj = new GameObject("StageRunUIController");
            DontDestroyOnLoad(uiObj);
            uiObj.AddComponent<StageRunUIController>();
        }

        private void Awake()
        {
            BuildUI();
            RefreshRunningState();
            HandleNutritionChanged(UnitManager.GetNutrition());
        }

        private void Update()
        {
            RefreshRunningState();
        }

        private void OnEnable()
        {
            UnitManager.NutritionChanged += HandleNutritionChanged;
            UnitManager.StageProgressChanged += HandleStageProgressChanged;
            UnitManager.StageSettled += HandleStageSettled;
            UnitManager.GameEnded += HandleGameEnded;
        }

        private void OnDisable()
        {
            UnitManager.NutritionChanged -= HandleNutritionChanged;
            UnitManager.StageProgressChanged -= HandleStageProgressChanged;
            UnitManager.StageSettled -= HandleStageSettled;
            UnitManager.GameEnded -= HandleGameEnded;
        }

        private void BuildUI()
        {
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 800;

            gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            gameObject.AddComponent<GraphicRaycaster>();

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var root = CreateUIObject("Root", _canvas.transform);
            StretchToParent(root.GetComponent<RectTransform>());

            var hudPanel = CreateUIObject("HudPanel", root.transform);
            var hudImage = hudPanel.AddComponent<Image>();
            hudImage.color = new Color(0f, 0f, 0f, 0.45f);

            var hudRect = hudPanel.GetComponent<RectTransform>();
            hudRect.anchorMin = new Vector2(0f, 1f);
            hudRect.anchorMax = new Vector2(0f, 1f);
            hudRect.pivot = new Vector2(0f, 1f);
            hudRect.anchoredPosition = new Vector2(20f, -20f);
            hudRect.sizeDelta = new Vector2(320f, 110f);

            var hudLayout = hudPanel.AddComponent<VerticalLayoutGroup>();
            hudLayout.padding = new RectOffset(16, 16, 10, 10);
            hudLayout.spacing = 8f;
            hudLayout.childControlHeight = true;
            hudLayout.childControlWidth = true;

            _nutritionText = CreateHUDText(hudPanel.transform, font, 26);
            _stageText = CreateHUDText(hudPanel.transform, font, 22);

            _settlementPanel = BuildSettlementPanel(root.transform, font);
            _settlementPanel.SetActive(false);
        }

        private GameObject BuildSettlementPanel(Transform root, Font font)
        {
            var panel = CreateUIObject("SettlementPanel", root);
            StretchToParent(panel.GetComponent<RectTransform>());

            var blocker = panel.AddComponent<Image>();
            blocker.color = new Color(0f, 0f, 0f, 0.7f);

            var content = CreateUIObject("SettlementContent", panel.transform);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.sizeDelta = new Vector2(860f, 500f);

            var bg = content.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.14f, 0.18f, 0.95f);

            var layout = content.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(30, 30, 30, 30);
            layout.spacing = 14f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;

            _settlementText = CreateUIObject("SettlementText", content.transform).AddComponent<Text>();
            _settlementText.font = font;
            _settlementText.fontSize = 32;
            _settlementText.color = Color.white;
            _settlementText.alignment = TextAnchor.MiddleCenter;
            _settlementText.gameObject.AddComponent<LayoutElement>().preferredHeight = 180f;

            CreateActionButton(content.transform, "购买青蛙（3营养）", () => TryBuyCompanion(InitialCompanionType.Frog), font);
            CreateActionButton(content.transform, "购买蜘蛛（3营养）", () => TryBuyCompanion(InitialCompanionType.Spider), font);
            CreateActionButton(content.transform, "购买蜥蜴（3营养）", () => TryBuyCompanion(InitialCompanionType.Lizard), font);
            CreateActionButton(content.transform, "不购买，继续闯关", ContinueWithoutBuying, font);

            return panel;
        }

        private static Text CreateHUDText(Transform parent, Font font, int fontSize)
        {
            var text = CreateUIObject("Text", parent).AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.gameObject.AddComponent<LayoutElement>().preferredHeight = 42f;
            return text;
        }

        private static void CreateActionButton(Transform parent, string label, UnityEngine.Events.UnityAction action, Font font)
        {
            var buttonObj = CreateUIObject($"Button_{label}", parent);
            var image = buttonObj.AddComponent<Image>();
            image.color = new Color(0.23f, 0.3f, 0.4f, 1f);

            var button = buttonObj.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);

            var layoutElement = buttonObj.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 64f;

            var text = CreateUIObject("Label", buttonObj.transform).AddComponent<Text>();
            text.font = font;
            text.fontSize = 28;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = label;
            StretchToParent(text.GetComponent<RectTransform>());
        }

        private void HandleNutritionChanged(int nutrition)
        {
            _nutritionText.text = $"营养值：{nutrition}";
        }

        private void HandleStageProgressChanged(StageProgressInfo info)
        {
            _stageText.text = $"难度{info.Tier} - 关卡 {info.StageInTier}/{info.TotalStageInTier}";
            _settlementPanel.SetActive(false);
            Time.timeScale = 1f;
        }

        private void HandleStageSettled(StageSettlementInfo info)
        {
            _settlementText.text = $"关卡通过！获得 +1 营养。\n当前营养：{info.Nutrition}\n" +
                                   $"已完成难度{info.Tier}的第 {info.ClearedStageInTier}/{info.TotalStageInTier} 关。\n" +
                                   "现在可选择是否花费3点营养购买新伙伴。";
            _settlementPanel.SetActive(true);
            Time.timeScale = 0f;
        }

        private void HandleGameEnded(bool _, string __)
        {
            _settlementPanel.SetActive(false);
            Time.timeScale = 1f;
        }

        private void TryBuyCompanion(InitialCompanionType companionType)
        {
            var purchased = UnitManager.BuyCompanionDuringSettlement(companionType);
            if (!purchased)
            {
                _settlementText.text += "\n营养不足，无法购买该伙伴。";
                return;
            }

            _settlementPanel.SetActive(false);
            Time.timeScale = 1f;
        }

        private void ContinueWithoutBuying()
        {
            UnitManager.ContinueAfterSettlement();
            _settlementPanel.SetActive(false);
            Time.timeScale = 1f;
        }

        private void RefreshRunningState()
        {
            _canvas.enabled = UnitManager.IsGameRunning() || _settlementPanel.activeSelf;
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
