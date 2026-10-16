using TMPro;
using UnityEngine;

namespace YuJanggi.InGame.Views.Chatting
{
    using Engine.Domain;

    public sealed class MessageView : MonoBehaviour
    {
        [SerializeField] private ChatStyleData _style;
        [SerializeField] private UnityEngine.UI.Image _icon;
        [SerializeField] private UnityEngine.UI.Image _bubble;
        [SerializeField] private TMP_Text _text;

        public void SetMessage(PlayerTeam team, string message, bool isLocal)
        {
            // 로컬 메시지처럼 아이콘이 없는 구성에서도 사용할 수 있습니다.
            if (_icon != null)
                _icon.sprite = _style.GetIcon(team);

            _bubble.sprite = _style.GetBubble(isLocal);
            _text.color = _style.GetTextColor(isLocal);
            _text.richText = false;
            _text.SetText(message);
        }

        public void SetServerMessage(string message)
        {
            if (_icon != null)
                _icon.sprite = _style.ServerIcon;

            _bubble.sprite = _style.ServerBubble;
            _text.color = _style.ServerTextColor;
            _text.richText = false;
            _text.SetText(message);
        }
    }
}
