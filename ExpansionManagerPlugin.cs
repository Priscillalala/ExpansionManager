using BepInEx.Logging;
using System.Security;
using System.Security.Permissions;
using Path = System.IO.Path;

[module: UnverifiableCode]
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace ExpansionManager;

[BepInPlugin(GUID, NAME, VERSION)]
public class ExpansionManagerPlugin : BaseUnityPlugin
{
    public const string
            GUID = "groovesalad." + NAME,
            NAME = "ExpansionManager",
            VERSION = "1.0.0";

    public static new ManualLogSource Logger { get; private set; }

    public void Awake()
    {
        Logger = base.Logger;

        string directoryName = Path.GetDirectoryName(Info.Location);

        ExpansionManagerAssets.assetsPath = Path.Combine(directoryName, "AssetBundles", "expansionmanagerassets.bundle");
        ExpansionManagerAssets.ModInit();

        Language.collectLanguageRootFolders += list => list.Add(Path.Combine(directoryName, "Language"));

        ExpansionRulesCatalog.ModInit();
        ExpansionManagerUI.ModInit();
    }
}
