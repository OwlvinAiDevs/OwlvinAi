using System;

public static class ResponseFormatter
{
    public static string CleanGPTResponse(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return "(Empty GPT response)";

        // Strip markdown/code formatting
        raw = raw.Replace("```json", "").Replace("```", "");

        // Normalize smart quotes
        raw = raw.Replace("“", "\"").Replace("”", "\"")
                 .Replace("‘", "'").Replace("’", "'");

        // Try to extract JSON if present
        int jsonStart = raw.IndexOf("[{");
        string naturalText = (jsonStart >= 0) ? raw.Substring(0, jsonStart).Trim() : raw.Trim();
        string jsonBlock = (jsonStart >= 0) ? raw.Substring(jsonStart).Trim() : "";

        // Reconstruct cleaned response
        string result = naturalText;

        if (!string.IsNullOrEmpty(jsonBlock))
            result += "\n\n📦 Inferred Task:\n" + jsonBlock;

        return result;
    }
}
