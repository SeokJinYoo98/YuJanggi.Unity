
using TMPro;
using UnityEngine;

namespace YuJanggi.InGame.Views.UI
{
    using Engine.Domain;
    using YuJanggi.Audio;
    using YuJanggi.BootStrap;

    public class LiveView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _recordText;
        [SerializeField] private TMP_Text _turnText;

        [SerializeField] private TMP_Text _choScoreText;
        [SerializeField] private TMP_Text _choTimerText;

        [SerializeField] private TMP_Text _hanScoreText;
        [SerializeField] private TMP_Text _hanTimerText;
        [SerializeField] private TMP_Text _displayModeText;

        int _totalTurn = 0;
        int _currTurn  = 0;

        private AudioManager Audio
            => YuJanggiBootStrap.Instance.AudioManager;

        public void Start()
        {
            _turnText.color = Color.green;

        }
        public void UpdateTotalTurn(int currTurn, int totalTurn)
        {
            _currTurn = currTurn;
            _totalTurn = totalTurn;
            UpdateRecord();
        }
        public void UpdateCurrTurn(int currTurn)
        {
            _currTurn = currTurn;
            UpdateRecord();
        }
        private void UpdateRecord()
            => _recordText.SetText("{0}수:{1}수", _currTurn, _totalTurn);
        public void SetReplayText()
            => _displayModeText.SetText("기보 보기");
        public void SetLiveText()
            => _displayModeText.SetText("라이브 보기");

        public void UpdateTurn(PlayerTeam turn, bool isLocal)
        {
            if (isLocal)
                Audio.PlaySfx(JanggiSfx.TurnAlert);

            if (turn == PlayerTeam.Cho)
            {
                _turnText.color = Color.green;
                _turnText.SetText("차례:초");
            }
            else
            {
                _turnText.color = Color.red;
                _turnText.SetText("차례:한");
            }
        }
        public void UpdateScore(PlayerTeam team, int score)
        {
            if (team == PlayerTeam.Cho)
                _choScoreText.SetText("점수:{0}", score);
            else
                _hanScoreText.SetText("{0}:점수", score);
        }
        public void UpdateTimer((PlayerTeam team, int time) info)
        {
            if (info.team == PlayerTeam.Han)
                _hanTimerText.SetText("{0}:시간", info.time);
            else
                _choTimerText.SetText("시간:{0}", info.time);
        }

    }
}
