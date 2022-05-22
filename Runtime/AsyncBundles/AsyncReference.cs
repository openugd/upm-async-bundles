using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenUGD.AsyncBundles.Bundles;
using UnityEngine;

namespace OpenUGD.AsyncBundles
{
    public class AsyncReference<T> : AsyncReference, IProgressProvider
    {
        public static AsyncReference<T> Resolve(AsyncReference<T> reference, T result)
        {
            reference.ResolveResult(result);
            return reference;
        }

        public static AsyncReference<T> Fail(AsyncReference<T> reference)
        {
            reference.ResolveFail();
            return reference;
        }

        private readonly IProgressProvider _progressProvider;
        private readonly TaskCompletionSource<T> _taskCompletionSource;
        private readonly Signal<T> _onResolved;

        public AsyncReference(IProgressProvider progressProvider, object context)
        {
            _progressProvider = progressProvider;
            Context = context ?? this;
            _onResolved = new Signal<T>(Lifetime);
            _taskCompletionSource = new TaskCompletionSource<T>();
            Task = _taskCompletionSource.Task;
            Lifetime.AddAction(() =>
            {
                if (!Task.IsCompleted)
                {
                    _taskCompletionSource.SetResult(default);
                }
            });
        }

        public float Progress => _progressProvider.Progress;
        public object Context { get; private set; }
        public Task<T> Task { get; private set; }
        public T Result { get; private set; }
        public bool IsResolved { get; private set; }
        public bool IsFail { get; private set; }

        public void ExecuteOnResolved(Lifetime lifetime, Action<T> listener)
        {
            if (IsResolved)
            {
                listener(Result);
            }
            else
            {
                _onResolved.Subscribe(lifetime, listener);
            }
        }

        private void ResolveResult(T value)
        {
            if (IsResolved)
            {
                Debug.LogException(new InvalidOperationException(
                    $"{this.GetType().Name} attempt to resolve with value:'{value}' (fail:{IsFail}) when it had already resolved with value:'{Result}'!"));
                return;
            }

            Result = value;
            IsFail = false;
            IsResolved = true;
            _taskCompletionSource.SetResult(value);
            _onResolved.Fire(value);
        }

        private void ResolveFail()
        {
            if (IsResolved)
            {
                Debug.LogException(new InvalidOperationException(
                    $"{this.GetType().Name} attempt to resolve with fail when it had already resolved with value:'{Result}'!"));
                return;
            }

            Debug.LogError($"fail {GetType().Name}");
            Result = default;
            IsFail = true;
            IsResolved = true;
            _taskCompletionSource.SetResult(default);
            _onResolved.Fire(default);
        }
    }

    public class AsyncReference : IDisposable
    {
        private readonly Lifetime.Definition _definition;

        public AsyncReference()
        {
            _definition = Lifetime.Eternal.DefineNested();
        }

        public Lifetime Lifetime => _definition.Lifetime;

        public void Dispose()
        {
            _definition.Terminate();
        }

        internal class LoaderInfoProgressProvider : IProgressProvider
        {
            public AsyncBundle.ILoaderInfo LoaderInfo;
            public float Progress => LoaderInfo != null ? LoaderInfo.Progress : 0f;
        }

        internal class GroupProgressProvider : IProgressProvider
        {
            public List<IProgressProvider> Providers;
            public float Progress => Providers != null ? Providers.Sum(p => p.Progress) / Providers.Count : 0f;
        }

        internal class ProgressProvider : IProgressProvider
        {
            public float Progress { get; set; }
        }
    }

    public interface IProgressProvider
    {
        float Progress { get; }
    }
}
