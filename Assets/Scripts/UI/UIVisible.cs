

using UnityEngine;

namespace YuJanggi.UI
{
    public class UIVisible : MonoBehaviour
    {
        private void Awake()
        {
            Close();
        }

        public void Open()
        {
            gameObject.SetActive(true);
            OnOpen();
        }

        public void Close()
        {
            OnClose();
            gameObject.SetActive(false);
        }

        protected virtual void OnOpen()
        {
        }

        protected virtual void OnClose()
        {
        }
    }
}


