using UnityEngine;
using UnityEngine.UI;

public class CooldownBar : MonoBehaviour
{
    public Slider slider;

    public void SetMaxCoolDown(float cooldown)
    {
        slider.maxValue = cooldown;
        slider.value = cooldown;
    }

    public void SetCooldown(float cooldown)
    {
        slider.value = cooldown;
    }
}
