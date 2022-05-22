using System.Collections;
using UnityEngine;

namespace OpenUGD.AsyncBundles.Utils
{
    public class CoroutineObject
    {
        private static CoroutineObject _singleton;

        public static CoroutineObject Singleton() => _singleton ?? (_singleton = new CoroutineObject());

        private GameObject _gameObject;
        private CoroutineProvider _provider;

        private CoroutineObject()
        {
            _gameObject = new GameObject(nameof(CoroutineObject));
            _provider = _gameObject.AddComponent<CoroutineProvider>();

            _gameObject.hideFlags = HideFlags.NotEditable | HideFlags.DontSave;
            GameObject.DontDestroyOnLoad(_gameObject);
        }

        public ICoroutineProvider GetProvider()
        {
            return _provider;
        }

        private class CoroutineProvider : MonoBehaviour, ICoroutineProvider
        {
            public Coroutine StartCoroutine(IEnumerator enumerator, Lifetime lifetime)
            {
                var coroutine = StartCoroutine(enumerator);
                if (coroutine != null)
                {
                    lifetime.AddAction(() => { StopCoroutine(coroutine); });
                }

                return coroutine;
            }
        }
    }
}
