using HG;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2.ExpansionManagement;

namespace ExpansionManager.ContentBlockers;

public static class DirectorBlockers
{
    [SystemInitializer]
    private static void Init()
    {
        IL.RoR2.DccsPool.AreConditionsMet += DccsPool_AreConditionsMet;
        IL.RoR2.DirectorCard.IsAvailable += DirectorCard_IsAvailable;
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
                        return result && !Run.instance.AreExpansionMonstersDisabled(requiredExpansion);
                    }
                    else if (dccsPool == ClassicStageInfo.instance.interactableDccsPool)
                    {
                        return result && !Run.instance.AreExpansionInteractablesDisabled(requiredExpansion);
                    }
                }
                return result;
            });
        }
        else ExpansionManagerPlugin.Logger.LogError($"{nameof(DirectorBlockers)}: {nameof(DccsPool_AreConditionsMet)} IL match failed");
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
                    CharacterSpawnCard => result && !Run.instance.AreExpansionMonstersDisabled(expansionRequirement.requiredExpansion),
                    InteractableSpawnCard => result && !Run.instance.AreExpansionInteractablesDisabled(expansionRequirement.requiredExpansion),
                    _ => result
                };
            });
        }
        else ExpansionManagerPlugin.Logger.LogError($"{nameof(DirectorBlockers)}: {nameof(DirectorCard_IsAvailable)} IL match failed");
    }
}
