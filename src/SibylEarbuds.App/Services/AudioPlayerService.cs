using System.IO;
using System.Windows.Media;
using SibylEarbuds.Core.Services;

namespace SibylEarbuds.App.Services;

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
        _player.MediaEnded += (sender, args) =>
        {
            // 自动循环播放
            if (_isPlaying)
            {
                _player.Position = TimeSpan.Zero;
                _player.Play();
            }
        };
    }

    public void Play(BuiltInSoundInfo sound, double volume = 0.6)
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string path = Path.Combine(baseDir, "Resources", "Audio", sound.FileName);

        if (!File.Exists(path))
        {
            // 兜底尝试向上查找
            path = Path.Combine(baseDir, sound.FileName);
        }

        if (File.Exists(path))
        {
            _currentPlaying = sound;
            _isPlaying = true;
            _player.Open(new Uri(path, UriKind.Absolute));
            _player.Volume = Math.Clamp(volume, 0.0, 1.0);
            _player.Play();
            PlaybackStateChanged?.Invoke(true, sound);
        }
    }

    public void Stop()
    {
        if (_isPlaying)
        {
            _player.Stop();
            _isPlaying = false;
            PlaybackStateChanged?.Invoke(false, _currentPlaying);
        }
    }

    public void SetVolume(double volume)
    {
        _player.Volume = Math.Clamp(volume, 0.0, 1.0);
    }

    public void Dispose()
    {
        _player.Stop();
        _player.Close();
    }
}
