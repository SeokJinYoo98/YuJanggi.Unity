using System.Collections.Generic;
using UnityEngine;


namespace YuJanggi.Runtime.GameSession
{
    using Core.Board;
    using Core.Domain;
    using Core.Match;
    using Particle;
    using Audio;
    using UI;
    using Board;
    public class MatchView 
    {
        public MatchView(
            ParticleView    particleView,
            MoveGuideView   moveGuideView,
            BoardView       boardView,
            ResultUI        resultUI,
            MatchUI         matchUI)
        {
            _particleView   = particleView;
            _moveGuideView  = moveGuideView;
            _boardView      = boardView;
            _resultUI       = resultUI;
            _matchUI        = matchUI;
            _audioManager   = AudioManager.Instance;
        }
        public void CheckOccured(PlayerTeam team)
        {
            _audioManager.PlaySfxOneShot(JanggiSfx.Check);
            _matchUI.PlayJanggun(team);
        }
        public void CheckReleased()
            => _audioManager.PlaySfxOneShot(JanggiSfx.UnCheck);
        public void SyncBoardState(ILiveMatch match)
        {
            var board = match.Board;
            _boardView.SyncBoardState(board);
        }
        public void HighlightPiece(int pieceId)
        {
            _audioManager.PlaySfxOneShot(JanggiSfx.Select);
            _boardView.HighlightOnlyPiece(pieceId);
        }
        public void HighlightWays(IReadOnlyList<Pos> legals, IReadOnlyList<Pos> illegals)
        {
            _moveGuideView.ShowHighlight(legals, true);
            _moveGuideView.ShowHighlight(illegals, false);
        }
        public void UnHighlight()
        {
            _boardView.UnHighlightPiece();
            _moveGuideView.HideHighlight();
        }
        public void ApplyMovement(MoveRecord record)
        {
            var fromPos = record.From;
            var toPos = record.To;
            _particleView.PlayMovementParticle(
                new Vector3(fromPos.X, 1f, fromPos.Z),
                new Vector3(toPos.X, 1f, toPos.Z));
            _boardView.MovePiece(record.MovedPiece.Id, toPos);
            _audioManager.PlaySfxOneShot(JanggiSfx.Move);
            if (record.IsCapture)
            {
                _particleView.PlayCapture(new Vector3(toPos.X, 0f, toPos.Z));
                _boardView.PlaceCapturedPiece(record.CapturedPiece.Id, record.CapturedPiece.Team);
                _audioManager.PlaySfxOneShot(JanggiSfx.Capture);
            }
        }
        public void RevertMovement(MoveRecord record)
        {
            var movedPiece = record.MovedPiece;
            var to = record.From;
            _boardView.MovePiece(movedPiece.Id, to);

            if (record.IsCapture)
            {
                to = record.To;
                var captured = record.CapturedPiece;
                _boardView.RestoreCapturedPiece(captured.Id, captured.Team, to);
            }
        }
        public void OnGameEnded(in GameResultInfo info, bool loserIsLocal)
        {
            if (loserIsLocal) _audioManager.PlaySfxOneShot(JanggiSfx.Lose);
            else _audioManager.PlaySfxOneShot(JanggiSfx.Win);
            _resultUI.EndGame(info);
        }
        public void ShowResultUI()
            => _resultUI.Show();
        public void HideResultUI()
            => _resultUI.Hide();


        public void BindUI(IMatchUIDatas match)
        {
            _matchUI.BindEvents(match);
        }
        public void UnBindUI(IMatchUIDatas match)
        {
            _matchUI.UnBindEvents(match);
        }

        public void OnTurnChanged(bool isLocal)
        {
            if (!isLocal) return;
            // Debug.Log($"Turn UI Update:{isLocal}");
            _audioManager.PlaySfxOneShot(JanggiSfx.TurnAlert);
        }


        public void ResetGame(IBoardModel boardModel)
        {
            _resultUI.Hide();
            _boardView.SyncBoardState(boardModel);
        }
        public void StartGame(IBoardModel boardModel)
        {
            _boardView.StartGame(boardModel);
        }


        private readonly ParticleView   _particleView;
        private readonly MoveGuideView  _moveGuideView;
        private readonly BoardView      _boardView;

        private readonly ResultUI       _resultUI;
        private readonly MatchUI        _matchUI;
        private readonly AudioManager   _audioManager;
    }
}
