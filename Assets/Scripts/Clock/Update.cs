using UnityEngine;
using TMPro;

public class TextSelfUpdaterTMP : MonoBehaviour
{
    public TMP_Text uiText;
    void Update()
    {
        if (uiText != null)
        {
            uiText.text = uiText.text;
            uiText.ForceMeshUpdate();
        }
    }
}
