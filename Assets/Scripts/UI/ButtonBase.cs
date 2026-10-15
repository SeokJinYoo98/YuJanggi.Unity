

using UnityEngine;
using YuJanggi.Audio;
using YuJanggi.BootStrap;

namespace YuJanggi.UI
{
    [RequireComponent(typeof(UnityEngine.UI.Button))]
    public sealed class ButtonBase : MonoBehaviour
    {
        [SerializeField] private bool _keepPressed;
        [SerializeField] private TMPro.TMP_Text _text;


        private UnityEngine.UI.Button _button;

        private void Awake()
            => _button = GetComponent<UnityEngine.UI.Button>();

        private void OnEnable()
            => _button.onClick.AddListener(HandleClick);

        private void OnDisable()
            => _button.onClick.RemoveListener(HandleClick);

        private void HandleClick()
        {
            YuJanggiBootStrap.Instance.AudioManager.PlayUI(UISfx.Button);

            if (_keepPressed)
                _button.interactable = false;
        }

        public void ResetButton()
            => _button.interactable = true;
        public void SetText(string text)
            => _text.SetText(text);
    }
}
