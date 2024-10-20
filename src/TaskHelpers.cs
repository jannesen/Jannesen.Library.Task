using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jannesen.Library.Tasks
{
    public static class TaskHelpers
    {
        public  static async    Task<bool>      WhenAllWithTimeout(int milliseconds, params Task[] tasks)
        {
            if (tasks == null) throw new ArgumentNullException(nameof(tasks));

            var timeoutCompletionSource = new TaskCompletionSource<object>();
            var timeoutTask             = timeoutCompletionSource.Task;

            if (milliseconds <= 0) {
                for (int i = 0 ; i < tasks.Length ; ++i) {
                    if (tasks[i].Status != TaskStatus.Running) {
                        return false;
                    }
                }

                return true;
            }

            using (var x = new Timer((object state) => { ((TaskCompletionSource<object>)state).TrySetResult(null); }, timeoutCompletionSource, milliseconds, 0)) {
                for (int i = 0 ; i < tasks.Length ; ++i) {
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
            if (tasks is null) throw new ArgumentNullException(nameof(tasks));

            TaskCompletionSource<object>    tcs = new TaskCompletionSource<object>();

            using (var x = ct.Register(() => { tcs.SetException(new TaskCanceledException()); })) {
                for (int i = 0 ; i < tasks.Length ; ++i) {
                    if (tasks[i] != null) {
                        await Task.WhenAny(tasks[i], tcs.Task);
                    }
                }
            }
        }
    }
}
