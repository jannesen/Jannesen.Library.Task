using System;
using System.Threading;
using System.Threading.Tasks;


namespace Jannesen.Library.Tasks
{
    public static class WaitHandlerHelper
    {
        public      static              Task<bool>          WaitOneAsync(this WaitHandle handle, int timeout)
        {
            return handle.WaitOneAsync(timeout, CancellationToken.None);
        }
        public      static              Task<bool>          WaitOneAsync(this WaitHandle handle, TimeSpan timeout)
        {
            return handle.WaitOneAsync((int)timeout.TotalMilliseconds, CancellationToken.None);
        }
        public      static              Task<bool>          WaitOneAsync(this WaitHandle handle, CancellationToken cancellationToken)
        {
            return handle.WaitOneAsync(Timeout.Infinite, cancellationToken);
        }
        public      static              Task<bool>          WaitOneAsync(this WaitHandle handle, TimeSpan timeout, CancellationToken cancellationToken)
        {
            return handle.WaitOneAsync((int)timeout.TotalMilliseconds, cancellationToken);
        }
        public      static async        Task<bool>          WaitOneAsync(this WaitHandle handle, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            var registeredHandle  = (RegisteredWaitHandle?)null;
            var tokenRegistration = (CancellationTokenRegistration?)null;
            var tcs               = new TaskCompletionSource<bool>();


            try {
                registeredHandle  = ThreadPool.RegisterWaitForSingleObject(handle,
                                                                            (state, timedOut) => ((TaskCompletionSource<bool>)state!).TrySetResult(!timedOut),
                                                                            tcs,
                                                                            millisecondsTimeout,
                                                                            true);

                if (cancellationToken.IsCancellationRequested) {
                    tokenRegistration = cancellationToken.Register(state => ((TaskCompletionSource<bool>)state!).TrySetCanceled(),
                                                                   tcs);
                }

                return await tcs.Task;
            }
            finally {
                registeredHandle?.Unregister(null);
                tokenRegistration?.Dispose();
            }
        }
    }
}
