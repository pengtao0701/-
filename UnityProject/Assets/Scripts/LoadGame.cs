using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// GameVolume  float
/// BestScore   int
/// </summary>

public class LoadGame : MonoBehaviour
{
    public GameObject gameSettingPanel;
    private GameManager gameManager;
    public AudioSource bgmAudio;
    public Slider audioSlider;
    // ”Œœ∑“Ù¡ø ƒ¨»œ30%
    private float gameVolume = 0.3f;

    private void Awake()
    {
        if (PlayerPrefs.HasKey("GameVolume"))
        {
            bgmAudio.volume = PlayerPrefs.GetFloat("GameVolume");
            audioSlider.value = PlayerPrefs.GetFloat("GameVolume");
        }
        else
        {
            PlayerPrefs.SetFloat("GameVolume", gameVolume);
            bgmAudio.volume = gameVolume;
            audioSlider.value = gameVolume;
        }

        
    }

    private void Update()
    {
        bgmAudio.volume = audioSlider.value;
    }

    public void LoadTheGame()
    {
        SceneManager.LoadScene(1);
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    public void OpenSetting()
    {
        gameSettingPanel.SetActive(true);
    }

    public void CloseSetting()
    {
        gameSettingPanel.SetActive(false);
    }

    public void VolumeChange()
    {
        PlayerPrefs.SetFloat("GameVolume", audioSlider.value);
    }
}
