using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Design",        "CA1031:Do not catch general exception types")]
[assembly: SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters")]
[assembly: SuppressMessage("Naming",        "CA1707:Identifiers should not contain underscores")]
[assembly: SuppressMessage("Reliability",   "CA2007:Consider calling ConfigureAwait on the awaited task")]
[assembly: SuppressMessage("Style",         "IDE1006:Naming Styles")]
[assembly: SuppressMessage("Reliability",   "CA2002:Do not lock on objects with weak identity")]

[assembly: SuppressMessage("Reliability",   "CA2000:Dispose objects before losing scope", Justification = "<Pending>", Scope = "member", Target = "~M:Jannesen.Library.Tasks.EventWaitTask.Wait(System.Int32,System.Threading.CancellationToken)~System.Threading.Tasks.Task{System.Boolean}")]
