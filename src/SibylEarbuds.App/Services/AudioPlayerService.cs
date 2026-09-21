using System.IO;
using SibylEarbuds.Core.Services;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace SibylEarbuds.App.Services;

/// <summary>
/// Plays the built-in therapy / white-noise clips that ship with the app.
/// Uses Windows.Media.Playback so it works on the WinUI 3 (Windows App SDK) stack.
/// </summary>
public class AudioPlayerService : IDisposable
{
    private readonly MediaPlayer _player = new();
    private BuiltInSoundInfo? _currentPlaying;
    private bool _isPlaying;

    public event Action<bool, BuiltInSoundInfo?>? PlaybackStateChanged;

    public bool IsPlaying => _isPlaying;

    public BuiltInSoundInfo? CurrentPlaying => _currentPlaying;

    public AudioPlayerService()
    {
        _player.MediaEnded += (_, _) =>
        {
            if (!_isPlaying)
            {
                return;
            }

            // Loop the ambience until the user stops it.
            _player.PlaybackSession.Position = TimeSpan.Zero;
            _player.Play();
        };
    }

    public void Play(BuiltInSoundInfo sound, double volume = 0.6)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Resources", "Audio", sound.FileName);
        if (!File.Exists(path))
        {
            path = Path.Combine(AppContext.BaseDirectory, sound.FileName);
        }

        if (!File.Exists(path))
        {
            return;
        }

        _currentPlaying = sound;
        _isPlaying = true;
        _player.AutoPlay = false;
        _player.Source = MediaSource.CreateFromUri(new Uri(path, UriKind.Absolute));
        _player.Volume = Math.Clamp(volume, 0.0, 1.0);
        _player.Play();

        PlaybackStateChanged?.Invoke(true, sound);
    }

    public void Stop()
    {
        if (!_isPlaying)
        {
            return;
        }

        _isPlaying = false;
        _player.Pause();
        _player.Source = null;
        PlaybackStateChanged?.Invoke(false, _currentPlaying);
    }

    public void SetVolume(double volume) => _player.Volume = Math.Clamp(volume, 0.0, 1.0);

    public void Dispose()
    {
        _isPlaying = false;
        _player.Dispose();
    }
}
