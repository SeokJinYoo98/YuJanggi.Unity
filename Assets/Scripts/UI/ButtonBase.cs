

using UnityEngine;
using YuJanggi.Audio;
using YuJanggi.BootStrap;

namespace YuJanggi.UI
{
    [RequireComponent(typeof(UnityEngine.UI.Button))]
    public class ButtonBase : MonoBehaviour
    {
        [SerializeField] private TMPro.TMP_Text _text;


        protected UnityEngine.UI.Button _button;

        protected virtual void Awake()
            => _button = GetComponent<UnityEngine.UI.Button>();

        protected virtual void OnEnable()
            => _button.onClick.AddListener(HandleClick);

        protected virtual void OnDisable()
            => _button.onClick.RemoveListener(HandleClick);

        protected virtual void HandleClick()
        {
            YuJanggiBootStrap.Instance.AudioManager.PlayUI(UISfx.Button);
        }

        public void SetText(string text)
            => _text.SetText(text);
    }
}
