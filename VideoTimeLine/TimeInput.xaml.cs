using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.ComponentModel.DataAnnotations;
using System.Windows;

namespace VideoTimeLine
{
    /// <summary>
    /// TimeInput.xaml の相互作用ロジック
    /// </summary>
    public partial class TimeInput : Window
    {
        readonly TimeInputViewModel viewModel;

        public TimeInput(TimeSpan current, TimeSpan maximum)
        {
            InitializeComponent();
            viewModel = new TimeInputViewModel(current, maximum);
            this.DataContext = viewModel;
        }

        public TimeSpan Result => viewModel.Result;
    }

    public partial class TimeInputViewModel : ObservableValidator
    {
        public TimeInputViewModel(TimeSpan current, TimeSpan maximum)
        {
            Hours = (int)current.TotalHours;
            Minutes = current.Minutes;
            Seconds = (current - TimeSpan.FromHours(current.Hours) - TimeSpan.FromMinutes(current.Minutes)).TotalSeconds;
            MaxTime = maximum;
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Result))]
        [NotifyCanExecuteChangedFor(nameof(OKCommand))]
        [NotifyDataErrorInfo]
        [Required]
        [CustomValidation(typeof(TimeInputViewModel), nameof(ValidateHour))]
        [CustomValidation(typeof(TimeInputViewModel), nameof(ValidateTime))]
        private int hours;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Result))]
        [NotifyCanExecuteChangedFor(nameof(OKCommand))]
        [NotifyDataErrorInfo]
        [Required]
        [Range(0, 59)]
        [CustomValidation(typeof(TimeInputViewModel), nameof(ValidateTime))]
        private int minutes;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Result))]
        [NotifyCanExecuteChangedFor(nameof(OKCommand))]
        [NotifyDataErrorInfo]
        [Required]
        [CustomValidation(typeof(TimeInputViewModel), nameof(ValidateSeconds))]
        [CustomValidation(typeof(TimeInputViewModel), nameof(ValidateTime))]
        private double seconds;

        public TimeSpan MaxTime { get; private init; } = TimeSpan.MaxValue;

        public TimeSpan Result => TimeSpan.FromSeconds(Seconds) + TimeSpan.FromMinutes(Minutes) + TimeSpan.FromHours(Hours);

        partial void OnHoursChanged(int value) => ValidateAllProperties();
        partial void OnMinutesChanged(int value) => ValidateAllProperties();
        partial void OnSecondsChanged(double value) => ValidateAllProperties();

        public static ValidationResult? ValidateHour(int hour, ValidationContext context)
        {
            TimeInputViewModel instance = (TimeInputViewModel)context.ObjectInstance;
            return instance.MaxTime.Hours >= hour && hour >= 0 ? ValidationResult.Success : new("Out of Range");
        }

        public static ValidationResult? ValidateSeconds(double seconds, ValidationContext _) => seconds switch
        {
            < 60 and >= 0 => ValidationResult.Success,
            _ => new("Out of Range")
        };

        public static ValidationResult? ValidateTime(object _, ValidationContext context)
        {
            TimeInputViewModel instance = (TimeInputViewModel)context.ObjectInstance;
            return instance.Result <= instance.MaxTime && instance.Result >= TimeSpan.Zero ? ValidationResult.Success : new("Out of Range");
        }

        bool NoErrors => !HasErrors;

        [RelayCommand(CanExecute = nameof(NoErrors))]
        void OK(Window window)
        {
            window.DialogResult = true;
            window.Close();
        }
    }
}
