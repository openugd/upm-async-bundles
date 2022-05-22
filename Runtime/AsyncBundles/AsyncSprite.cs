using System;
using UnityEngine;

namespace OpenUGD.AsyncBundles
{
    public abstract class AsyncSprite : MonoBehaviour, IProgressProvider
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
            Resolved,
            Fail
        }

        private Lifetime.Definition _definition;
        private Signal<LoadState> _onChange;
        private AsyncReference<Sprite> _reference;
        private LoadState _state = LoadState.Unloaded;

        public LinkType Type;
        public AssetReferenceSprite Reference;
        public string Name;
        [Tooltip("restore last sprite")] public bool RestoreSprite;

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

        public AsyncReference<Sprite> AsyncReference => _reference;

        public float Progress => _reference != null ? _reference.Progress : 0f;

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

        void OnDestroy()
        {
            if (_definition != null)
            {
                _definition.Terminate();
            }
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

        void UnloadInternal()
        {
            if (_reference != null)
            {
                _reference.Dispose();
            }
        }

        public void Unload()
        {
            UnloadInternal();
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
                _reference = AsyncAssets.RetainByName<Sprite>(Name, this);
            }

            var sprite = await _reference.Task;
            if (sprite != null)
            {
                State = LoadState.Loaded;
                OnSetSprite(sprite);
                State = LoadState.Resolved;

                _reference.Lifetime.AddAction(OnUnsetSprite);
            }
            else if (_reference.IsFail)
            {
                State = LoadState.Fail;
            }
        }

        protected abstract void OnSetSprite(Sprite sprite);
        protected abstract void OnUnsetSprite();
    }
}
