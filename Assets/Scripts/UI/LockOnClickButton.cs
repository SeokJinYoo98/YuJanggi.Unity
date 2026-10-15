using UnityEngine;

namespace YuJanggi.UI
{
    public sealed class LockOnClickButton : ButtonBase
    {
        private Sprite _defaultSprite;

        protected override void Awake()
        {
            base.Awake();
            _defaultSprite = _button.image.sprite;
        }

        protected override void HandleClick()
        {
            base.HandleClick();
            _button.interactable = false;
        }

        public void ResetButton()
        {
            _button.interactable = true;
            _button.image.overrideSprite = null;
            _button.image.sprite = _defaultSprite;
        }
    }
}
