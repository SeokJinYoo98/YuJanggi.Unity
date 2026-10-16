using UnityEngine;

namespace YuJanggi.InGame.Views.Chatting
{
    using Engine.Domain;

    [CreateAssetMenu(
        fileName = "Chat Style Data",
        menuName = "YuJanggi/Chat/Chat Style Data")]
    public sealed class ChatStyleData : ScriptableObject
    {
        [Header("Sender Icons")]
        [SerializeField] private Sprite _choIcon;
        [SerializeField] private Sprite _hanIcon;
        [SerializeField] private Sprite _serverIcon;

        [Header("Speech Bubbles")]
        [SerializeField] private Sprite _localBubble;
        [SerializeField] private Sprite _remoteBubble;
        [SerializeField] private Sprite _serverBubble;

        [Header("Text Colors")]
        [SerializeField] private Color _localTextColor  = new Color(0.16f, 0.12f, 0.09f, 1f);
        [SerializeField] private Color _remoteTextColor = new Color(0.16f, 0.12f, 0.09f, 1f);
        [SerializeField] private Color _serverTextColor = Color.white;

        public Sprite ServerIcon        => _serverIcon;
        public Sprite ServerBubble      => _serverBubble;
        public Color ServerTextColor    => _serverTextColor;

        public Sprite GetIcon(PlayerTeam team)
            => team switch
            {
                PlayerTeam.Cho => _choIcon,
                PlayerTeam.Han => _hanIcon,
                _ => null
            };

        public Sprite GetBubble(bool isLocal)
            => isLocal ? _localBubble : _remoteBubble;

        public Color GetTextColor(bool isLocal)
            => isLocal ? _localTextColor : _remoteTextColor;
    }
}
