using UnityEngine;
using UnityEngine.UI;

public class Bar : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Slider slider;
    [SerializeField] private Image fill;

    [Header("Visual Settings")]
    [SerializeField] private Gradient gradient;
    [SerializeField] private bool useGradient = true;

    /// <summary>
    /// Sets the maximum scale of the bar and resets the current value to full.
    /// Also updates the color if a gradient is used.
    /// </summary>
    /// <param name="value">The maximum integer value for the slider.</param>
    public void SetMaxValue(int value)
    {
        if (slider == null) return;

        slider.maxValue = value;
        slider.value = value;

        UpdateColor();
    }

    /// <summary>
    /// Updates the current value of the bar and adjusts the fill color based on the gradient.
    /// </summary>
    /// <param name="value">The new integer value for the slider.</param>
    public void SetValue(int value)
    {
        if (slider == null) return;

        slider.value = value;
        UpdateColor();
    }

    /// <summary>
    /// Internal method to update the color of the fill image based on the slider's normalized value.
    /// </summary>
    private void UpdateColor()
    {
        if (useGradient && fill != null && gradient != null)
        {
            fill.color = gradient.Evaluate(slider.normalizedValue);
        }
    }
}