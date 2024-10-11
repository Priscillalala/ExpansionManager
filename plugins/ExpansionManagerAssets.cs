namespace ExpansionManager;

public static class ExpansionManagerAssets
{
    public static string assetsPath;
    private static AssetBundleCreateRequest assetBundleCreateRequest;
    private static AssetBundle assetBundle;

    public static AssetBundle Bundle
    {
        get
        {
            if (!assetBundle)
            {
                assetBundle = assetBundleCreateRequest?.assetBundle;
            }
            return assetBundle;
        }
    }

    public static void ModInit()
    {
        assetBundleCreateRequest = AssetBundle.LoadFromFileAsync(assetsPath);
    }
}
