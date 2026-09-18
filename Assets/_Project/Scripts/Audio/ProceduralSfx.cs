using UnityEngine;

namespace RPG.Audio
{
    /// <summary>
    /// Synthesises the game's sound effects at startup, so the game has sound without a
    /// single audio file in the project.
    ///
    /// Every clip is a few hundred milliseconds of a swept oscillator and/or filtered noise
    /// under an envelope - the vocabulary of every 8-bit sound chip, and enough to make a
    /// swing, a hit and a death unmistakable from each other. Real recorded effects can
    /// replace any of these later one clip at a time (see SfxPlayer's overrides); nothing
    /// else has to change.
    ///
    /// Everything is generated once and cached on the SfxPlayer that asked for it.
    /// </summary>
    public static class ProceduralSfx
    {
        private const int SampleRate = 22050;

        public enum Wave { Sine, Square, Saw, Triangle }

        // ------------------------------------------------------------------ recipes

        /// <summary>A short whoosh: filtered noise with a fast attack and a pitch that rises.</summary>
        public static AudioClip Swing() => Build("sfx_swing", 0.13f, (t, p) =>
            Noise(t, 1800f + 2600f * p) * Env(p, 0.02f, 0.9f) * 0.5f);

        /// <summary>A thud: a low sine dropping fast plus a click of noise on the front.</summary>
        public static AudioClip Hit() => Build("sfx_hit", 0.1f, (t, p) =>
            Osc(Wave.Sine, t, Sweep(190f, 55f, p)) * Env(p, 0.005f, 0.6f) * 0.8f +
            Noise(t, 3000f) * Env(p, 0.002f, 0.12f) * 0.35f);

        /// <summary>The thud, bigger and brighter, with a ring on top.</summary>
        public static AudioClip Crit() => Build("sfx_crit", 0.2f, (t, p) =>
            Osc(Wave.Sine, t, Sweep(280f, 50f, p)) * Env(p, 0.005f, 0.5f) * 0.85f +
            Osc(Wave.Triangle, t, 1320f) * Env(p, 0.01f, 0.25f) * 0.25f +
            Noise(t, 2500f) * Env(p, 0.002f, 0.15f) * 0.45f);

        /// <summary>The player taking a hit: a harsher, squarer note than an enemy hit.</summary>
        public static AudioClip Hurt() => Build("sfx_hurt", 0.18f, (t, p) =>
            Osc(Wave.Square, t, Sweep(240f, 110f, p)) * Env(p, 0.005f, 0.55f) * 0.4f +
            Noise(t, 1500f) * Env(p, 0.002f, 0.3f) * 0.3f);

        /// <summary>A miss: a tiny high blip, over almost before it started.</summary>
        public static AudioClip Dodge() => Build("sfx_dodge", 0.06f, (t, p) =>
            Osc(Wave.Sine, t, Sweep(900f, 1400f, p)) * Env(p, 0.005f, 0.5f) * 0.35f);

        /// <summary>An enemy dying: a falling tone under a longer puff of noise.</summary>
        public static AudioClip Death() => Build("sfx_death", 0.32f, (t, p) =>
            Osc(Wave.Saw, t, Sweep(160f, 35f, p)) * Env(p, 0.01f, 0.8f) * 0.45f +
            Noise(t, 900f + 1500f * (1f - p)) * Env(p, 0.01f, 0.7f) * 0.4f);

        /// <summary>The dash: a rising airy sweep.</summary>
        public static AudioClip Dash() => Build("sfx_dash", 0.2f, (t, p) =>
            Noise(t, 600f + 3400f * p) * Env(p, 0.03f, 0.85f) * 0.45f +
            Osc(Wave.Sine, t, Sweep(260f, 720f, p)) * Env(p, 0.02f, 0.6f) * 0.2f);

        /// <summary>A four-note rising arpeggio.</summary>
        public static AudioClip LevelUp() => Arpeggio("sfx_levelup", 0.085f, 0.4f,
            523.25f, 659.25f, 783.99f, 1046.5f);

        /// <summary>Two-note chime for a cleared room.</summary>
        public static AudioClip RoomClear() => Arpeggio("sfx_roomclear", 0.12f, 0.35f,
            659.25f, 987.77f);

        /// <summary>A short fanfare for a completed stage.</summary>
        public static AudioClip StageComplete() => Arpeggio("sfx_stagecomplete", 0.11f, 0.45f,
            523.25f, 523.25f, 659.25f, 783.99f, 1046.5f);

        /// <summary>A falling square-wave figure for a defeat.</summary>
        public static AudioClip StageFailed() => Build("sfx_stagefailed", 0.7f, (t, p) =>
            Osc(Wave.Square, t, Sweep(330f, 90f, p)) * Env(p, 0.01f, 0.95f) * 0.28f *
            (0.7f + 0.3f * Mathf.Sin(t * 2f * Mathf.PI * 9f)));

        /// <summary>A low, growling sawtooth with tremolo, for the boss changing phase.</summary>
        public static AudioClip Enrage() => Build("sfx_enrage", 0.75f, (t, p) =>
            Osc(Wave.Saw, t, Sweep(70f, 110f, p)) * Env(p, 0.08f, 0.9f) * 0.5f *
            (0.6f + 0.4f * Mathf.Sin(t * 2f * Mathf.PI * 14f)) +
            Noise(t, 400f) * Env(p, 0.1f, 0.8f) * 0.15f);

        /// <summary>A dull heavy hit for a boss spawning.</summary>
        public static AudioClip BossSpawn() => Build("sfx_bossspawn", 0.5f, (t, p) =>
            Osc(Wave.Sine, t, Sweep(120f, 40f, p)) * Env(p, 0.01f, 0.85f) * 0.8f +
            Noise(t, 700f) * Env(p, 0.005f, 0.3f) * 0.3f);

        /// <summary>A tick for a button press.</summary>
        public static AudioClip Click() => Build("sfx_click", 0.04f, (t, p) =>
            Osc(Wave.Sine, t, 1100f) * Env(p, 0.002f, 0.4f) * 0.3f);

        // ------------------------------------------------------------------ building blocks

        private delegate float Sampler(float timeSeconds, float progress01);

        private static AudioClip Build(string name, float seconds, Sampler sampler)
        {
            int count = Mathf.Max(1, Mathf.RoundToInt(seconds * SampleRate));
            var samples = new float[count];

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                samples[i] = Mathf.Clamp(sampler(t, i / (float)count), -1f, 1f);
            }

            // A couple of milliseconds of fade at the very end, so a clip cut off mid-cycle
            // never ends on a click.
            int tail = Mathf.Min(count, SampleRate / 400);
            for (int i = 0; i < tail; i++) samples[count - 1 - i] *= i / (float)tail;

            var clip = AudioClip.Create(name, count, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip Arpeggio(string name, float noteSeconds, float tailSeconds,
            params float[] frequencies)
        {
            float total = noteSeconds * frequencies.Length + tailSeconds;
            float lastStart = noteSeconds * (frequencies.Length - 1);

            return Build(name, total, (t, p) =>
            {
                float sum = 0f;
                for (int n = 0; n < frequencies.Length; n++)
                {
                    float start = noteSeconds * n;
                    if (t < start) break;

                    // Every note rings on under the ones that follow; the last one rings longest.
                    float length = n == frequencies.Length - 1 ? noteSeconds + tailSeconds : noteSeconds * 1.6f;
                    float local = Mathf.Clamp01((t - start) / length);
                    if (local >= 1f) continue;

                    float f = frequencies[n];
                    sum += (Osc(Wave.Triangle, t, f) * 0.6f + Osc(Wave.Sine, t, f * 2f) * 0.2f)
                           * Env(local, 0.01f, 0.5f);
                }

                return sum * 0.4f;
            });
        }

        private static float Sweep(float from, float to, float p) => Mathf.Lerp(from, to, p * p);

        private static float Osc(Wave wave, float t, float frequency)
        {
            float phase = (t * frequency) % 1f;
            switch (wave)
            {
                case Wave.Square: return phase < 0.5f ? 1f : -1f;
                case Wave.Saw: return phase * 2f - 1f;
                case Wave.Triangle: return 1f - 4f * Mathf.Abs(phase - 0.5f);
                default: return Mathf.Sin(phase * 2f * Mathf.PI);
            }
        }

        // A one-pole low-pass over white noise. The cutoff is per call, so a sweep is just a
        // cutoff that moves with progress.
        private static float _noiseState;
        private static uint _noiseSeed = 0x9E3779B9u;

        private static float Noise(float t, float cutoffHz)
        {
            _noiseSeed ^= _noiseSeed << 13;
            _noiseSeed ^= _noiseSeed >> 17;
            _noiseSeed ^= _noiseSeed << 5;
            float white = (_noiseSeed & 0xFFFFFF) / 8388607.5f - 1f;

            float alpha = Mathf.Clamp01(cutoffHz / (SampleRate * 0.5f));
            _noiseState += alpha * (white - _noiseState);
            return _noiseState;
        }

        /// <summary>Attack then exponential-ish decay, both as fractions of the clip.</summary>
        private static float Env(float p, float attack, float decay)
        {
            if (p < attack) return p / attack;
            float d = Mathf.Clamp01((p - attack) / Mathf.Max(0.0001f, decay));
            return (1f - d) * (1f - d);
        }
    }
}
