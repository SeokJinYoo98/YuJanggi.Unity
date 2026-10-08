using DG.Tweening;
using TMPro;
using UnityEngine;


namespace YuJanggi.InGame.Views.UI
{
    using Engine.Domain;

    using Audio;
    using BootStrap;

    public sealed class CheckEffectView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _effectText;

        private Tween _effectTween;
      
        private PlayerTeam?  _prevJanggun;

        private AudioManager Audio
            => YuJanggiBootStrap.Instance.AudioManager;

        public void PlayJanggun(PlayerTeam team)
        {
            Audio.PlaySfx(JanggiSfx.Check);
            _effectText.SetText("장군");
            PlayEffect(team, fromLeft: true);
            _prevJanggun = team;
        }

        public void PlayMeonggun()
        {
            if (!_prevJanggun.HasValue)
                return;

            var team = _prevJanggun.Value;
            _prevJanggun = null;

            Audio.PlaySfx(JanggiSfx.UnCheck);
            _effectText.SetText("멍군");
            PlayEffect(team, fromLeft: false);
        }

        private void PlayEffect(PlayerTeam team, bool fromLeft)
        {
            _effectTween?.Kill();

            _effectText.gameObject.SetActive(true);
            SetTeamColor(team);

            var rect = _effectText.rectTransform;
            float distance = GetTravelDistance(rect);
            float startX = fromLeft ? -distance : distance;

            SetPositionX(rect, startX);
            _effectTween = CreateEffectAnimation(rect, startX)
                .OnComplete(() => _effectText.gameObject.SetActive(false));
        }

        private void SetTeamColor(PlayerTeam team)
        {
            _effectText.color = team == PlayerTeam.Cho
                ? Color.green
                : Color.red;
        }

        private static float GetTravelDistance(RectTransform rect)
        {
            Canvas.ForceUpdateCanvases();

            var parent = (RectTransform)rect.parent;
            return (parent.rect.width + rect.rect.width) * 0.5f;
        }
        private static void SetPositionX(RectTransform rect, float x)
        {
            var position = rect.anchoredPosition;
            position.x = x;
            rect.anchoredPosition = position;
        }
        private Sequence    CreateEffectAnimation(
            RectTransform rect,
            float startX)
        {
            return DOTween.Sequence()
                .Append(rect.DOAnchorPosX(0f, 0.4f).SetEase(Ease.OutCubic))
                .AppendInterval(0.8f)
                .Append(rect.DOAnchorPosX(-startX, 0.4f).SetEase(Ease.InCubic))
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }
    }
}
