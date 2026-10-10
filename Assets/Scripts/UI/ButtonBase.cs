

using UnityEngine;
using YuJanggi.Audio;
using YuJanggi.BootStrap;

namespace YuJanggi.UI
{
    [RequireComponent(typeof(UnityEngine.UI.Button))]
    public sealed class ButtonBase : MonoBehaviour
    {
        private UnityEngine.UI.Button _button;

        private void Awake()
            => _button = GetComponent<UnityEngine.UI.Button>();

        private void OnEnable()
            => _button.onClick.AddListener(PlayClickSound);

        private void OnDisable()
            => _button.onClick.RemoveListener(PlayClickSound);

        private void PlayClickSound()
            => YuJanggiBootStrap.Instance.AudioManager.PlayUI(UISfx.Button);
    }
}
