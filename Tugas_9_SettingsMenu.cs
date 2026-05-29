using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Audio;

public class SettingsMenu : MonoBehaviour
{
    public Slider musicSlider;
    public Slider sfxSlider;
    public AudioMixer audioMixer;
    public void BackToGame()
    {
        StartCoroutine(LoadSceneDelay(previousScene));
    }

    private string previousScene;

    void Start()
    {
        float music = PlayerPrefs.GetFloat("MusicVolume", 0.75f);
        float sfx = PlayerPrefs.GetFloat("SFXVolume", 0.75f);

        musicSlider.SetValueWithoutNotify(music);
        sfxSlider.SetValueWithoutNotify(sfx);

        ApplyMusicVolume(music);
        ApplySFXVolume(sfx);
    }

    void ApplyMusicVolume(float value)
    {
        float dB = value > 0.001f ? Mathf.Log10(value) * 20 : -80f;
        audioMixer.SetFloat("MusicVolume", dB);
    }

    void ApplySFXVolume(float value)
    {
        float dB = value > 0.001f ? Mathf.Log10(value) * 20 : -80f;
        audioMixer.SetFloat("SFXVolume", dB);
    }

    public void SetMusicVolume(float value)
    {
        ApplyMusicVolume(value);
        PlayerPrefs.SetFloat("MusicVolume", value);
    }

    public void SetSFXVolume(float value)
    {
        ApplySFXVolume(value);
        PlayerPrefs.SetFloat("SFXVolume", value);
    }

    public void BackToMenu()
    {
        StartCoroutine(LoadSceneDelay("MainMenu"));
    }

    System.Collections.IEnumerator LoadSceneDelay(string sceneName)
    {
        yield return new WaitForSeconds(0.2f);
        SceneManager.LoadScene(sceneName);
    }
}