using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jannesen.Library.Tasks
{
    public class EventWaitAsync
    {
        private readonly    bool                        _autoreset;
        private volatile    bool                        _set;
        private volatile    TaskCompletionSource<bool>? _waitSource;

        public                                          EventWaitAsync(bool initValue, bool autoReset)
        {
            _set        = initValue;
            _autoreset  = autoReset;
            _waitSource = null;
        }
        public              void                        Set()
        {
            _set = true;
            _waitSource?.TrySetResult(true);
        }
        public              void                        Reset()
        {
            _set = false;
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
            var ctr   = (CancellationTokenRegistration?)null;
            var timer = (Timer?)null;

            // Fast track _set is true just reset and done.
            if (_set) {
                if (_autoreset) {
                    _set = false;
                }
                return true;
            }

            // timeout is 0 just return false;
            if (timeout == 0) {
                return false;
            }

            try {
                var waitSource = new TaskCompletionSource<bool>();

                if (_waitSource != null) {
                    throw new InvalidOperationException("EventWaitAsync.WaitAsync already active.");
                }

                // Important that _waitSource is set before _set is tested second time or else a loophole exists.
                _waitSource = waitSource;

                if (_set) {
                    if (_autoreset) {
                        _set = false;
                    }

                    return true;
                }

                if (cancellationToken.CanBeCanceled) {
                    ctr = cancellationToken.Register(_callbackCancellation);
                }

                if (timeout != Timeout.Infinite) {
                    timer = new Timer(_callbackTimer, null, timeout, Timeout.Infinite);
                }

                if (await waitSource.Task) {
                    if (_autoreset) {
                        _set = false;
                    }
                    return true;
                }
                else {
                    return false;
                }
            }
            finally {
                _waitSource = null;
                ctr?.Dispose();
                timer?.Dispose();
            }
        }

        private             void                        _callbackCancellation()
        {
            _waitSource?.TrySetException(new TaskCanceledException());
        }
        private             void                        _callbackTimer(object? _)
        {
            _waitSource?.TrySetResult(false);
        }
    }
}
