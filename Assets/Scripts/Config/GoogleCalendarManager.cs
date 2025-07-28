using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Manages interactions with Google Calendar API.
/// This is a static class that provides methods to fetch and create calendar events.
/// It needs to be initialized with a CalendarService instance from GoogleAuthenticator.
/// </summary>
public static class GoogleCalendarManager
{
    private static CalendarService calendarService;

    /// <summary>
    /// Sets the CalendarService instance. This should be called once after successful authentication.
    /// </summary>
    /// <param name="service">The authenticated CalendarService instance.</param>
    public static void Initialize(CalendarService service)
    {
        if (service == null)
        {
            Debug.LogError("Cannot initialize GoogleCalendarManager with a null CalendarService.");
            return;
        }
        calendarService = service;
        Debug.Log("GoogleCalendarManager initialized successfully.");
    }

    /// <summary>
    /// Fetches events from the user's "primary" calendar within a specified time range.
    /// </summary>
    /// <param name="startTime">The start of the time range.</param>
    /// <param name="endTime">The end of the time range.</param>
    /// <returns>A Task that resolves to a list of calendar events.</returns>
    public static async Task<List<Google.Apis.Calendar.v3.Data.Event>> GetCalendarEventsAsync(DateTime startTime, DateTime endTime)
    {
        if (calendarService == null)
        {
            Debug.LogError("GoogleCalendarManager is not initialized. Call Initialize() first.");
            return new List<Google.Apis.Calendar.v3.Data.Event>();
        }

        Debug.Log($"Fetching calendar events from {startTime} to {endTime}...");

        try
        {
            // Define the request to get the events from the "primary" calendar
            EventsResource.ListRequest request = calendarService.Events.List("primary");
            request.TimeMinDateTimeOffset = new DateTimeOffset(startTime);
            request.TimeMaxDateTimeOffset = new DateTimeOffset(endTime);
            request.ShowDeleted = false;
            request.SingleEvents = true; // Expand recurring events into single instances
            request.MaxResults = 100; // Limit the number of results
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

            // Execute the request asynchronously
            Events events = await request.ExecuteAsync();

            if (events.Items != null && events.Items.Count > 0)
            {
                Debug.Log($"{events.Items.Count} events found.");
                return new List<Google.Apis.Calendar.v3.Data.Event>(events.Items);
            }
            else
            {
                Debug.Log("No events found in the specified time range.");
                return new List<Google.Apis.Calendar.v3.Data.Event>();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error fetching calendar events: {e.Message}\n{e.StackTrace}");
            return new List<Google.Apis.Calendar.v3.Data.Event>();
        }
    }

    /// <summary>
    /// Creates a new event on the user's "primary" calendar.
    /// </summary>
    /// <param name="summary">The title or summary of the event (e.g., "Study Session: Physics").</param>
    /// <param name="description">The detailed description of the event.</param>
    /// <param name="startDateTime">The start date and time of the event.</param>
    /// <param name="endDateTime">The end date and time of the event.</param>
    /// <returns>A Task that resolves to the created Google Calendar Event, or null if it fails.</returns>
    public static async Task<Google.Apis.Calendar.v3.Data.Event> CreateCalendarEventAsync(string summary, string description, DateTime startDateTime, DateTime endDateTime)
    {
        if (calendarService == null)
        {
            Debug.LogError("Google Calendar service is not initialized. Call Initialize() first.");
            return null;
        }

        // Create the event object with all the necessary details
        Google.Apis.Calendar.v3.Data.Event newEvent = new Google.Apis.Calendar.v3.Data.Event()
        {
            Summary = summary,
            Description = description,
            Start = new EventDateTime()
            {
                DateTimeDateTimeOffset = new DateTimeOffset(startDateTime),
            },
            End = new EventDateTime()
            {
                DateTimeDateTimeOffset = new DateTimeOffset(endDateTime),
            },
        };

        Debug.Log($"Creating event: '{summary}' from {startDateTime} to {endDateTime}...");

        try
        {
            // Define and execute the insert request
            EventsResource.InsertRequest request = calendarService.Events.Insert(newEvent, "primary");
            Google.Apis.Calendar.v3.Data.Event createdEvent = await request.ExecuteAsync();
            Debug.Log($"Event created successfully. View at: {createdEvent.HtmlLink}");
            return createdEvent;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error creating calendar event: {e.Message}\n{e.StackTrace}");
            return null;
        }
    }
}