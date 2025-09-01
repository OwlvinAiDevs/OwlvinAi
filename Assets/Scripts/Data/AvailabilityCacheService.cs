public static class AvailabilityCacheService
{
    // Recalculate and cache available time slots for a user
    public static void RecalculateAvailability(int userId)
    {
        DateTime now = DateTime.UtcNow;
        DateTime future = now.AddDays(7);

        // Remove old cached availability for this user in the next 7 days
        DatabaseManager.db.Execute("DELETE FROM cached_availability WHERE user_id = ? AND start_time >= ? AND end_time <= ?", userId, now, future);

        // Get all blocked times for this user in the next 7 days
        var blocked = DatabaseManager.db.Table<BlockedTime>()
            .Where(b => b.user_id == userId && b.end_time > now && b.start_time < future)
            .OrderBy(b => b.start_time)
            .ToList();

        // Get all scheduled sessions for this user in the next 7 days
        var sessions = DatabaseManager.db.Table<ScheduledSession>()
            .Where(s => s.user_id == userId && s.end_time > now && s.start_time < future)
            .OrderBy(s => s.start_time)
            .ToList();

        // Merge blocked and scheduled into a single list of unavailable periods
        var unavailable = new List<(DateTime start, DateTime end)>();
        unavailable.AddRange(blocked.Select(b => (b.start_time, b.end_time)));
        unavailable.AddRange(sessions.Select(s => (s.start_time, s.end_time)));
        unavailable = unavailable.OrderBy(u => u.start).ToList();

        // Find available slots between unavailable periods
        var available = new List<(DateTime start, DateTime end)>();
        DateTime current = now;
        foreach (var period in unavailable)
        {
            if (period.start > current)
            {
                available.Add((current, period.start));
            }
            if (period.end > current)
            {
                current = period.end;
            }
        }
        if (current < future)
        {
            available.Add((current, future));
        }

        // Save available slots to cached_availability
        foreach (var slot in available)
        {
            var avail = new CachedAvailability
            {
                user_id = userId,
                start_time = slot.start,
                end_time = slot.end,
                source = "inferred"
            };
            DatabaseManager.db.Insert(avail);
        }
    }
}
