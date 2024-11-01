using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VolumeSettings : MonoBehaviour
{
    [Header("Volume Sliders")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    private const string MASTER_VOL_KEY = "MasterVolume";
    private const string MUSIC_VOL_KEY = "MusicVolume";
    private const string SFX_VOL_KEY = "SFXVolume";

    private void Start()
    {
        // Load saved volumes
        LoadVolumes();

        // Add listeners to sliders
        masterSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
    }

    private void LoadVolumes()
    {
        // Load saved values (default to 1 if not found)
        masterSlider.value = PlayerPrefs.GetFloat(MASTER_VOL_KEY, 1f);
        musicSlider.value = PlayerPrefs.GetFloat(MUSIC_VOL_KEY, 1f);
        sfxSlider.value = PlayerPrefs.GetFloat(SFX_VOL_KEY, 1f);

        // Apply loaded values
        OnMasterVolumeChanged(masterSlider.value);
        OnMusicVolumeChanged(musicSlider.value);
        OnSFXVolumeChanged(sfxSlider.value);
    }

    private void OnMasterVolumeChanged(float volume)
    {
        AudioManager.Instance.SetMasterVolume(volume);
        PlayerPrefs.SetFloat(MASTER_VOL_KEY, volume);
        PlayerPrefs.Save();
    }

    private void OnMusicVolumeChanged(float volume)
    {
        AudioManager.Instance.SetMusicVolume(volume);
        PlayerPrefs.SetFloat(MUSIC_VOL_KEY, volume);
        PlayerPrefs.Save();
    }

    private void OnSFXVolumeChanged(float volume)
    {
        AudioManager.Instance.SetSFXVolume(volume);
        PlayerPrefs.SetFloat(SFX_VOL_KEY, volume);
        PlayerPrefs.Save();
    }

    // Optional: Method to play a test sound when adjusting SFX volume
    public void PlayTestSound()
    {
        AudioManager.Instance.PlaySFX("UIClick");  // Replace with whatever sound you want to use for testing
    }
}
