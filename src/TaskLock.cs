﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Jannesen.Library.Tasks
{
    public sealed class TaskLock: IDisposable
    {
        private sealed class Entry
        {
            public      TaskCompletionSource<TaskSingletonAutoLeave>        TackCompletion;
            public      Timer?                                              Timer;
            public      CancellationTokenRegistration?                      Ctr;

            public                                                          Entry(TaskCompletionSource<TaskSingletonAutoLeave> tackCompletion)
            {
                TackCompletion = tackCompletion;
            }
            public      bool                                                TrySetResult(TaskSingletonAutoLeave rtn)
            {
                _dispose();
                return TackCompletion.TrySetResult(rtn);
            }
            public      void                                                SetException(Exception err)
            {
                _dispose();
                TackCompletion.SetException(err);
            }

            private     void                                                _dispose()
            {
                Timer?.Dispose();
                Ctr?.Dispose();
            }
        }

        private             int                                 _count;
        private             List<Entry>?                        _queue;

        public                                                  TaskLock()
        {
            _count = 0;
            _queue = new List<Entry>();
        }
        public              void                                Dispose()
        {
            lock(this) {
                if (_queue != null) {
                    var queue = _queue;
                    _queue = null;
                    foreach(var entry in queue) {
                        entry.SetException(new ObjectDisposedException("TaskLock"));
                    }
                }
            }
        }

        public              Task<TaskSingletonAutoLeave>        Enter()
        {
            return Enter(0, CancellationToken.None);
        }
        public              Task<TaskSingletonAutoLeave>        Enter(int timeout)
        {
            return Enter(timeout, CancellationToken.None);
        }
        public              Task<TaskSingletonAutoLeave>        Enter(CancellationToken ct)
        {
            return Enter(0, ct);
        }
        public              Task<TaskSingletonAutoLeave>        Enter(int timeout, CancellationToken ct)
        {
            lock(this) {
                ObjectDisposedException.ThrowIf(_queue == null, this);

                if (ct.IsCancellationRequested)
                   return Task.FromCanceled<TaskSingletonAutoLeave>(ct);

                if (_count > 0) {
                    var taskCompletion = new TaskCompletionSource<TaskSingletonAutoLeave>();
                    var entry          = new Entry(taskCompletion);

                    _queue.Add(entry);

                    if (timeout > 0)
                        entry.Timer = new Timer(_timeoutCallback, entry, timeout, Timeout.Infinite);

                    if (ct.CanBeCanceled)
                        entry.Ctr = ct.Register(_cancelCallback, entry);

                    return taskCompletion.Task;
                }
                else {
                    ++_count;
                    return Task.FromResult<TaskSingletonAutoLeave>(new TaskSingletonAutoLeave(this));
                }
            }
        }

        public              void                                Leave()
        {
            lock(this) {
                if (_queue != null) {
                    while (_queue.Count > 0) {
                        var entry = _queue[0];
                        _queue.RemoveAt(0);

                        if (_queue.Count == 0) {
                            _queue.TrimExcess();
                        }

                        if (entry.TrySetResult(new TaskSingletonAutoLeave(this)))
                            return;
                    }
                }

                --_count;
            }
        }

        private             void                                _timeoutCallback(object? state)
        {
            lock(this) {
                if (_queue != null) {
                    var index = _queue.IndexOf((Entry)state!);

                    if (index >= 0) {
                        var entry = _queue[index];
                        _queue.RemoveAt(index);
                        entry.SetException(new TimeoutException());
                    }
                }
            }
        }
        private             void                                _cancelCallback(object? state)
        {
            lock(this) {
                if (_queue != null) {
                    var index = _queue.IndexOf((Entry)state!);

                    if (index >= 0) {
                        var entry = _queue[index];
                        _queue.RemoveAt(index);
                        entry.SetException(new OperationCanceledException());
                    }
                }
            }
        }
    }

    public readonly struct TaskSingletonAutoLeave: IDisposable
    {
        private readonly    TaskLock                            _taskLock;

        public                                                  TaskSingletonAutoLeave(TaskLock taskLock)
        {
            _taskLock = taskLock;
        }

        public              void                                Dispose()
        {
            _taskLock.Leave();
        }
    }
}
