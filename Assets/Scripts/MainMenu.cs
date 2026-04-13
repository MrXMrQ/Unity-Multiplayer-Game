using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void Play()
    {
        SceneManager.LoadScene("PlayScene");
    }

    public void Options()
    {
        // Hier könntest du ein Optionsmenü öffnen oder eine andere Szene laden
        SceneManager.LoadScene("OptionsScene");
    }

    public void Quit()
    {
        Application.Quit();
    }
}