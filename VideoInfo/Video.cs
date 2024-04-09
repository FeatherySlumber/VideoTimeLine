using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Media;

namespace VideoInfo
{
    /// <summary>
    /// ビデオ再生の情報管理のためのクラス
    /// </summary>
    public class Video : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, pceaDictionary.GetOrAdd(propertyName));
        static readonly Dictionary<string, PropertyChangedEventArgs> pceaDictionary = new();

        public string Name { get; init; }
        public TimeSpan Duration { get; init; }
        public uint Width { get; init; }
        public uint Height { get; init; }
        internal readonly Lazy<MediaPlayer> player;
        internal MediaPlayer Player => player.Value;
        internal readonly Lazy<DrawingBrush> VideoBrush;

        private TimeSpan start = TimeSpan.Zero;
        public TimeSpan Start
        {
            get => start; set
            {
                start = value;
                NotifyPropertyChanged(nameof(Start));
                NotifyPropertyChanged(nameof(End));
            }
        }

        public TimeSpan End => Start + Duration;

        /// <summary>
        /// 指定パラメーターで <see cref="Video"/> クラスの新しいインスタンスを初期化
        /// </summary>
        /// <param name="name">ビデオの名前</param>
        /// <param name="uri">ビデオファイルの URI</param>
        /// <param name="duration">ビデオの再生時間</param>
        /// <param name="width">ビデオの幅</param>
        /// <param name="height">ビデオの高さ</param>
        public Video(string name, Uri uri, TimeSpan duration, uint width, uint height)
        {
            Name = name;
            Duration = duration;
            Width = width;
            Height = height;

            player = new(() =>
            {
                var mp = new MediaPlayer();
                mp.Open(uri);
                return mp;
            });
            VideoBrush = new(() => new(new VideoDrawing
            {
                Player = Player,
                Rect = new(0, 0, Width, Height)
            }));
        }

        /// <summary>
        /// 指定された時間がビデオ再生範囲内にあるかどうか判定
        /// </summary>
        /// <param name="timeSpan">判定する時間</param>
        /// <returns>指定された時間がビデオ再生範囲内にある場合は <see langword="true"/>、それ以外の場合は <see langword="false"/>。</returns>
        public bool IsTimeWithinRange(TimeSpan value) => Start <= value && value < End;

        ~Video()
        {
            Player.Close();
        }
    }

    /// <summary>
    /// ビデオの再生や操作を管理するためのクラス
    /// </summary>
    public class VideoPlayer : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, pceaDictionary.GetOrAdd(propertyName));
        static readonly Dictionary<string, PropertyChangedEventArgs> pceaDictionary = new();

        /// <summary>
        /// ビデオの再生位置を更新
        /// </summary>
        private void UpdatePosition(TimeSpan time)
        {
            if (Video is Video v && IsPlaying)
            {
                if (v.IsTimeWithinRange(Position))
                {
                    Position = v.Start + v.Player.Position;
                }
                else
                {
                    Position += time;
                }
            }
        }

        public VideoPlayer()
        {
            updateVideoPosition = new()
            {
                IntervalCallback = UpdatePosition,
            };
        }

        private Video? video;
        public Video? Video
        {
            get => video;
            private set
            {
                if (video is not null) video.PropertyChanged -= VideoPropertyChanged;
                video = value;
                if (video is not null) video.PropertyChanged += VideoPropertyChanged;
                OnPropertyChanged(nameof(Brush));
            }
        }

        private void VideoPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Video.Start))
            {
                Seek(Position);
                OnPropertyChanged(nameof(Brush));
            }
        }

        /// <summary>
        /// ビデオを設定
        /// </summary>
        /// <param name="value">設定するビデオ</param>
        /// <exception cref="Exception">ビデオが再生中の場合に発火</exception>
        public void SetVideo(Video value)
        {
            Video = IsPaused ? value : throw new Exception("Now Video Is Playing");
        }

        private TimeSpan playLimit = TimeSpan.MaxValue;
        public TimeSpan PlayLimit
        {
            get => playLimit;
            set
            {
                playLimit = value;
                OnPropertyChanged(nameof(PlayLimit));
            }
        }

        bool isPlaying = false;
        public bool IsPlaying
        {
            get => isPlaying;
            internal set
            {
                isPlaying = value;
                OnIsPlayingChanged(value);
                OnPropertyChanged(nameof(IsPlaying));
                OnPropertyChanged(nameof(IsPaused));
            }
        }
        public bool IsPaused => !IsPlaying;

        void OnIsPlayingChanged(bool value)
        {
            if (Video?.Player is not MediaPlayer mp)
            {
                IsPlaying = false;
                return;
            }
            if (value && Video.IsTimeWithinRange(Position))
            {
                mp.Position = Position - Video.Start;
                mp.Play();
            }
            else
            {
                if (mp.CanPause) mp.Pause();
            }
            if (value)
            {
                updateVideoPosition.Start();
            }
            else
            {
                updateVideoPosition.Stop();
            }
        }

        private TimeSpan position = TimeSpan.Zero;
        public TimeSpan Position
        {
            get => position;
            private set
            {
                var temp = position;
                position = value;
                OnPositionChanged(value, temp);
                OnPropertyChanged(nameof(Position));
            }
        }

        void OnPositionChanged(TimeSpan newValue, TimeSpan oldValue)
        {
            if (newValue > PlayLimit)
            {
                IsPlaying = false;
                Position = PlayLimit;
            }

            if (Video is Video v
                && v.IsTimeWithinRange(newValue) != v.IsTimeWithinRange(oldValue))
            {
                OnPropertyChanged(nameof(Brush));
            }
        }

        public Brush DefaultBrush { internal get; set; } = Brushes.DeepSkyBlue;
        public Brush Brush => Video is not null && Video.IsTimeWithinRange(Position) ? Video.VideoBrush.Value : DefaultBrush;


        private readonly TimeIntervalTracker updateVideoPosition;

        /// <summary>
        /// 指定された再生位置にビデオをシーク
        /// </summary>
        /// <param name="position">シークする再生位置</param>
        public void Seek(TimeSpan position)
        {
            if (Video is not null && Video.IsTimeWithinRange(position))
            {
                Video.Player.Position = position - Video.Start;
            }
            if (IsPlaying)
            {
                Pause();
                Position = position;
                Play();
            }
            else
            {
                Position = position;
            }
        }

        CancellationTokenSource? cts = null;
        CancellationTokenSource? Cts
        {
            get => cts; set
            {
                if (cts?.IsCancellationRequested ?? false)
                {
                    cts.Cancel();
                }
                cts = value;
            }
        }

        /// <summary>
        /// ビデオを再生
        /// </summary>
        public void Play()
        {
            if (IsPlaying) return;
            IsPlaying = true;
            if (Video is null)
            {
                IsPlaying = false;
                return;
            }
            if (Position < Video.Start)
            {
                Cts ??= new CancellationTokenSource();
                TimerManager.RunOnce(Video.Start - Position, () =>
                {
                    // Startになったら発火のはずだが上手くいかない場合があるので
                    if (IsPlaying)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Seek(Video.Start);
                            Cts = null;
                        });
                    }
                },
                Cts.Token);
            }
            else if (Position < Video.End)
            {
                Cts ??= new CancellationTokenSource();
                TimerManager.RunOnce(Video.End - Position, () =>
                {
                    if (IsPlaying)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Seek(Video.End);
                            Cts = null;
                        });
                    }
                }, Cts.Token);
            }
        }

        /// <summary>
        /// ビデオの再生を一時停止
        /// </summary>
        public void Pause()
        {
            if (IsPaused) return;
            IsPlaying = false;

            updateVideoPosition.Stop();

            Cts?.Cancel();
            Cts = null;
        }


        ~VideoPlayer()
        {
            Pause();
            video = null;
        }
    }

}
