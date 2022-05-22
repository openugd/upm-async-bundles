using UnityEngine;

namespace OpenUGD.AsyncBundles
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class AsyncSpriteRendererSprite : AsyncSprite
    {
        public SpriteRenderer Renderer;
        private Sprite _restoreSprite;

        private void Awake()
        {
            if (Renderer == null)
            {
                Renderer = GetComponent<SpriteRenderer>();
            }
        }

        protected override void OnSetSprite(Sprite sprite)
        {
            _restoreSprite = sprite;
            Renderer.sprite = sprite;
        }

        protected override void OnUnsetSprite()
        {
            if (RestoreSprite)
            {
                Renderer.sprite = _restoreSprite;
            }
            else
            {
                Renderer.sprite = null;
            }
        }
    }
}