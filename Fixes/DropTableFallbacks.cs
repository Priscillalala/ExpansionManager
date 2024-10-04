namespace ExpansionManager.Fixes;

// If a drop table has no available drops, we pull from a secondary drop table
// to ensure that the player always gets something

public static class DropTableFallbacks
{
    public static readonly Dictionary<string, string> fallbacksDictionary = new()
    {
        { "dtVoidChest", "dtChest1" },
        { "PrismDroptable", "dtCategoryChest2Damage" }, // Rulers of the Red Plane drop table that is unused as of 0.1.5
        { string.Empty, "dtChest1" }, // Rulers of the Red Plane bloody prism drop table as of 0.1.5
    };

    public static bool DropTableFallback(PickupDropTable dropTable, out PickupDropTable fallbackDropTable)
    {
        if (dropTable.GetPickupCount() == 0 && fallbacksDictionary.TryGetValue(dropTable.name, out string fallbackName))
        {
            return fallbackDropTable = PickupDropTable.instancesList.Find(x => x.name == fallbackName);
        }
        fallbackDropTable = null;
        return false;
    }

    [SystemInitializer]
    private static void Init()
    {
        On.RoR2.PickupDropTable.GenerateDrop += PickupDropTable_GenerateDrop;
        On.RoR2.PickupDropTable.GenerateUniqueDrops += PickupDropTable_GenerateUniqueDrops;
    }

    private static PickupIndex[] PickupDropTable_GenerateUniqueDrops(On.RoR2.PickupDropTable.orig_GenerateUniqueDrops orig, PickupDropTable self, int maxDrops, Xoroshiro128Plus rng)
    {
        if (DropTableFallback(self, out PickupDropTable fallbackDropTable))
        {
            return fallbackDropTable.GenerateUniqueDrops(maxDrops, rng);
        }
        return orig(self, maxDrops, rng);
    }

    private static PickupIndex PickupDropTable_GenerateDrop(On.RoR2.PickupDropTable.orig_GenerateDrop orig, PickupDropTable self, Xoroshiro128Plus rng)
    {
        if (DropTableFallback(self, out PickupDropTable fallbackDropTable))
        {
            return fallbackDropTable.GenerateDrop(rng);
        }
        return orig(self, rng);
    }
}
