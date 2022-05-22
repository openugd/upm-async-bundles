using System;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace OpenUGD.AsyncBundles.Bundles.Loaders
{
    public class WWWBundleLoader : IDisposable
    {
        private bool _isDisposed;
        private UnityWebRequest _loader;
        private UnityWebRequestAsyncOperation _request;
        private bool _isDone;
        private bool _isHttpError;
        private bool _isNetworkError;
        private long _responseCode;
        private string _error;
        private float _progress;
        private AssetBundle _bundle;

        public WWWBundleLoader(string url, string hash, int timeout)
        {
            _loader = UnityWebRequestAssetBundle.GetAssetBundle(url, Hash128.Parse(hash));
#if !UNITY_2019_1_OR_NEWER
            _loader.chunkedTransfer = false;
#endif
            _loader.useHttpContinue = false;

            _loader.timeout = timeout;
        }

        public float Progress => UpdateProperties()._progress;

        public bool IsDone => UpdateProperties()._isDone;

        public bool IsHttpError => UpdateProperties()._isHttpError;

        public bool IsNetworkError => UpdateProperties()._isNetworkError;

        public bool IsError => IsNetworkError || IsHttpError;

        public long ResponseCode => UpdateProperties()._responseCode;

        public string Error => UpdateProperties()._error;

        public AssetBundle Bundle => UpdateProperties()._bundle;

        public bool IsDisposed => UpdateProperties()._isDisposed;

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                _bundle = null;

                try
                {
                    _loader.Dispose();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

                _loader = null;
                _request = null;
            }
        }

        private WWWBundleLoader UpdateProperties()
        {
            if (_loader != null && !_isDisposed)
            {
                if (_request == null)
                {
                    try
                    {
                        _request = _loader.SendWebRequest();
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        return this;
                    }

                    if (_request == null)
                    {
                        Debug.LogError($"{GetType().FullName}.{nameof(UpdateProperties)} request is null");
                    }
                }

                if (_request != null)
                {
                    _isDone = _loader.isDone && _request.isDone;
                    _progress = (_loader.downloadProgress + _request.progress) / 2f;
                }

                //Debug.Log($"WWWBundleLoader.UpdateProperties url {_loader.url}, response headers [ {ResponseHeaders()} ]");

                if (_isDone)
                {
                    _isHttpError = _loader.isHttpError;
                    _isNetworkError = _loader.isNetworkError;
                    _responseCode = _loader.responseCode;
                    _error = _loader.error;

                    if (!_isNetworkError && !_isHttpError && _bundle == null)
                    {
                        try
                        {
                            _bundle = DownloadHandlerAssetBundle.GetContent(_loader);
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                        }
                    }
                }
            }

            return this;
        }

        private string ResponseHeaders()
        {
            if (_loader == null)
            {
                return "Loader is null";
            }

            var dict = _loader.GetResponseHeaders();
            if (dict == null)
            {
                return "No headers";
            }

            var sb = new StringBuilder();

            foreach (var kv in dict)
            {
                sb.Append(kv.Key);
                sb.Append(" : ");
                sb.Append(kv.Value);
                sb.Append(", ");
            }

            return sb.ToString();
        }
    }
}
