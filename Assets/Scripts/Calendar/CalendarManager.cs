using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class CalendarManager : MonoBehaviour
{
    public TextMeshProUGUI monthLabel;
    public TextMeshProUGUI selectedDateLabel;
    public Button prevButton;
    public Button nextButton;
    public GameObject dayCellPrefab;
    public Transform dayGrid;
    public GameObject UIPanel1;
    public GameObject UIPanel2;

    public TMP_InputField noteInputField;
    public TextMeshProUGUI noteDisplayText;
    public Button saveNoteButton;
    public Button clearNoteButton;

    public TextMeshProUGUI googleEventsText;
    public int userId = 1; // Default user ID, can be set dynamically

    private DateTime currentDate;
    private int todayDay;
    private bool clickedTodayThisMonth = false;
    private string currentDateKey;

    void Start()
    {
        currentDate = DateTime.Now;
        todayDay = currentDate.Day;
        GenerateCalendar(currentDate.Year, currentDate.Month);

        prevButton.onClick.AddListener(() => ChangeMonth(-1));
        nextButton.onClick.AddListener(() => ChangeMonth(1));
        saveNoteButton.onClick.AddListener(SaveNote);
        clearNoteButton?.onClick.AddListener(ClearNote);
    }

    void ChangeMonth(int offset)
    {
        currentDate = currentDate.AddMonths(offset);
        GenerateCalendar(currentDate.Year, currentDate.Month);
    }

    async void GenerateCalendar(int year, int month)
    {
        monthLabel.text = $"{year}\n{new DateTime(year, month, 1):MMMM}";
        clickedTodayThisMonth = false;

        foreach (Transform child in dayGrid)
            Destroy(child.gameObject);

        // Fetch events from Google Calendar for the current month
        List<Google.Apis.Calendar.v3.Data.Event> monthEvents = new List<Google.Apis.Calendar.v3.Data.Event>();
        if (GoogleAuthenticator.IsAuthenticated)
        {
            DateTime firstDayOfMonth = new DateTime(year, month, 1);
            // Fetch events for the entire visible month
            monthEvents = await GoogleCalendarManager.GetCalendarEventsAsync(firstDayOfMonth, firstDayOfMonth.AddMonths(1));
        }

        int daysInMonth = DateTime.DaysInMonth(year, month);
        int startDay = (int)new DateTime(year, month, 1).DayOfWeek;

        for (int i = 0; i < startDay; i++)
        {
            GameObject blankCell = Instantiate(dayCellPrefab, dayGrid);
            var img = blankCell.GetComponent<Image>();
            if (img) img.color = new Color(0, 0, 0, 0);
        }

        for (int day = 1; day <= daysInMonth; day++)
        {
            GameObject cell = Instantiate(dayCellPrefab, dayGrid);
            TextMeshProUGUI dayText = cell.GetComponentInChildren<TextMeshProUGUI>();
            dayText.text = day.ToString();

            // Check for events on this day
            DateTime currentDay = new DateTime(year, month, day);
            bool hasEvent = monthEvents.Any(e => e.Start.DateTimeDateTimeOffset.HasValue && e.Start.DateTimeDateTimeOffset.Value.Date == currentDay.Date);

            // Activate event indicator if there are events
            var eventIndicator = cell.transform.Find("EventIndicator");
            if (eventIndicator != null)
            {
                eventIndicator.gameObject.SetActive(hasEvent);
            }

            int capturedDay = day;
            cell.GetComponent<Button>().onClick.AddListener(() => OnDayClicked(capturedDay));

            // Highlight today
            if (year == DateTime.Now.Year && month == DateTime.Now.Month && day == DateTime.Now.Day)
            {
                cell.GetComponent<Image>().color = new Color(1f, 0.9f, 0.6f);

                if (!clickedTodayThisMonth)
                {
                    
                }
            }
        }
    }

    async void OnDayClicked(int day)
    {
        DateTime clickedDate = new DateTime(currentDate.Year, currentDate.Month, day);
        currentDateKey = clickedDate.ToString("yyyy-MM-dd");

        string formattedDate = $"{clickedDate:dddd MMMM} {GetDayWithSuffix(day)} {clickedDate:yyyy}";
        selectedDateLabel.text = formattedDate;
        if (UIPanel1 != null)
            UIPanel1.SetActive(!UIPanel1.activeSelf);

        if (UIPanel2 != null)
            UIPanel2.SetActive(!UIPanel2.activeSelf);

        var savedNote = DatabaseManager.db.Table<UserNote>()
            .Where(n => n.date_key == currentDateKey && n.user_id == this.userId)
            .FirstOrDefault();

        string noteText = savedNote?.note_text ?? "";
        noteInputField.text = ""; // Clear input field
        noteDisplayText.text = string.IsNullOrWhiteSpace(noteText) ? "(Add a New Note)" : noteText;

        // Fetch and display Google Calendar events for the selected day
        if (GoogleAuthenticator.IsAuthenticated)
        {
            var dayEvents = await GoogleCalendarManager.GetCalendarEventsAsync(clickedDate.Date, clickedDate.Date.AddDays(1).AddTicks(-1));

            if (dayEvents.Count > 0)
            {
                string eventsInfo = "Today's Events:\n";
                foreach (var calEvent in dayEvents)
                {
                    string time = calEvent.Start.DateTimeDateTimeOffset.HasValue ? calEvent.Start.DateTimeDateTimeOffset.Value.ToLocalTime().ToString("t") : "All Day";
                    eventsInfo += $"- {calEvent.Summary} ({time})\n";
                }
                googleEventsText.text = eventsInfo;
            }
            else
            {
                googleEventsText.text = "No events scheduled in Google Calendar for this day.";
            }
        }
        else
        {
            googleEventsText.text = "Sign in to Google to view events.";
        }
    }

    // Create Google Calendar event from the note input
    public async void SyncScheduleToGoogleCalendar()
    {
        if (!GoogleAuthenticator.IsAuthenticated)
        {
            Debug.LogWarning("Cannot sync schedule, user is not authenticated with Google.");
            return;
        }

        int currentUserId = 1;
        var scheduleLocalStorage = FindObjectOfType<ScheduleLocalStorage>();
        List<ScheduledSession> sessionsToSync = scheduleLocalStorage.GetScheduleForUser(currentUserId);

        Debug.Log($"Found {sessionsToSync.Count} sessions to sync with Google Calendar.");

        foreach (var session in sessionsToSync)
        {
            var task = DatabaseManager.db.Find<Task>(session.task_id);

            if (task != null)
            {
                // Use the properties from the fetched task object
                string summary = $"Study: {task.title}";
                string description = $"A {task.duration_minutes} minute study session for '{task.title}'.\n\nDetails: {task.description}";

                await GoogleCalendarManager.CreateCalendarEventAsync(summary, description, session.start_time, session.end_time);
            }
            else
            {
                Debug.LogWarning($"Could not find task with ID {session.task_id} for scheduled session. Skipping sync for session ID {session.id}.");
            }
        }

        // Refresh the calendar to show the newly created events
        GenerateCalendar(currentDate.Year, currentDate.Month);
    }

    void SaveNote()
    {
        if (string.IsNullOrEmpty(noteInputField.text)) return;

        string newNoteText = noteInputField.text.Trim();
        if (string.IsNullOrEmpty(newNoteText)) return;

        var existingNote = DatabaseManager.db.Table<UserNote>()
            .Where(n => n.date_key == currentDateKey && n.user_id == this.userId)
            .FirstOrDefault();

        if (existingNote != null)
        {
            // Update existing note
            existingNote.note_text += "\n" + newNoteText;
            existingNote.last_modified = DateTime.UtcNow;
            DatabaseManager.db.Update(existingNote);
            noteDisplayText.text = existingNote.note_text;
        }
        else
        {
            // Insert new note
            var note = new UserNote
            {
                user_id = this.userId,
                date_key = currentDateKey,
                note_text = newNoteText,
                last_modified = DateTime.UtcNow
            };
            DatabaseManager.db.Insert(note);
            noteDisplayText.text = note.note_text;
        }
    }


    void ClearNote()
    {
        if (string.IsNullOrEmpty(currentDateKey)) return;

        string savedNotes = PlayerPrefs.GetString(currentDateKey, "");
        if (string.IsNullOrEmpty(savedNotes)) return;

        string[] lines = savedNotes.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length == 0) return;

        string[] updatedLines = new string[lines.Length - 1];
        Array.Copy(lines, updatedLines, lines.Length - 1);

        string updatedNotes = string.Join("\n", updatedLines);

        if (string.IsNullOrEmpty(updatedNotes))
            PlayerPrefs.DeleteKey(currentDateKey);
        else
            PlayerPrefs.SetString(currentDateKey, updatedNotes);

        noteDisplayText.text = string.IsNullOrEmpty(updatedNotes) ? "(Add a New Note)" : updatedNotes;
    }

    public void ToggleUIPanels()
    {
        if (UIPanel1 != null)
            UIPanel1.SetActive(!UIPanel1.activeSelf);

        if (UIPanel2 != null)
            UIPanel2.SetActive(!UIPanel2.activeSelf);
    }

    string GetDayWithSuffix(int day)
    {
        if (day >= 11 && day <= 13) return day + "th";

        switch (day % 10)
        {
            case 1: return day + "st";
            case 2: return day + "nd";
            case 3: return day + "rd";
            default: return day + "th";
        }
    }
}
