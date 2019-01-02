using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Jannesen.Library.Tasks
{
    public sealed class TaskLock: IDisposable
    {
        private class Entry
        {
            public      TaskCompletionSource<TaskSingletonAutoLeave>        TackCompletion;
            public      Timer                                               Timer;
            public      CancellationTokenRegistration?                      Ctr;

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
                if (Timer != null)
                    Timer.Dispose();

                if (Ctr.HasValue)
                    Ctr.Value.Dispose();
            }
        }

        private             int                                 _count;
        private             List<Entry>                         _queue;

        public                                                  TaskLock()
        {
            _count = 0;
            _queue = new List<Entry>();
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public              void                                Dispose()
        {
            lock(this)
            {
                while (_queue.Count > 0) {
                    Entry entry = _queue[0];
                    _queue.RemoveAt(0);
                    entry.SetException(new ObjectDisposedException("TaskLock"));
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
            lock(this)
            {
                if (ct.IsCancellationRequested)
                   return Task.FromCanceled<TaskSingletonAutoLeave>(ct);

                if (_count++ > 0) {
                    var taskCompletion = new TaskCompletionSource<TaskSingletonAutoLeave>();
                    var entry          = new Entry() { TackCompletion = taskCompletion };

                    _queue.Add(entry);

                    if (timeout > 0)
                        entry.Timer = new Timer(_timeoutCallback, entry, timeout, Timeout.Infinite);

                    if (ct.CanBeCanceled)
                        entry.Ctr = ct.Register(_cancelCallback, entry);

                    return taskCompletion.Task;
                }
                else
                    return Task.FromResult<TaskSingletonAutoLeave>(new TaskSingletonAutoLeave(this));
            }
        }

        public              void                                Leave()
        {
            lock(this)
            {
                while (--_count > 0) {
                    Entry entry = _queue[0];
                    _queue.RemoveAt(0);

                    if (_queue.Count == 0)
                        _queue.TrimExcess();

                    if (entry.TrySetResult(new TaskSingletonAutoLeave(this)))
                        return;
                }
            }
        }

        private             void                                _timeoutCallback(object state)
        {
            lock(this)
            {
                int     index = _queue.IndexOf((Entry)state);

                if (index >= 0) {
                    var entry = _queue[index];
                    _queue.RemoveAt(index);
                    entry.SetException(new TimeoutException());
                    --_count;
                }
            }
        }
        private             void                                _cancelCallback(object state)
        {
            lock(this)
            {
                int     index = _queue.IndexOf((Entry)state);

                if (index >= 0) {
                    var entry = _queue[index];
                    _queue.RemoveAt(index);
                    entry.SetException(new OperationCanceledException());
                    --_count;
                }
            }
        }
    }

    public struct TaskSingletonAutoLeave: IDisposable
    {
        private             TaskLock                            _taskLock;

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
