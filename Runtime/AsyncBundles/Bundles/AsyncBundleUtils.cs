namespace OpenUGD.AsyncBundles.Bundles
{
    public static class AsyncBundleUtils
    {
        public static bool IsWWW(string value)
        {
            return value.StartsWith("http://") || value.StartsWith("https://");
        }

        public static bool IsStreamingAssets(string value)
        {
            return value.StartsWith("/") || value.Contains(":/");
        }
    }
}
