using UnityEngine;
using TMPro;

using YuJanggi.UI;

namespace YuJanggi.InGame.Views.UI
{
    using Engine.Domain;
    using YuJanggi.Audio;
    using YuJanggi.BootStrap;

    public class ResultView : UIVisible
    {
        [SerializeField] private TMP_Text _winner;
        [SerializeField] private TMP_Text _cnt;
        [SerializeField] private TMP_Text _result;
        public void ShowResult(in GameResultInfo info, bool loserIsLocal)
        {
            Audio.PlaySfx(loserIsLocal ? JanggiSfx.Lose : JanggiSfx.Win);
            EndGame(in info);
            Open();
        }
        private void EndGame(in GameResultInfo info)
        {
            SetWinnerType(info.Loser);
            SetWinType(info.Type);
            SetMoveCnt(info.MoveCnt);
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


