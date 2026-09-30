using System;
using UnityEngine;

namespace YuJanggi.InGame.Controller
{
    using Engine.Domain;

    using Runtime.Input;
    using Engine.JanggiEngine;
    using Handler;

    internal sealed class InGameNetworkController
        : InGameLocalController
    {
        #region Fields
        // 내부 상태와 참조를 저장하는 변수
        private readonly InGameHandler _networkHandler;
        #endregion

        #region Properties
        // 상태를 조회하거나 변경하는 접근 속성

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
        internal InGameNetworkController(
            PlayerTeam team,
            IControllerQuery query,
            IInputHandler input,
            InGameHandler networkHandler)
            : base(team, query, input)
        {
            Debug.Log("NetworkController 생성");

            _networkHandler = networkHandler;
        }
        #endregion

        #region Public Methods
        // 외부에서 호출하는 기능
        public override void BindEvents(IGameInputReceiver receiver)
        {
            base.BindEvents(receiver);
        }
        public override void UnBindEvents(IGameInputReceiver receiver)
        {
            base.UnBindEvents(receiver);
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


