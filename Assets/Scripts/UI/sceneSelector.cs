using UnityEngine;
using UnityEngine.SceneManagement;

public class sceneSelectButton : MonoBehaviour
{
    public string sceneName;
    public void changeScene()
    {
        SceneManager.LoadScene(sceneName);
    }
}
