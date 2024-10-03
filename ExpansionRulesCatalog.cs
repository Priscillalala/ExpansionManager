using HG;
using IL.RoR2.Networking;
using MonoMod.Cil;
using RoR2.ContentManagement;
using RoR2.ExpansionManagement;
using RoR2.UI;
using RoR2.UI.SkinControllers;
using UnityEngine.AddressableAssets;
using UnityEngine.Assertions.Must;
using UnityEngine.UI;
using static RoR2.UI.CarouselController;

namespace ExpansionManager;

public static class ExpansionRulesCatalog
{
    public struct ExpansionContentRule
    {
        public bool enabled;
        public ExpansionDef expansionDef;
    }

    public class DisabledMeshEffect : BaseMeshEffect
    {
        public Color color = new Color(1f, 1f, 1f, 0f);

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive())
            {
                return;
            }
            Debug.Log("hi");
            Debug.Log(vh.currentVertCount);
            Debug.Log(vh.currentIndexCount);

            UIVertex vertex = default;
            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref vertex, i);
                if (vertex.position.y > 0f && vertex.position.x > 0f)
                {
                    vertex.color = color;
                    vh.SetUIVertex(vertex, i);
                }
            }
        }
    }

    public static readonly Dictionary<ExpansionIndex, RuleCategoryDef> expansionRuleCategories = [];
    public static readonly Dictionary<RuleCategoryDef, ExpansionDef> ruleCategoryExpansions = [];
    public static readonly Dictionary<ExpansionIndex, RuleChoiceDef> disableExpansionItemsChoices = [];
    public static readonly Dictionary<ExpansionIndex, RuleChoiceDef> disableExpansionElitesChoices = [];
    public static readonly Dictionary<ExpansionIndex, RuleChoiceDef> disableExpansionStagesChoices = [];
    public static readonly Dictionary<ExpansionIndex, RuleChoiceDef> disableExpansionMonstersChoices = [];
    public static readonly Dictionary<ExpansionIndex, RuleChoiceDef> disableExpansionInteractablesChoices = [];

    public static void Init()
    {
        On.RoR2.UI.RuleChoiceController.SetChoice += RuleChoiceController_SetChoice;
        On.RoR2.UI.RuleChoiceController.UpdateChoiceDisplay += RuleChoiceController_UpdateChoiceDisplay;
        On.RoR2.UI.RuleCategoryController.SetData += RuleCategoryController_SetData;
        IL.RoR2.RuleCatalog.Init += RuleCatalog_Init;
    }

    private static void RuleChoiceController_SetChoice(On.RoR2.UI.RuleChoiceController.orig_SetChoice orig, RuleChoiceController self, RuleChoiceDef newChoiceDef)
    {
        if (self.choiceDef?.extraData is ExpansionDef && newChoiceDef?.extraData is not ExpansionDef)
        {
            self.tooltipProvider.overrideBodyText = null;
            Transform notificationIconTransform = self.transform.Find("NotificationIcon");
            if (notificationIconTransform)
            {
                notificationIconTransform.gameObject.SetActive(false);
            }
            if (self.image)
            {
                DisabledMeshEffect disabledMeshEffect = self.image.gameObject.GetComponent<DisabledMeshEffect>();
                if (disabledMeshEffect)
                {
                    disabledMeshEffect.enabled = false;
                }
            }
        }
        orig(self, newChoiceDef);
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
        if (displayChoiceDef.extraData is ExpansionContentRule expansionContentRule)
        {
            if (self.image)
            {
                Transform subIconTransform = self.image.transform.parent.Find("SubIcon");
                Image subIcon = subIconTransform ? subIconTransform.GetComponent<Image>() : null;
                if (!subIcon)
                {
                    subIcon = new GameObject("SubIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                    subIcon.transform.SetParent(self.image.transform.parent, false);
                    Transform hoverOutline = subIcon.transform.parent.Find("HoverOutline");
                    if (hoverOutline)
                    {
                        ExpansionManagerPlugin.Logger.LogInfo("Setting sibling index");
                        subIcon.transform.SetSiblingIndex(hoverOutline.transform.GetSiblingIndex());
                    }
                    subIcon.gameObject.layer = LayerIndex.ui.intVal;
                    RectTransform rectTransform = subIcon.GetComponent<RectTransform>();
                    rectTransform.sizeDelta = new Vector2(32f, 32f);
                    rectTransform.localPosition = new Vector3(-20f, 20f);
                }
                subIcon.sprite = expansionContentRule.enabled ? expansionContentRule.expansionDef.iconSprite : expansionContentRule.expansionDef.disabledIconSprite;
            }
            foreach (RuleChoiceController ruleChoiceController in RuleChoiceController.instancesList)
            {
                if (ruleChoiceController.choiceDef != null && ruleChoiceController.choiceDef.extraData as ExpansionDef == expansionContentRule.expansionDef)
                {
                    ruleChoiceController.UpdateChoiceDisplay(ruleChoiceController.choiceDef);
                }
            }
        }
        else if (displayChoiceDef.extraData is ExpansionDef expansionDef)
        {
            if (!expansionRuleCategories.TryGetValue(expansionDef.expansionIndex, out RuleCategoryDef expansionCategoryDef))
            {
                return;
            }

            if (PreGameController.instance && self.tooltipProvider && expansionCategoryDef.children.Count > 0)
            {
                self.tooltipProvider.overrideBodyText = null;
                string bodyText = self.tooltipProvider.bodyText;
                bodyText += "\n";
                foreach (RuleDef ruleDef in expansionCategoryDef.children.OrderByDescending(x => PreGameController.instance.readOnlyRuleBook.GetRuleChoiceIndex(x) == x.defaultChoiceIndex))
                {
                    RuleChoiceDef choice = PreGameController.instance.readOnlyRuleBook.GetRuleChoice(ruleDef);
                    if (choice != null)
                    {
                        bodyText += Language.GetStringFormatted("EXPANSION_CONTENT_" + choice.localName.ToUpperInvariant(), choice.getTooltipName(choice));
                    }
                }
                self.tooltipProvider.overrideBodyText = bodyText;
            }

            if (!self.GetComponentInParent<RuleCategoryController>())
            {
                return;
            }

            Transform baseOutlineTransform = self.transform.Find("BaseOutline");
            Image baseOutline = baseOutlineTransform ? baseOutlineTransform.GetComponent<Image>() : null;
            Transform hoverOutlineTransform = self.transform.Find("HoverOutline");
            Image hoverOutline = hoverOutlineTransform ? hoverOutlineTransform.GetComponent<Image>() : null;

            if (!baseOutline)
            {
                baseOutline = new GameObject("BaseOutline", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                baseOutline.transform.SetParent(self.transform, false);
                baseOutline.gameObject.layer = LayerIndex.ui.intVal;
                baseOutline.sprite = ExpansionManagerPlugin.assets.LoadAsset<Sprite>("texUIHeaderSharp"); //Addressables.LoadAssetAsync<Sprite>("RoR2/Base/UI/texUIHeaderSingle.png").WaitForCompletion();
                baseOutline.color = new Color32(255, 255, 255, 30);
                RectTransform rectTransform = baseOutline.GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(48f, 8f);
                rectTransform.localPosition = new Vector3(0f, -30f, 0f);
            }
            if (!hoverOutline)
            {
                hoverOutline = new GameObject("HoverOutline", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                hoverOutline.transform.SetParent(self.transform, false);
                hoverOutline.gameObject.layer = LayerIndex.ui.intVal;
                hoverOutline.sprite = ExpansionManagerPlugin.assets.LoadAsset<Sprite>("texUIHeaderSharp"); //Addressables.LoadAssetAsync<Sprite>("RoR2/Base/UI/texUIHeaderSingle.png").WaitForCompletion();
                hoverOutline.color = Color.white;
                RectTransform rectTransform = hoverOutline.GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(48f, 8f);
                rectTransform.localPosition = new Vector3(0f, -32f, 0f);
            }
            if (self.hgButton)
            {
                self.hgButton.showImageOnHover = true;
                self.hgButton.imageOnInteractable = baseOutline;
                self.hgButton.imageOnHover = hoverOutline;
            }

            Transform notificationIconTransform = self.transform.Find("NotificationIcon");
            Image notificationIcon = notificationIconTransform ? notificationIconTransform.GetComponent<Image>() : null;

            bool rulesNonDefault = false;
            if (PreGameController.instance && expansionCategoryDef.children.Count > 0)
            {
                rulesNonDefault = expansionCategoryDef.children.Any(x => PreGameController.instance.readOnlyRuleBook.GetRuleChoiceIndex(x) != x.defaultChoiceIndex);
            }

            if (rulesNonDefault && !notificationIcon)
            {
                notificationIcon = new GameObject("NotificationIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                notificationIcon.transform.SetParent(self.transform, false);
                notificationIcon.gameObject.layer = LayerIndex.ui.intVal;
                notificationIcon.sprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/UI/texPlayerVoteStatus.png").WaitForCompletion();
                notificationIcon.material = Addressables.LoadAssetAsync<Material>("RoR2/Base/UI/matUIOverbrighten2x.mat").WaitForCompletion();
                notificationIcon.color = new Color32(235, 235, 235, 200);
                RectTransform rectTransform = notificationIcon.GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(20f, 30f);
                rectTransform.localPosition = new Vector3(16f, 16f);
            }

            if (notificationIcon)
            {
                notificationIcon.gameObject.SetActive(rulesNonDefault);
            }

            if (self.image)
            {
                DisabledMeshEffect disabledMeshEffect = self.image.gameObject.GetComponent<DisabledMeshEffect>();
                if (rulesNonDefault && !disabledMeshEffect)
                {
                    disabledMeshEffect = self.image.gameObject.AddComponent<DisabledMeshEffect>();
                }
                if (disabledMeshEffect) 
                {
                    disabledMeshEffect.enabled = rulesNonDefault;
                }
            }
        }
        /*if (self.tooltipProvider && displayChoiceDef.extraData is ExpansionDef expansionDef && expansionRuleCategories.TryGetValue(expansionDef.expansionIndex, out RuleCategoryDef expansionCategoryDef))// && expansionCategoryDef.children.Count > 0)
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
        }*/
    }

    private static void RuleCategoryController_SetData(On.RoR2.UI.RuleCategoryController.orig_SetData orig, RuleCategoryController self, RuleCategoryDef categoryDef, RuleChoiceMask availability, RuleBook ruleBook)
    {
        orig(self, categoryDef, availability, ruleBook);
        if (ruleCategoryExpansions.TryGetValue(categoryDef, out ExpansionDef expansionDef)) 
        {
            if (self.popoutPanelInstance)
            {
                self.popoutPanelInstance.popoutPanelDescriptionText.formatArgs = [Language.GetString(expansionDef.nameToken)];
            }
            if (self.rulesToDisplay != null && self.popoutButtonIconAllocator?.elements != null)
            {
                for (int i = 0; i < self.rulesToDisplay.Count; i++) 
                {
                    self.popoutButtonIconAllocator.elements[i].UpdateFromVotes();
                }
            }
            RuleBookViewer ruleBookViewer = self.GetComponentInParent<RuleBookViewer>();
            if (ruleBookViewer && ruleBookViewer.categoryElementAllocator != null)
            {
                RuleCategoryController expansionsCategoryController = ruleBookViewer.categoryElementAllocator?.elements?.FirstOrDefault(x => x.currentCategory != null && x.currentCategory.displayToken == "RULE_HEADER_EXPANSIONS");
                if (expansionsCategoryController && expansionsCategoryController.rulesToDisplay != null)
                {
                    int ruleIndex = expansionsCategoryController.rulesToDisplay.FindIndex(x => x.choices[x.defaultChoiceIndex].extraData as ExpansionDef == expansionDef);
                    if (ruleIndex >= 0)
                    {
                        RuleChoiceController ruleChoiceController = expansionsCategoryController.voteResultIconAllocator?.elements?[ruleIndex];
                        if (ruleChoiceController && ruleChoiceController.TryGetComponent(out HGButton hGButton))
                        {
                            hGButton.onClick.RemoveListener(self.TogglePopoutPanel);
                            hGButton.onClick.AddListener(self.TogglePopoutPanel);
                            hGButton.allowAllEventSystems = true;
                            hGButton.submitOnPointerUp = true;
                            hGButton.selectOnPointerEnter = true;
                            hGButton.disablePointerClick = false;
                            hGButton.disableGamepadClick = false;
                            hGButton.defaultFallbackButton = true;
                            hGButton.navigation = new Navigation { mode = Navigation.Mode.Automatic };
                            //hGButton.requiredTopLayer = self.stripPrefab?.GetComponent<RuleBookViewerStrip>()?.choicePrefab?.GetComponentInChildren<HGButton>()?.requiredTopLayer;
                        }
                    }
                }
            }
        }
        else if (categoryDef.displayToken == "RULE_HEADER_EXPANSIONS")
        {
            GridLayoutGroup gridLayoutGroup = self.voteResultGridContainer ? self.voteResultGridContainer.GetComponent<GridLayoutGroup>() : null;
            if (gridLayoutGroup)
            {
                gridLayoutGroup.padding = new RectOffset(6, 6, 12, 12);
                gridLayoutGroup.spacing = Vector2.zero;
            }
            /*ExpansionManagerPlugin.Logger.LogInfo("Found Expansions");
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
            }*/
        }
    }

    private static void RuleCatalog_Init(ILContext il)
    {
        ILCursor c = new ILCursor(il);
        if (c.TryGotoNext(MoveType.After,
            x => x.MatchCallOrCallvirt<RuleDef>(nameof(RuleDef.FromExpansion)))
            && c.TryGotoNext(MoveType.Before,
            x => x.MatchCallOrCallvirt(typeof(RuleCatalog), nameof(RuleCatalog.AddCategory)))
            )
        {
            c.EmitDelegate(AddExpansionRules);
        }
        else ExpansionManagerPlugin.Logger.LogError($"{nameof(ExpansionRulesCatalog)}: {nameof(RuleCatalog_Init)} IL match failed");
    }

    public static void AddExpansionRules()
    {
        foreach (ExpansionDef expansionDef in ExpansionCatalog.expansionDefs)
        {
            RuleCategoryDef ruleCategory = RuleCatalog.AddCategory(expansionDef.nameToken, expansionDef.descriptionToken, new Color32(219, 114, 114, byte.MaxValue), null, "RULE_HEADER_EXPANSIONS_EDIT", RuleCatalog.HiddenTestTrue, RuleCatalog.RuleCategoryType.VoteResultGrid);
            expansionRuleCategories[expansionDef.expansionIndex] = ruleCategory;
            ruleCategoryExpansions[ruleCategory] = expansionDef;

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
                RuleCatalog.AddRule(GenerateContentRule(expansionDef, "Stages", disableExpansionStagesChoices, ExpansionManagerPlugin.assets.LoadAsset<Sprite>("texRuleExpansionStagesOn"), ExpansionManagerPlugin.assets.LoadAsset<Sprite>("texRuleExpansionStagesOff")));
            }
            if (Array.Exists(ContentManager.networkedObjectPrefabs, InteractableMatchesExpansion))
            {
                RuleCatalog.AddRule(GenerateContentRule(expansionDef, "Interactables", disableExpansionInteractablesChoices, ExpansionManagerPlugin.assets.LoadAsset<Sprite>("texRuleExpansionInteractablesOn"), ExpansionManagerPlugin.assets.LoadAsset<Sprite>("texRuleExpansionInteractablesOff")));
            }
            if (Array.Exists(ContentManager.eliteDefs, EliteMatchesExpansion))
            {
                RuleCatalog.AddRule(GenerateContentRule(expansionDef, "Elites", disableExpansionElitesChoices, ExpansionManagerPlugin.assets.LoadAsset<Sprite>("texRuleExpansionElitesOn"), ExpansionManagerPlugin.assets.LoadAsset<Sprite>("texRuleExpansionElitesOff")));
            }
            if (Array.Exists(ContentManager.masterPrefabs, MonsterMatchesExpansion))
            {
                RuleCatalog.AddRule(GenerateContentRule(expansionDef, "Monsters", disableExpansionMonstersChoices, ExpansionManagerPlugin.assets.LoadAsset<Sprite>("texRuleExpansionMonstersOn"), ExpansionManagerPlugin.assets.LoadAsset<Sprite>("texRuleExpansionMonstersOff")));
            }
            if (Array.Exists(ContentManager.itemDefs, ItemMatchesExpansion) || Array.Exists(ContentManager.equipmentDefs, EquipmentMatchesExpansion))
            {
                RuleCatalog.AddRule(GenerateContentRule(expansionDef, "Items", disableExpansionItemsChoices, ExpansionManagerPlugin.assets.LoadAsset<Sprite>("texRuleExpansionItemsOn"), ExpansionManagerPlugin.assets.LoadAsset<Sprite>("texRuleExpansionItemsOff")));
            }
        }
    }

    public static RuleDef GenerateContentRule(ExpansionDef expansionDef, string contentName, Dictionary<ExpansionIndex, RuleChoiceDef> disableExpansionContentChoices, Sprite enabledIcon, Sprite disabledIcon = null)
    {
        RuleDef rule = new RuleDef($"Expansions.{expansionDef.name}.{contentName}", expansionDef.nameToken)
        {
            forceLobbyDisplay = true
        };

        string token = "EXPANSION_" + contentName.ToUpperInvariant();
        string descriptionToken = token + "_DESC";

        RuleChoiceDef enabledChoice = rule.AddChoice("On", new ExpansionContentRule { enabled = true, expansionDef = expansionDef });
        enabledChoice.sprite = enabledIcon;
        enabledChoice.tooltipNameToken = token;
        enabledChoice.tooltipNameColor = new Color32(219, 114, 114, byte.MaxValue);
        enabledChoice.tooltipBodyToken = descriptionToken;
        enabledChoice.requiredEntitlementDef = expansionDef.requiredEntitlement;
        enabledChoice.requiredExpansionDef = expansionDef;
        rule.MakeNewestChoiceDefault();

        RuleChoiceDef disabledChoice = rule.AddChoice("Off", new ExpansionContentRule { enabled = false, expansionDef = expansionDef });
        disabledChoice.sprite = disabledIcon ? disabledIcon : expansionDef.disabledIconSprite;
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
