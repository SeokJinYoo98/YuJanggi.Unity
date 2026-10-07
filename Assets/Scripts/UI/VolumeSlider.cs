using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace YuJanggi.UI.Volume
{
    public class VolumeSlider : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _valueText;
        [SerializeField] private Slider   _slider;

        public float NormalizedValue
            => PercentToNormalized(_slider.value);

        public void SetValue(float value)
        {
            float percent = NormalizedToPercent(value);

            _slider.SetValueWithoutNotify(percent);
            SetValueText(percent);
        }

        public void HandleValueChanged(float value)
        {
            SetValueText(value);
        }

        private void SetValueText(float value)
            => _valueText.text = $"{Mathf.RoundToInt(value)}%";

        private static float NormalizedToPercent(float value)
            => Mathf.Clamp01(value) * 100f;

        private static float PercentToNormalized(float value)
            => Mathf.Clamp(value, 0f, 100f) / 100f;
    }
}
