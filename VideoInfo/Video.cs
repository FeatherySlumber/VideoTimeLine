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
    /// �r�f�I�Đ��̏��Ǘ��̂��߂̃N���X
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
        /// �w��p�����[�^�[�� <see cref="Video"/> �N���X�̐V�����C���X�^���X��������
        /// </summary>
        /// <param name="name">�r�f�I�̖��O</param>
        /// <param name="uri">�r�f�I�t�@�C���� URI</param>
        /// <param name="duration">�r�f�I�̍Đ�����</param>
        /// <param name="width">�r�f�I�̕�</param>
        /// <param name="height">�r�f�I�̍���</param>
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
        /// �w�肳�ꂽ���Ԃ��r�f�I�Đ��͈͓��ɂ��邩�ǂ�������
        /// </summary>
        /// <param name="timeSpan">���肷�鎞��</param>
        /// <returns>�w�肳�ꂽ���Ԃ��r�f�I�Đ��͈͓��ɂ���ꍇ�� <see langword="true"/>�A����ȊO�̏ꍇ�� <see langword="false"/>�B</returns>
        public bool IsTimeWithinRange(TimeSpan value) => Start <= value && value < End;

        ~Video()
        {
            Player.Close();
        }
    }

    /// <summary>
    /// �r�f�I�̍Đ��⑀����Ǘ����邽�߂̃N���X
    /// </summary>
    public class VideoPlayer : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, pceaDictionary.GetOrAdd(propertyName));
        static readonly Dictionary<string, PropertyChangedEventArgs> pceaDictionary = new();

        /// <summary>
        /// �r�f�I�̍Đ��ʒu���X�V
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
        /// �r�f�I��ݒ�
        /// </summary>
        /// <param name="value">�ݒ肷��r�f�I</param>
        /// <exception cref="Exception">�r�f�I���Đ����̏ꍇ�ɔ���</exception>
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
        /// �w�肳�ꂽ�Đ��ʒu�Ƀr�f�I���V�[�N
        /// </summary>
        /// <param name="position">�V�[�N����Đ��ʒu</param>
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
        /// �r�f�I���Đ�
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
                    // Start�ɂȂ����甭�΂̂͂�������肭�����Ȃ��ꍇ������̂�
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
        /// �r�f�I�̍Đ����ꎞ��~
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
