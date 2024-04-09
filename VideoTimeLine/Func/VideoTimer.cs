using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Threading;
using Timer = System.Timers.Timer;

namespace VideoTimeLine
{
    static class VideoTimer
    {
        private static readonly DispatcherTimer timer = new()
        {
            Interval = TimeSpan.FromSeconds(0.25),
        };
        public static DispatcherTimer Get() => timer;

    }

}
