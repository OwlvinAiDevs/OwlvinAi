using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NoteItemUI : MonoBehaviour
{
    public TextMeshProUGUI noteText;
    public Button deleteButton;

    private int noteId;
    private CalendarManager calendarManager;

    public void Setup(UserNote note, CalendarManager manager)
    {
        this.noteId = note.id;
        this.noteText.text = note.note_text;
        this.calendarManager = manager;

        deleteButton.onClick.AddListener(OnDeleteButtonClicked);
    }

    void OnDeleteButtonClicked()
    {
        calendarManager.ClearNote(noteId);
    }
}