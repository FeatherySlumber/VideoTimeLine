using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using VideoInfo;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace VideoTimeLine
{
    internal partial class ViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<Video> videos = new();

        [ObservableProperty]
        private Video? currentVideo;

        public VideoPlayer Player { get; private set; } = new();

        partial void OnCurrentVideoChanged(Video? value)
        {
            if (value is null) return;

            if (Player.IsPlaying)
            {
                Player.Pause();
                Player.SetVideo(value);
                Player.Play();
            }
            else
            {
                Player.SetVideo(value);
            }
        }

        partial void OnVideosChanged(ObservableCollection<Video>? oldValue, ObservableCollection<Video> newValue)
        {
            if (Videos.Count > 0)
            {
                Player.PlayLimit = Videos.Select(v => v.End).Max();
            }
            else
            {
                Player.PlayLimit = TimeSpan.MaxValue;
            }
        }

        [RelayCommand]
        private async Task DirectorySelectAsync()
        {
            FolderPicker folderPicker = new();
            folderPicker.FileTypeFilter.Add("*");

            IntPtr handle;
            if (Application.Current.Windows.OfType<Window>().SingleOrDefault(x => x.IsActive) is Window window)
            {
                handle = new System.Windows.Interop.WindowInteropHelper(window).Handle;
            }
            else
            {
                handle = Process.GetCurrentProcess().MainWindowHandle;
            }

            InitializeWithWindow.Initialize(folderPicker, handle);

            StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            if (folder is null) return;

            if (Player.IsPlaying) Player.Pause();

            ObservableCollection<Video> videos = new();

            IReadOnlyList<StorageFile> files = await folder.GetFilesAsync();
            foreach (StorageFile file in files)
            {
                if (file.ContentType.StartsWith("video"))
                {
                    VideoProperties vp = await file.Properties.GetVideoPropertiesAsync();
                    Video video = new(file.Name, new Uri(file.Path), vp.Duration, vp.Width, vp.Height);
                    video.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(Video.End))
                        {
                            Player.PlayLimit = Videos.Select(v => v.End).Max();
                        }
                    };
                    videos.Add(video);
                }
            }
            Videos = videos;
        }

        [RelayCommand]
        private void VideoPlayOrStop()
        {
            if (CurrentVideo is null) return;
            if (Player.IsPlaying)
            {
                Player.Pause();
            }
            else
            {
                Player.Play();
            }
        }

        [RelayCommand]
        private void Seek(TimeSpan newTime)
        {
            if (newTime != Player.Position)
            {
                Player.Seek(newTime);
            }
        }

        [RelayCommand]
        private void TimeChange(Video video)
        {
            TimeInput window = new(video.Start, Player.PlayLimit - video.Duration);
            if (window.ShowDialog() == true)
            {
                video.Start = window.Result;
            }
        }
    }
}
