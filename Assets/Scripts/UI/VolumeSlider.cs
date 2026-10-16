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

        public float Value => _slider.value;

        public void SetValue(float value)
        {
            value = Mathf.Clamp01(value);

            _slider.SetValueWithoutNotify(value);
            SetValueText(value);
        }

        public void HandleValueChanged()
        {
            SetValueText(_slider.value);
        }

        private void SetValueText(float value)
            => _valueText.text =
                $"{Mathf.RoundToInt(value * 100f)}%";
    }
}
