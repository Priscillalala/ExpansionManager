using Mono.Cecil.Cil;
using MonoMod.Cil;
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
        On.RoR2.EliteDef.IsAvailable += EliteDef_IsAvailable;
        IL.RoR2.DccsPool.AreConditionsMet += DccsPool_AreConditionsMet;
        IL.RoR2.DirectorCard.IsAvailable += DirectorCard_IsAvailable;
        IL.RoR2.BazaarController.SetUpSeerStations += BazaarController_SetUpSeerStations;
        On.RoR2.Run.CanPickStage += Run_CanPickStage;
        Run.onRunSetRuleBookGlobal += Run_onRunSetRuleBookGlobal;
        On.RoR2.RuleCatalog.Init += RuleCatalog_Init;
    }

    private static bool EliteDef_IsAvailable(On.RoR2.EliteDef.orig_IsAvailable orig, EliteDef self)
    {
        return orig(self) && (!Run.instance || !Run.instance.ExpansionHasEliteDisabled(self));
    }

    private static void DccsPool_AreConditionsMet(ILContext il)
    {
        ILCursor c = new ILCursor(il);
        int locExpansionIndex = -1;
        if (c.TryGotoNext(MoveType.After,
            x => x.MatchLdloc(out locExpansionIndex),
            x => x.MatchCallOrCallvirt<Run>(nameof(Run.IsExpansionEnabled)))
            )
        {
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc, locExpansionIndex);
            c.EmitDelegate<Func<bool, DccsPool, ExpansionDef, bool>>((result, dccsPool, requiredExpansion) =>
            {
                if (ClassicStageInfo.instance)
                {
                    if (dccsPool == ClassicStageInfo.instance.monsterDccsPool)
                    {
                        return result && !Run.instance.ExpansionHasMonstersDisabled(requiredExpansion);
                    }
                    else if (dccsPool == ClassicStageInfo.instance.interactableDccsPool)
                    {
                        return result && !Run.instance.ExpansionHasInteractablesDisabled(requiredExpansion);
                    }
                }
                return result;
            });
        }
        else Debug.LogError($"{nameof(ExpansionRulesCatalog)}: {nameof(DccsPool_AreConditionsMet)} IL match failed");
    }

    private static void DirectorCard_IsAvailable(ILContext il)
    {
        ILCursor c = new ILCursor(il);
        int locExpansionRequirementIndex = -1;
        if (c.TryGotoNext(MoveType.After,
            x => x.MatchLdloc(out locExpansionRequirementIndex),
            x => x.MatchLdfld<ExpansionRequirementComponent>(nameof(ExpansionRequirementComponent.requiredExpansion)),
            x => x.MatchCallOrCallvirt<Run>(nameof(Run.IsExpansionEnabled)))
            )
        {
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc, locExpansionRequirementIndex);
            c.EmitDelegate<Func<bool, DirectorCard, ExpansionRequirementComponent, bool>>((result, directorCard, expansionRequirement) =>
            {
                return directorCard.spawnCard switch
                {
                    CharacterSpawnCard => result && !Run.instance.ExpansionHasMonstersDisabled(expansionRequirement.requiredExpansion),
                    InteractableSpawnCard => result && !Run.instance.ExpansionHasInteractablesDisabled(expansionRequirement.requiredExpansion),
                    _ => result
                };
            });
        }
        else Debug.LogError($"{nameof(ExpansionRulesCatalog)}: {nameof(DirectorCard_IsAvailable)} IL match failed");
    }

    private static void BazaarController_SetUpSeerStations(ILContext il)
    {
        ILCursor c = new ILCursor(il);
        int locSceneDefIndex = -1;
        if (c.TryGotoNext(MoveType.After,
            x => x.MatchLdloc(out locSceneDefIndex),
            x => x.MatchLdfld<SceneDef>(nameof(SceneDef.requiredExpansion)),
            x => x.MatchCallOrCallvirt<Run>(nameof(Run.IsExpansionEnabled)))
            )
        {
            c.Emit(OpCodes.Ldloc, locSceneDefIndex);
            c.EmitDelegate<Func<bool, SceneDef, bool>>((result, sceneDef) =>
            {
                return result && !Run.instance.ExpansionHasStagesDisabled(sceneDef.requiredExpansion);
            });
        }
        else Debug.LogError($"{nameof(ExpansionRulesCatalog)}: {nameof(BazaarController_SetUpSeerStations)} IL match failed");
    }

    private static bool Run_CanPickStage(On.RoR2.Run.orig_CanPickStage orig, Run self, SceneDef sceneDef)
    {
        return orig(self, sceneDef) && (!sceneDef.requiredExpansion || !self.ExpansionHasStagesDisabled(sceneDef.requiredExpansion));
    }

    private static void Run_onRunSetRuleBookGlobal(Run run, RuleBook ruleBook)
    {
        foreach (ItemDef itemDef in ItemCatalog.allItemDefs)
        {
            if (itemDef && itemDef.requiredExpansion && run.ExpansionHasItemsDisabled(itemDef.requiredExpansion))
            {
                run.availableItems.Remove(itemDef.itemIndex);
                run.expansionLockedItems.Add(itemDef.itemIndex);
            }
        }
        foreach (EquipmentDef equipmentDef in EquipmentCatalog.equipmentDefs)
        {
            if (equipmentDef && equipmentDef.requiredExpansion && run.ExpansionHasItemsDisabled(equipmentDef.requiredExpansion) && !IsEliteEquipment(equipmentDef))
            {
                run.availableEquipment.Remove(equipmentDef.equipmentIndex);
                run.expansionLockedEquipment.Add(equipmentDef.equipmentIndex);
            }
        }
    }

    private static void RuleCatalog_Init(On.RoR2.RuleCatalog.orig_Init orig)
    {
        foreach (ExpansionDef expansionDef in ExpansionCatalog.expansionDefs)
        {
            RuleCatalog.AddCategory(expansionDef.nameToken, expansionDef.descriptionToken, new Color32(219, 114, 114, byte.MaxValue), null, "RULE_HEADER_EXPANSIONS_EDIT", RuleCatalog.HiddenTestFalse, RuleCatalog.RuleCategoryType.VoteResultGrid);
            RuleCatalog.AddRule(GenerateContentRule(expansionDef, "Items", disableExpansionItemsChoices, Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texLootIconOutlined.png").WaitForCompletion()));
            RuleCatalog.AddRule(GenerateContentRule(expansionDef, "Elites", disableExpansionElitesChoices, Addressables.LoadAssetAsync<Sprite>("RoR2/Base/EliteFire/texBuffAffixRed.tif").WaitForCompletion()));
            RuleCatalog.AddRule(GenerateContentRule(expansionDef, "Stages", disableExpansionStagesChoices, Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texRuleMapIsRandom.png").WaitForCompletion()));
            RuleCatalog.AddRule(GenerateContentRule(expansionDef, "Monsters", disableExpansionMonstersChoices, Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texAttackIcon.png").WaitForCompletion()));
            RuleCatalog.AddRule(GenerateContentRule(expansionDef, "Interactables", disableExpansionInteractablesChoices, Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texInventoryIconOutlined.png").WaitForCompletion()));
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

    public static bool IsEliteEquipment(EquipmentDef equipmentDef)
    {
        return equipmentDef.passiveBuffDef && equipmentDef.passiveBuffDef.isElite;
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

    public static bool ExpansionHasEliteDisabled(this Run run, EliteDef eliteDef)
    {
        return eliteDef.eliteEquipmentDef && eliteDef.eliteEquipmentDef.requiredExpansion && ExpansionHasElitesDisabled(run, eliteDef.eliteEquipmentDef.requiredExpansion);
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
