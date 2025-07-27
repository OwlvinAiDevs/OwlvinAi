using UnityEngine;

public class ForceResolution : MonoBehaviour
{
    void Awake()
    {
        int width = 350; 
        int height = 750;
        bool fullscreen = false; 
        Screen.SetResolution(width, height, fullscreen);
    }
}
