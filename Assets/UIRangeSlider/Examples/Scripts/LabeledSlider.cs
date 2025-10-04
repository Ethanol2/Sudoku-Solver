using TMPro;
using UnityEngine;

namespace UIMinMaxSliderExamples
{
	using UIRangeSliderNamespace;
	public class LabeledSlider : MonoBehaviour
	{
		[SerializeField] private UIRangeSlider slider;
		[SerializeField] private TMP_Text minValue, maxValue;
		[SerializeField] private string numberFormat = string.Empty;

		private void SetValues(float min, float max)
		{
			minValue.text = min.ToString(numberFormat);
			maxValue.text = max.ToString(numberFormat);
		}

		private void OnEnable()
		{
			slider.onValuesChanged.AddListener(SetValues);
			SetValues(slider.valueMin, slider.valueMax);
		}

		private void OnDisable()
		{
			slider.onValuesChanged.RemoveListener(SetValues);
		}

		private void Awake()
		{
			SetValues(slider.valueMin, slider.valueMax);
		}

	}
}

