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
        void Start()
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
