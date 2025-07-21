using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class InferredTask
{
    public string task;
    public string start;
    public string end;
    public string category;
    public int break_after = 5; // Optional default fallback
}

public static class ResponseFormatter
{
    public static string CleanGPTResponse(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return "(Empty GPT response)";

        // Strip markdown/code formatting
        raw = raw.Replace("```json", "").Replace("```", "").Trim();

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
