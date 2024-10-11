using RoR2.ExpansionManagement;
using System.Collections;
using UnityEngine.AddressableAssets;

namespace ExpansionManager.Fixes;

// The Shrine of Shaping has no expansion requirement
// This is presumably an oversight so we give it one
public static class ShapingShrineExpansionRequirement
{
    [SystemInitializer]
    private static IEnumerator Init()
    {
        var load_ShrineColossusAccess = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC2/ShrineColossusAccess.prefab");
        var load_DLC2 = Addressables.LoadAssetAsync<ExpansionDef>("RoR2/DLC2/Common/DLC2.asset");
        while (!load_ShrineColossusAccess.IsDone)
        {
            yield return null;
        }
        GameObject ShrineColossusAccess = load_ShrineColossusAccess.Result;
        if (!ShrineColossusAccess || ShrineColossusAccess.GetComponent<ExpansionRequirementComponent>())
        {
            yield break;
        }
        ExpansionRequirementComponent expansionRequirement = ShrineColossusAccess.AddComponent<ExpansionRequirementComponent>();
        while (!load_DLC2.IsDone)
        {
            yield return null;
        }
        expansionRequirement.requiredExpansion = load_DLC2.Result;
    }
}
