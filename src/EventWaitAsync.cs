using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jannesen.Library.Tasks
{
    public class EventWaitAsync
    {
        private             bool                        _set;
        private             TaskCompletionSource<bool>  _waitSource;
        private readonly    object                      _lockObject;

        public                                          EventWaitAsync()
        {
            _set        = false;
            _waitSource   = null;
            _lockObject = new object();
        }
        public              void                        Signal()
        {
            lock(_lockObject) {
                _set = true;

                if (_waitSource != null) {
                    _waitSource.TrySetResult(true);
                }
            }
        }
        public              Task<Boolean>               WaitAsync()
        {
            return WaitAsync(Timeout.Infinite, CancellationToken.None);
        }
        public              Task<Boolean>               WaitAsync(int timeout)
        {
            return WaitAsync(timeout, CancellationToken.None);
        }
        public              Task<Boolean>               WaitAsync(CancellationToken cancellationToken)
        {
            return WaitAsync(Timeout.Infinite, cancellationToken);
        }
        public  async       Task<Boolean>               WaitAsync(int timeout, CancellationToken cancellationToken)
        {
            bool                            lockTaken = false;
            CancellationTokenRegistration?  ctr       = null;
            Timer                           timer     = null;

            try {
                Monitor.Enter(_lockObject, ref lockTaken);

                if (_set) {
                    return true;
                }

                if (timeout == 0) {
                    return false;
                }

                _waitSource = new TaskCompletionSource<bool>();

                if (cancellationToken.CanBeCanceled) {
                    ctr   = cancellationToken.Register(_callbackCancellation);
                }

                if (timeout != Timeout.Infinite) {
                    timer = new Timer(_callbackTimer, null, timeout, Timeout.Infinite);
                }

                Monitor.Exit(_lockObject);
                lockTaken = false;

                return await _waitSource.Task;
            }
            finally {
                if (!lockTaken) {
                    Monitor.Enter(_lockObject, ref lockTaken);
                }

                if (ctr.HasValue) {
                    ctr.Value.Dispose();
                }

                if (timer != null) {
                    timer.Dispose();
                }

                _waitSource = null;
                _set        = false;

                if (lockTaken) { 
                    Monitor.Exit(_lockObject);
                }
            }
        }

        private             void                        _callbackCancellation()
        {
            lock(_lockObject) {
                if (_waitSource != null) {
                    _waitSource.TrySetException(new TaskCanceledException());
                }
            }
        }
        private             void                        _callbackTimer(object _)
        {
            lock(_lockObject) {
                if (_waitSource != null) {
                    _waitSource.TrySetResult(false);
                }
            }
        }
    }
}
