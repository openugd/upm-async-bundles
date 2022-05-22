using System;
using System.Collections.Generic;

namespace OpenUGD.AsyncBundles.Manifests
{
    public class AssetManifestUtils
    {
        public struct Progress
        {
            public float Percent;
            public string Message;
            public bool IsRecursion;
            public Queue<string> Bundles;
        }

        public static bool HasRecursion(AssetsManifest manifest, IProgress<Progress> progress = null)
        {
            var bundles = new Dictionary<string, Bundle>();
            foreach (var bundle in manifest.Bundles)
            {
                bundles[bundle.Name] = bundle;
            }

            var hashSet = new HashSet<string>();
            var stack = new Queue<string>();
            var total = manifest.Bundles.Length;
            for (var index = 0; index < manifest.Bundles.Length; index++)
            {
                var bundle = manifest.Bundles[index];
                var percent = index / (float) total;

                hashSet.Clear();
                hashSet.Add(bundle.Name);

                stack.Clear();
                stack.Enqueue(bundle.Name);

                if (HasRecursionDependencies(bundle, hashSet, stack, bundles))
                {
                    if (progress != null)
                    {
                        progress.Report(new Progress
                            {Percent = percent, Message = bundle.Name, IsRecursion = true, Bundles = stack});
                    }

                    return true;
                }

                if (progress != null)
                {
                    progress.Report(new Progress
                    {
                        Percent = percent,
                        Message = bundle.Name
                    });
                }
            }

            return false;
        }

        private static bool HasRecursionDependencies(Bundle bundle, HashSet<string> hashSet, Queue<string> stack,
            Dictionary<string, Bundle> bundles)
        {
            if (bundle.Dependencies != null)
            {
                foreach (var dependency in bundle.Dependencies)
                {
                    if (hashSet.Contains(dependency))
                    {
                        stack.Enqueue(dependency);

                        return true;
                    }


                    var localHashSet = new HashSet<string>();
                    foreach (string s in hashSet)
                    {
                        localHashSet.Add(s);
                    }

                    localHashSet.Add(dependency);

                    var localStack = new Queue<string>();

                    localStack.Enqueue(dependency);

                    var dependBundle = bundles[dependency];
                    if (HasRecursionDependencies(dependBundle, localHashSet, localStack, bundles))
                    {
                        foreach (string s in localHashSet)
                        {
                            hashSet.Add(s);
                        }

                        foreach (string s in localStack)
                        {
                            stack.Enqueue(s);
                        }


                        return true;
                    }
                }
            }

            return false;
        }
    }
}
