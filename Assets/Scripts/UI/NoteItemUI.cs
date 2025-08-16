using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NoteItemUI : MonoBehaviour
{
    public TextMeshProUGUI noteText;
    public Button deleteButton;

    private int noteId;
    private CalendarManager calendarManager;

    void Awake()
    {
        Debug.Log("--- Running Diagnostic on NoteItemUI for " + this.gameObject.name + " ---");
        if (noteText == null)
        {
            Debug.LogError("DIAGNOSTIC RESULT: The 'noteText' variable IS NULL on this prefab instance!");
        }
        else
        {
            Debug.Log("DIAGNOSTIC RESULT: The 'noteText' variable is ASSIGNED.");
        }

        if (deleteButton == null)
        {
            Debug.LogError("DIAGNOSTIC RESULT: The 'deleteButton' variable IS NULL on this prefab instance!");
        }
        else
        {
            Debug.Log("DIAGNOSTIC RESULT: The 'deleteButton' variable is ASSIGNED.");
        }
        Debug.Log("--- End of Diagnostic ---");
    }

    public void Setup(UserNote note, CalendarManager manager)
    {
        this.noteId = note.id;
        this.noteText.text = note.note_text;
        this.calendarManager = manager;

        deleteButton.onClick.RemoveAllListeners();
        deleteButton.onClick.AddListener(OnDeleteButtonClicked);
    }

    void OnDeleteButtonClicked()
    {
        if (calendarManager != null)
        {
            calendarManager.ClearNote(noteId);
        }
    }
}