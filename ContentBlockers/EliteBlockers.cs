namespace ExpansionManager.ContentBlockers;

public static class EliteBlockers
{
    [SystemInitializer]
    private static void Init()
    {
        On.RoR2.EliteDef.IsAvailable += EliteDef_IsAvailable;
    }

    private static bool EliteDef_IsAvailable(On.RoR2.EliteDef.orig_IsAvailable orig, EliteDef self)
    {
        return orig(self) && !(self.eliteEquipmentDef && self.eliteEquipmentDef.requiredExpansion && Run.instance && Run.instance.AreExpansionElitesDisabled(self.eliteEquipmentDef.requiredExpansion));
    }
}
