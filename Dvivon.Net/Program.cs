﻿using CsvHelper;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Runtime;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

Console.WriteLine("Starting memory dump essence extraction...");

var processingStopwatch = Stopwatch.StartNew();

var commandLineArgs = Environment.GetCommandLineArgs();
if (commandLineArgs.Length == 1)
{
    Console.WriteLine("Error: Dump path not specified.");
    return;
}

using (var dataTarget = DataTarget.LoadDump(commandLineArgs[1]))
{
    var clrRuntime = dataTarget.ClrVersions[0].CreateRuntime();
    IEnumerable<ClrObject> objects = clrRuntime.Heap.EnumerateObjects();

    var objectsPerType = objects.ToLookup(x => x.Type?.Name ?? "", x => new DumpObject(x.Type?.Name ?? "", x.Size, GetRefencedTypesNames(x).ToArray()));

    var typesStatistics = objectsPerType.Select(x => new DumpType(x.Key, x.Count(), x.Sum(y => (decimal)y.Size)));

    using (var writer = new StreamWriter("Types.csv"))
    {
        using (CsvWriter csvWriter = new(writer, CultureInfo.InvariantCulture))
        {
            csvWriter.WriteRecords(typesStatistics);
        }
    }
}

static IEnumerable<string> GetRefencedTypesNames(ClrObject x)
{
    return x.EnumerateReferences().Select(y => y.Type?.Name ?? "");
}

class DumpType
{
    public string Name { get; }
    public int Count { get; }
    public decimal Size { get; }

    public DumpType(string typeName, int count, decimal size)
    {
        Name = typeName;
        Count = count;
        Size = size;
    }
}
