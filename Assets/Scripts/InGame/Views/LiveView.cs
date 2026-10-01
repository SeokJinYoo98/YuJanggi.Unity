using TMPro;

namespace YuJanggi.InGame.Views
{
    using Engine.JanggiEngine;
    using Engine.Domain;
    using Audio;
    using BootStrap;
    using Runtime.UI;

    /// <summary>게임 UI와 HUD, Live/Replay 모드 표시를 담당합니다.</summary>
    public class LiveView
    {
        private readonly ResultUI _resultUI;
        private readonly MatchUI _liveUI;
        private readonly AudioManager _audioManager;
        private readonly TMP_Text _displayModeText;

        public LiveView(ResultUI resultUI, MatchUI matchUI, TMP_Text displayMode)
        {
            _resultUI = resultUI;
            _liveUI = matchUI;
            _displayModeText = displayMode;
            _audioManager = YuJanggiBootStrap.Instance.AudioManager;
        }

        public void CheckOccured(PlayerTeam team)
        {
            _audioManager.PlaySfxOneShot(JanggiSfx.Check);
            _liveUI.PlayJanggun(team);
        }

        public void CheckReleased()
            => _audioManager.PlaySfxOneShot(JanggiSfx.UnCheck);

        public void OnGameEnded(in GameResultInfo info, bool loserIsLocal)
        {
            _audioManager.PlaySfxOneShot(loserIsLocal ? JanggiSfx.Lose : JanggiSfx.Win);
            _resultUI.EndGame(info);
        }

        public void ShowResultUI() => _resultUI.Show();
        public void HideResultUI() => _resultUI.Hide();

        public void ShowReplayMode() => _displayModeText.SetText("기보 보기");
        public void ShowLiveMode() => _displayModeText.SetText("라이브 보기");

        public void BindUI(IReadOnlyGameStateEvents events)
        {
            events.OnRecordChanged += _liveUI.UpdateTotalTurn;
            events.OnTimeChanged += _liveUI.UpdateTimer;
            events.OnScoreChanged += _liveUI.UpdateScore;
        }

        public void UnBindUI(IReadOnlyGameStateEvents events)
        {
            events.OnRecordChanged -= _liveUI.UpdateTotalTurn;
            events.OnTimeChanged -= _liveUI.UpdateTimer;
            events.OnScoreChanged -= _liveUI.UpdateScore;
        }

        public void UpdateTurnInfo(PlayerTeam next, bool isLocal)
        {
            if (isLocal)
                _audioManager.PlaySfxOneShot(JanggiSfx.TurnAlert);

            _liveUI.UpdateTurn(next);
        }

        public void ResetGame() => _resultUI.Hide();
    }
}
