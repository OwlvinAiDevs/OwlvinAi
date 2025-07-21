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
    public static string CleanGPTResponse(string raw, out List<InferredTask> extractedTasks)
    {
        extractedTasks = new List<InferredTask>();

        if (string.IsNullOrEmpty(raw))
            return "(Empty GPT response)";

        // Clean formatting
        raw = raw.Replace("```json", "").Replace("```", "").Trim();
        raw = raw.Replace("“", "\"").Replace("”", "\"").Replace("‘", "'").Replace("’", "'");

        // Try extract JSON block
        int jsonStart = raw.IndexOf("[{");
        string naturalText = (jsonStart >= 0) ? raw.Substring(0, jsonStart).Trim() : raw.Trim();
        string jsonBlock = (jsonStart >= 0) ? raw.Substring(jsonStart).Trim() : "";

        if (!string.IsNullOrEmpty(jsonBlock))
        {
            try
            {
                InferredTask[] tasks = JsonHelper.FromJson<InferredTask>(jsonBlock);
                extractedTasks.AddRange(tasks);
            }
            catch (Exception e)
            {
                Debug.LogError("❌ Failed to parse inferred tasks from GPT JSON block: " + e.Message);
                Debug.Log("Raw JSON block:\n" + jsonBlock);
            }
        }

        return naturalText;
    }
}
