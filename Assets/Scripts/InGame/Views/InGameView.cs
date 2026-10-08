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

        public void BindEvents(IReadOnlyGameStateEvents events)
        {
            events.OnRecordChanged += _liveView.UpdateTotalTurn;
            events.OnTimeChanged   += _liveView.UpdateTimer;
            events.OnScoreChanged  += _liveView.UpdateScore;
        }
        public void UnBindEvents(IReadOnlyGameStateEvents events)
        {
            events.OnRecordChanged -= _liveView.UpdateTotalTurn;
            events.OnTimeChanged   -= _liveView.UpdateTimer;
            events.OnScoreChanged  -= _liveView.UpdateScore;
        }
    }
}
