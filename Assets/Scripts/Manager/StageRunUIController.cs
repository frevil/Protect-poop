using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Core;
using Manager.Evolution;
using Scripts.Core;

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
        private GameObject _statusPanel;
        private GameObject _detailPanel;
        private Text _detailTitle;
        private Text _detailContent;
        private readonly Dictionary<int, UnitStatusView> _statusViewsByUnitId = new();
        private readonly Dictionary<string, Sprite> _portraitSpriteCacheByType = new();
        private readonly Dictionary<string, PortraitVisualConfig> _visualConfigByType = new();
        private readonly List<EvolutionaryMomentOption> _selectedOptionBuffer = new();
        private readonly List<EvolutionSkillRuntime> _skillRuntimeBuffer = new();

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

            LoadVisualConfigs();
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

            _statusPanel = BuildStatusPanel(root.transform);
            _detailPanel = BuildDetailPanel(root.transform, font);
            _detailPanel.SetActive(false);

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
            _detailPanel.SetActive(false);
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
            RefreshStatusPanel();
        }

        private void RefreshStatusPanel()
        {
            if (_statusPanel == null) return;
            if (!_canvas.enabled)
            {
                _statusPanel.SetActive(false);
                return;
            }

            _statusPanel.SetActive(true);
            var units = UnitManager.GetUnits();
            var playerUnits = new List<UnitRuntimeData>();
            for (var i = 0; i < units.Count; i++)
            {
                var unit = units[i];
                if (!unit.alive || unit.faction != Faction.Player) continue;
                playerUnits.Add(unit);
            }

            playerUnits.Sort((a, b) =>
            {
                var aOrder = a.unitType == "PlayerBase" ? 0 : 1;
                var bOrder = b.unitType == "PlayerBase" ? 0 : 1;
                if (aOrder != bOrder) return aOrder.CompareTo(bOrder);
                return a.id.CompareTo(b.id);
            });

            var activeIds = new HashSet<int>();
            for (var i = 0; i < playerUnits.Count; i++)
            {
                var unit = playerUnits[i];
                activeIds.Add(unit.id);
                if (!_statusViewsByUnitId.TryGetValue(unit.id, out var view) || view?.root == null)
                {
                    view = CreateStatusCell(unit.id);
                    _statusViewsByUnitId[unit.id] = view;
                }

                view.root.SetActive(true);
                view.root.transform.SetSiblingIndex(i);
                UpdateStatusCell(view, unit);
            }

            foreach (var pair in _statusViewsByUnitId)
            {
                if (pair.Value?.root == null) continue;
                pair.Value.root.SetActive(activeIds.Contains(pair.Key));
            }
        }

        private GameObject BuildStatusPanel(Transform root)
        {
            var panel = CreateUIObject("PlayerStatusPanel", root);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(20f, -150f);
            rect.sizeDelta = new Vector2(340f, 420f);

            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;
            return panel;
        }

        private UnitStatusView CreateStatusCell(int unitId)
        {
            var root = CreateUIObject($"Status_{unitId}", _statusPanel.transform);
            var layout = root.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 8, 8);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            var rootImage = root.AddComponent<Image>();
            rootImage.color = new Color(0f, 0f, 0f, 0.52f);
            var layoutElement = root.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 78f;
            layoutElement.preferredWidth = 300f;

            var button = root.AddComponent<Button>();
            button.targetGraphic = rootImage;

            var portraitRoot = CreateUIObject("PortraitRoot", root.transform);
            var portraitRect = portraitRoot.GetComponent<RectTransform>();
            portraitRect.sizeDelta = new Vector2(62f, 62f);
            portraitRoot.AddComponent<LayoutElement>().preferredWidth = 62f;

            var ringBgObj = CreateUIObject("RingBG", portraitRoot.transform);
            StretchToParent(ringBgObj.GetComponent<RectTransform>());
            var ringBg = ringBgObj.AddComponent<Image>();
            ringBg.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
            ringBg.type = Image.Type.Filled;
            ringBg.fillMethod = Image.FillMethod.Radial360;
            ringBg.fillAmount = 1f;
            ringBg.color = new Color(1f, 1f, 1f, 0.2f);

            var hpRingObj = CreateUIObject("HPRing", portraitRoot.transform);
            StretchToParent(hpRingObj.GetComponent<RectTransform>());
            var hpRing = hpRingObj.AddComponent<Image>();
            hpRing.sprite = ringBg.sprite;
            hpRing.type = Image.Type.Filled;
            hpRing.fillMethod = Image.FillMethod.Radial360;
            hpRing.fillOrigin = 2;
            hpRing.fillAmount = 1f;
            hpRing.color = new Color(0.25f, 0.9f, 0.38f, 1f);

            var avatarMaskObj = CreateUIObject("AvatarMask", portraitRoot.transform);
            var avatarMaskRect = avatarMaskObj.GetComponent<RectTransform>();
            avatarMaskRect.anchorMin = new Vector2(0.17f, 0.17f);
            avatarMaskRect.anchorMax = new Vector2(0.83f, 0.83f);
            avatarMaskRect.offsetMin = Vector2.zero;
            avatarMaskRect.offsetMax = Vector2.zero;
            var avatarMaskImage = avatarMaskObj.AddComponent<Image>();
            avatarMaskImage.sprite = ringBg.sprite;
            avatarMaskImage.color = new Color(0f, 0f, 0f, 0.65f);
            avatarMaskObj.AddComponent<Mask>().showMaskGraphic = true;

            var avatarObj = CreateUIObject("Avatar", avatarMaskObj.transform);
            StretchToParent(avatarObj.GetComponent<RectTransform>());
            var avatarImage = avatarObj.AddComponent<Image>();
            avatarImage.preserveAspect = true;

            var nameObj = CreateUIObject("NameText", root.transform);
            var nameText = nameObj.AddComponent<Text>();
            nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameText.fontSize = 24;
            nameText.color = Color.white;
            nameText.alignment = TextAnchor.MiddleLeft;
            nameObj.AddComponent<LayoutElement>().preferredWidth = 200f;

            button.onClick.AddListener(() => ShowUnitDetails(unitId));

            return new UnitStatusView
            {
                root = root,
                portrait = avatarImage,
                hpRing = hpRing,
                nameText = nameText
            };
        }

        private void UpdateStatusCell(UnitStatusView view, UnitRuntimeData unit)
        {
            view.nameText.text = unit.name;
            view.portrait.sprite = GetPortraitSprite(unit.unitType);

            var hpPercent = unit.maxHp <= 0.01f ? 0f : Mathf.Clamp01(unit.hp / unit.maxHp);
            view.hpRing.fillAmount = hpPercent;
            view.hpRing.color = Color.Lerp(new Color(0.9f, 0.22f, 0.22f, 1f), new Color(0.25f, 0.9f, 0.38f, 1f), hpPercent);
        }

        private GameObject BuildDetailPanel(Transform root, Font font)
        {
            var panel = CreateUIObject("UnitDetailPanel", root);
            StretchToParent(panel.GetComponent<RectTransform>());
            var blocker = panel.AddComponent<Image>();
            blocker.color = new Color(0f, 0f, 0f, 0.68f);

            var card = CreateUIObject("DetailCard", panel.transform);
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(560f, 700f);
            var cardBg = card.AddComponent<Image>();
            cardBg.color = new Color(0.09f, 0.1f, 0.14f, 0.98f);

            var closeButtonObj = CreateUIObject("CloseButton", card.transform);
            var closeRect = closeButtonObj.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-16f, -16f);
            closeRect.sizeDelta = new Vector2(44f, 44f);
            var closeImage = closeButtonObj.AddComponent<Image>();
            closeImage.color = new Color(0.3f, 0.12f, 0.14f, 1f);
            var closeButton = closeButtonObj.AddComponent<Button>();
            closeButton.targetGraphic = closeImage;
            closeButton.onClick.AddListener(() => _detailPanel.SetActive(false));

            var closeText = CreateUIObject("Label", closeButtonObj.transform).AddComponent<Text>();
            closeText.font = font;
            closeText.alignment = TextAnchor.MiddleCenter;
            closeText.fontSize = 28;
            closeText.color = Color.white;
            closeText.text = "×";
            StretchToParent(closeText.GetComponent<RectTransform>());

            _detailTitle = CreateUIObject("Title", card.transform).AddComponent<Text>();
            _detailTitle.font = font;
            _detailTitle.fontSize = 30;
            _detailTitle.color = Color.white;
            _detailTitle.alignment = TextAnchor.MiddleLeft;
            var titleRect = _detailTitle.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.offsetMin = new Vector2(24f, -80f);
            titleRect.offsetMax = new Vector2(-24f, -24f);

            var scrollObj = CreateUIObject("DetailScroll", card.transform);
            var scrollRect = scrollObj.AddComponent<ScrollRect>();
            var scrollRectTransform = scrollObj.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0f, 0f);
            scrollRectTransform.anchorMax = new Vector2(1f, 1f);
            scrollRectTransform.offsetMin = new Vector2(24f, 24f);
            scrollRectTransform.offsetMax = new Vector2(-40f, -96f);

            var viewport = CreateUIObject("Viewport", scrollObj.transform);
            var viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.2f);
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            StretchToParent(viewport.GetComponent<RectTransform>());

            var content = CreateUIObject("Content", viewport.transform);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 800f);

            _detailContent = content.AddComponent<Text>();
            _detailContent.font = font;
            _detailContent.fontSize = 24;
            _detailContent.alignment = TextAnchor.UpperLeft;
            _detailContent.color = new Color(0.92f, 0.96f, 1f, 1f);
            _detailContent.horizontalOverflow = HorizontalWrapMode.Wrap;
            _detailContent.verticalOverflow = VerticalWrapMode.Overflow;

            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            scrollRect.viewport = viewport.GetComponent<RectTransform>();
            scrollRect.content = contentRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 20f;

            return panel;
        }

        private void ShowUnitDetails(int unitId)
        {
            var units = UnitManager.GetUnits();
            UnitRuntimeData unit = UnitRuntimeData.Empty;
            for (var i = 0; i < units.Count; i++)
            {
                if (units[i].id != unitId) continue;
                unit = units[i];
                break;
            }

            if (unit.IsEmpty()) return;
            _detailPanel.SetActive(true);
            _detailTitle.text = $"{unit.name} 详情";
            _detailContent.text = BuildUnitDetailText(unit);
        }

        private string BuildUnitDetailText(UnitRuntimeData unit)
        {
            var effectiveAttackInterval = EvolutionaryMomentSystem.GetEffectiveAttackInterval(unit);
            var attackPerSec = effectiveAttackInterval <= 0f ? 0f : 1f / effectiveAttackInterval;

            var lines = new List<string>
            {
                $"类型：{unit.unitType}",
                $"生命：{unit.hp:0.#}/{unit.maxHp:0.#}",
                $"攻击力：{unit.attack:0.#}",
                $"攻击范围：{unit.attackRange:0.##}",
                $"攻击速度：{unit.attackSpeed:0.##}",
                $"攻击间隔：{effectiveAttackInterval:0.##} 秒/次（约 {attackPerSec:0.##} 次/秒）",
                $"弹道数量：{unit.projectileCount}",
                $"移速：{unit.moveSpeed:0.##}",
                ""
            };

            EvolutionaryMomentSystem.GetSelectedOptionsForUnit(unit, _selectedOptionBuffer);
            if (_selectedOptionBuffer.Count > 0)
            {
                lines.Add("已拥有天赋：");
                for (var i = 0; i < _selectedOptionBuffer.Count; i++)
                {
                    var option = _selectedOptionBuffer[i];
                    lines.Add($"• {option.title}：{option.description}");
                }
            }
            else
            {
                lines.Add("已拥有天赋：暂无");
            }

            lines.Add("");
            EvolutionaryMomentSystem.GetSkillRuntimesForOwner(unit.id, _skillRuntimeBuffer);
            if (_skillRuntimeBuffer.Count > 0)
            {
                lines.Add("技能：");
                for (var i = 0; i < _skillRuntimeBuffer.Count; i++)
                {
                    var runtime = _skillRuntimeBuffer[i];
                    lines.Add($"• {runtime.skillId}（冷却 {runtime.cooldown:0.#}s，持续 {runtime.duration:0.#}s）");
                }
            }
            else
            {
                lines.Add("技能：暂无");
            }

            return string.Join("\n", lines);
        }

        private void LoadVisualConfigs()
        {
            _visualConfigByType.Clear();
            var configText = Resources.Load<TextAsset>("Configs/UnitVisualConfigs");
            if (configText == null) return;

            var list = JsonUtility.FromJson<PortraitVisualConfigList>(configText.text);
            if (list?.visuals == null) return;

            for (var i = 0; i < list.visuals.Count; i++)
            {
                var visual = list.visuals[i];
                if (string.IsNullOrEmpty(visual.unitType)) continue;
                _visualConfigByType[visual.unitType] = visual;
            }
        }

        private Sprite GetPortraitSprite(string unitType)
        {
            if (_portraitSpriteCacheByType.TryGetValue(unitType, out var sprite)) return sprite;
            if (!_visualConfigByType.TryGetValue(unitType, out var config)) return null;

            var texture = Resources.Load<Texture2D>(config.textureResourcePath);
            if (texture == null) return null;

            sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            _portraitSpriteCacheByType[unitType] = sprite;
            return sprite;
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

        private sealed class UnitStatusView
        {
            public GameObject root;
            public Image portrait;
            public Image hpRing;
            public Text nameText;
        }

        [System.Serializable]
        private sealed class PortraitVisualConfigList
        {
            public List<PortraitVisualConfig> visuals = new();
        }

        [System.Serializable]
        private sealed class PortraitVisualConfig
        {
            public string unitType;
            public string textureResourcePath;
        }
    }
}
