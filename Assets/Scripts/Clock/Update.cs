using UnityEngine;
using TMPro;

public class TextSelfUpdaterTMP : MonoBehaviour
{
    public TMP_Text uiText;

    void Update()
    {
        if (uiText != null)
        {
            // Re-assign the same value so TMP forces update
            uiText.text = uiText.text;
            uiText.ForceMeshUpdate();
        }
    }
}
