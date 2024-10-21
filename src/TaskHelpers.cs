using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jannesen.Library.Tasks
{
    public static class TaskHelpers
    {
        public  static async    Task<bool>      WhenAllWithTimeout(Task[] tasks, int milliseconds)
        {
            ArgumentNullException.ThrowIfNull(tasks);

            var timeoutCompletionSource = new TaskCompletionSource();
            var timeoutTask             = timeoutCompletionSource.Task;

            if (milliseconds <= 0) {
                for (var i = 0 ; i < tasks.Length ; ++i) {
                    if (tasks[i].Status != TaskStatus.Running) {
                        return false;
                    }
                }

                return true;
            }

            using (var x = new Timer((object? state) => {
                                        ((TaskCompletionSource)state!).TrySetResult();
                                     },
                                     timeoutCompletionSource, milliseconds, 0)) {
                for (var i = 0 ; i < tasks.Length ; ++i) {
                    await Task.WhenAny(tasks[i], timeoutTask);

                    if (timeoutTask.IsCompleted) {
                        return false;
                    }
                }

                return true;
            }
        }
        public  static async    Task            WhenAllWithCancellation(Task[] tasks, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(tasks);

            var tcs = new TaskCompletionSource();

            using (var x = ct.Register(() => {
                                           tcs.SetException(new TaskCanceledException());
                                       })) {
                for (var i = 0 ; i < tasks.Length ; ++i) {
                    if (tasks[i] != null) {
                        await Task.WhenAny(tasks[i], tcs.Task);
                    }
                }
            }
        }
    }
}
