using UnityEngine;
using UnityEngine.UI;

public class AlarmVolumeSlider : MonoBehaviour
{
    [SerializeField] public Slider volumeSlider;
    [SerializeField] public AudioSource alarmSource;
    private void Start()
    {
        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.value = alarmSource != null ? alarmSource.volume : 0.5f;
            volumeSlider.onValueChanged.AddListener(UpdateAlarmVolume);
        }
    }
    private void UpdateAlarmVolume(float value)
    {
        if (alarmSource != null)
        {
            alarmSource.volume = value;
        }
    }
}
