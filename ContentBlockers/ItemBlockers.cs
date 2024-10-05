using ExpansionManager.Util;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2.ExpansionManagement;

namespace ExpansionManager.ContentBlockers;

public static class ItemBlockers
{
    [SystemInitializer]
    private static void Init()
    {
        CharacterBody.onBodyAwakeGlobal += CharacterBody_onBodyAwakeGlobal;
        On.RoR2.ExplicitPickupDropTable.GenerateWeightedSelection += ExplicitPickupDropTable_GenerateWeightedSelection;
        IL.RoR2.PreGameController.RecalculateModifierAvailability += PreGameController_RecalculateModifierAvailability;
    }

    private static void CharacterBody_onBodyAwakeGlobal(CharacterBody body)
    {
        if (body.TryGetComponent(out DeathRewards deathRewards))
        {
            if (deathRewards.bossDropTable && deathRewards.bossDropTable.GetPickupCount() == 0)
            {
                deathRewards.bossDropTable = null;
            }
            PickupDef pickupDef = PickupCatalog.GetPickupDef((PickupIndex)deathRewards.bossPickup);
            if (pickupDef != null && (pickupDef.itemIndex != ItemIndex.None || pickupDef.equipmentIndex != EquipmentIndex.None))
            {
                ExpansionDef requiredExpansion = pickupDef.GetRequiredExpansion();
                if (requiredExpansion && Run.instance.AreExpansionItemsDisabled(requiredExpansion))
                {
                    deathRewards.bossPickup = default;
                }
            }
        }
    }

    private static void ExplicitPickupDropTable_GenerateWeightedSelection(On.RoR2.ExplicitPickupDropTable.orig_GenerateWeightedSelection orig, ExplicitPickupDropTable self)
    {
        orig(self);
        if (!Run.instance)
        {
            return;
        }
        for (int i = self.weightedSelection.Count; i >= 0; i--)
        {
            PickupIndex pickupIndex = self.weightedSelection.GetChoice(i).value;
            PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
            if (pickupDef != null && (pickupDef.itemIndex != ItemIndex.None || pickupDef.equipmentIndex != EquipmentIndex.None))
            {
                ExpansionDef requiredExpansion = pickupDef.GetRequiredExpansion();
                if (requiredExpansion && Run.instance.AreExpansionItemsDisabled(requiredExpansion))
                {
                    self.weightedSelection.RemoveChoice(i);
                }
            }
        }
    }

    private static void PreGameController_RecalculateModifierAvailability(ILContext il)
    {
        ILCursor c = new ILCursor(il);
        int locChoiceDefIndex = -1;
        if (c.TryGotoNext(MoveType.After,
            x => x.MatchLdloc(out locChoiceDefIndex),
            x => x.MatchLdfld<RuleChoiceDef>(nameof(RuleChoiceDef.requiredExpansionDef)),
            x => x.MatchLdfld<ExpansionDef>(nameof(ExpansionDef.enabledChoice)),
            x => x.MatchCallOrCallvirt<RuleBook>(nameof(RuleBook.IsChoiceActive)))
            )
        {
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc, locChoiceDefIndex);
            c.EmitDelegate<Func<bool, PreGameController, RuleChoiceDef, bool>>((result, preGameController, choiceDef) =>
            {
                bool IsValidItem()
                {
                    return choiceDef.itemIndex != ItemIndex.None;
                }
                bool IsValidEquipment()
                {
                    if (choiceDef.equipmentIndex == EquipmentIndex.None)
                    {
                        return false;
                    }
                    EquipmentDef equipmentDef = EquipmentCatalog.GetEquipmentDef(choiceDef.equipmentIndex);
                    return equipmentDef && !equipmentDef.IsElite();
                }

                if (IsValidItem() || IsValidEquipment())
                {
                    return result && !ExpansionRulesCatalog.IsExpansionContentDisabled(preGameController.readOnlyRuleBook, choiceDef.requiredExpansionDef, ExpansionRulesCatalog.disableExpansionItemsChoices);
                }
                return result;
            });
        }
        else ExpansionManagerPlugin.Logger.LogError($"{nameof(ItemBlockers)}: {nameof(PreGameController_RecalculateModifierAvailability)} IL match failed");
    }
}
