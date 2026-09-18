
using TMPro;

using UnityEngine;

namespace YuJanggi.Runtime.UI
{
    public class LocalPanelView : UIVisible
    {
        [SerializeField] private TMP_Dropdown _choFormationDropdown;
        [SerializeField] private TMP_Dropdown _hanFormationDropdown;
        [SerializeField] private TMP_Dropdown _timeDropdown;
        public int ChoFormation => _choFormationDropdown.value;
        public int HanFormation => _hanFormationDropdown.value; 
        public int TurnTime     => _timeDropdown.value;
    }

}
