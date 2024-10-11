using RoR2.ExpansionManagement;

namespace ExpansionManager.Util;

public static class PickupUtil
{
    public static ExpansionDef GetRequiredExpansion(this PickupDef pickupDef)
    {
        ExpansionDef requiredExpansion = null;
        if (pickupDef.itemIndex != ItemIndex.None)
        {
            ItemDef itemDef = ItemCatalog.GetItemDef(pickupDef.itemIndex);
            if (itemDef)
            {
                requiredExpansion = itemDef.requiredExpansion;
            }
        }
        else if (pickupDef.equipmentIndex != EquipmentIndex.None)
        {
            EquipmentDef equipmentDef = EquipmentCatalog.GetEquipmentDef(pickupDef.equipmentIndex);
            if (equipmentDef)
            {
                requiredExpansion = equipmentDef.requiredExpansion;
            }
        }
        else if (pickupDef.artifactIndex != ArtifactIndex.None)
        {
            ArtifactDef artifactDef = ArtifactCatalog.GetArtifactDef(pickupDef.artifactIndex);
            if (artifactDef)
            {
                requiredExpansion = artifactDef.requiredExpansion;
            }
        }
        return requiredExpansion;
    }
}
