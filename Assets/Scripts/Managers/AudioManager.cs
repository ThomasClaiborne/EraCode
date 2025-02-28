using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource weaponTailSource;


    [Header("Mixer")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;
    [SerializeField] private AudioMixerGroup musicMixerGroup;

    [System.Serializable]
    public class Sound
    {
        public string name;
        public AudioClip clip;
        [Range(0f, 1f)]
        public float volume = 1f;
        [Range(0.1f, 3f)]
        public float pitch = 1f;
    }

    [System.Serializable]
    public class WeaponSoundSet
    {
        public string setName;              // e.g., "HeavyMachineGun", "Pistol"
        public bool isAutomatic = false;    // Determines if weapon uses automatic weapon sound logic

        [Header("Basic Sounds")]
        public AudioClip[] normalShots;     // All weapons need at least normal shots

        [Header("Automatic Weapon Sounds")]
        [Tooltip("Only used if isAutomatic is true")]
        public AudioClip firstShot;         // Optional for automatic weapons
        public AudioClip tailSound;         // Optional for automatic weapons
        public AudioClip lastShot;          // Optional for automatic weapons

        [Header("Automatic Sound Settings")]
        public float tailFadeTime = 0.2f;
        public float sequenceResetTime = 0.5f;
    }

    [System.Serializable]
    public class SoundSet
    {
        public string setName;          // e.g., "EnemyImpact", "PlayerImpact"
        public AudioClip[] Sounds;  // Array of variations
    }

    [Header("Sound Collections")]
    [SerializeField] private Sound[] sfx;
    [SerializeField] private Sound[] music;
    [SerializeField] private WeaponSoundSet[] weaponSoundSets;
    [SerializeField] private SoundSet[] sfxSets;

    private Dictionary<string, Sound> sfxLookup = new Dictionary<string, Sound>();
    private Dictionary<string, Sound> musicLookup = new Dictionary<string, Sound>();
    private Dictionary<string, WeaponSoundSet> weaponSoundLookup = new Dictionary<string, WeaponSoundSet>();
    private Dictionary<string, SoundSet> sfxSoundLookup = new Dictionary<string, SoundSet>();
    private Dictionary<string, float> lastShotTimes = new Dictionary<string, float>();
    private Coroutine currentTailFade;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioDictionaries();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void InitializeAudioDictionaries()
    {
        // Initialize SFX dictionary
        foreach (Sound s in sfx)
        {
            if (!sfxLookup.ContainsKey(s.name))
            {
                sfxLookup.Add(s.name, s);
            }
        }

        // Initialize Music dictionary
        foreach (Sound s in music)
        {
            if (!musicLookup.ContainsKey(s.name))
            {
                musicLookup.Add(s.name, s);
            }
        }

        // Initialize Weapon Sounds dictionary
        foreach (WeaponSoundSet ws in weaponSoundSets)
        {
            if (!weaponSoundLookup.ContainsKey(ws.setName))
            {
                weaponSoundLookup.Add(ws.setName, ws);
            }
        }

        // Initialize Enemy Impact Sounds dictionary
        foreach (SoundSet eis in sfxSets)
        {
            if (!sfxSoundLookup.ContainsKey(eis.setName))
            {
                sfxSoundLookup.Add(eis.setName, eis);
            }
        }

        // Setup AudioSources
        if (sfxSource != null) sfxSource.outputAudioMixerGroup = sfxMixerGroup;
        if (musicSource != null) musicSource.outputAudioMixerGroup = musicMixerGroup;
        if (weaponTailSource != null) weaponTailSource.outputAudioMixerGroup = sfxMixerGroup;

        }

    public void PlaySFX(string name)
    {
        if (sfxLookup.TryGetValue(name, out Sound sound))
        {
            sfxSource.PlayOneShot(sound.clip, sound.volume);
        }
        else
        {
            Debug.LogWarning($"SFX not found: {name}");
        }
    }

    public void PlayMusic(string name, bool loop = true)
    {
        if (musicLookup.TryGetValue(name, out Sound sound))
        {
            musicSource.clip = sound.clip;
            musicSource.loop = loop;
            musicSource.volume = sound.volume;
            musicSource.pitch = sound.pitch;
            musicSource.Play();
        }
        else
        {
            Debug.LogWarning($"Music not found: {name}");
        }
    }

    private float ConvertToDecibels(float volume)
    {
        // Return minimum decibels (-80dB) when volume is 0
        if (volume <= 0)
            return -80f;

        // Convert linear (0-1) volume to decibels
        return Mathf.Log10(volume) * 20f;
    }

    public void SetMasterVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        float mixerVolume = ConvertToDecibels(volume);
        audioMixer.SetFloat("MasterVolume", mixerVolume);
    }

    public void SetSFXVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        float mixerVolume = ConvertToDecibels(volume);
        audioMixer.SetFloat("SFXVolume", mixerVolume);
    }

    public void SetMusicVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        float mixerVolume = ConvertToDecibels(volume);
        audioMixer.SetFloat("MusicVolume", mixerVolume);
    }

    public void PlayWeaponSound(string soundSetName, bool isLastShot = false)
    {
        if (!weaponSoundLookup.TryGetValue(soundSetName, out WeaponSoundSet soundSet))
        {
            Debug.LogWarning($"Weapon sound set not found: {soundSetName}");
            return;
        }

        AudioClip shotToPlay;

        if (soundSet.isAutomatic)
        {
            // Automatic weapon logic
            float lastShotTime = 0f;
            lastShotTimes.TryGetValue(soundSetName, out lastShotTime);
            float timeSinceLastShot = Time.time - lastShotTime;

            if (isLastShot && soundSet.lastShot != null)
            {
                shotToPlay = soundSet.lastShot;
            }
            else if (timeSinceLastShot > soundSet.sequenceResetTime && soundSet.firstShot != null)
            {
                shotToPlay = soundSet.firstShot;
            }
            else
            {
                int randomIndex = Random.Range(0, soundSet.normalShots.Length);
                shotToPlay = soundSet.normalShots[randomIndex];
            }

            // Play tail sound for automatic weapons
            if (soundSet.tailSound != null)
            {
                PlayWeaponTail(soundSet);
            }

            // Update last shot time for automatic weapons
            lastShotTimes[soundSetName] = Time.time;
        }
        else
        {
            // Simple weapon logic - just play random normal shot
            int randomIndex = Random.Range(0, soundSet.normalShots.Length);
            shotToPlay = soundSet.normalShots[randomIndex];
        }

        // Play the main shot sound
        sfxSource.PlayOneShot(shotToPlay);
    }

    private void PlayWeaponTail(WeaponSoundSet soundSet)
    {
        if (currentTailFade != null)
        {
            StopCoroutine(currentTailFade);
        }

        weaponTailSource.clip = soundSet.tailSound;
        weaponTailSource.volume = 1f;
        weaponTailSource.Play();

        currentTailFade = StartCoroutine(FadeOutTail(soundSet.tailFadeTime));
    }

    private IEnumerator FadeOutTail(float fadeTime)
    {
        float startVolume = weaponTailSource.volume;
        float elapsedTime = 0f;

        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.deltaTime;
            weaponTailSource.volume = Mathf.Lerp(startVolume, 0f, elapsedTime / fadeTime);
            yield return null;
        }

        weaponTailSource.Stop();
        weaponTailSource.volume = startVolume;
    }

    public void PlaySFXSet(string setName)
    {
        if (!sfxSoundLookup.TryGetValue(setName, out SoundSet set))
        {
            Debug.LogWarning($"sound set not found: {setName}");
            return;
        }

        if (set.Sounds == null || set.Sounds.Length == 0)
        {
            Debug.LogWarning($"No sounds in set: {setName}");
            return;
        }

        int randomIndex = Random.Range(0, set.Sounds.Length);
        AudioClip sfx = set.Sounds[randomIndex];

        sfxSource.PlayOneShot(sfx);
    }
}