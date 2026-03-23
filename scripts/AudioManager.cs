using Godot;

namespace ARPG;

/// <summary>
/// Singleton audio manager that generates simple procedural sounds at runtime.
/// Handles combat hits, pickups, effects, level progression, and background music.
/// Replace the procedural generation with real audio files later.
/// </summary>
public partial class AudioManager : Node
{
    private static AudioManager _instance;
    public static AudioManager Instance => _instance;

    // One-shot sound players
    private AudioStreamPlayer _sfxPlayer;
    private AudioStreamPlayer _sfxPlayer2; // overlap channel
    private AudioStreamPlayer _musicPlayer;

    // Pre-generated audio streams
    private AudioStream _hitSound;
    private AudioStream _playerHitSound;
    private AudioStream _pickupSound;
    private AudioStream _effectApplySound;
    private AudioStream _levelUpSound;
    private AudioStream _enemyDeathSound;
    private AudioStream _bossDeathSound;

    // Music streams
    private AudioStream _exploreMusic;
    private AudioStream _combatMusic;
    private bool _inCombat;

    private const float SfxVolume = -8.0f;
    private const float MusicVolume = -18.0f;
    private const int SampleRate = 22050;

    public override void _Ready()
    {
        _instance = this;

        _sfxPlayer = new AudioStreamPlayer();
        _sfxPlayer.Bus = "Master";
        _sfxPlayer.VolumeDb = SfxVolume;
        AddChild(_sfxPlayer);

        _sfxPlayer2 = new AudioStreamPlayer();
        _sfxPlayer2.Bus = "Master";
        _sfxPlayer2.VolumeDb = SfxVolume;
        AddChild(_sfxPlayer2);

        _musicPlayer = new AudioStreamPlayer();
        _musicPlayer.Bus = "Master";
        _musicPlayer.VolumeDb = MusicVolume;
        AddChild(_musicPlayer);

        GenerateAllSounds();
    }

    private void GenerateAllSounds()
    {
        // Combat sounds
        _hitSound = GenerateTone(220f, 0.08f, decay: true);             // Short punch
        _playerHitSound = GenerateTone(150f, 0.12f, decay: true);       // Lower thud
        _pickupSound = GenerateChirp(600f, 900f, 0.15f);                // Rising chirp
        _effectApplySound = GenerateChirp(400f, 700f, 0.2f);            // Softer chirp
        _levelUpSound = GenerateFanfare();                                // Ascending tones
        _enemyDeathSound = GenerateNoiseBurst(0.15f);                    // Short noise
        _bossDeathSound = GenerateNoiseBurst(0.35f);                     // Longer noise

        // Music (very simple procedural loops)
        _exploreMusic = GenerateExploreLoop();
        _combatMusic = GenerateCombatLoop();
    }

    public void PlayHit() => PlaySfx(_hitSound);
    public void PlayPlayerHit() => PlaySfx(_playerHitSound, true);
    public void PlayPickup() => PlaySfx(_pickupSound);
    public void PlayEffectApply() => PlaySfx(_effectApplySound);
    public void PlayEnemyDeath() => PlaySfx(_enemyDeathSound);
    public void PlayBossDeath() => PlaySfx(_bossDeathSound);
    public void PlayLevelUp() => PlaySfx(_levelUpSound);

    public void StartExploreMusic()
    {
        if (_inCombat || !_musicPlayer.Playing)
        {
            _inCombat = false;
            _musicPlayer.Stream = _exploreMusic;
            _musicPlayer.Play();
        }
    }

    public void StartCombatMusic()
    {
        if (!_inCombat)
        {
            _inCombat = true;
            _musicPlayer.Stream = _combatMusic;
            _musicPlayer.Play();
        }
    }

    public void StopCombatMusic()
    {
        _inCombat = false;
        _musicPlayer.Stream = _exploreMusic;
        _musicPlayer.Play();
    }

    private void PlaySfx(AudioStream stream, bool useChannel2 = false)
    {
        var player = useChannel2 ? _sfxPlayer2 : _sfxPlayer;
        player.Stream = stream;
        player.Play();
    }

    // --- Procedural sound generation ---

    private static AudioStreamWav GenerateTone(float freq, float duration, bool decay = false)
    {
        int samples = (int)(SampleRate * duration);
        var data = new byte[samples * 2]; // 16-bit mono

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / SampleRate;
            float amplitude = decay ? 1.0f - (float)i / samples : 1.0f;
            float sample = Mathf.Sin(t * freq * Mathf.Tau) * amplitude * 0.4f;

            short s16 = (short)(sample * 32767);
            data[i * 2] = (byte)(s16 & 0xFF);
            data[i * 2 + 1] = (byte)((s16 >> 8) & 0xFF);
        }

        return MakeWav(data);
    }

    private static AudioStreamWav GenerateChirp(float startFreq, float endFreq, float duration)
    {
        int samples = (int)(SampleRate * duration);
        var data = new byte[samples * 2];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / SampleRate;
            float progress = (float)i / samples;
            float freq = Mathf.Lerp(startFreq, endFreq, progress);
            float amplitude = 1.0f - progress * 0.5f;
            float sample = Mathf.Sin(t * freq * Mathf.Tau) * amplitude * 0.3f;

            short s16 = (short)(sample * 32767);
            data[i * 2] = (byte)(s16 & 0xFF);
            data[i * 2 + 1] = (byte)((s16 >> 8) & 0xFF);
        }

        return MakeWav(data);
    }

    private static AudioStreamWav GenerateNoiseBurst(float duration)
    {
        int samples = (int)(SampleRate * duration);
        var data = new byte[samples * 2];
        var rng = new RandomNumberGenerator();
        rng.Randomize();

        for (int i = 0; i < samples; i++)
        {
            float progress = (float)i / samples;
            float amplitude = (1.0f - progress) * 0.3f;
            float sample = (rng.Randf() * 2.0f - 1.0f) * amplitude;

            short s16 = (short)(sample * 32767);
            data[i * 2] = (byte)(s16 & 0xFF);
            data[i * 2 + 1] = (byte)((s16 >> 8) & 0xFF);
        }

        return MakeWav(data);
    }

    private static AudioStreamWav GenerateFanfare()
    {
        // Three ascending tones: C, E, G
        float[] notes = { 262f, 330f, 392f };
        float noteDuration = 0.15f;
        float totalDuration = noteDuration * notes.Length;
        int totalSamples = (int)(SampleRate * totalDuration);
        var data = new byte[totalSamples * 2];

        for (int i = 0; i < totalSamples; i++)
        {
            float t = (float)i / SampleRate;
            int noteIndex = Mathf.Min((int)(t / noteDuration), notes.Length - 1);
            float noteT = t - noteIndex * noteDuration;
            float amplitude = 1.0f - noteT / noteDuration * 0.6f;
            float sample = Mathf.Sin(noteT * notes[noteIndex] * Mathf.Tau) * amplitude * 0.35f;

            short s16 = (short)(sample * 32767);
            data[i * 2] = (byte)(s16 & 0xFF);
            data[i * 2 + 1] = (byte)((s16 >> 8) & 0xFF);
        }

        return MakeWav(data);
    }

    private static AudioStreamWav GenerateExploreLoop()
    {
        // Gentle ambient: slow sine wave with harmonics, 4 seconds loop
        float duration = 4.0f;
        int samples = (int)(SampleRate * duration);
        var data = new byte[samples * 2];

        float[] notes = { 130.8f, 164.8f, 196.0f, 164.8f }; // C3, E3, G3, E3
        float noteDur = duration / notes.Length;

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / SampleRate;
            int noteIdx = Mathf.Min((int)(t / noteDur), notes.Length - 1);
            float freq = notes[noteIdx];
            float sample = Mathf.Sin(t * freq * Mathf.Tau) * 0.12f
                         + Mathf.Sin(t * freq * 2 * Mathf.Tau) * 0.04f;

            // Fade at edges for smooth loop
            float fadeIn = Mathf.Min((float)i / (SampleRate * 0.1f), 1.0f);
            float fadeOut = Mathf.Min((float)(samples - i) / (SampleRate * 0.1f), 1.0f);
            sample *= fadeIn * fadeOut;

            short s16 = (short)(sample * 32767);
            data[i * 2] = (byte)(s16 & 0xFF);
            data[i * 2 + 1] = (byte)((s16 >> 8) & 0xFF);
        }

        var wav = MakeWav(data);
        wav.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
        wav.LoopEnd = samples;
        return wav;
    }

    private static AudioStreamWav GenerateCombatLoop()
    {
        // More intense: faster tempo, minor key, 3 seconds loop
        float duration = 3.0f;
        int samples = (int)(SampleRate * duration);
        var data = new byte[samples * 2];

        float[] notes = { 146.8f, 174.6f, 130.8f, 155.6f }; // D3, F3, C3, Eb3
        float noteDur = duration / notes.Length;

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / SampleRate;
            int noteIdx = Mathf.Min((int)(t / noteDur), notes.Length - 1);
            float freq = notes[noteIdx];

            // Square-ish wave for more edge
            float sine = Mathf.Sin(t * freq * Mathf.Tau);
            float sample = (sine > 0 ? 0.15f : -0.15f) * 0.7f
                         + sine * 0.08f;

            float fadeIn = Mathf.Min((float)i / (SampleRate * 0.05f), 1.0f);
            float fadeOut = Mathf.Min((float)(samples - i) / (SampleRate * 0.05f), 1.0f);
            sample *= fadeIn * fadeOut;

            short s16 = (short)(sample * 32767);
            data[i * 2] = (byte)(s16 & 0xFF);
            data[i * 2 + 1] = (byte)((s16 >> 8) & 0xFF);
        }

        var wav = MakeWav(data);
        wav.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
        wav.LoopEnd = samples;
        return wav;
    }

    private static AudioStreamWav MakeWav(byte[] data)
    {
        var wav = new AudioStreamWav();
        wav.Format = AudioStreamWav.FormatEnum.Format16Bits;
        wav.MixRate = SampleRate;
        wav.Stereo = false;
        wav.Data = data;
        return wav;
    }
}
