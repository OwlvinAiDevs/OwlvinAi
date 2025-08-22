using UnityEngine;

public class SoundController : MonoBehaviour
{
    [Header("Assign the AudioSource to control")]
    public AudioSource audioSource;

    
    public void StopSound()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
}
