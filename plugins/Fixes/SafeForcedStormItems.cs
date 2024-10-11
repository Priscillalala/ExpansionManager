using HG;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace ExpansionManager.Fixes;

// Pickups with guaranteed Seekers of the Storm items do not handle the case where no Seekers of the Storm items are available
// This is mostly handled by drop table fallbacks for the Seekers of the Storm drop tables but this fix is included for extra safety
public static class SafeForcedStormItems
{
    [SystemInitializer]
    private static void Init()
    {
        IL.RoR2.PickupPickerController.GenerateOptionsFromDropTablePlusForcedStorm += PickupPickerController_GenerateOptionsFromDropTablePlusForcedStorm;
    }

    private static void PickupPickerController_GenerateOptionsFromDropTablePlusForcedStorm(ILContext il)
    {
        ILCursor c = new ILCursor(il);
        int locStormDropsArrayIndex = -1;
        int locElementIndex = -1;
        ILLabel breakLabel = null;
        if (c.TryGotoNext(MoveType.After,
            x => x.MatchLdarg(2),
            x => x.MatchLdarg(0),
            x => x.MatchLdarg(3),
            x => x.MatchCallOrCallvirt<PickupDropTable>(nameof(PickupDropTable.GenerateUniqueDrops)),
            x => x.MatchStloc(out locStormDropsArrayIndex))
            && c.TryGotoNext(MoveType.Before,
            x => x.MatchLdloc(locStormDropsArrayIndex),
            x => x.MatchLdloc(out locElementIndex),
            x => x.MatchLdelemAny<PickupIndex>())
            && c.TryGotoPrev(MoveType.After,
            x => x.MatchBgt(out breakLabel))
            )
        {
            c.Emit(OpCodes.Ldloc, locStormDropsArrayIndex);
            c.Emit(OpCodes.Ldloc, locElementIndex);
            c.EmitDelegate<Func<PickupIndex[], int, bool>>((stormDropsArray, i) =>
            {
                return ArrayUtils.IsInBounds(stormDropsArray, i);
            });
            c.Emit(OpCodes.Brfalse, breakLabel);
        }
        else ExpansionManagerPlugin.Logger.LogError($"{nameof(SafeForcedStormItems)}: {nameof(PickupPickerController_GenerateOptionsFromDropTablePlusForcedStorm)} IL match failed");
    }
}
