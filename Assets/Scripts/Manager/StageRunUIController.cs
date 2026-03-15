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
        private GameObject _preparationPanel;
        private Text _preparationText;

        private int _gridColumns = 15;
        private int _gridRows = 8;
        private Texture2D _whiteTexture;

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
            _whiteTexture = new Texture2D(1, 1);
            _whiteTexture.SetPixel(0, 0, Color.white);
            _whiteTexture.Apply();
        }

        private void Update()
        {
            RefreshRunningState();
        }

        private void OnGUI()
        {
            if (!UnitManager.IsBattlePreparing()) return;
            DrawPreparationGrid();
        }

        private void OnEnable()
        {
            UnitManager.NutritionChanged += HandleNutritionChanged;
            UnitManager.StageProgressChanged += HandleStageProgressChanged;
            UnitManager.StageSettled += HandleStageSettled;
            UnitManager.GameEnded += HandleGameEnded;
            UnitManager.BattlePreparationStarted += HandleBattlePreparationStarted;
            UnitManager.BattlePreparationEnded += HandleBattlePreparationEnded;
        }

        private void OnDisable()
        {
            UnitManager.NutritionChanged -= HandleNutritionChanged;
            UnitManager.StageProgressChanged -= HandleStageProgressChanged;
            UnitManager.StageSettled -= HandleStageSettled;
            UnitManager.GameEnded -= HandleGameEnded;
            UnitManager.BattlePreparationStarted -= HandleBattlePreparationStarted;
            UnitManager.BattlePreparationEnded -= HandleBattlePreparationEnded;
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

            _preparationPanel = BuildPreparationPanel(root.transform, font);
            _preparationPanel.SetActive(false);
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

        private GameObject BuildPreparationPanel(Transform root, Font font)
        {
            var panel = CreateUIObject("PreparationPanel", root);
            StretchToParent(panel.GetComponent<RectTransform>());

            var blocker = panel.AddComponent<Image>();
            blocker.color = new Color(0f, 0f, 0f, 0.35f);

            var top = CreateUIObject("PreparationCard", panel.transform);
            var topRect = top.GetComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0.5f, 0.5f);
            topRect.anchorMax = new Vector2(0.5f, 0.5f);
            topRect.pivot = new Vector2(0.5f, 0.5f);
            topRect.anchoredPosition = new Vector2(0f, 160f);
            topRect.sizeDelta = new Vector2(620f, 220f);

            var topBg = top.AddComponent<Image>();
            topBg.color = new Color(0.08f, 0.1f, 0.14f, 0.92f);

            var layout = top.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 24, 24);
            layout.spacing = 14f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childAlignment = TextAnchor.UpperCenter;

            _preparationText = CreateHUDText(top.transform, font, 26);
            _preparationText.alignment = TextAnchor.MiddleCenter;
            _preparationText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _preparationText.text = "战斗准备中";

            CreateActionButton(top.transform, "确认站位并开始战斗", UnitManager.ConfirmBattlePreparation, font, 52f, 22, 420f);
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

        private static void CreateActionButton(Transform parent, string label, UnityEngine.Events.UnityAction action, Font font, float preferredHeight = 64f, int fontSize = 28, float preferredWidth = -1f)
        {
            var buttonObj = CreateUIObject($"Button_{label}", parent);
            var image = buttonObj.AddComponent<Image>();
            image.color = new Color(0.23f, 0.3f, 0.4f, 1f);

            var button = buttonObj.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);

            var layoutElement = buttonObj.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = preferredHeight;
            if (preferredWidth > 0f)
            {
                layoutElement.preferredWidth = preferredWidth;
            }

            var text = CreateUIObject("Label", buttonObj.transform).AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
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
            _preparationPanel.SetActive(false);
            Time.timeScale = 0f;
        }

        private void HandleBattlePreparationStarted(BattlePreparationInfo info)
        {
            _gridColumns = Mathf.Max(1, info.GridColumns);
            _gridRows = Mathf.Max(1, info.GridRows);
            _preparationText.text = $"战斗准备：拖动伙伴到格子中。当前分割 {_gridColumns} x {_gridRows}";
            _preparationPanel.SetActive(true);
            _settlementPanel.SetActive(false);
            Time.timeScale = 1f;
        }

        private void HandleBattlePreparationEnded()
        {
            _preparationPanel.SetActive(false);
        }

        private void HandleGameEnded(bool _, string __)
        {
            _settlementPanel.SetActive(false);
            _preparationPanel.SetActive(false);
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
            _canvas.enabled = UnitManager.IsGameRunning() || UnitManager.IsBattlePreparing() || _settlementPanel.activeSelf;
        }

        private void DrawPreparationGrid()
        {
            if (!BattleViewBounds.TryGetViewRectOnBattlePlane(out var center, out var halfSize) || Camera.main == null)
            {
                return;
            }

            var min = center - halfSize;
            var max = center + halfSize;
            var camera = Camera.main;
            var color = new Color(1f, 1f, 1f, 0.25f);

            for (var x = 0; x <= _gridColumns; x++)
            {
                var worldX = Mathf.Lerp(min.x, max.x, x / (float)_gridColumns);
                DrawWorldLine(camera, new Vector3(worldX, min.y, 0f), new Vector3(worldX, max.y, 0f), color, 1f);
            }

            for (var y = 0; y <= _gridRows; y++)
            {
                var worldY = Mathf.Lerp(min.y, max.y, y / (float)_gridRows);
                DrawWorldLine(camera, new Vector3(min.x, worldY, 0f), new Vector3(max.x, worldY, 0f), color, 1f);
            }
        }

        private void DrawWorldLine(Camera camera, Vector3 startWorld, Vector3 endWorld, Color color, float thickness)
        {
            var start = camera.WorldToScreenPoint(startWorld);
            var end = camera.WorldToScreenPoint(endWorld);
            if (start.z <= 0 || end.z <= 0) return;

            var p1 = new Vector2(start.x, Screen.height - start.y);
            var p2 = new Vector2(end.x, Screen.height - end.y);
            DrawLine(p1, p2, color, thickness);
        }

        private void DrawLine(Vector2 p1, Vector2 p2, Color color, float thickness)
        {
            var matrix = GUI.matrix;
            var colorBak = GUI.color;

            var angle = Vector3.Angle(p2 - p1, Vector2.right);
            if (p1.y > p2.y)
            {
                angle = -angle;
            }

            GUI.color = color;
            GUIUtility.RotateAroundPivot(angle, p1);
            GUI.DrawTexture(new Rect(p1.x, p1.y - thickness / 2f, (p2 - p1).magnitude, thickness), _whiteTexture);
            GUI.matrix = matrix;
            GUI.color = colorBak;
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
