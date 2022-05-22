using UnityEngine;
using UnityEngine.UI;

namespace OpenUGD.AsyncBundles
{
    [RequireComponent(typeof(Image))]
    public class AsyncImageSprite : AsyncSprite
    {
        public Image Image;
        public bool SetNativeSize;
        private Sprite _restoreSprite;

        private void Awake()
        {
            if (Image == null)
            {
                Image = GetComponent<Image>();
            }
        }

        protected override void OnSetSprite(Sprite sprite)
        {
            if (RestoreSprite)
            {
                _restoreSprite = Image.sprite;
            }

            Image.sprite = sprite;
            if (SetNativeSize)
            {
                Image.SetNativeSize();
            }
        }

        protected override void OnUnsetSprite()
        {
            if (RestoreSprite)
            {
                Image.sprite = _restoreSprite;

                if (SetNativeSize)
                {
                    Image.SetNativeSize();
                }
            }
            else
            {
                Image.sprite = null;
            }
        }
    }
}
