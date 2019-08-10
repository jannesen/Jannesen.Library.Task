using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jannesen.Library.Tasks
{
    public static class TaskHelpers
    {
        public  static async    Task<bool>      WhenAllWithTimeout(int milliseconds, params Task[] tasks)
        {
            bool    rtn = true;

            using (CancellationTokenSource cts = new CancellationTokenSource()) {
                var timeoutTask = Task.Delay(milliseconds, cts.Token);

                for (int i = 0 ; i < tasks.Length ; ++i) {
                    await Task.WhenAny(tasks[i], timeoutTask);

                    if (timeoutTask.IsCompleted) {
                        rtn = false;
                        break;
                    }
                }

                try {
                    if (rtn)
                        cts.Cancel();

                    timeoutTask.Wait();
                }
                catch(Exception) {
                }

                timeoutTask.Dispose();
            }

            return rtn;
        }
    }
}
