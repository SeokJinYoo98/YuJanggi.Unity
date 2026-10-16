using UnityEngine;

using YuJanggi.UI;

namespace YuJanggi.InGame.Views.Chatting
{
    public sealed class ChattingPanel : UIToggle
    {
        [SerializeField] private MessageView _localMessagePrefab;
        [SerializeField] private MessageView _remoteMessagePrefab;
        [SerializeField] private UnityEngine.UI.ScrollRect _scrollRect;
        public void HandleSendMessage()
        {

        }
    }
}
