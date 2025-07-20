using System;
using System.Collections;
using UnityEngine;
using TMPro;

public class AIScheduleNoteImporter : MonoBehaviour
{
    
    public TextMeshProUGUI aiOutputText;

    
    public float timeoutSeconds = 10f;

    public void StartImport()
    {
        StartCoroutine(WaitForAIAndImport());
    }

    private IEnumerator WaitForAIAndImport()
    {
        float timer = 0f;

        while (string.IsNullOrWhiteSpace(aiOutputText?.text) && timer < timeoutSeconds)
        {
            yield return new WaitForSeconds(0.1f);
            timer += 0.1f;
        }

        if (!string.IsNullOrWhiteSpace(aiOutputText?.text))
        {
            ImportScheduleNotes(aiOutputText.text);
            Debug.Log("✅ Schedule notes imported from AI output.");
        }
        else
        {
            Debug.LogWarning("⚠ Timed out waiting for AI response.");
        }
    }

    private void ImportScheduleNotes(string scheduleText)
    {
        string[] lines = scheduleText.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

        string currentTask = "";
        string currentCategory = "";
        string currentDateKey = "";

        foreach (string line in lines)
        {
            if (line.StartsWith("📝 Task: "))
            {
                currentTask = line.Substring("📝 Task: ".Length).Trim();
            }
            else if (line.StartsWith("📂 Category: "))
            {
                currentCategory = line.Substring("📂 Category: ".Length).Trim();
            }
            else if (line.StartsWith("⏰ Start: "))
            {
                DateTime start;
                if (DateTime.TryParse(line.Substring("⏰ Start: ".Length).Trim(), out start))
                {
                    currentDateKey = start.ToString("yyyy-MM-dd");
                    string note = $"{currentTask} ({currentCategory}) at {start:HH:mm}";

                    string existingNotes = PlayerPrefs.GetString(currentDateKey, "");
                    string updatedNotes = string.IsNullOrEmpty(existingNotes) ? note : existingNotes + "\n" + note;

                    PlayerPrefs.SetString(currentDateKey, updatedNotes);
                }
            }
        }
    }
}
