using UnityEngine;
using TMPro;
using System;

using YuJanggi.UI;

namespace YuJanggi.Lobby.Panel
{
    using InGame.Controller;
    public class AIPanel : UIVisible
    {
        [SerializeField] private TMP_Dropdown _teamDropdown;
        [SerializeField] private TMP_Dropdown _timeDropdown;
        [SerializeField] private TMP_Dropdown _formationDropdown;
        [SerializeField] private TMP_Dropdown _strategyDropdown;


        public int LocalPlayer          => _teamDropdown.value;
        public int TurnTime             => _timeDropdown.value;
        public int LocalPlayerFormation => _formationDropdown.value;
        public event Action StartRequested;
        public void RequestStart() => StartRequested?.Invoke();
        public AIMoveStrategyType Strategy
        {
            get
            {
                if (_strategyDropdown == null ||
                    _strategyDropdown.value < (int)AIMoveStrategyType.Random ||
                    _strategyDropdown.value > (int)AIMoveStrategyType.Minimax)
                    return AIMoveStrategyType.Random;

                return (AIMoveStrategyType)_strategyDropdown.value;
            }
        }
    
    }

}


