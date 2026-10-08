using Cysharp.Threading.Tasks;
using System;
using TMPro;
using UnityEngine;

namespace YuJanggi.Lobby.Panel
{
    using AI.Data;
    using Store;

    public class AIPanel : Panel, IGameStartPanel
    {
        [SerializeField] private TMP_Dropdown _team;
        [SerializeField] private TMP_Dropdown _time;
        [SerializeField] private TMP_Dropdown _form;
        [SerializeField] private TMP_Dropdown _strategy;
      
        public UniTask<bool> PrepareGameAsync()
        {
            var options = JanggiOptionFactory.CreateAI(
                _team.value,
                _form.value,
                _time.value);

            var strategy = ToStrategy(_strategy.value);

            JanggiOptionStore.SaveOptions(
                options,
                strategy);

            return UniTask.FromResult(true);
        }

        private static AIMoveStrategyType ToStrategy(int value)
          => Enum.IsDefined(typeof(AIMoveStrategyType), value)
              ? (AIMoveStrategyType)value : AIMoveStrategyType.Random;
    }

}


