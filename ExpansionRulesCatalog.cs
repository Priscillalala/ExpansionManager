using HG;
using RoR2.ContentManagement;
using RoR2.ExpansionManagement;
using RoR2.UI;
using RoR2.UI.SkinControllers;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace ExpansionManager;

public static class ExpansionRulesCatalog
{
    public static readonly Dictionary<ExpansionIndex, RuleCategoryDef> expansionRuleCategories = [];
    public static readonly Dictionary<ExpansionIndex, RuleChoiceDef> disableExpansionItemsChoices = [];
    public static readonly Dictionary<ExpansionIndex, RuleChoiceDef> disableExpansionElitesChoices = [];
    public static readonly Dictionary<ExpansionIndex, RuleChoiceDef> disableExpansionStagesChoices = [];
    public static readonly Dictionary<ExpansionIndex, RuleChoiceDef> disableExpansionMonstersChoices = [];
    public static readonly Dictionary<ExpansionIndex, RuleChoiceDef> disableExpansionInteractablesChoices = [];

    public static void Init()
    {
        On.RoR2.UI.RuleChoiceController.UpdateChoiceDisplay += RuleChoiceController_UpdateChoiceDisplay;
        On.RoR2.UI.RuleCategoryController.SetData += RuleCategoryController_SetData;
        On.RoR2.RuleCatalog.Init += RuleCatalog_Init;
    }

    private static void RuleChoiceController_UpdateChoiceDisplay(On.RoR2.UI.RuleChoiceController.orig_UpdateChoiceDisplay orig, RuleChoiceController self, RuleChoiceDef displayChoiceDef)
    {
        orig(self, displayChoiceDef);
        /*if (PreGameController.instance && self.tooltipProvider && displayChoiceDef.extraData is ExpansionDef expansionDef && expansionRuleCategories.TryGetValue(expansionDef.expansionIndex, out RuleCategoryDef expansionCategoryDef) && expansionCategoryDef.children.Count > 0)
        {
            string bodyText = Language.GetString(self.tooltipProvider.bodyToken);
            bodyText += "<style=cStack>\n>";
            foreach (RuleDef ruleDef in expansionCategoryDef.children)
            {
                RuleChoiceDef choice = PreGameController.instance.readOnlyRuleBook.GetRuleChoice(ruleDef);
                if (choice != null)
                {
                    bodyText += Language.GetStringFormatted("EXPANSION_CONTENT_" + choice.localName.ToUpperInvariant(), Language.GetString(choice.tooltipNameToken));
                }
            }
            self.tooltipProvider.overrideBodyText = bodyText;
        }*/
        if (self.tooltipProvider && displayChoiceDef.extraData is ExpansionDef expansionDef && expansionRuleCategories.TryGetValue(expansionDef.expansionIndex, out RuleCategoryDef expansionCategoryDef))// && expansionCategoryDef.children.Count > 0)
        {
            RuleBookViewer ruleBookViewer = self.GetComponentInParent<RuleBookViewer>();
            if (ruleBookViewer && ruleBookViewer.categoryElementAllocator != null)
            {
                var categoryController = ruleBookViewer.categoryElementAllocator.elements.FirstOrDefault(x => x.currentCategory == expansionCategoryDef);
                if (categoryController && categoryController.voteResultGridContainer)
                {
                    self.tooltipProvider.extraUIDisplayPrefab = categoryController.voteResultGridContainer.gameObject;
                }
            }

            if (self.canVote)
            {
                return;
            }

            GameObject baseOutline = self.transform.Find("BaseOutline")?.gameObject;
            GameObject hoverOutline = self.transform.Find("HoverOutline")?.gameObject;

            if (!baseOutline)
            {
                baseOutline = new GameObject("BaseOutline", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                baseOutline.transform.SetParent(self.transform, false);
                baseOutline.gameObject.layer = LayerIndex.ui.intVal;
                Image image = baseOutline.GetComponent<Image>();
                image.sprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/UI/texUIHeaderSingle.png").WaitForCompletion();
                image.color = new Color32(255, 255, 255, 40);
                RectTransform rectTransform = baseOutline.GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(56f, 8f);
                rectTransform.localPosition = new Vector3(0f, -30f, 0f);
            }
            if (!hoverOutline)
            {
                hoverOutline = new GameObject("HoverOutline", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                hoverOutline.transform.SetParent(self.transform, false);
                hoverOutline.gameObject.layer = LayerIndex.ui.intVal;
                Image image = hoverOutline.GetComponent<Image>();
                image.sprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/UI/texUIHeaderSingle.png").WaitForCompletion();
                image.color = Color.white * 3f;
                //image.color = new Color32(255, 255, 255, 40);
                RectTransform rectTransform = hoverOutline.GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(64f, 12f);
                rectTransform.localPosition = new Vector3(0f, -32f, 0f);
            }
            if (self.hgButton)
            {
                self.hgButton.showImageOnHover = true;
                self.hgButton.imageOnInteractable = baseOutline.GetComponent<Image>();
                self.hgButton.imageOnHover = hoverOutline.GetComponent<Image>();
            }
        }
    }

    private static void RuleCategoryController_SetData(On.RoR2.UI.RuleCategoryController.orig_SetData orig, RuleCategoryController self, RuleCategoryDef categoryDef, RuleChoiceMask availability, RuleBook ruleBook)
    {
        orig(self, categoryDef, availability, ruleBook);
        if (categoryDef.displayToken == "RULE_HEADER_EXPANSIONS")
        {
            ExpansionManagerPlugin.Logger.LogInfo("Found Expansions");
            for (int i = 0; i < self.rulesToDisplay.Count; i++)
            {
                RuleDef rule = self.rulesToDisplay[i];
                RuleChoiceDef defaultChoice = rule.choices[rule.defaultChoiceIndex];
                if (defaultChoice.extraData is not ExpansionDef expansionDef || !expansionRuleCategories.TryGetValue(expansionDef.expansionIndex, out RuleCategoryDef expansionCategoryDef))
                {
                    ExpansionManagerPlugin.Logger.LogWarning($"skipping rule {rule.globalName}");
                    continue;
                }
                RuleChoiceController ruleChoiceController = self.voteResultIconAllocator.elements[i];
                RuleBookViewer ruleBookViewer = self.GetComponentInParent<RuleBookViewer>();
                ExpansionManagerPlugin.Logger.LogInfo("locating category");
                var categoryController = ruleBookViewer.categoryElementAllocator.elements.FirstOrDefault(x => x.currentCategory == expansionCategoryDef);
                ExpansionManagerPlugin.Logger.LogInfo("found category");
                if (ruleChoiceController.TryGetComponent(out HGButton hGButton))
                {
                    hGButton.onClick.RemoveListener(categoryController.TogglePopoutPanel);
                    hGButton.onClick.AddListener(categoryController.TogglePopoutPanel);
                    hGButton.allowAllEventSystems = true;
                    hGButton.submitOnPointerUp = true;
                    hGButton.selectOnPointerEnter = true;
                    hGButton.disablePointerClick = false;
                    hGButton.disableGamepadClick = false;
                    hGButton.defaultFallbackButton = true;
                    hGButton.navigation = new Navigation { mode = Navigation.Mode.Automatic };
                    hGButton.requiredTopLayer = self.stripPrefab?.GetComponent<RuleBookViewerStrip>()?.choicePrefab?.GetComponentInChildren<HGButton>()?.requiredTopLayer;
                }
                if (categoryController.popoutPanelInstance)
                {
                    categoryController.popoutPanelInstance.popoutPanelDescriptionText.formatArgs = [Language.GetString(expansionDef.nameToken)];
                }
            }
            GridLayoutGroup gridLayoutGroup = self.voteResultGridContainer?.GetComponent<GridLayoutGroup>();
            if (gridLayoutGroup)
            {
                gridLayoutGroup.padding = new RectOffset(6, 6, 12, 12);
                gridLayoutGroup.spacing = Vector2.zero;
            }
        }
    }

    private static void RuleCatalog_Init(On.RoR2.RuleCatalog.orig_Init orig)
    {
        foreach (ExpansionDef expansionDef in ExpansionCatalog.expansionDefs)
        {
            RuleCategoryDef ruleCategory = RuleCatalog.AddCategory(expansionDef.nameToken, expansionDef.descriptionToken, new Color32(219, 114, 114, byte.MaxValue), null, "RULE_HEADER_EXPANSIONS_EDIT", RuleCatalog.HiddenTestTrue, RuleCatalog.RuleCategoryType.VoteResultGrid);
            expansionRuleCategories[expansionDef.expansionIndex] = ruleCategory;

            bool ItemMatchesExpansion(ItemDef itemDef)
            {
                return itemDef.requiredExpansion == expansionDef;
            }
            bool EquipmentMatchesExpansion(EquipmentDef equipmentDef)
            {
                return equipmentDef.requiredExpansion == expansionDef && (!equipmentDef.passiveBuffDef || !equipmentDef.passiveBuffDef.isElite);
            }
            bool EliteMatchesExpansion(EliteDef eliteDef)
            {
                return eliteDef.eliteEquipmentDef && eliteDef.eliteEquipmentDef.requiredExpansion == expansionDef;
            }
            bool StageMatchesExpansion(SceneDef sceneDef)
            {
                return sceneDef.requiredExpansion == expansionDef && sceneDef.sceneType is SceneType.Stage or SceneType.Intermission or SceneType.TimedIntermission or SceneType.UntimedStage;
            }
            bool MonsterMatchesExpansion(GameObject masterPrefab)
            {
                if (masterPrefab.TryGetComponent(out ExpansionRequirementComponent expansionRequirement))
                {
                    return expansionRequirement.requiredExpansion == expansionDef && !expansionRequirement.requireEntitlementIfPlayerControlled;
                }
                if (masterPrefab.TryGetComponent(out CharacterMaster characterMaster) && characterMaster.bodyPrefab && characterMaster.bodyPrefab.TryGetComponent(out expansionRequirement))
                {
                    return expansionRequirement.requiredExpansion == expansionDef && !expansionRequirement.requireEntitlementIfPlayerControlled;
                }
                return false;
            }
            bool InteractableMatchesExpansion(GameObject networkedObjectPrefab)
            {
                return networkedObjectPrefab.TryGetComponent(out ExpansionRequirementComponent expansionRequirement) && expansionRequirement.requiredExpansion == expansionDef;
            }

            if (Array.Exists(ContentManager.sceneDefs, StageMatchesExpansion))
            {
                RuleCatalog.AddRule(GenerateContentRule(expansionDef, "Stages", disableExpansionStagesChoices, Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texRuleMapIsRandom.png").WaitForCompletion()));
            }
            if (Array.Exists(ContentManager.networkedObjectPrefabs, InteractableMatchesExpansion))
            {
                RuleCatalog.AddRule(GenerateContentRule(expansionDef, "Interactables", disableExpansionInteractablesChoices, Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texInventoryIconOutlined.png").WaitForCompletion()));
            }
            if (Array.Exists(ContentManager.masterPrefabs, MonsterMatchesExpansion))
            {
                RuleCatalog.AddRule(GenerateContentRule(expansionDef, "Monsters", disableExpansionMonstersChoices, Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texAttackIcon.png").WaitForCompletion()));
            }
            if (Array.Exists(ContentManager.eliteDefs, EliteMatchesExpansion))
            {
                RuleCatalog.AddRule(GenerateContentRule(expansionDef, "Elites", disableExpansionElitesChoices, Addressables.LoadAssetAsync<Sprite>("RoR2/Base/EliteFire/texBuffAffixRed.tif").WaitForCompletion()));
            }
            if (Array.Exists(ContentManager.itemDefs, ItemMatchesExpansion) || Array.Exists(ContentManager.equipmentDefs, EquipmentMatchesExpansion))
            {
                RuleCatalog.AddRule(GenerateContentRule(expansionDef, "Items", disableExpansionItemsChoices, Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texLootIconOutlined.png").WaitForCompletion()));
            }
        }
        orig();
    }

    public static RuleDef GenerateContentRule(ExpansionDef expansionDef, string contentName, Dictionary<ExpansionIndex, RuleChoiceDef> disableExpansionContentChoices, Sprite icon)
    {
        RuleDef rule = new RuleDef($"Expansions.{expansionDef.name}.{contentName}", expansionDef.nameToken)
        {
            forceLobbyDisplay = true
        };

        string token = "EXPANSION_" + contentName.ToUpperInvariant();
        string descriptionToken = token + "_DESC";

        RuleChoiceDef enabledChoice = rule.AddChoice("On");
        enabledChoice.sprite = icon;
        enabledChoice.tooltipNameToken = token;
        enabledChoice.tooltipNameColor = new Color32(219, 114, 114, byte.MaxValue);
        enabledChoice.tooltipBodyToken = descriptionToken;
        enabledChoice.requiredEntitlementDef = expansionDef.requiredEntitlement;
        enabledChoice.requiredExpansionDef = expansionDef;
        rule.MakeNewestChoiceDefault();

        RuleChoiceDef disabledChoice = rule.AddChoice("Off");
        disabledChoice.sprite = expansionDef.disabledIconSprite;
        disabledChoice.tooltipNameToken = token;
        disabledChoice.tooltipNameColor = ColorCatalog.GetColor(ColorCatalog.ColorIndex.Unaffordable);
        disabledChoice.getTooltipName = RuleChoiceDef.GetOffTooltipNameFromToken;
        disabledChoice.tooltipBodyToken = descriptionToken + "_OFF";
        disableExpansionContentChoices[expansionDef.expansionIndex] = disabledChoice;

        return rule;
    }

    public static bool ExpansionHasContentDisabled(Run run, ExpansionDef expansionDef, Dictionary<ExpansionIndex, RuleChoiceDef> disableExpansionContentChoices)
    {
        if (expansionDef && run.IsExpansionEnabled(expansionDef) && disableExpansionContentChoices.TryGetValue(expansionDef.expansionIndex, out var disabledChoice))
        {
            return run.ruleBook.IsChoiceActive(disabledChoice);
        }
        return false;
    }

    public static bool ExpansionHasItemsDisabled(this Run run, ExpansionDef expansionDef)
    {
        return ExpansionHasContentDisabled(run, expansionDef, disableExpansionItemsChoices);
    }

    public static bool ExpansionHasElitesDisabled(this Run run, ExpansionDef expansionDef)
    {
        return ExpansionHasContentDisabled(run, expansionDef, disableExpansionElitesChoices);
    }

    public static bool ExpansionHasStagesDisabled(this Run run, ExpansionDef expansionDef)
    {
        return ExpansionHasContentDisabled(run, expansionDef, disableExpansionStagesChoices);
    }

    public static bool ExpansionHasMonstersDisabled(this Run run, ExpansionDef expansionDef)
    {
        return ExpansionHasContentDisabled(run, expansionDef, disableExpansionMonstersChoices);
    }

    public static bool ExpansionHasInteractablesDisabled(this Run run, ExpansionDef expansionDef)
    {
        return ExpansionHasContentDisabled(run, expansionDef, disableExpansionInteractablesChoices);
    }
}
