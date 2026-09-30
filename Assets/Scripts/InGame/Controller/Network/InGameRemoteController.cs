using System;
using UnityEngine;

namespace YuJanggi.InGame.Controller
{
    using Engine.Domain;
    using Engine.JanggiEngine;
    using InGame.Handler;

    internal sealed class InGameRemoteController : IInGameController
    {
        #region Fields
        // 내부 상태와 참조를 저장하는 변수
        private readonly InGameHandler _networkHandler;
        private readonly IControllerQuery _query;
        #endregion

        #region Properties
        // 상태를 조회하거나 변경하는 접근 속성
        public PlayerTeam Team { get; }
        public bool IsLocal
            => true;
        public bool IsTurn
            => Team == _query.CurrentTurn;
        #endregion

        #region Events
        // 상태 변화나 특정 동작을 외부에 알리는 이벤트

        public event MoveRequestHandler OnMoveRequest;
        public event SelectionChangedHandler OnSelectionChanged;
        #endregion

        #region Constructors
        // 순수 C#

        /*
         Service
        - MovePieceEvent 같은 Protocol DTO를 직접 받음
        - DTO를 클라이언트에서 쓰기 좋은 값으로 변환
        - 상태 저장이 필요하면 저장
        - 예: ProtocolPlayerTeam → PlayerTeam, 좌표 → Pos

        Handler
        - Service에서 올라온 결과를 보고
        - 어떤 게임 이벤트인지 분배
        - OnPieceMoved, OnGameStarted 같은 이벤트 발생
        - Request 전송 흐름도 조정

        Controller
        - Handler 이벤트 구독
        - NetworkController / RemoteController 역할에 따라 자기 것이 맞는지 판단
        - 최종적으로 Engine 쪽 이벤트 호출
        */
        internal InGameRemoteController(
            PlayerTeam team,
            IControllerQuery query,
            InGameHandler networkHandler)
        {
            Debug.Log("InGameRemoteController 생성");
            Team = team;
            _query = query;
            _networkHandler = networkHandler;
        }
        #endregion

        #region Public Methods
        // 외부에서 호출하는 기능
        public void BeginTurn()
        {
            throw new NotImplementedException();
        }

        public void EndTurn()
        {
            throw new NotImplementedException();
        }

        public void BindEvents(IGameInputReceiver receiver)
        {
            throw new NotImplementedException();
        }

        public void UnBindEvents(IGameInputReceiver receiver)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Event Handlers
        // 구독한 이벤트가 발생했을 때 실행하는 처리 메서드
        #endregion

        #region Private Methods
        // 클래스 내부에서 사용하는 보조 로직
        #endregion
    }
}


