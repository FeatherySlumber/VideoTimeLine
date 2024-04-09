using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Threading;
using Timer = System.Timers.Timer;

namespace VideoInfo
{
    /// <summary>
    /// 時間間隔を追跡するためのクラス
    /// </summary>
    internal class TimeIntervalTracker
    {
        readonly DispatcherTimer timer = new();
        public TimeIntervalTracker()
        {
            timer.Interval = Interval;
            timer.Tick += Handler;
        }

        /// <summary>
        /// 時間間隔が経過したときに呼び出されるコールバックアクションを取得または設定します。
        /// </summary>
        public Action<TimeSpan> IntervalCallback { get; set; } = (_) => { };

        void Handler(object? sender, EventArgs e)
        {
            TimeSpan ts = beforeFire is DateTime dt ? DateTime.Now - dt : TimeSpan.Zero;
            IntervalCallback(ts);
            beforeFire = DateTime.Now;
        }

        DateTime? beforeFire = null;
        /// <summary>
        /// タイマーの間隔を取得または設定
        /// </summary>
        public TimeSpan Interval { get; init; } = TimeSpan.FromSeconds(0.25);

        /// <summary>
        /// タイマーを開始
        /// </summary>
        public void Start()
        {
            beforeFire = null;
            timer.Start();
        }

        /// <summary>
        /// タイマーを停止
        /// </summary>
        public void Stop() => timer.Stop();
        /// <summary>
        /// タイマーが実行中かどうかを示す値を取得または設定
        /// </summary>
        /// <returns>タイマーが有効なら<see langword="true"/>、そうでなければ<see langword="false"/> デフォルトは<see langword="false"/></returns>
        public bool IsEnabled => timer.IsEnabled;
    }

    internal static class TimerManager
    {
        static TimerManager()
        {
            _ = new GarbageCollector();
        }

        /// <summary>
        /// 1つのタイマーインスタンスを制御
        /// </summary>
        class TimerController : IDisposable
        {
            private readonly Timer timer = new();

            public bool IsInUse { get; private set; } = false;

            public void Dispose()
            {
                ((IDisposable)timer).Dispose();
            }

            /// <summary>
            /// 指定された間隔とイベントハンドラでタイマーを開始
            /// </summary>
            /// <param name="handler">タイマーが経過したときに実行されるイベントハンドラ</param>
            /// <param name="time">タイマーが経過するまでの間隔</param>
            public void Start(ElapsedEventHandler handler, TimeSpan time)
            {
                if (IsInUse) return;
                IsInUse = true;
                timer.Interval = time.TotalMilliseconds;
                timer.Elapsed += handler;
                timer.Start();
            }
            /// <summary>
            /// タイマーを停止し、指定されたイベントハンドラを削除
            /// </summary>
            /// <param name="handler">削除するイベントハンドラ</param>
            public void Stop(ElapsedEventHandler handler)
            {
                if (!IsInUse) return;
                if (timer.Enabled) timer.Stop();
                timer.Elapsed -= handler;
                IsInUse = false;
            }
        }

        static readonly List<TimerController> activeTimers = new();

        /// <summary>
        /// 指定された時間間隔後に指定されたアクションを一度だけ実行
        /// </summary>
        /// <param name="time">アクションを実行するまでの時間間隔</param>
        /// <param name="action">実行するアクション</param>
        /// <param name="ct">アクションの実行をキャンセルするためのキャンセルトークン</param>
        /// <returns>非同期操作を表すタスク</returns>
        static public Task RunOnce(TimeSpan time, Action action, CancellationToken ct = default)
        {
            TimerController timer;
            if (activeTimers.Find(x => !x.IsInUse) is TimerController ut)
            {
                timer = ut;
            }
            else
            {
                timer = new TimerController();
                activeTimers.Add(timer);
            }

            TaskCompletionSource tcs = new();

            var dt = DateTime.Now;
            void handler(object? sender, EventArgs e)
            {
                timer.Stop(handler);

                action();
                tcs.TrySetResult();
            }

            ct.Register(() =>
            {
                timer.Stop(handler);
                tcs.TrySetCanceled();
            });

            timer.Start(handler, time);

            return tcs.Task;
        }

        /// <summary>
        /// 未使用のタイマーインスタンスを管理するクラス
        /// </summary>
        class GarbageCollector
        {
            ~GarbageCollector()
            {
                Debug.WriteLine($"GC{GC.GetGeneration(this)}");
                // 少なくとも1つ残し使用していないインスタンスへの参照を削除
                for (int cnt = activeTimers.Count - 1; cnt > 0; cnt--)
                {
                    if (activeTimers[cnt].IsInUse == false)
                    {
                        activeTimers[cnt].Dispose();
                        activeTimers.RemoveAt(cnt);
                    }
                }
                if (!AppDomain.CurrentDomain.IsFinalizingForUnload() && !Environment.HasShutdownStarted)
                {
                    GC.ReRegisterForFinalize(this);
                }
            }
        }
    }
}
