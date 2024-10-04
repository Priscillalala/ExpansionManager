using ExpansionManager.Util;
using MonoMod.Cil;
using RoR2.ContentManagement;
using RoR2.ExpansionManagement;

namespace ExpansionManager;

public static class ExpansionRulesCatalog
{
    public struct ExpansionContentRule
    {
        public bool enabled;
        public ExpansionDef expansionDef;
    }

    public static event Action<ExpansionDef, RuleCategoryDef> GenerateRulesForExpansion;
    public static readonly Dictionary<ExpansionIndex, RuleCategoryDef> expansionByRuleCategory = [];
    public static readonly Dictionary<RuleCategoryDef, ExpansionDef> ruleCategoryByExpansion = [];
    public static readonly Dictionary<ExpansionIndex, RuleChoiceDef> disableExpansionItemsChoices = [];
    public static readonly Dictionary<ExpansionIndex, RuleChoiceDef> disableExpansionElitesChoices = [];
    public static readonly Dictionary<ExpansionIndex, RuleChoiceDef> disableExpansionStagesChoices = [];
    public static readonly Dictionary<ExpansionIndex, RuleChoiceDef> disableExpansionMonstersChoices = [];
    public static readonly Dictionary<ExpansionIndex, RuleChoiceDef> disableExpansionInteractablesChoices = [];

    public static void ModInit()
    {
        IL.RoR2.RuleCatalog.Init += RuleCatalog_Init;
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

    private static void AddExpansionRules()
    {
        Sprite
            texRuleExpansionStagesOn = ExpansionManagerAssets.Bundle.LoadAsset<Sprite>("texRuleExpansionStagesOn"),
            texRuleExpansionStagesOff = ExpansionManagerAssets.Bundle.LoadAsset<Sprite>("texRuleExpansionStagesOff"),
            texRuleExpansionInteractablesOn = ExpansionManagerAssets.Bundle.LoadAsset<Sprite>("texRuleExpansionInteractablesOn"),
            texRuleExpansionInteractablesOff = ExpansionManagerAssets.Bundle.LoadAsset<Sprite>("texRuleExpansionInteractablesOff"),
            texRuleExpansionElitesOn = ExpansionManagerAssets.Bundle.LoadAsset<Sprite>("texRuleExpansionElitesOn"),
            texRuleExpansionElitesOff = ExpansionManagerAssets.Bundle.LoadAsset<Sprite>("texRuleExpansionElitesOff"),
            texRuleExpansionMonstersOn = ExpansionManagerAssets.Bundle.LoadAsset<Sprite>("texRuleExpansionMonstersOn"),
            texRuleExpansionMonstersOff = ExpansionManagerAssets.Bundle.LoadAsset<Sprite>("texRuleExpansionMonstersOff"),
            texRuleExpansionItemsOn = ExpansionManagerAssets.Bundle.LoadAsset<Sprite>("texRuleExpansionItemsOn"),
            texRuleExpansionItemsOff = ExpansionManagerAssets.Bundle.LoadAsset<Sprite>("texRuleExpansionItemsOff");

        foreach (ExpansionDef expansionDef in ExpansionCatalog.expansionDefs)
        {
            RuleCategoryDef ruleCategory = RuleCatalog.AddCategory(expansionDef.nameToken, expansionDef.descriptionToken, new Color32(219, 114, 114, byte.MaxValue), null, "RULE_HEADER_EXPANSIONS_EDIT", RuleCatalog.HiddenTestTrue, RuleCatalog.RuleCategoryType.VoteResultGrid);
            expansionByRuleCategory[expansionDef.expansionIndex] = ruleCategory;
            ruleCategoryByExpansion[ruleCategory] = expansionDef;

            if (Array.Exists(ContentManager.sceneDefs, StageMatchesExpansion))
            {
                RuleCatalog.AddRule(GenerateContentRule(expansionDef, "Stages", texRuleExpansionStagesOn, texRuleExpansionStagesOff, disableExpansionStagesChoices));
            }
            if (Array.Exists(ContentManager.networkedObjectPrefabs, InteractableMatchesExpansion))
            {
                RuleCatalog.AddRule(GenerateContentRule(expansionDef, "Interactables", texRuleExpansionInteractablesOn, texRuleExpansionInteractablesOff, disableExpansionInteractablesChoices));
            }
            if (Array.Exists(ContentManager.eliteDefs, EliteMatchesExpansion))
            {
                RuleCatalog.AddRule(GenerateContentRule(expansionDef, "Elites", texRuleExpansionElitesOn, texRuleExpansionElitesOff, disableExpansionElitesChoices));
            }
            if (Array.Exists(ContentManager.masterPrefabs, MonsterMatchesExpansion))
            {
                RuleCatalog.AddRule(GenerateContentRule(expansionDef, "Monsters", texRuleExpansionMonstersOn, texRuleExpansionMonstersOff, disableExpansionMonstersChoices));
            }
            if (Array.Exists(ContentManager.itemDefs, ItemMatchesExpansion) || Array.Exists(ContentManager.equipmentDefs, EquipmentMatchesExpansion))
            {
                RuleCatalog.AddRule(GenerateContentRule(expansionDef, "Items", texRuleExpansionItemsOn, texRuleExpansionItemsOff, disableExpansionItemsChoices));
            }

            if (GenerateRulesForExpansion != null)
            {
                GenerateRulesForExpansion(expansionDef, ruleCategory);
                GenerateRulesForExpansion = null;
            }

            bool ItemMatchesExpansion(ItemDef itemDef)
            {
                return itemDef.requiredExpansion == expansionDef;
            }
            bool EquipmentMatchesExpansion(EquipmentDef equipmentDef)
            {
                return equipmentDef.requiredExpansion == expansionDef && !equipmentDef.IsElite();
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
        }
    }

    public static RuleDef GenerateContentRule(ExpansionDef expansionDef, string contentName, Sprite enabledIcon, Sprite disabledIcon, Dictionary<ExpansionIndex, RuleChoiceDef> disableExpansionContentChoices = null)
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
        disabledChoice.sprite = disabledIcon;
        disabledChoice.tooltipNameToken = token;
        disabledChoice.tooltipNameColor = ColorCatalog.GetColor(ColorCatalog.ColorIndex.Unaffordable);
        disabledChoice.getTooltipName = RuleChoiceDef.GetOffTooltipNameFromToken;
        disabledChoice.tooltipBodyToken = descriptionToken + "_OFF";
        if (disableExpansionContentChoices != null)
        {
            disableExpansionContentChoices[expansionDef.expansionIndex] = disabledChoice;
        }

        return rule;
    }

    public static bool IsExpansionContentDisabled(RuleBook ruleBook, ExpansionDef expansionDef, Dictionary<ExpansionIndex, RuleChoiceDef> disableExpansionContentChoices)
    {
        if (expansionDef && disableExpansionContentChoices.TryGetValue(expansionDef.expansionIndex, out var disabledChoice))
        {
            return ruleBook.IsChoiceActive(disabledChoice);
        }
        return false;
    }

    public static bool AreExpansionItemsDisabled(this Run run, ExpansionDef expansionDef)
    {
        return IsExpansionContentDisabled(run.ruleBook, expansionDef, disableExpansionItemsChoices);
    }

    public static bool AreExpansionElitesDisabled(this Run run, ExpansionDef expansionDef)
    {
        return IsExpansionContentDisabled(run.ruleBook, expansionDef, disableExpansionElitesChoices);
    }

    public static bool AreExpansionStagesDisabled(this Run run, ExpansionDef expansionDef)
    {
        return IsExpansionContentDisabled(run.ruleBook, expansionDef, disableExpansionStagesChoices);
    }

    public static bool AreExpansionMonstersDisabled(this Run run, ExpansionDef expansionDef)
    {
        return IsExpansionContentDisabled(run.ruleBook, expansionDef, disableExpansionMonstersChoices);
    }

    public static bool AreExpansionInteractablesDisabled(this Run run, ExpansionDef expansionDef)
    {
        return IsExpansionContentDisabled(run.ruleBook, expansionDef, disableExpansionInteractablesChoices);
    }
}
