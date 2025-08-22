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
    public GameObject CalendarUI;
    public GameObject DayUI;
    public TMP_InputField noteInputField;
    public Button saveNoteButton;
    public Button clearNoteButton;
    public GameObject noteItemPrefab;
    public Transform notesContainer;
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

        // Display all notes for the selected day
        DisplayNotesForDay(currentDateKey);

        string formattedDate = $"{clickedDate:dddd MMMM} {GetDayWithSuffix(day)} {clickedDate:yyyy}";
        selectedDateLabel.text = formattedDate;
        if (CalendarUI != null)
            CalendarUI.SetActive(!CalendarUI.activeSelf);

        if (DayUI != null)
            DayUI.SetActive(!DayUI.activeSelf);

        var savedNote = DatabaseManager.db.Table<UserNote>()
            .Where(n => n.date_key == currentDateKey && n.user_id == this.userId)
            .FirstOrDefault();

        string noteText = savedNote?.note_text ?? "";
        noteInputField.text = ""; // Clear input field

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

    void DisplayNotesForDay(string dateKey)
    {
        // Clear any old note items from the list
        foreach (Transform child in notesContainer)
        {
            Destroy(child.gameObject);
        }

        // Get all notes for this day from the database
        var notesForDay = DatabaseManager.db.Table<UserNote>()
            .Where(n => n.date_key == dateKey && n.user_id == this.userId)
            .ToList();

        // Create a new UI element for each note
        foreach (var note in notesForDay)
        {
            GameObject noteObject = Instantiate(noteItemPrefab, notesContainer);
            noteObject.GetComponent<NoteItemUI>().Setup(note, this);
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

        // Create a new note and insert into the database
        var newNote = new UserNote
        {
            user_id = this.userId,
            date_key = currentDateKey,
            note_text = newNoteText,
            last_modified = DateTime.UtcNow
        };
        DatabaseManager.db.Insert(newNote);

        DisplayNotesForDay(currentDateKey);
        noteInputField.text = ""; // Clear input field after saving
    }


    public void ClearNote(int noteId)
    {
        DatabaseManager.db.Delete<UserNote>(noteId);
        DisplayNotesForDay(currentDateKey);
    }

    public void ToggleUIPanels()
    {
        if (CalendarUI != null)
            CalendarUI.SetActive(!CalendarUI.activeSelf);

        if (DayUI != null)
            DayUI.SetActive(!DayUI.activeSelf);
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
