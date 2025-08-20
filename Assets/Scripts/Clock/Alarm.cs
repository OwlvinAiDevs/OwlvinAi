using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

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
    private List<Alarms> alarms = new();
    public int userId = 1; // Default user ID, can be set dynamically

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

        LoadAlarms();
        UpdateAlarmsDisplay();
    }

    void LoadAlarms()
    {
        alarms = DatabaseManager.db.Table<Alarms>()
            .Where(a => a.user_id == userId)
            .ToList();
    }

    private void AddAlarm()
    {
        int h = hourDropdown.value;
        int m = minuteDropdown.value;

        if (!alarms.Any(a => a.hour == h && a.minute == m))
        {
            // Save new alarm to database
            var newAlarm = new Alarms { user_id = userId, hour = h, minute = m };
            DatabaseManager.db.Insert(newAlarm);
            alarms.Add(newAlarm);
            Debug.Log($"[AlarmManager] Added alarm: {h:00}:{m:00}");
        }
    }

    private void RemoveLastAlarm()
    {
        if (alarms.Count > 0)
        {
            var lastAlarm = alarms.Last();

            // Delete alarm from database
            DatabaseManager.db.Delete(lastAlarm);
            alarms.Remove(lastAlarm); // Removes alarm from local list
            UpdateAlarmsDisplay();
            Debug.Log($"[AlarmManager] Removed alarm: {lastAlarm.hour:00}:{lastAlarm.minute:00} from database.");
        }
    }

    private void UpdateAlarmsDisplay()
    {
        if (alarmsListText == null)
            return;

        StringBuilder sb = new();
        sb.AppendLine("Alarms Set:");

        foreach (var alarm in alarms) // Loops through Alarms objects
        {
            sb.AppendLine($"{alarm.hour:00}:{alarm.minute:00}");
        }

        alarmsListText.text = sb.ToString();
    }

    private void Update()
    {
        DateTime now = DateTime.Now;

        foreach (var alarm in alarms) // Loops through alarms objects
        {
            if (now.Hour == alarm.hour && now.Minute == alarm.minute && now.Second == 0)
            {
                alarmSound?.Play();
            }
        }
    }
}
