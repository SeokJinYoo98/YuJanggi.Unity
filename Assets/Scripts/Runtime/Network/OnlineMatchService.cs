using System;
using UnityEngine;
using Cysharp.Threading.Tasks;


namespace YuJanggi.Runtime.Network
{
    using Runtime.Network.Protocol;
    using YuJanggiCommon;

    public enum OnlineMatchState
    {
        Local,
        Connecting, Connected, ConnectionFailed,
        MatchMaking, MatchFound
    }
    public sealed class OnlineMatchService : IDisposable
    {
        public OnlineMatchService(IOnlineGameClient client)
        {
            _client = client
                ?? throw new ArgumentNullException(nameof(client));

            RegisterOnlineEvents();
        }
        public void Dispose()
        {
            UnregisterOnlineEvents();
        }




        private readonly IOnlineGameClient _client;
        private OnlineMatchState           _state = OnlineMatchState.Local;
        public event Action<OnlineMatchState> OnStateChanged;

        #region Server Message Handlers
        private void RegisterOnlineEvents()
        {
            _client.OnMessageReceived += HandleServerMessage;
        }
        private void UnregisterOnlineEvents()
        {
            _client.OnMessageReceived -= HandleServerMessage;
        }
        private void HandleServerMessage(ChatMessage message)
        {
            /*
            Join,
            MatchmakingStart,
            MatchmakingCancel,
            MatchmakingStatus,
            MatchFound,
            GameChatSend,
            GameChatReceived,
            GameStart,
            LegalMovesRequest,
            LegalMovesResult,
            MoveRequest,
            MoveResult,
            TurnChanged,
            GameEnd,
            Error,
            SelectFormation,
            FormationSelected
             */
            switch (message.Type)
            {
                case MessageType.Join:
                    Debug.Log("Server: Join message.");
                    ChangeState(OnlineMatchState.Connected);
                    break;
                case MessageType.MatchmakingStart:
                    Debug.Log("Server: MatchmakingStart.");
                   
                    break;
                case MessageType.MatchFound:
                    Debug.Log("Server: MatchFound.");
                    ChangeState(OnlineMatchState.MatchFound);
                    break;
                case MessageType.MatchmakingCancel:
                    Debug.Log("Server: MatchmakingCancel.");
                    Disconnect();
                    break;
                default:
                    break;
            }
        }
        #endregion
        #region Unitask Methods
        public async UniTask StartOnlineSessionAsync()
        {
            if (IsConnectedOrConnecting)
                return;

            try
            {
               ChangeState(OnlineMatchState.Connecting);

                bool connected = await _client.ConnectAsync();

                if (!connected)
                {
                    ChangeState(OnlineMatchState.ConnectionFailed);
                    Disconnect();
                    return;
                }
              

                await UniTask.Delay(500);

                await _client.SendAsync(
                    ServerMessageFactory.CreateJoin(_client.PlayerName));
            }
            catch (OperationCanceledException)
            {
                Disconnect();
            }
        }
        public async UniTask StartMatchMakingAsync()
        {
            if (_state != OnlineMatchState.Connected)
                return;
            ChangeState(OnlineMatchState.MatchMaking);
            _client.ClearMatch();
            await _client.SendAsync(ServerMessageFactory.CreateMatchmakingStart());
        }
        public async UniTask DisconnectAsync()
        {
            if (_state is OnlineMatchState.MatchFound)
                return;
            if (_state is OnlineMatchState.MatchMaking)
            {
                Debug.Log("Client: MatchMaking 중 연결 해제 요청, 서버에 매치메이킹 취소 요청 전송.");
                await _client.SendAsync(ServerMessageFactory.CreateMatchmakingCancel());
                return;
            }
            Disconnect();
        }
        #endregion
        private void Disconnect()
        {
            //StateCheck?.Invoke(stateCheckMessage);
            _client.Disconnect();
            ChangeState(OnlineMatchState.Local);
        }
        private void ChangeState(OnlineMatchState state)
        {
            if (_state == state)
                return;

            _state = state;
            OnStateChanged?.Invoke(_state);
        }

        //
        public event Action<string> StateCheck;
        private readonly string stateCheckMessage = "_client 서버 연결 해제.";

        //

        private bool IsConnectedOrConnecting
            => _state is OnlineMatchState.Connecting or OnlineMatchState.Connected;
    }
}
