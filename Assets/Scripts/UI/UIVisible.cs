

using UnityEngine;
using YuJanggi.Audio;
using YuJanggi.BootStrap;

namespace YuJanggi.UI
{
    public class UIVisible : MonoBehaviour
    {
        protected virtual void Awake()
            => Close();
        public void Open()
            => gameObject.SetActive(true);

        public void Close()
            => gameObject.SetActive(false);

        protected static AudioManager Audio
            => YuJanggiBootStrap.Instance.AudioManager;
    }
}


