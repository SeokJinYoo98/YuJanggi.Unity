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
        public void PrepareRematchView(
            IReadOnlyBoard board,
            PlayerTeam team,
            PlayerType type)
        {
            CloseResultView();
            SyncBoardState(board);
            SyncLiveUI(team, type);
        }
        public void Initialize(IReadOnlyBoard board)
        {
            _boardView.InitPieces(board);
            _liveView.SetLiveText();
        }
        public void StartGame(
            PlayerTeam turn,
            PlayerType type)
        {
            _liveView.UpdateTurn(turn, type);
        }
        public void SyncLiveUI(
            PlayerTeam team,
            PlayerType type)
        {
            _liveView.SetLiveText();
            _liveView.UpdateTurn(team, type);
        }

        public void SyncBoardState(IReadOnlyBoard board)
            => _boardView.SyncBoardState(board);

        public void ClearSelection()
            => _boardView.ClearSelection();

        public void SelectPiece(
            int pieceId,
            IReadOnlyList<Pos> legal,
            IReadOnlyList<Pos> illegal)
        {
            ClearSelection();
            _boardView.SelectPiece(pieceId);
            _boardView.ShowMoveGuides(legal, illegal);
        }

        public void BindEvents(IReadOnlyEngine engine)
        {
            engine.Turn.OnTimeChanged                += _liveView.HandleTimeChanged;
            engine.ReadOnlyRecord.OnRecordChanged    += _liveView.HandleRecordChanged;
            engine.Score.OnScoreChanged              += _liveView.HandleUpdateScore;

        }
        public void UnBindEvents(IReadOnlyEngine engine)
        {
            engine.Turn.OnTimeChanged                -= _liveView.HandleTimeChanged;
            engine.ReadOnlyRecord.OnRecordChanged    -= _liveView.HandleRecordChanged;
            engine.Score.OnScoreChanged              -= _liveView.HandleUpdateScore;
        }
        public void ApplyLiveUI(
            PlayerTeam nextTeam,
            PlayerType nextType)
        {
            _liveView.UpdateTurn(nextTeam, nextType);
        }

        public void ApplyMoveRecord(
            MoveRecord record)
        {
            _boardView.ApplyMovement(
               record.MovedPiece,
               record.From,
               record.To);

            if (record.IsCaptured)
                _boardView.ApplyCapture(
                    record.CapturedPiece,
                    record.To);
        }
        public void RevertMoveRecord(MoveRecord record)
        {
            _boardView.RevertMovement(
               record.MovedPiece,
               record.From,
               record.To);

            if (record.IsCaptured)
                _boardView.RevertCapture(
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
        }
        public void OpenResultView()
            => _resultView.Open();
        public void CloseResultView()
            => _resultView.Close();
    }
}
