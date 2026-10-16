using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YuJanggi.UI;

namespace YuJanggi.InGame.Views.Chatting
{
    using Cysharp.Threading.Tasks;
    using Engine.Domain;
    using Handler;

    public sealed class ChattingPanel : UIToggle
    {
        [SerializeField] private MessageView    _localMessagePrefab;
        [SerializeField] private MessageView    _remoteMessagePrefab;

        [SerializeField] private ScrollRect     _scrollRect;
        [SerializeField] private TMP_InputField _inputField;

        private PlayerTeam              _localTeam;
        private InGameChattingHandler   _netHandler;
        private void OnEnable()
            => _inputField.onSubmit.AddListener(HandleSendMessage);

        private void OnDisable()
            => _inputField.onSubmit.RemoveListener(HandleSendMessage);
  
        private void OnDestroy()
        {
            if (_netHandler != null)
                _netHandler.OnChatReceived -= HandleChatReceived;
        }
        public void Initialize()
        {
            _inputField.characterLimit = Protocol.Chat.ChatRules.MaxLen;

            if (_netHandler != null || !Store.NetworkSessionStore.HasData)
                return;

            _localTeam = Store.NetworkSessionStore.MyTeam;
            _netHandler = BootStrap.YuJanggiBootStrap.Instance
                .NetworkManager.InGameChat;

            _netHandler.OnChatReceived += HandleChatReceived;
        }
        private void HandleChatReceived(PlayerTeam team, string message)
        {
            if (!IsValidMessage(message))
                return;

            if (team == PlayerTeam.None)
                AddServerMessage(message);
            else if (team == _localTeam)
                AddLocalMessage(message);
            else
                AddRemoteMessage(team, message);
        }

        private async UniTask SendMessageAsync(string message)
        {
            bool accepted = await _netHandler.SendMessageAsync(
                message,
                this.GetCancellationTokenOnDestroy());

            if (accepted)
                ResetInputField();
        }
        public void HandleSendMessage(string message)
        {
            if (_netHandler == null || !IsValidMessage(message))
                return;

            SendMessageAsync(message).Forget();
        }
        public void HandleSendMessage()
            => HandleSendMessage(_inputField.text);

        private static bool IsValidMessage(string message)
            => !string.IsNullOrWhiteSpace(message)
                && message.Length <= Protocol.Chat.ChatRules.MaxLen;
        private void AddServerMessage(string message)
        {
            var view = Instantiate(_remoteMessagePrefab, _scrollRect.content);
            view.SetServerMessage(message);
            view.gameObject.SetActive(true);
            ScrollToBottom();
        }
        private void AddLocalMessage(string message)
        {
            var view = Instantiate(_localMessagePrefab, _scrollRect.content);
            view.SetMessage(_localTeam, message, isLocal: true);
            view.gameObject.SetActive(true);
            ScrollToBottom();
        }

        private void AddRemoteMessage(PlayerTeam team, string message)
        {
            if (!IsValidMessage(message))
                return;

            var view = Instantiate(_remoteMessagePrefab, _scrollRect.content);
            view.SetMessage(team, message, isLocal: false);
            view.gameObject.SetActive(true);
            ScrollToBottom();
        }

        private void ResetInputField()
        {
            _inputField.SetTextWithoutNotify(string.Empty);
            _inputField.ActivateInputField();
        }

        private void ScrollToBottom()
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_scrollRect.content);
            _scrollRect.StopMovement();
            _scrollRect.verticalNormalizedPosition = 0f;
        }
    }
}
