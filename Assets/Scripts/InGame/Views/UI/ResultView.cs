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

        public void PlayAuido(bool localWin)
            => Audio.PlaySfx(localWin ? JanggiSfx.Lose : JanggiSfx.Win);
        public void SetWinnerType(PlayerTeam winner)
        {
            if (winner == PlayerTeam.Cho)
            {
                _winner.color = Color.green;
                _winner.SetText("초");
            }
            else
            {
                _winner.color = Color.red;
                _winner.SetText("한");
            }
        }
        public void SetWinType(GameResult result)
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
        public void SetMoveCnt(int moveCnt)
            => _cnt.SetText("{0}", moveCnt);
    }
}


