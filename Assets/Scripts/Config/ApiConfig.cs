public static class ApiConfig
{
    public const string BASE_URL = "https://studybuddy-api-w8g5.onrender.com";
    
    public static class Endpoints
    {
        public const string GenerateSchedule = "/generate_ai_schedule";
        public const string Ping = "/ping";
    }

    public static string GetFullUrl(string endpoint)
    {
        return $"{BASE_URL}{endpoint}";
    }
}