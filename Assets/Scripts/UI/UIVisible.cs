

using UnityEngine;

namespace YuJanggi.UI
{
    public class UIVisible : MonoBehaviour
    {
        private void Awake()
        {
            Hide();
        }
        public virtual void Show()
            => gameObject.SetActive(true);
        public virtual void Hide()
            => gameObject.SetActive(false);
    }
}


