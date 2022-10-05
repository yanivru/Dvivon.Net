using Microsoft.Diagnostics.Runtime;
using System.Diagnostics;

Console.WriteLine("Starting memory dump essence extraction...");

var processingStopwatch = Stopwatch.StartNew();

var commandLineArgs = Environment.GetCommandLineArgs();
if(commandLineArgs.Length == 1)
{
    Console.WriteLine("Error: Dump path not specified.");
}

using (var dataTarget = DataTarget.LoadDump(commandLineArgs[1]))
{
    var clrRuntime = dataTarget.ClrVersions[0].CreateRuntime();
    IEnumerable<ClrObject> objects = clrRuntime.Heap.EnumerateObjects();
    objects.Select(x => x.Ty)
}