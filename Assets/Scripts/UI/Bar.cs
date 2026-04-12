using UnityEngine;
using UnityEngine.UI;

public class Bar : MonoBehaviour
{
    public Slider slider;
    public Gradient gradient;
    public Image fill;
    public bool useGradient = true;

    public void SetMaxValue(int value)
    {
        slider.maxValue = value;
        slider.value = value;

        if (useGradient)
        {
            fill.color = gradient.Evaluate(1f);
        }
    }

    public void SetValue(int value)
    {
        slider.value = value;

        if (useGradient)
        {
            fill.color = gradient.Evaluate(slider.normalizedValue);
        }
    }
}
