using UnityEngine;
using TMPro;

namespace YuJanggi.Runtime.UI
{
    using Core.Domain;

    public class ResultUI : UIVisible
    {
        [SerializeField] private TMP_Text _winner;
        [SerializeField] private TMP_Text _cnt;
        [SerializeField] private TMP_Text _result;

        public void EndGame(in GameResultInfo result)
        {
            SetWinnerType(result.Loser);
            SetWinType(result.Type);
            SetMoveCnt(result.MoveCnt);
        }
        private void SetWinnerType(PlayerTeam loser)
        {
            if (loser == PlayerTeam.Cho)
            {
                _winner.color = Color.red;
                _winner.SetText("한");
            }
            else
            {
                _winner.color = Color.green;
                _winner.SetText("초");
            }
            
        }
        private void SetWinType(GameResult result)
        {
            switch (result)
            {
                case GameResult.CheckMate:
                    _result.SetText("[외통수]");
                    break;
                case GameResult.GiveUp:
                    _result.SetText("[기권승]");
                    break;
                default:
                    break;
            }
        }
        private void SetMoveCnt(int moveCnt)
            => _cnt.SetText("{0}", moveCnt);
    }
}
