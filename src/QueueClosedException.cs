using System;

namespace Jannesen.Library.Tasks
{
    public sealed class QueueClosedException: Exception
    {
        public                          QueueClosedException(string message): base(message)
        {
        }
    }
}
