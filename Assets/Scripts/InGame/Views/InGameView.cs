using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace YuJanggi.InGame.Views
{
    using Engine.JanggiEngine;
    using Engine.JanggiBoard;
    using Engine.Domain;

    using Chatting;
    using Board;
    using UI;
    using YuJanggi.UI;

    public class InGameView : MonoBehaviour
    {
        [SerializeField] private ResultView      _resultView;
        [SerializeField] private LiveView        _liveView;
        [SerializeField] private BoardView       _boardView;
        [SerializeField] private CheckEffectView _janggunEffect;

        [SerializeField] private ChattingPanel   _chatPanel;

        [SerializeField] private ButtonBase        _modeActionButton;
        [SerializeField] private LockOnClickButton _rematchButton;
        public void PrepareRematchView(
            IReadOnlyBoard board,
            PlayerTeam team,
            PlayerType type)
        {
            CloseResultView();
            SyncBoardState(board);
            SyncLiveUI();
            _liveView.UpdateTurn(team, type);
            _rematchButton.ResetButton();
        }
        public void Initialize(
            IReadOnlyBoard board,
            bool isNetworkMode)
        {
            _boardView.InitPieces(board);
            _liveView.SetLiveText();
            _modeActionButton.SetText("무르기");

            if (isNetworkMode)
            {
                _modeActionButton.SetText("채팅열기");
                _chatPanel.Initialize();
            }     
        }
        public void StartGame(
            PlayerTeam turn,
            PlayerType type)
        {
            _liveView.UpdateTurn(turn, type);
        }
        public void SyncReplayUI()
            => _liveView.SetReplayText();
        public void SyncLiveUI()
            => _liveView.SetLiveText();

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

            if (legal == null && illegal == null)
                return;

            _boardView.ShowMoveGuides(legal, illegal);
        }

        public void HandleTimeChanged((PlayerTeam team, int time) info)
            => _liveView.HandleTimeChanged(info);

        public void BindEvents(IReadOnlyEngine engine, bool useEngineTime = true)
        {
            if (useEngineTime)
                engine.Turn.OnTimeChanged            += HandleTimeChanged;
            engine.ReadOnlyRecord.OnRecordChanged    += _liveView.HandleRecordChanged;
            engine.Score.OnScoreChanged              += _liveView.HandleUpdateScore;

        }
        public void UnBindEvents(IReadOnlyEngine engine)
        {
            engine.Turn.OnTimeChanged                -= HandleTimeChanged;
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

        public void ResetRematchButton()
            => _rematchButton.ResetButton();

        public void ToggleChattingPanel()
        {
            _chatPanel.Toggle();

            if (_chatPanel.IsToggled)
                _modeActionButton.SetText("채팅닫기");
            else
                _modeActionButton.SetText("채팅열기");
        }
    }
}
