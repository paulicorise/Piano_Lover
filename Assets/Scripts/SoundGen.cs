using System.Collections.Generic;
using UnityEngine;

public class SoundGen : MonoBehaviour
{
    float sampleRate;
    AudioSource audioSource;

    private class Voice
    {
        public int keyNumber;
        public float frequency;
        public float pitch;
        public float phase;
        public float amp;       // current envelope amplitude (0..1)
        public bool releasing;  // note-off, fading out
    }

    private readonly List<Voice> voices = new List<Voice>();
    private readonly object voiceLock = new object();

    const float attackRate = 200f;   // amplitude units per second (tune to taste)
    const float releaseRate = 150f;
    const int maxPoly = 8;           // used for headroom scaling
    const float masterGain = 0.20f;

    void Awake()
    {
        sampleRate = AudioSettings.outputSampleRate;
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = true;
        audioSource.volume = 0.5f;
        audioSource.reverbZoneMix = 0.6f;
    }

    public void OnKey(int keyNumber)
    {
        float freq = 440f * Mathf.Pow(2f, (keyNumber - 69f) / 12f);
        lock (voiceLock)
        {
            Voice v = voices.Find(x => x.keyNumber == keyNumber);
            if (v != null)
            {
                v.frequency = freq;
                v.pitch = 0f;
                v.releasing = false; // retrigger cancels release
                return;
            }
            voices.Add(new Voice { keyNumber = keyNumber, frequency = freq, pitch = 0f, phase = 0f, amp = 0f, releasing = false });
        }
    }

    public void changePitch(int keyNumber, float pitch)
    {
        lock (voiceLock)
        {
            Voice v = voices.Find(x => x.keyNumber == keyNumber);
            if (v != null) v.pitch = pitch;
        }
    }

    public void OnKeyOff(int keyNumber)
    {
        lock (voiceLock)
        {
            Voice v = voices.Find(x => x.keyNumber == keyNumber);
            if (v != null) v.releasing = true; // don't remove yet, let it fade out
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (!audioSource.isPlaying) audioSource.Play();
            else audioSource.Stop();
        }
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        lock (voiceLock)
        {
            float dt = 1f / sampleRate;

            for (int i = 0; i < data.Length; i += channels)
            {
                float sample = 0f;
                int active = 0;

                for (int index = voices.Count - 1; index >= 0; index--)
                {
                    Voice v = voices[index];

                    // envelope
                    if (v.releasing)
                    {
                        v.amp -= releaseRate * dt;
                        if (v.amp <= 0f)
                        {
                            voices.RemoveAt(index);
                            continue;
                        }
                    }
                    else if (v.amp < 1f)
                    {
                        v.amp = Mathf.Min(1f, v.amp + attackRate * dt);
                    }

                    float incrementAmount = (v.frequency + v.frequency / 10f * v.pitch) * 2f * Mathf.PI / sampleRate;
                    v.phase += incrementAmount;
                    if (v.phase > Mathf.PI * 2f) v.phase -= Mathf.PI * 2f; // wrap, don't reset

                    sample += Mathf.Sin(v.phase) * v.amp;
                    active++;
                }

                if (active > 0)
                {
                    // headroom scaling: prevents clipping when several notes play together
                    sample /= Mathf.Sqrt(Mathf.Max(1, Mathf.Min(active, maxPoly)));
                    sample *= masterGain;
                    sample = Mathf.Clamp(sample, -0.95f, 0.95f);
                }

                for (int c = 0; c < channels; c++)
                {
                    data[i + c] += sample;
                }
            }
        }
    }
}