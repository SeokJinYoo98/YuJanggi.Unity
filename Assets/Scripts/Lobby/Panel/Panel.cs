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
        void Close();
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

        public void Close()
        {
            if (CanClosePanel is false)
                return;

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
