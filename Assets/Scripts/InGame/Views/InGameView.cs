using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace YuJanggi.InGame.Views
{
    using Engine.JanggiEngine;
    using Engine.JanggiBoard;
    using Engine.Domain;

    using BootStrap;
    using Audio;
    using Board;
    using UI;

    public class InGameView : MonoBehaviour
    {
        [SerializeField] private ResultView      _resultView;
        [SerializeField] private LiveView        _liveView;
        [SerializeField] private BoardView       _boardView;
        [SerializeField] private CheckEffectView _janggunEffect;

        public ResultView Result
            => _resultView;
        public LiveView Live
            => _liveView;
        public BoardView Board
            => _boardView;
        public CheckEffectView CheckEffect
            => _janggunEffect;

   
        public void Initialize(IReadOnlyBoard board)
        {
            _boardView.InitPieces(board);
            _liveView.SetLiveText();
        }

        public void SelectPiece(
            int? pieceId,
            IReadOnlyList<Pos> legal,
            IReadOnlyList<Pos> illegal)
        {
            _boardView.ClearSelection();
            if (pieceId is null)
                return;

            _boardView.SelectPiece(pieceId.Value);
            _boardView.ShowMoveGuides(legal, illegal);
        }
 
        public void BindEvents(IReadOnlyGameStateEvents events)
        {
            events.OnRecordChanged += _liveView.UpdateTotalTurn;
            events.OnTimeChanged   += _liveView.UpdateTimer;
        }
        public void UnBindEvents(IReadOnlyGameStateEvents events)
        {
            events.OnRecordChanged -= _liveView.UpdateTotalTurn;
            events.OnTimeChanged   -= _liveView.UpdateTimer;
        }
        public void ApplyLiveUI(
            PlayerTeam nextTeam,
            PlayerType nextType,
            (int Cho, int Han) score,
            int nextMoveNumber)
        {
            _liveView.UpdateTurn(nextTeam, nextType);
            _liveView.UpdateScore(PlayerTeam.Cho, score.Cho);
            _liveView.UpdateScore(PlayerTeam.Han, score.Han);
        }
        public void ApplyMoveRecord(
            MoveRecord record)
        {
            _boardView.ClearSelection();

            _boardView.ApplyMovement(
               record.MovedPiece,
               record.From,
               record.To);

            if (record.IsCaptured)
                _boardView.ApplyCapture(
                    record.CapturedPiece,
                    record.To);
        }
        public void PlayJanggunEffect(PlayerTeam team)
            => _janggunEffect.PlayJanggun(team);
        public void PlayMeonggunEffect(PlayerTeam team)
            => _janggunEffect.PlayMeonggun(team);

        public void OnGameEnded(
            GameResultInfo info,
            bool isLocalWin,
            int moveCnt)
        {
            _resultView.PlayAuido(isLocalWin);

            _resultView.SetMoveCnt(moveCnt);
            _resultView.SetWinnerType(info.Winner);
            _resultView.SetWinType(info.Type);

            _resultView.Open();
        }
    }
}
