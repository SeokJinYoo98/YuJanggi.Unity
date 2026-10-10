using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using YuJanggi.Core.AI;
using YuJanggi.Core.InGame;
using YuJanggi.Engine.Domain;
using YuJanggi.Engine.JanggiEngine;

namespace YuJanggi.InGame.Input
{
    internal sealed class AIInputHandler
    {
        private readonly IAIMoveService _moves;
        private readonly IControllerQuery _query;

        public AIInputHandler(IAIMoveService moves, IControllerQuery query)
        {
            _moves = moves;
            _query = query;
        }

        public async UniTask RequestMoveAsync(
            PlayerTeam team,
            IGameCommandReceiver receiver,
            CancellationToken token)
        {
            if (_query.CurrentTurn != team)
                return;

            var result = await _moves.SelectMoveAsync(team, token);

            // 계산 중 턴이 끝났거나 게임이 종료된 요청은 전달하지 않습니다.
            token.ThrowIfCancellationRequested();
            if (_query.CurrentTurn != team)
                return;

            if (result is not AIMove move)
            {
                Debug.LogWarning($"[AIInputHandler] 선택할 수 있는 이동이 없습니다: {team}");
                return;
            }

            receiver.HandleRequestMove(move.From, move.To);
        }
    }
}
