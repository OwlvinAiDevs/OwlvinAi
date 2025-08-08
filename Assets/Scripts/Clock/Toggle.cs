using UnityEngine;
using TMPro;

public class ToggleButton : MonoBehaviour
{
    public Transform targetObject;
    public Transform textObject;
    public GameObject objectToToggle1;
    public GameObject objectToToggle2;

    private bool toggleState = false;

    public void FlipX()
    {
        // Flip scale X on target and text
        if (targetObject != null)
        {
            Vector3 scale = targetObject.localScale;
            scale.x *= -1f;
            targetObject.localScale = scale;
        }

        if (textObject != null)
        {
            Vector3 scale = textObject.localScale;
            scale.x *= -1f;
            textObject.localScale = scale;
        }

        toggleState = !toggleState;

        SetCanvasGroupState(objectToToggle1, toggleState);
        SetCanvasGroupState(objectToToggle2, !toggleState);
    }

    private void SetCanvasGroupState(GameObject obj, bool visible)
    {
        if (obj == null) return;

        CanvasGroup cg = obj.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = obj.AddComponent<CanvasGroup>();
        }

        cg.alpha = visible ? 1f : 0f;
        cg.interactable = visible;
        cg.blocksRaycasts = visible;
    }
}
