using System;
using UnityEngine;

namespace OpenUGD.AsyncBundles.Bundles.Loaders
{
    public interface IAsyncBundleLoader
    {
        void SubscribeOnComplete(Lifetime lifetime, Action<IAsyncBundleLoader> listener);
        void Load(string url, string hash);
        float Progress { get; }
        bool IsLoading { get; }
        bool IsLoaded { get; }
        void Unload(bool unloadAllLoadedObjects);
        AssetBundle Bundle { get; }
        Func<IAsyncBundleLoader> Next { get; set; }
    }
}
