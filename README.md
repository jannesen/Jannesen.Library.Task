# Jannesen.Library.Task

Task helper functions.

- WhenAllWithTimeout Task.WhenAll with a timeout.
- WaitOneAsync with optional cancellation token and/or optional timeout
- TaskLock Please a task-lock around a async section.

## TaskLock
```C#
    var taskLock = new TaskLock();


    async test() {
        using (await taskLock.Enter()) {
            // one 1 task active.
        }
    }

    async test_with_timeout() {
        using (await taskLock.Enter(1000)) {
            // one 1 task active.
        }
    }
```
