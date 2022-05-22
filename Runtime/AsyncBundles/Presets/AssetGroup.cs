using System;
using System.Linq;
using UnityEngine;

namespace OpenUGD.AsyncBundles.Presets
{
    [Serializable]
    [CreateAssetMenu(menuName = "Assets/Group", fileName = "Group.asset")]
    public class AssetGroup : ScriptableObject
    {
        public bool PackToBundle;
        public PackType PackType;
        public UnloadType UnloadType;
        [Range(0, 10), Tooltip("Seconds")] public float DelayToUnload;
        public AssetGroupPath Path;
        public AssetInfo[] Assets;
        public AssetGroupBuildProcessor[] Processors;
        public AssetGroupBuildCondition[] Conditions;

        public bool CanBuild
        {
            get
            {
                if (PackToBundle)
                {
                    if (Path != null)
                    {
                        if (Conditions == null || Conditions.Length == 0)
                        {
                            return true;
                        }

                        return Conditions.All(c => c == null || c.Success);
                    }
                }

                return false;
            }
        }

        public virtual void OnBundleCopied(string bundlePath, string bundleName)
        {
        }
    }
}
