using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using TMPro;




namespace YuJanggi.Lobby.Panel
{
    using UI;
    using YuJanggi.AI.Data;
    using Core.Panel;

    public class AIPanel : UIVisible, IGameStartPanel
    {


        [SerializeField] private TMP_Dropdown _teamDropdown;
        [SerializeField] private TMP_Dropdown _timeDropdown;
        [SerializeField] private TMP_Dropdown _formationDropdown;
        [SerializeField] private TMP_Dropdown _strategyDropdown;



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

        public UniTask<bool> PrepareGameAsync()
        {
            throw new NotImplementedException();
        }
    }

}


