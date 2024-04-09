using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace TimeLineControl
{

    /// <summary>
    /// 時間軸を表すカスタムコントロール
    /// </summary>
    public class TimeLine : Control
    {
        static TimeLine()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TimeLine), new FrameworkPropertyMetadata(typeof(TimeLine)));
        }

        public TimeLine()
        {
            this.Loaded += (sender, e) =>
            {
                ScaleCurrent = Current * Scale;
                ScaleStart = Start * Scale;
                ScaleDuration = Duration * Scale;
            };

            window = new(() =>
            {
                static Window getWindow(DependencyObject obj) => obj is Window window ? window : getWindow(VisualTreeHelper.GetParent(obj));
                return getWindow(this);
            });
        }

        public double Current
        {
            get => (double)GetValue(CurrentProperty);
            set => SetValue(CurrentProperty, value);
        }

        public static readonly DependencyProperty CurrentProperty = 
            DependencyProperty.Register(
                nameof(Current),
                typeof(double),
                typeof(TimeLine),
                new PropertyMetadata(0.0, static (sender, e) =>
                    {
                        if (sender is TimeLine tl) tl.ScaleCurrent = (double)e.NewValue * tl.Scale;
                    }
                )
            );

        public double ScaleCurrent
        {
            get => (double)GetValue(ScaleCurrentProperty);
            private set => SetValue(ScaleCurrentProperty, value);
        }

        public static readonly DependencyProperty ScaleCurrentProperty = 
            DependencyProperty.Register(
                nameof(ScaleCurrent),
                typeof(double),
                typeof(TimeLine),
                new PropertyMetadata(0.0)
            );


        public double Start
        {
            get => (double)GetValue(StartProperty);
            set => SetValue(StartProperty, value);
        }

        public static readonly DependencyProperty StartProperty = 
            DependencyProperty.Register(
                nameof(Start),
                typeof(double),
                typeof(TimeLine),
                new PropertyMetadata(0.0, static (sender, e) =>
                {
                    if (sender is TimeLine tl) tl.ScaleStart = (double)e.NewValue * tl.Scale;
                })
            );

        public double ScaleStart
        {
            get => (double)GetValue(ScaleStartProperty);
            private set => SetValue(ScaleStartProperty, value);
        }

        public static readonly DependencyProperty ScaleStartProperty = DependencyProperty.Register(
            nameof(ScaleStart), 
            typeof(double), 
            typeof(TimeLine), 
            new PropertyMetadata(0.0)
        );


        public double Duration
        {
            get => (double)GetValue(DurationProperty);
            set => SetValue(DurationProperty, value);
        }

        public static readonly DependencyProperty DurationProperty =
        DependencyProperty.Register(
            nameof(Duration),
            typeof(double),
            typeof(TimeLine),
            new PropertyMetadata(0.0, static (sender, e) =>
            {
                if (sender is TimeLine tl) tl.ScaleDuration = (double)e.NewValue * tl.Scale;
            })
        );

        public double ScaleDuration
        {
            get => (double)GetValue(ScaleDurationProperty);
            private set => SetValue(ScaleDurationProperty, value);
        }

        public static readonly DependencyProperty ScaleDurationProperty = 
            DependencyProperty.Register(
                nameof(ScaleDuration), 
                typeof(double),
                typeof(TimeLine), 
                new PropertyMetadata(0.0)
            );


        public double End
        {
            get => (double)GetValue(EndProperty);
            set => SetValue(EndProperty, value);
        }

        public static readonly DependencyProperty EndProperty = 
            DependencyProperty.Register(
                nameof(End), 
                typeof(double), 
                typeof(TimeLine), 
                new PropertyMetadata(0.0, static (sender, e) =>
                {
                    if (sender is TimeLine tl) tl.ScaleEnd = (double)e.NewValue * tl.Scale;
                })
            );

        public double ScaleEnd
        {
            get => (double)GetValue(ScaleEndProperty);
            private set
            {
                SetValue(ScaleEndProperty, value);
                SetValue(WidthProperty, value);
            }
        }

        public static readonly DependencyProperty ScaleEndProperty =
            DependencyProperty.Register(
                nameof(ScaleEnd), 
                typeof(double), 
                typeof(TimeLine),
                new PropertyMetadata(0.0)
            );


        public double Scale
        {
            get => (double)GetValue(ScaleProperty);
            set => SetValue(ScaleProperty, value);
        }

        public static readonly DependencyProperty ScaleProperty =
        DependencyProperty.Register(
            nameof(Scale),
            typeof(double),
            typeof(TimeLine),
            new PropertyMetadata(1.5, static (sender, e) =>
            {
                if (sender is TimeLine tl)
                {
                    double value = (double)e.NewValue;
                    tl.ScaleCurrent = value * tl.Current;
                    tl.ScaleStart = value * tl.Start;
                    tl.ScaleDuration = value * tl.Duration;
                    tl.ScaleEnd = value * tl.End;
                }
            })
        );

        private Canvas? baseRect;
        private Rectangle? durationRect;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (durationRect is not null)
            {
                durationRect.MouseDown -= CaptureAtDown;
            }

            baseRect = (Canvas)this.GetTemplateChild("PART_Canvas");
            durationRect = (Rectangle)this.GetTemplateChild("PART_DurationRect");

            durationRect.MouseDown += CaptureAtDown;
        }

        readonly Lazy<Window> window;
        double diff;

        private void CaptureAtDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not UIElement el) return;

            if (!Mouse.Capture(el)) return;
            diff = e.GetPosition(durationRect).X;
            window.Value.MouseMove += MoveStart;
            window.Value.MouseLeave += WindowLeave;
            window.Value.Deactivated += WindowDeactivated;
            window.Value.MouseUp += ReleaseAtUp;
        }

        private void MoveStart(object sender, MouseEventArgs e)
        {
            var temp = (e.MouseDevice.GetPosition(baseRect).X - diff) / Scale;
            if (temp < 0)
            {
                Start = 0;
            }
            else if(temp > End - Duration)
            {
                Start = End - Duration;
            }
            else
            {
                Start = temp;
            }
        }

        private void ReleaseMouse()
        {
            Mouse.Capture(null);
            window.Value.MouseMove -= MoveStart;
            window.Value.MouseLeave -= WindowLeave;
            window.Value.Deactivated -= WindowDeactivated;
            window.Value.MouseUp -= ReleaseAtUp;
        }

        private void ReleaseAtUp(object sender, MouseButtonEventArgs e) => ReleaseMouse();

        private void WindowLeave(object sender, MouseEventArgs e) => ReleaseMouse();

        private void WindowDeactivated(object? sender, EventArgs e) => ReleaseMouse();
    }
}
