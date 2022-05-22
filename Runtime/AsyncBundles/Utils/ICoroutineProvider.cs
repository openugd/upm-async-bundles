using System.Collections;
using UnityEngine;

namespace OpenUGD.AsyncBundles.Utils
{
    public interface ICoroutineProvider
    {
        Coroutine StartCoroutine(IEnumerator enumerator, Lifetime lifetime);
        Coroutine StartCoroutine(IEnumerator enumerator);
        void StopCoroutine(Coroutine coroutine);
    }
}
