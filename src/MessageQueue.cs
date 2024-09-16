using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Jannesen.Library.Tasks
{
    public sealed class MessageQueue<T> where T: class
    {
        private readonly    Queue<T>                            _queue;
        private readonly    List<TaskCompletionSource<T>>       _waiting;
        private             bool                                _closed;

        public                                                  MessageQueue(int capacity)
        {
            _queue      = new Queue<T>(capacity);
            _waiting    = new List<TaskCompletionSource<T>>();
            _closed     = false;
        }

        public              void                                Send(T message)
        {
            TaskCompletionSource<T>     waitTask = null;

            lock(_queue) {
                if (_closed) {
                    throw new QueueClosedException("Close closed.");
                }

                if (_waiting.Count > 0) {
                    waitTask = _waiting[0];
                    _waiting.RemoveAt(0);
                }
                else {
                    _queue.Enqueue(message);
                }
            }

            if (waitTask != null) {
                if (!waitTask.TrySetResult(message)) {
                    throw new InvalidOperationException("MessageQueue.Send TrySetResult failed.");
                }
            }
        }
        public      async   Task<T>                             Receive(CancellationToken ct)
        {
            TaskCompletionSource<T>     waitTask;

            lock(_queue) {
                if (_queue.Count > 0) {
                    return _queue.Dequeue();
                }

                if (_closed) {
                    return null;
                }

                waitTask = new TaskCompletionSource<T>();
                _waiting.Add(waitTask);
            }

            using (ct.Register(() => {
                                   lock(_queue) {
                                       _waiting.Remove(waitTask);
                                   }
                                   if (!waitTask.TrySetCanceled(ct)) {
                                        throw new InvalidOperationException("MessageQueue.Receive TrySetCanceled failed.");
                                   }
                               })) {
                return await waitTask.Task;
            }
        }
        public              void                                Close()
        {
            TaskCompletionSource<T>[]   toStop; 
            lock(_queue) {
                _closed = true;
                toStop = _waiting.ToArray();
                _waiting.Clear();
            }

            foreach (var t in toStop) {
                t.TrySetResult(null);
            }
        }
    }
}
