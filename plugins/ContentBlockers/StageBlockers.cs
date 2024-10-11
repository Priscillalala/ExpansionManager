using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace ExpansionManager.ContentBlockers;

public static class StageBlockers
{
    [SystemInitializer]
    private static void Init()
    {
        On.RoR2.PortalSpawner.isValidStage += PortalSpawner_isValidStage;
        IL.RoR2.BazaarController.SetUpSeerStations += BazaarController_SetUpSeerStations;
        On.RoR2.Run.CanPickStage += Run_CanPickStage;
    }

    private static bool PortalSpawner_isValidStage(On.RoR2.PortalSpawner.orig_isValidStage orig, PortalSpawner self)
    {
        return orig(self) && (!self.requiredExpansion || !Run.instance.AreExpansionStagesDisabled(self.requiredExpansion));
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
                return result && !Run.instance.AreExpansionStagesDisabled(sceneDef.requiredExpansion);
            });
        }
        else ExpansionManagerPlugin.Logger.LogError($"{nameof(StageBlockers)}: {nameof(BazaarController_SetUpSeerStations)} IL match failed");
    }

    private static bool Run_CanPickStage(On.RoR2.Run.orig_CanPickStage orig, Run self, SceneDef sceneDef)
    {
        return orig(self, sceneDef) && (!sceneDef.requiredExpansion || !self.AreExpansionStagesDisabled(sceneDef.requiredExpansion));
    }
}
