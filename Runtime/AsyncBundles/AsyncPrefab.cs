using System;
using UnityEngine;

namespace OpenUGD.AsyncBundles
{
    [ExecuteInEditMode]
    public class AsyncPrefab : MonoBehaviour, IProgressProvider
    {
        public enum LinkType
        {
            Reference = 0,
            Name = 1
        }

        public enum LoadState
        {
            Unloaded,
            Loading,
            Loaded,
            Fail
        }

        private Lifetime.Definition _definition;
        private Signal<LoadState> _onChange;
        private AsyncReference<GameObject> _reference;
        private LoadState _state = LoadState.Unloaded;

        public LinkType Type;
        public AssetReferenceGameObject Reference;
        public string Name;

        public LoadState State
        {
            get => _state;
            private set
            {
                if (_state != value)
                {
                    _state = value;

                    if (_onChange != null)
                    {
                        _onChange.Fire(value);
                    }
                }
            }
        }

        public float Progress => _reference != null ? _reference.Progress : 0f;
        public AsyncReference<GameObject> AsyncReference => _reference;

        public void Subscribe(Lifetime lifetime, Action<LoadState> listener)
        {
            if (_onChange == null)
            {
                _definition = Lifetime.Eternal.DefineNested(name);
                _onChange = new Signal<LoadState>(_definition.Lifetime);
            }

            _onChange.Subscribe(lifetime, listener);
        }

        void OnEnable()
        {
            Load();
        }

        void OnDisable()
        {
            Unload();
        }

        bool IsRuntime
        {
            get
            {
                var isRuntime = true;
#if UNITY_EDITOR
                isRuntime = Application.isPlaying;
#endif
                return isRuntime;
            }
        }

        public void Unload()
        {
            if (_reference != null)
            {
                _reference.Dispose();
            }

            State = LoadState.Unloaded;
        }

        public async void Load()
        {
            Unload();

            if (Type == LinkType.Reference)
            {
                if (Reference == null)
                {
                    State = LoadState.Fail;
                    return;
                }

                if (!Reference.HasGuid)
                {
                    Debug.LogError($"{nameof(Reference)}:{gameObject.name} is not assigned, but you try load it");
                    State = LoadState.Fail;
                    return;
                }

                State = LoadState.Loading;
                _reference = Reference.LoadAsync(this);
            }
            else if (Type == LinkType.Name)
            {
                State = LoadState.Loading;
                _reference = AsyncAssets.RetainByName<GameObject>(Name, this);
            }

            var prefab = await _reference.Task;
            if (prefab != null)
            {
                var instance = Instantiate(prefab, transform, false);
                if (!IsRuntime)
                {
                    instance.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
                }

                State = LoadState.Loaded;

                _reference.Lifetime.AddAction(() =>
                {
                    if (IsRuntime)
                    {
                        Destroy(instance);
                    }
                    else
                    {
                        DestroyImmediate(instance);
                    }
                });
            }
            else if (_reference.IsFail)
            {
                State = LoadState.Fail;
            }
        }
    }
}
