using Cysharp.Threading.Tasks;
using UnityEngine;
using TMPro;

namespace YuJanggi.Lobby.Panel
{
    using Store;

    public class LocalPanel : Panel, IGameStartPanel
    {
        [SerializeField] private TMP_Dropdown _choForm;
        [SerializeField] private TMP_Dropdown _hanForm;
        [SerializeField] private TMP_Dropdown _time;

        public UniTask<bool> PrepareGameAsync()
        {
            var option = JanggiOptionFactory.CreateLocal(
                    _choForm.value,
                    _hanForm.value,
                    _time.value);

            JanggiOptionStore.SaveOptions(option);

            return UniTask.FromResult(true);
        }
    }
}


