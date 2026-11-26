using System;
using System.Collections.Generic;
using UnityEngine;

namespace Platformer.Managers
{
    [Serializable]
    public struct NamedAudioClip
    {
        public string name;
        public AudioClip clip;
    }

    /// <summary>
    /// Singleton audio manager managing music and sound effect channels.
    /// Supported named clips: jump, coin, hit, enemy death, checkpoint, win, lose, music.
    /// Missing clips are skipped silently with no console noise.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager instance;

        public static AudioManager Instance
        {
            get
            {
                if (instance == null)
                {
                    var existing = UnityEngine.Object.FindFirstObjectByType<AudioManager>();
                    if (existing != null)
                    {
                        instance = existing;
                    }
                    else
                    {
                        GameObject go = new GameObject("[AudioManager]");
                        instance = go.AddComponent<AudioManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource musicSource;

        [Header("Named Sound Clips")]
        [SerializeField] private NamedAudioClip[] clips;

        private Dictionary<string, AudioClip> clipMap;

        public float MusicVolume => musicSource != null ? musicSource.volume : 0.75f;
        public float SFXVolume => sfxSource != null ? sfxSource.volume : 0.75f;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);

            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
            if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();

            sfxSource.playOnAwake = false;
            musicSource.playOnAwake = false;
            musicSource.loop = true;

            float savedMusic = PlayerPrefs.GetFloat("MusicVolume", 0.7f);
            float savedSfx = PlayerPrefs.GetFloat("SFXVolume", 0.8f);
            SetMusicVolume(savedMusic);
            SetSFXVolume(savedSfx);

            BuildMap();
        }

        public void BuildMap()
        {
            clipMap = new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);
            if (clips != null)
            {
                foreach (var item in clips)
                {
                    if (!string.IsNullOrEmpty(item.name) && item.clip != null)
                    {
                        clipMap[item.name] = item.clip;
                    }
                }
            }
        }

        private void Start()
        {
            PlayMusic("music");
        }

        /// <summary>
        /// Plays an SFX by registered name. Skips silently if missing or null.
        /// </summary>
        public void PlaySound(string clipName, float volumeScale = 1f)
        {
            if (string.IsNullOrEmpty(clipName)) return;
            if (clipMap == null) BuildMap();

            AudioClip clip = null;
            if (!clipMap.TryGetValue(clipName, out clip) || clip == null)
            {
                clip = GenerateSynthesizedClip(clipName);
                if (clip != null) clipMap[clipName] = clip;
            }

            if (clip != null && sfxSource != null)
            {
                sfxSource.PlayOneShot(clip, volumeScale);
            }
        }

        /// <summary>
        /// Loops background music by registered name. Skips silently if missing or null.
        /// </summary>
        public void PlayMusic(string clipName)
        {
            if (string.IsNullOrEmpty(clipName)) return;
            if (clipMap == null) BuildMap();

            AudioClip clip = null;
            if (!clipMap.TryGetValue(clipName, out clip) || clip == null)
            {
                clip = GenerateSynthesizedClip(clipName);
                if (clip != null) clipMap[clipName] = clip;
            }

            if (clip != null && musicSource != null)
            {
                if (musicSource.clip == clip && musicSource.isPlaying) return;
                musicSource.clip = clip;
                musicSource.Play();
            }
        }

        private AudioClip GenerateSynthesizedClip(string clipName)
        {
            int sampleRate = 44100;
            float duration = 0.2f;
            Func<float, float> waveFunc = null;

            switch (clipName.ToLower())
            {
                case "sword_slash":
                    duration = 0.16f;
                    waveFunc = t => {
                        float noise = (float)(new System.Random((int)(t * 32000)).NextDouble() * 2.0 - 1.0);
                        float freq = Mathf.Lerp(900f, 250f, t / duration);
                        return (Mathf.Sin(2f * Mathf.PI * freq * t) * 0.35f + noise * 0.65f) * Mathf.Exp(-9f * t);
                    };
                    break;
                case "ui_deny":
                    duration = 0.18f;
                    waveFunc = t => {
                        float freq = t < 0.09f ? 220f : 165f;
                        return Mathf.Sin(2f * Mathf.PI * freq * t) * 0.6f * (1f - (t / duration));
                    };
                    break;
                case "menu_click":
                    duration = 0.08f;
                    waveFunc = t => {
                        float freq = 800f;
                        return Mathf.Sin(2f * Mathf.PI * freq * t) * Mathf.Exp(-25f * t);
                    };
                    break;
                case "combo_up":
                    duration = 0.22f;
                    waveFunc = t => {
                        float freq = Mathf.Lerp(450f, 900f, t / duration);
                        return Mathf.Sin(2f * Mathf.PI * freq * t) * (1f - (t / duration) * 0.7f);
                    };
                    break;
                case "shadow_dash":
                    duration = 0.25f;
                    waveFunc = t => {
                        float noise = (float)(new System.Random((int)(t * 26000)).NextDouble() * 2.0 - 1.0);
                        float sweep = Mathf.Lerp(750f, 180f, t / duration);
                        return (Mathf.Sin(2f * Mathf.PI * sweep * t) * 0.45f + noise * 0.55f) * Mathf.Exp(-3.5f * t);
                    };
                    break;
                case "charm_equip":
                    duration = 0.28f;
                    waveFunc = t => {
                        float f1 = 880f;
                        float f2 = 1320f;
                        return (Mathf.Sin(2f * Mathf.PI * f1 * t) * 0.6f + Mathf.Sin(2f * Mathf.PI * f2 * t) * 0.4f) * Mathf.Exp(-7f * t);
                    };
                    break;
                case "charm_unequip":
                    duration = 0.15f;
                    waveFunc = t => {
                        float f = Mathf.Lerp(600f, 350f, t / duration);
                        return Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Exp(-14f * t);
                    };
                    break;
                case "crystal_clink":
                    duration = 0.22f;
                    waveFunc = t => {
                        float f = 2400f;
                        return Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Exp(-16f * t);
                    };
                    break;
                case "crystal_shatter":
                    duration = 0.45f;
                    waveFunc = t => {
                        float noise = (float)(new System.Random((int)(t * 35000)).NextDouble() * 2.0 - 1.0);
                        float f = Mathf.Lerp(3000f, 800f, t / duration);
                        return (Mathf.Sin(2f * Mathf.PI * f * t) * 0.4f + noise * 0.6f) * Mathf.Exp(-5f * t);
                    };
                    break;
                case "speedrun_split":
                    duration = 0.18f;
                    waveFunc = t => {
                        float f = t < 0.09f ? 1174.66f : 1760f;
                        return Mathf.Sin(2f * Mathf.PI * f * t) * (1f - (t / duration));
                    };
                    break;
                case "speedrun_pb":
                    duration = 0.55f;
                    waveFunc = t => {
                        float p = t / duration;
                        float f = p < 0.25f ? 659.25f : (p < 0.5f ? 880f : (p < 0.75f ? 1174.66f : 1760f));
                        return Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Exp(-2.5f * t);
                    };
                    break;
                case "combo_sss":
                    duration = 0.45f;
                    waveFunc = t => {
                        float f1 = 523.25f; // C5
                        float f2 = 659.25f; // E5
                        float f3 = 783.99f; // G5
                        float f4 = 1046.5f; // C6
                        float chord = (Mathf.Sin(2f * Mathf.PI * f1 * t) + Mathf.Sin(2f * Mathf.PI * f2 * t) + Mathf.Sin(2f * Mathf.PI * f3 * t) + Mathf.Sin(2f * Mathf.PI * f4 * t)) * 0.25f;
                        return chord * Mathf.Exp(-4f * t);
                    };
                    break;
                case "blade_tempest":
                    duration = 0.5f;
                    waveFunc = t => {
                        float noise = (float)(new System.Random((int)(t * 30000)).NextDouble() * 2.0 - 1.0);
                        float freq = Mathf.Sin(t * 120f) * 600f + 1200f;
                        return (Mathf.Sin(2f * Mathf.PI * freq * t) * 0.4f + noise * 0.6f) * (1f - (t / duration) * 0.5f);
                    };
                    break;
                case "secret_radar":
                    duration = 0.35f;
                    waveFunc = t => {
                        float f = 1760f; // A6 chime
                        return Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Exp(-12f * t);
                    };
                    break;
                case "stalactite_fall":
                    duration = 0.3f;
                    waveFunc = t => {
                        float noise = (float)(new System.Random((int)(t * 15000)).NextDouble() * 2.0 - 1.0);
                        float freq = Mathf.Lerp(400f, 150f, t / duration);
                        return (Mathf.Sin(2f * Mathf.PI * freq * t) * 0.5f + noise * 0.5f) * Mathf.Exp(-6f * t);
                    };
                    break;
                case "geyser":
                    duration = 0.42f;
                    waveFunc = t => {
                        float noise = (float)(new System.Random((int)(t * 22000)).NextDouble() * 2.0 - 1.0);
                        float freq = Mathf.Lerp(200f, 600f, t / duration);
                        return (Mathf.Sin(2f * Mathf.PI * freq * t) * 0.3f + noise * 0.7f) * (1f - (t / duration) * 0.4f);
                    };
                    break;
                case "achievement":
                    duration = 0.65f;
                    waveFunc = t => {
                        float stepTime = t / duration;
                        float note = stepTime < 0.25f ? 587.33f : (stepTime < 0.5f ? 783.99f : 1174.66f);
                        return Mathf.Sin(2f * Mathf.PI * note * t) * (1f - stepTime * 0.6f);
                    };
                    break;
                case "dialogue_blip":
                    duration = 0.05f;
                    waveFunc = t => {
                        return Mathf.Sin(2f * Mathf.PI * 650f * t) * (1f - (t / duration));
                    };
                    break;
                case "parry":
                    duration = 0.25f;
                    waveFunc = t => {
                        float freq1 = 1850f;
                        float freq2 = 2950f;
                        return (Mathf.Sin(2f * Mathf.PI * freq1 * t) * 0.6f + Mathf.Sin(2f * Mathf.PI * freq2 * t) * 0.4f) * Mathf.Exp(-16f * t);
                    };
                    break;
                case "deflect":
                    duration = 0.28f;
                    waveFunc = t => {
                        float freq = Mathf.Lerp(1200f, 2600f, t / duration);
                        float ring = Mathf.Sin(2f * Mathf.PI * 2200f * t) * 0.5f;
                        return (Mathf.Sin(2f * Mathf.PI * freq * t) * 0.5f + ring) * Mathf.Exp(-12f * t);
                    };
                    break;
                case "meteor_impact":
                    duration = 0.45f;
                    waveFunc = t => {
                        float noise = (float)(new System.Random((int)(t * 8000)).NextDouble() * 2.0 - 1.0);
                        float freq = Mathf.Lerp(140f, 30f, t / duration);
                        return (Mathf.Sin(2f * Mathf.PI * freq * t) * 0.65f + noise * 0.35f) * Mathf.Exp(-7f * t);
                    };
                    break;
                case "clamber":
                    duration = 0.18f;
                    waveFunc = t => {
                        float freq = Mathf.Lerp(300f, 550f, t / duration);
                        float noise = (float)(new System.Random((int)(t * 12000)).NextDouble() * 2.0 - 1.0);
                        return (Mathf.Sin(2f * Mathf.PI * freq * t) * 0.4f + noise * 0.6f) * (1f - (t / duration));
                    };
                    break;
                case "dash":
                    duration = 0.16f;
                    waveFunc = t => {
                        float noise = (float)(new System.Random((int)(t * 25000)).NextDouble() * 2.0 - 1.0);
                        float freq = Mathf.Lerp(700f, 250f, t / duration);
                        return (Mathf.Sin(2f * Mathf.PI * freq * t) * 0.35f + noise * 0.65f) * (1f - (t / duration));
                    };
                    break;
                case "spring":
                    duration = 0.28f;
                    waveFunc = t => {
                        float freq = Mathf.Lerp(220f, 750f, t / duration) + Mathf.Sin(t * 80f) * 40f;
                        return Mathf.Sin(2f * Mathf.PI * freq * t) * (1f - (t / duration) * 0.6f);
                    };
                    break;
                case "crumble":
                    duration = 0.35f;
                    waveFunc = t => {
                        float noise = (float)(new System.Random((int)(t * 18000)).NextDouble() * 2.0 - 1.0);
                        float freq = Mathf.Lerp(180f, 60f, t / duration);
                        return (Mathf.Sin(2f * Mathf.PI * freq * t) * 0.4f + noise * 0.7f) * (1f - (t / duration));
                    };
                    break;
                case "jump":
                    duration = 0.14f;
                    waveFunc = t => {
                        float freq = Mathf.Lerp(280f, 680f, t / duration);
                        return Mathf.Sin(2f * Mathf.PI * freq * t) * (1f - (t / duration));
                    };
                    break;
                case "coin":
                    duration = 0.22f;
                    waveFunc = t => {
                        float freq = (t < 0.1f) ? 987f : 1318f;
                        return Mathf.Sin(2f * Mathf.PI * freq * t) * (1f - (t / duration) * 0.7f);
                    };
                    break;
                case "hit":
                    duration = 0.15f;
                    waveFunc = t => {
                        float noise = (float)(new System.Random((int)(t * 10000)).NextDouble() * 2.0 - 1.0);
                        return noise * Mathf.Exp(-18f * t);
                    };
                    break;
                case "enemy death":
                    duration = 0.22f;
                    waveFunc = t => {
                        float freq = Mathf.Lerp(450f, 100f, t / duration);
                        float noise = (float)(new System.Random((int)(t * 10000)).NextDouble() * 0.6 - 0.3);
                        return (Mathf.Sin(2f * Mathf.PI * freq * t) * 0.7f + noise) * (1f - (t / duration));
                    };
                    break;
                case "checkpoint":
                    duration = 0.35f;
                    waveFunc = t => {
                        float freq = (t < 0.12f) ? 523f : ((t < 0.24f) ? 659f : 784f);
                        return Mathf.Sin(2f * Mathf.PI * freq * t) * (1f - (t / duration) * 0.5f);
                    };
                    break;
                case "win":
                    duration = 0.65f;
                    waveFunc = t => {
                        float freq = (t < 0.18f) ? 523f : ((t < 0.36f) ? 659f : ((t < 0.5f) ? 784f : 1046f));
                        return Mathf.Sin(2f * Mathf.PI * freq * t) * 0.8f;
                    };
                    break;
                case "lose":
                    duration = 0.5f;
                    waveFunc = t => {
                        float freq = Mathf.Lerp(350f, 120f, t / duration);
                        return Mathf.Sin(2f * Mathf.PI * freq * t) * (1f - (t / duration));
                    };
                    break;
                case "music":
                    duration = 4.0f;
                    waveFunc = t => {
                        float step = (t % 1.0f);
                        int chordIndex = (int)(t / 1.0f) % 4;
                        float[] baseFreqs = { 261.6f, 220f, 174.6f, 196f };
                        float f = baseFreqs[chordIndex];
                        float mel = (step < 0.25f) ? f : ((step < 0.5f) ? f * 1.25f : f * 1.5f);
                        return Mathf.Sin(2f * Mathf.PI * mel * t) * 0.18f;
                    };
                    break;
                case "slam":
                    duration = 0.35f;
                    waveFunc = t => {
                        float noise = (float)(new System.Random((int)(t * 15000)).NextDouble() * 2.0 - 1.0);
                        float freq = Mathf.Lerp(140f, 35f, t / duration);
                        return (Mathf.Sin(2f * Mathf.PI * freq * t) * 0.7f + noise * 0.45f) * Mathf.Exp(-7f * t);
                    };
                    break;
                case "boss_roar":
                    duration = 0.65f;
                    waveFunc = t => {
                        float noise = (float)(new System.Random((int)(t * 22000)).NextDouble() * 2.0 - 1.0);
                        float rumble = Mathf.Sin(2f * Mathf.PI * 65f * t) * Mathf.Sin(2f * Mathf.PI * 18f * t);
                        return (rumble * 0.6f + noise * 0.4f) * (1f - (t / duration) * 0.3f);
                    };
                    break;
                case "boss_defeat":
                    duration = 1.2f;
                    waveFunc = t => {
                        float freq = (t < 0.25f) ? 330f : ((t < 0.5f) ? 440f : ((t < 0.75f) ? 554f : 659f));
                        float noise = (float)(new System.Random((int)(t * 10000)).NextDouble() * 0.3 - 0.15);
                        return (Mathf.Sin(2f * Mathf.PI * freq * t) * 0.7f + noise) * (1f - (t / duration) * 0.4f);
                    };
                    break;
                default:
                    return null;
            }

            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                data[i] = Mathf.Clamp(waveFunc(t), -1f, 1f);
            }

            AudioClip clipObj = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
            clipObj.SetData(data, 0);
            return clipObj;
        }

        public void SetMusicVolume(float volume)
        {
            float v = Mathf.Clamp01(volume);
            if (musicSource != null) musicSource.volume = v;
            PlayerPrefs.SetFloat("MusicVolume", v);
        }

        public void SetSFXVolume(float volume)
        {
            float v = Mathf.Clamp01(volume);
            if (sfxSource != null) sfxSource.volume = v;
            PlayerPrefs.SetFloat("SFXVolume", v);
        }
    }
}
