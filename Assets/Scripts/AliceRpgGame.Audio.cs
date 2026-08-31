using UnityEngine;

public sealed partial class AliceRpgGame
{
    private AudioSource audioSource;
    private AudioClip moveSound;
    private AudioClip confirmSound;
    private AudioClip hitSound;
    private AudioClip magicSound;
    private AudioSource musicSource;
    private AudioClip musicClip;

    private void CreateAudio()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.volume = 0.22f * sfxVolume;
        moveSound = MakeTone("step", 330f, 0.035f);
        confirmSound = MakeTone("confirm", 660f, 0.07f);
        hitSound = MakeTone("hit", 120f, 0.09f);
        magicSound = MakeTone("magic", 880f, 0.14f);

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.volume = 0.075f * musicVolume;
        musicClip = MakeMusic();
        musicSource.clip = musicClip;
        musicSource.Play();
    }

    private AudioClip MakeTone(string clipName, float frequency, float duration)
    {
        const int rate = 22050;
        int count = Mathf.CeilToInt(rate * duration);
        float[] samples = new float[count];
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)rate;
            float fade = 1f - i / (float)count;
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * fade * 0.35f;
        }
        AudioClip clip = AudioClip.Create(clipName, count, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip MakeMusic()
    {
        const int rate = 22050;
        const float duration = 8f;
        int count = Mathf.CeilToInt(rate * duration);
        float[] samples = new float[count];
        float[] notes = { 261.63f, 329.63f, 392f, 523.25f, 392f, 329.63f, 293.66f, 349.23f };
        int noteLength = count / notes.Length;
        for (int i = 0; i < count; i++)
        {
            int noteIndex = Mathf.Min(notes.Length - 1, i / noteLength);
            float local = (i % noteLength) / (float)noteLength;
            float envelope = Mathf.Sin(local * Mathf.PI) * 0.24f;
            float t = i / (float)rate;
            samples[i] = (Mathf.Sin(2f * Mathf.PI * notes[noteIndex] * t) + Mathf.Sin(2f * Mathf.PI * notes[noteIndex] * 0.5f * t) * 0.24f) * envelope;
        }
        AudioClip clip = AudioClip.Create("wonderland_theme", count, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void Play(AudioClip clip)
    {
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
    }
}
