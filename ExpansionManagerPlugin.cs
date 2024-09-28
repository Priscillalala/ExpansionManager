using BepInEx.Logging;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2.ExpansionManagement;
using System.Security;
using System.Security.Permissions;

[module: UnverifiableCode]
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace ExpansionManager;

[BepInPlugin(GUID, NAME, VERSION)]
public class ExpansionManagerPlugin : BaseUnityPlugin
{
    public const string
            GUID = "groovesalad." + NAME,
            NAME = "ExpansionManager",
            VERSION = "1.0.0";

    public static new ManualLogSource Logger { get; private set; }

    public void Awake()
    {
        Logger = base.Logger;

        ExpansionRulesCatalog.Init();
        DropTableFallbacks.Init();
        DeadDccsAdditions.Init();

        On.RoR2.EliteDef.IsAvailable += EliteDef_IsAvailable;

        IL.RoR2.DccsPool.AreConditionsMet += DccsPool_AreConditionsMet;
        IL.RoR2.DirectorCard.IsAvailable += DirectorCard_IsAvailable;

        On.RoR2.PortalSpawner.isValidStage += PortalSpawner_isValidStage;
        IL.RoR2.BazaarController.SetUpSeerStations += BazaarController_SetUpSeerStations;
        On.RoR2.Run.CanPickStage += Run_CanPickStage;

        Run.onRunSetRuleBookGlobal += Run_onRunSetRuleBookGlobal;
    }

    private static bool EliteDef_IsAvailable(On.RoR2.EliteDef.orig_IsAvailable orig, EliteDef self)
    {
        return orig(self) && (!Run.instance || !self.eliteEquipmentDef || !self.eliteEquipmentDef.requiredExpansion || !Run.instance.ExpansionHasElitesDisabled(self.eliteEquipmentDef.requiredExpansion));
    }

    private void DccsPool_AreConditionsMet(ILContext il)
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
        else Logger.LogError($"{nameof(ExpansionManagerPlugin)}: {nameof(DccsPool_AreConditionsMet)} IL match failed");
    }

    private void DirectorCard_IsAvailable(ILContext il)
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
        else Logger.LogError($"{nameof(ExpansionManagerPlugin)}: {nameof(DirectorCard_IsAvailable)} IL match failed");
    }

    private bool PortalSpawner_isValidStage(On.RoR2.PortalSpawner.orig_isValidStage orig, PortalSpawner self)
    {
        return orig(self) && (!self.requiredExpansion || !Run.instance.ExpansionHasStagesDisabled(self.requiredExpansion));
    }

    private void BazaarController_SetUpSeerStations(ILContext il)
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
        else Logger.LogError($"{nameof(ExpansionManagerPlugin)}: {nameof(BazaarController_SetUpSeerStations)} IL match failed");
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
            if (equipmentDef && equipmentDef.requiredExpansion && run.ExpansionHasItemsDisabled(equipmentDef.requiredExpansion) && (!equipmentDef.passiveBuffDef || !equipmentDef.passiveBuffDef.isElite))
            {
                run.availableEquipment.Remove(equipmentDef.equipmentIndex);
                run.expansionLockedEquipment.Add(equipmentDef.equipmentIndex);
            }
        }
    }
}
