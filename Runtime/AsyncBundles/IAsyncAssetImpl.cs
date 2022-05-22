using System;
using System.Collections.Generic;
using OpenUGD.AsyncBundles.Manifests;

namespace OpenUGD.AsyncBundles
{
    public interface IAsyncAssetImpl
    {
        bool IsReady { get; }
        void ExecuteOnReady(Lifetime lifetime, Action<IManifestProvider> listener);
        void Release(AsyncReference reference);

        AsyncReference<List<AsyncReference<T>>> RetainByFilter<T>(AssetFilterFunc filter,
            object context = null) where T : UnityEngine.Object;

        AsyncReference<List<AsyncReference<T>>> RetainByTag<T>(string tag, object context = null)
            where T : UnityEngine.Object;

        AsyncReference<T> RetainByName<T>(string name, object context = null) where T : UnityEngine.Object;
        AsyncReference<T> RetainByGuid<T>(string guid, object context = null) where T : UnityEngine.Object;
    }
}
