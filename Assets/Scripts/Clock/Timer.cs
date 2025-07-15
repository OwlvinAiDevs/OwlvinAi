using UnityEngine;
using TMPro;

public class TimerController : MonoBehaviour
{
    public TextMeshProUGUI timerDisplay;
    private int totalSeconds = 0;
    private float countdown = 0f;
    private bool isRunning = false;
    private int lastDisplayedSeconds = -1;
    private bool hasPlayedSound = false;

    public AudioClip timerEndSound;
    private AudioSource audioSource;

    // Pomodoro Variables
    private bool pomodoroActive = false;
    private int pomodoroStep = 0;
    private int pomodoroCycles = 0;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource != null && timerEndSound != null)
        {
            audioSource.clip = timerEndSound;
        }

        UpdateDisplay();
    }

    void Update()
    {
        if (isRunning && totalSeconds > 0)
        {
            countdown += Time.deltaTime;

            if (countdown >= 1f)
            {
                int secondsToSubtract = Mathf.FloorToInt(countdown);
                totalSeconds -= secondsToSubtract;
                countdown -= secondsToSubtract;

                if (totalSeconds <= 0)
                {
                    totalSeconds = 0;
                    isRunning = false;

                    if (!hasPlayedSound)
                    {
                        PlayTimerEndSound();
                        hasPlayedSound = true;
                    }

                    if (pomodoroActive)
                    {
                        AdvancePomodoro();
                    }
                }

                UpdateDisplay();
            }
        }
    }

    public void AddSeconds(int amount)
    {
        totalSeconds = Mathf.Max(0, totalSeconds + amount);
        hasPlayedSound = false;
        UpdateDisplay();
    }

    public void PlayTimer()
    {
        if (!isRunning && totalSeconds > 0)
        {
            isRunning = true;
        }
    }

    public void PauseTimer()
    {
        isRunning = false;
    }

    void UpdateDisplay()
    {
        if (totalSeconds != lastDisplayedSeconds)
        {
            int hours = totalSeconds / 3600;
            int minutes = (totalSeconds % 3600) / 60;
            int seconds = totalSeconds % 60;
            timerDisplay.text = $"{hours:00}:{minutes:00}:{seconds:00}";
            lastDisplayedSeconds = totalSeconds;
        }
    }

    void PlayTimerEndSound()
    {
        if (timerEndSound != null && audioSource != null)
        {
            audioSource.Play();
        }
    }

   

    public void TogglePomodoro()
    {
        if (pomodoroActive)
        {
           
            pomodoroActive = false;
            isRunning = false;
            totalSeconds = 0;
            pomodoroStep = 0;
            pomodoroCycles = 0;
            UpdateDisplay();
        }
        else
        {
            
            pomodoroActive = true;
            pomodoroStep = 0;
            pomodoroCycles = 0;
            StartPomodoroPhase();
        }
    }

    private void StartPomodoroPhase()
    {
        switch (pomodoroStep)
        {
            case 0: 
                totalSeconds = 25 * 60;
                break;
            case 1:
            case 2:
            case 3:
            case 4: 
                totalSeconds = 5 * 60;
                break;
            case 5: 
                totalSeconds = 60 * 60;
                break;
            default:
                pomodoroStep = 0;
                totalSeconds = 25 * 60;
                break;
        }

        isRunning = true;
        hasPlayedSound = false;
        UpdateDisplay();
    }

    private void AdvancePomodoro()
    {
        if (pomodoroStep == 0)
        {
           
            pomodoroCycles++;
            pomodoroStep = (pomodoroCycles % 4 == 0) ? 5 : 1; 
        }
        else
        {

            pomodoroStep = 0;
        }

        StartPomodoroPhase();
    }
}
