using RoR2.ExpansionManagement;

namespace ExpansionManager.Util;

public static class EquipmentUtil
{
    public static bool IsElite(this EquipmentDef equipmentDef)
    {
        return equipmentDef.passiveBuffDef && equipmentDef.passiveBuffDef.isElite;
    }
}
