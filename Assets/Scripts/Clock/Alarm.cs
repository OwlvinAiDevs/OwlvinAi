using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using System.Text;

public class AlarmManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Dropdown hourDropdown;
    public TMP_Dropdown minuteDropdown;
    public Button addAlarmButton;
    public Button removeAlarmButton;
    public TextMeshProUGUI alarmsListText;  // Single TMP text showing all alarms
    public AudioSource alarmSound;

    // List of alarms (hour, minute)
    private List<(int hour, int minute)> alarms = new();

    private void Start()
    {
        // Populate dropdowns
        hourDropdown.ClearOptions();
        for (int h = 0; h < 24; h++)
            hourDropdown.options.Add(new TMP_Dropdown.OptionData(h.ToString("00")));

        minuteDropdown.ClearOptions();
        for (int m = 0; m < 60; m++)
            minuteDropdown.options.Add(new TMP_Dropdown.OptionData(m.ToString("00")));

        hourDropdown.value = 0;
        minuteDropdown.value = 0;

        addAlarmButton.onClick.AddListener(AddAlarm);
        removeAlarmButton.onClick.AddListener(RemoveLastAlarm);

        UpdateAlarmsDisplay();
    }

    private void Update()
    {
        DateTime now = DateTime.Now;

        foreach (var alarm in alarms)
        {
            if (now.Hour == alarm.hour && now.Minute == alarm.minute && now.Second == 0)
            {
                alarmSound?.Play();
            }
        }
    }

    private void AddAlarm()
    {
        int h = hourDropdown.value;
        int m = minuteDropdown.value;

        // Optional: prevent duplicate alarms
        if (!alarms.Contains((h, m)))
        {
            alarms.Add((h, m));
            UpdateAlarmsDisplay();
            Debug.Log($"Added alarm {h:00}:{m:00}");
        }
        else
        {
            Debug.Log("Alarm already set for this time.");
        }
    }

    private void RemoveLastAlarm()
    {
        if (alarms.Count > 0)
        {
            alarms.RemoveAt(alarms.Count - 1);
            UpdateAlarmsDisplay();
            Debug.Log("Removed last alarm");
        }
    }

    private void UpdateAlarmsDisplay()
    {
        if (alarmsListText == null)
            return;

        StringBuilder sb = new();
        sb.AppendLine("Alarms Set:");

        foreach (var alarm in alarms)
        {
            sb.AppendLine($"{alarm.hour:00}:{alarm.minute:00}");
        }

        alarmsListText.text = sb.ToString();
    }
}
