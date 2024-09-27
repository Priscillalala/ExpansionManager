using HG;
using RoR2.ContentManagement;
using RoR2.ExpansionManagement;
using UnityEngine.AddressableAssets;

namespace ExpansionManager;

public static class ExpansionRulesCatalog
{
    public static readonly Dictionary<ExpansionIndex, RuleChoiceDef> disableExpansionItemsChoices = [];
    public static readonly Dictionary<ExpansionIndex, RuleChoiceDef> disableExpansionElitesChoices = [];
    public static readonly Dictionary<ExpansionIndex, RuleChoiceDef> disableExpansionStagesChoices = [];
    public static readonly Dictionary<ExpansionIndex, RuleChoiceDef> disableExpansionMonstersChoices = [];
    public static readonly Dictionary<ExpansionIndex, RuleChoiceDef> disableExpansionInteractablesChoices = [];

    public static void Init()
    {
        On.RoR2.RuleCatalog.Init += RuleCatalog_Init;
    }

    private static void RuleCatalog_Init(On.RoR2.RuleCatalog.orig_Init orig)
    {
        foreach (ExpansionDef expansionDef in ExpansionCatalog.expansionDefs)
        {
            RuleCatalog.AddCategory(expansionDef.nameToken, expansionDef.descriptionToken, new Color32(219, 114, 114, byte.MaxValue), null, "RULE_HEADER_EXPANSIONS_EDIT", RuleCatalog.HiddenTestFalse, RuleCatalog.RuleCategoryType.VoteResultGrid);

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

            if (Array.Exists(ContentManager.itemDefs, ItemMatchesExpansion) || Array.Exists(ContentManager.equipmentDefs, EquipmentMatchesExpansion))
            {
                RuleCatalog.AddRule(GenerateContentRule(expansionDef, "Items", disableExpansionItemsChoices, Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texLootIconOutlined.png").WaitForCompletion()));
            }
            if (Array.Exists(ContentManager.eliteDefs, EliteMatchesExpansion))
            {
                RuleCatalog.AddRule(GenerateContentRule(expansionDef, "Elites", disableExpansionElitesChoices, Addressables.LoadAssetAsync<Sprite>("RoR2/Base/EliteFire/texBuffAffixRed.tif").WaitForCompletion()));
            }
            if (Array.Exists(ContentManager.sceneDefs, StageMatchesExpansion))
            {
                RuleCatalog.AddRule(GenerateContentRule(expansionDef, "Stages", disableExpansionStagesChoices, Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texRuleMapIsRandom.png").WaitForCompletion()));
            }
            if (Array.Exists(ContentManager.masterPrefabs, MonsterMatchesExpansion))
            {
                RuleCatalog.AddRule(GenerateContentRule(expansionDef, "Monsters", disableExpansionMonstersChoices, Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texAttackIcon.png").WaitForCompletion()));
            }
            if (Array.Exists(ContentManager.networkedObjectPrefabs, InteractableMatchesExpansion))
            {
                RuleCatalog.AddRule(GenerateContentRule(expansionDef, "Interactables", disableExpansionInteractablesChoices, Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texInventoryIconOutlined.png").WaitForCompletion()));
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
        
        RuleChoiceDef enabledChoice = rule.AddChoice("On");
        enabledChoice.sprite = icon;
        enabledChoice.tooltipNameToken = "EXPANSION_" + contentName.ToUpperInvariant();
        enabledChoice.tooltipNameColor = new Color32(219, 114, 114, byte.MaxValue);
        enabledChoice.tooltipBodyToken = expansionDef.descriptionToken;
        enabledChoice.requiredEntitlementDef = expansionDef.requiredEntitlement;
        enabledChoice.requiredExpansionDef = expansionDef;
        rule.MakeNewestChoiceDefault();

        RuleChoiceDef disabledChoice = rule.AddChoice("Off");
        disabledChoice.sprite = expansionDef.disabledIconSprite;
        disabledChoice.tooltipNameToken = expansionDef.nameToken;
        disabledChoice.tooltipNameColor = ColorCatalog.GetColor(ColorCatalog.ColorIndex.Unaffordable);
        disabledChoice.getTooltipName = RuleChoiceDef.GetOffTooltipNameFromToken;
        disabledChoice.tooltipBodyToken = expansionDef.descriptionToken;
        disableExpansionContentChoices[expansionDef.expansionIndex] = disabledChoice;

        return rule;
    }

    /*public static bool IsEliteEquipment(EquipmentDef equipmentDef)
    {
        return equipmentDef.passiveBuffDef && equipmentDef.passiveBuffDef.isElite;
    }*/

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

    /*public static bool ExpansionHasEliteDisabled(this Run run, EliteDef eliteDef)
    {
        return eliteDef.eliteEquipmentDef && eliteDef.eliteEquipmentDef.requiredExpansion && ExpansionHasElitesDisabled(run, eliteDef.eliteEquipmentDef.requiredExpansion);
    }*/

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
