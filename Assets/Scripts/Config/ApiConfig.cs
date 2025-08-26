public static class ApiConfig
{
    public const string BASE_URL = "https://studybuddy-api-w8g5.onrender.com";

    public static class Endpoints
    {
        public const string GenerateAiSchedule = "/generate_ai_schedule";
        public const string Ping = "/ping";
        public const string GetUserState = "/user_state";
        public const string Chat = "/chat";
    }

    public static string GetFullUrl(string endpoint)
    {
        return $"{BASE_URL}{endpoint}";
    }
}