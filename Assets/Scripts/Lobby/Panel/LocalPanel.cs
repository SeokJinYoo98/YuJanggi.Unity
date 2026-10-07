using System;

using UnityEngine;
using TMPro;

using YuJanggi.UI;

namespace YuJanggi.Lobby.UI
{
    public class LocalPanel : UIVisible
    {
        [SerializeField] private TMP_Dropdown _choFormationDropdown;
        [SerializeField] private TMP_Dropdown _hanFormationDropdown;
        [SerializeField] private TMP_Dropdown _timeDropdown;
        public int ChoFormation => _choFormationDropdown.value;
        public int HanFormation => _hanFormationDropdown.value;
        public int TurnTime => _timeDropdown.value;

        public event Action StartRequested;
        public void RequestStart()
            => StartRequested?.Invoke();


    }

}


