using Cysharp.Threading.Tasks;
using UnityEngine;

namespace YuJanggi.Lobby.Panel
{
    public interface IGameStartPanel
    {
        UniTask<bool> PrepareGameAsync();
    }
    public interface IPanel
    {
        void Open();
        bool Close();
    }
    public abstract class Panel : MonoBehaviour, IPanel
    {
        protected bool CanClosePanel = true;
        protected virtual void Start()
        {
            gameObject.SetActive(false);
        }
        public void Open()
        {
            gameObject.SetActive(true);
            OnOpen();
        }

        public bool Close()
        {
            if (!CanClosePanel)
                return false;

            OnClose();
            gameObject.SetActive(false);
            return true;
        }

        protected virtual void OnOpen()
        {
        }

        protected virtual void OnClose()
        {
        }
    }
}
