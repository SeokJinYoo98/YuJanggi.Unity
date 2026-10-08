using Cysharp.Threading.Tasks;
using System;
using System.Threading;

using Unity.Profiling;

using YuJanggi.Core.InGame;
using YuJanggi.Core.AI;

namespace YuJanggi.InGame.Controller
{
    using Engine.Domain;
    using Engine.JanggiEngine;

    internal class InGameAIController : IInGameController
    {
        private static readonly ProfilerMarker ApplyMoveMarker =
            new("AI.ApplyMove");

        private IGameInputReceiver _receiver;
        private readonly IControllerQuery   _query;
        private readonly IAIMoveService _moves;
        private CancellationTokenSource _turnCancellation;
        private bool _bound;
        private bool _searchRunning;
        private bool _restartWhenFinished;
        public PlayerTeam Team { get; }

        public InGameAIController(
            PlayerTeam team,
            IControllerQuery query,
            IAIMoveService moves)
        {
            Team = team;
            _query = query;
            _moves = moves;
        }


        public bool IsLocal => false;



        public void BeginTurn()
        {
            if (!_bound || _query.CurrentTurn != Team)
                return;

            if (_searchRunning)
            {
                _restartWhenFinished = true;
                return;
            }

            var cancellation = new CancellationTokenSource();
            _turnCancellation = cancellation;
            _searchRunning = true;
            SelectAndApplyMoveAsync(cancellation).Forget();
        }

        private async UniTask SelectAndApplyMoveAsync(CancellationTokenSource cancellation)
        {
            try
            {
                var selected = await _moves.SelectMoveAsync(Team, cancellation.Token);
                await UniTask.SwitchToMainThread();

                if (!selected.HasValue || !_bound ||
                    !ReferenceEquals(_turnCancellation, cancellation) ||
                    cancellation.IsCancellationRequested ||
                    _query.CurrentTurn != Team)
                    return;

                var move = selected.Value;
                using var _ = ApplyMoveMarker.Auto();
                _receiver.RequestMove(move.From, move.To);
            }
            catch (OperationCanceledException)
            {
                // EndTurn or event unbinding invalidated this search.
            }
            finally
            {
                await UniTask.SwitchToMainThread();
                if (ReferenceEquals(_turnCancellation, cancellation))
                    _turnCancellation = null;
                cancellation.Dispose();
                _searchRunning = false;
                if (_restartWhenFinished)
                {
                    _restartWhenFinished = false;
                    BeginTurn();
                }
            }
        }

        public void Initialize(IGameInputReceiver receiver)
        {
            _receiver = receiver;
            _bound = true;
        }

        public void EndTurn()
        {
            _restartWhenFinished = false;
            var cancellation = _turnCancellation;
            _turnCancellation = null;
            cancellation?.Cancel();
        }



        public void BindEvents()
        {
          
        }

        public void UnBindEvents()
        {
           
        }
    }
}


