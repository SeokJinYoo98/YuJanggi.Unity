using Cysharp.Threading.Tasks;
using System.Threading;
using YuJanggi.Core.InGame;
using YuJanggi.Engine.Domain;
using YuJanggi.InGame.Input;

namespace YuJanggi.InGame.Player
{
    internal sealed class AIPlayer : InGamePlayer, IInGamePlayer
    {
        private readonly AIInputHandler _input;

        public AIPlayer(
            PlayerTeam team,
            IGameCommandReceiver receiver,
            AIInputHandler input)
            : base(team, PlayerType.AI, receiver)
        {
            _input = input;
        }

        public UniTask RequestMoveAsync(CancellationToken token)
            => _input.RequestMoveAsync(Team, Receiver, token);
    }
}
