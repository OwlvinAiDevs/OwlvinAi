using UnityEngine;

public class UIToggle2 : MonoBehaviour
{
    public GameObject UIPanel1;
    public GameObject UIPanel2;

    public void ToggleUI2()
    {
        if (UIPanel1 != null)
        {
            UIPanel1.SetActive(!UIPanel1.activeSelf);
        }

        if (UIPanel2 != null)
        {
            UIPanel2.SetActive(!UIPanel2.activeSelf);
        }
    }
}
