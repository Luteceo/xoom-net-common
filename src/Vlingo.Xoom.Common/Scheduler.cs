﻿// Copyright © 2012-2022 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Vlingo.Xoom.Common
{
    /// <summary>
    /// Provide time-based notifications to a <code>IScheduled&lt;T&gt;</code> once or any number of
    /// times until cancellation. The implementor of the <code>IScheduled&lt;T&gt;</code> protocol
    /// is not assumed to be an <code>Actor</code> and may be a POCO, but the notifications
    /// are quite effectively used in an Actor-based asynchronous environment.
    /// </summary>
    public class Scheduler: IDisposable
    {
        private bool _disposed;
        private readonly ConcurrentStack<ICancellable> _tasks = new ConcurrentStack<ICancellable>();
        
        /// <summary>
        /// Answer a <code>ICancellable</code> for the repeating scheduled notifier.
        /// </summary>
        /// <typeparam name="T">The type of data to be sent with each notification.</typeparam>
        /// <param name="scheduled">The <code>IScheduled&lt;T&gt;</code> to receive notification.</param>
        /// <param name="data">The data to be sent with each notification.</param>
        /// <param name="delayBefore">The duration before notification interval timing will begin.</param>
        /// <param name="interval">The duration between each notification.</param>
        /// <returns><code>ICancellable</code></returns>
        public virtual ICancellable Schedule<T>(IScheduled<T> scheduled, T data, TimeSpan delayBefore, TimeSpan interval)
            => CreateAndStore(
                scheduled,
                data,
                delayBefore,
                interval,
                true);

        /// <summary>
        /// Answer a <code>ICancellable</code> for single scheduled notifier.
        /// </summary>
        /// <typeparam name="T">The type of data to be sent with each notification.</typeparam>
        /// <param name="scheduled">The <code>IScheduled&lt;T&gt;</code> to receive notification.</param>
        /// <param name="data">The data to be sent with the notification.</param>
        /// <param name="delayBefore">The duration before notification interval timing will begin.</param>
        /// <param name="interval">The duration before the single notification.</param>
        /// <returns><code>ICancellable</code></returns>
        public virtual ICancellable ScheduleOnce<T>(IScheduled<T> scheduled, T data, TimeSpan delayBefore, TimeSpan interval)
            => CreateAndStore(
                scheduled,
                data,
                delayBefore + interval,
                TimeSpan.FromMilliseconds(Timeout.Infinite),
                false);


        public virtual void Close()
        {
            foreach(var task in _tasks)
            {
                task.Cancel();
            }

            _tasks.Clear();
            Dispose(true);
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;    
            }
      
            if (disposing)
            {
                
                if (!_tasks.IsEmpty)
                {
                    Close();
                }
            }
      
            _disposed = true;
        }

        private SchedulerTask<T> CreateAndStore<T>(
            IScheduled<T> scheduled,
            T data,
            TimeSpan delayBefore,
            TimeSpan interval,
            bool repeats)
        {
            var task = new SchedulerTask<T>(scheduled, data, delayBefore, interval, repeats);
            _tasks.Push(task);
            return task;
        }

        private class SchedulerTask<T> : ICancellable, IRunnable
        {
            private readonly IScheduled<T> _scheduled;
            private readonly T _data;
            private readonly bool _repeats;
            private Timer? _timer;
            private bool _hasRun;

            public SchedulerTask(IScheduled<T> scheduled, T data, TimeSpan delayBefore, TimeSpan interval, bool repeats)
            {
                _scheduled = scheduled;
                _data = data;
                _repeats = repeats;
                _hasRun = false;
                _timer = new Timer(Tick, null, delayBefore, interval);
            }

            private void Tick(object? state) => Run();

            public void Run()
            {
                _hasRun = true;
                _scheduled.IntervalSignal(_scheduled, _data);

                if (!_repeats)
                {
                    Cancel();
                }
            }

            public bool Cancel()
            {
                if (_timer != null)
                {
                    _timer.Dispose();
                    _timer = null;
                }

                return _repeats || !_hasRun;
            }
        }
    }
}