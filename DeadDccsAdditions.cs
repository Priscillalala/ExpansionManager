using MonoMod.Cil;
using Mono.Cecil.Cil;
using RoR2.ExpansionManagement;
using HG;
using UnityEngine.AddressableAssets;

namespace ExpansionManager;

public static class DeadDccsAdditions
{
    const string
        CHAMPIONS = "Champions",
        MINIBOSSES = "Minibosses",
        BASIC_MONSTERS = "Basic Monsters",
        SPECIAL = "Special";

    public static readonly Dictionary<string, Action<DirectorCardCategorySelection>> monsterSelectionAdditions = new()
    {
        { "dccsAncientLoftMonstersDLC1", AncientLoftMonstersDLC1 },
        { "dccsSulfurPoolsMonstersDLC1", SulfurPoolsMonstersDLC1 },
        { "dccsHelminthRoostMonstersDLC2Only", HelminthRoostMonstersDLC2Only },
    };

    public static void AncientLoftMonstersDLC1(DirectorCardCategorySelection dccs)
    {
        dccs.AttemptAddCard(MINIBOSSES, new DirectorCard
        {
            spawnCard = Addressables.LoadAssetAsync<SpawnCard>("RoR2/Base/Bell/cscBell.asset").WaitForCompletion(),
            selectionWeight = 1,
        });
        dccs.AttemptAddCard(BASIC_MONSTERS, new DirectorCard
        {
            spawnCard = Addressables.LoadAssetAsync<SpawnCard>("RoR2/Base/Wisp/cscLesserWisp.asset").WaitForCompletion(),
            selectionWeight = 1,
            preventOverhead = true,
        });
    }

    public static void SulfurPoolsMonstersDLC1(DirectorCardCategorySelection dccs)
    {
        dccs.AttemptAddCard(CHAMPIONS, new DirectorCard
        {
            spawnCard = Addressables.LoadAssetAsync<SpawnCard>("RoR2/Base/MagmaWorm/cscMagmaWorm.asset").WaitForCompletion(),
            selectionWeight = 1,
        });
        dccs.AttemptAddCard(BASIC_MONSTERS, new DirectorCard
        {
            spawnCard = Addressables.LoadAssetAsync<SpawnCard>("RoR2/DLC2/Scorchling/cscScorchling.asset").WaitForCompletion(),
            selectionWeight = 1,
            spawnDistance = DirectorCore.MonsterSpawnDistance.Far,
        });
        dccs.AttemptAddCard(BASIC_MONSTERS, new DirectorCard
        {
            spawnCard = Addressables.LoadAssetAsync<SpawnCard>("RoR2/Base/HermitCrab/cscHermitCrab.asset").WaitForCompletion(),
            selectionWeight = 1,
            spawnDistance = DirectorCore.MonsterSpawnDistance.Far,
            preventOverhead = true,
        });
        dccs.AttemptAddCard(BASIC_MONSTERS, new DirectorCard
        {
            spawnCard = Addressables.LoadAssetAsync<SpawnCard>("RoR2/Base/Lemurian/cscLemurian.asset").WaitForCompletion(),
            selectionWeight = 2,
        });
    }

    public static void HelminthRoostMonstersDLC2Only(DirectorCardCategorySelection dccs)
    {
        dccs.AttemptAddCard(BASIC_MONSTERS, new DirectorCard
        {
            spawnCard = Addressables.LoadAssetAsync<SpawnCard>("RoR2/Base/Imp/cscImp.asset").WaitForCompletion(),
            selectionWeight = 2,
        });
    }

    public static bool AttemptAddCard(this DirectorCardCategorySelection dccs, string categoryName, DirectorCard card)
    {
        int categoryIndex = dccs.FindCategoryIndexByName(categoryName);
        if (ArrayUtils.IsInBounds(dccs.categories, categoryIndex))
        {
            dccs.AddCard(categoryIndex, card);
            return true;
        }
        return false;
    }

    public static void Init()
    {
        IL.RoR2.ClassicStageInfo.RebuildCards += ClassicStageInfo_RebuildCards;
    }

    private static void ClassicStageInfo_RebuildCards(ILContext il)
    {
        ILCursor c = new ILCursor(il);
        if (c.TryGotoNext(MoveType.After,
            x => x.MatchStfld<ClassicStageInfo>(nameof(ClassicStageInfo.modifiableMonsterCategories)))
            )
        {
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Action<ClassicStageInfo>>(classicStageInfo =>
            {
                if (classicStageInfo.modifiableMonsterCategories)
                {
                    string name = classicStageInfo.modifiableMonsterCategories.name;
                    if (name.EndsWith("(Clone)"))
                    {
                        name = name.Remove(name.Length - 7);
                    }
                    if (monsterSelectionAdditions.TryGetValue(name, out var addCards))
                    {
                        ExpansionDef requiredExpansion = SceneInfo.instance?.sceneDef?.requiredExpansion;
                        if (requiredExpansion && Run.instance.ExpansionHasMonstersDisabled(requiredExpansion))
                        {
                            ExpansionManagerPlugin.Logger.LogInfo($"{nameof(DeadDccsAdditions)}: {requiredExpansion.name} has monsters disabled, Adding monster cards to {name}");
                            addCards(classicStageInfo.modifiableMonsterCategories);
                        }
                    }
                }
            });
        }
        else ExpansionManagerPlugin.Logger.LogError($"{nameof(DeadDccsAdditions)}: {nameof(ClassicStageInfo_RebuildCards)} IL match failed");
    }
}
