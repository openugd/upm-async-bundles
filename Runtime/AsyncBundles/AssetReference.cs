using System;
using UnityEngine;
using UnityEngine.U2D;

namespace OpenUGD.AsyncBundles
{
    [Serializable]
    public abstract class AssetReference
    {
        public class Internal
        {
            public static readonly string GuidProperty = nameof(_guid);
            public static readonly string SubNameProperty = nameof(_subName);
        }

#pragma warning disable CS0649
        [SerializeField] private string _guid;
        [SerializeField] private string _subName;
#pragma warning restore CS0649

        public string Guid => _guid;

        public bool HasGuid => !string.IsNullOrEmpty(_guid);

#if UNITY_EDITOR
        public AssetReference SetGuid(string guid)
        {
            _guid = guid;
            return this;
        }
#endif
    }

    [Serializable]
    public class AssetReference<T> : AssetReference where T : UnityEngine.Object
    {
        public AsyncReference<T> LoadAsync(object context = null)
        {
            return AsyncAssets.RetainByGuid<T>(Guid, context);
        }
    }

    [Serializable]
    public class AssetReferenceGameObject : AssetReference<GameObject>
    {
    }

    [Serializable]
    public class AssetReferenceTexture : AssetReference<Texture>
    {
    }

    [Serializable]
    public class AssetReferenceTexture2D : AssetReference<Texture2D>
    {
    }

    [Serializable]
    public class AssetReferenceSprite : AssetReference<Sprite>
    {
    }

    [Serializable]
    public class AssetReferenceSpriteAtlas : AssetReference<SpriteAtlas>
    {
    }
}
