#nullable enable
using System;
using System.IO;
using System.Text.Json;
using YuJanggi.Network;
using YuJanggi.Protocol.V2.Messages;

namespace YuJanggi.InGame.Handler
{
    /// <summary>인게임 서버 이벤트의 검증·해석 경계입니다. 게임 상태나 화면은 소유하지 않습니다.</summary>
    public sealed class InGameHandler : IDisposable
    {
        private readonly NetworkConnection _connection;
        private readonly RequestDispatcher _requests;
        private bool _disposed;

        public InGameHandler(NetworkConnection connection, RequestDispatcher requests)
        {
            _connection = connection;
            _requests = requests;
            _connection.MessageReceived += HandleMessage;
        }

        private void HandleMessage(ServerMessage message)
        {
            if (_disposed || message.RequestId is not null)
                return;

            // TODO:
            // 현재 Protocol에는 GameStart 메시지 타입과 DTO가 없어 수신 분기를 활성화할 수 없습니다.
            // 정의 추가 후 아래 switch에 GameStart 분기를 연결하고 HandleGameStart에서 DTO를 해석합니다.
            // GameReady는 매칭 준비 이벤트이므로 기존 MatchingHandler가 계속 처리합니다.
            switch (message.Type)
            {
                // case ServerMessageType.GameStart:
                //     HandleGameStart(message);
                //     break;
                default:
                    break;
            }

            // TODO:
            // MoveApplied / TurnChanged / GameEnded 계약이 추가되면 이곳에서 각각 분기합니다.
            // 현재는 해당 이벤트를 처리하지 않으며, 결과 반영은 InGameSession 등 인게임 계층에 연결해야 합니다.
            // 향후 요청 송신은 _requests를 사용하고 응답 대기는 RequestDispatcher에 맡깁니다.
        }

        private void HandleGameStart(ServerMessage message)
        {
            if (message.Payload is not { ValueKind: JsonValueKind.Object })
                throw new InvalidDataException("GameStart Payload는 JSON 객체여야 합니다.");

            // TODO:
            // GameStart DTO가 정의되면 GetPayload<GameStart>()와 계약상 필수 필드 검증을 추가합니다.
            // 현재는 Payload 형태만 검증하는 틀이며 게임 시작 알림이나 상태 변경은 수행하지 않습니다.
            // 시작 여부·현재 턴·종료 여부를 지속 보관해야 할 때 InGameService 도입을 검토하고
            // 해당 상태의 소유와 전이를 위임합니다. Handler에서 엔진이나 View를 직접 조작하지 않습니다.
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _connection.MessageReceived -= HandleMessage;
        }
    }
}
