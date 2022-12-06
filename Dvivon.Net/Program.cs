using CsvHelper;
using Microsoft.Diagnostics.Runtime;
using System.Diagnostics;
using System.Globalization;

Console.WriteLine("Starting memory dump essence extraction...");

var processingStopwatch = Stopwatch.StartNew();

string?[] commandLineArgs = Environment.GetCommandLineArgs();
if (commandLineArgs.Length == 1)
{
    Console.WriteLine("Error: Dump path not specified.");
    return;
}

using var dataTarget = DataTarget.LoadDump(commandLineArgs[1]!);
{
    var clrRuntime = CreateRuntime(dataTarget, commandLineArgs.Length > 2 ? commandLineArgs[3] : null);
    IEnumerable<ClrObject> objects = clrRuntime.Heap.EnumerateObjects();

    var objectsPerType = objects.ToLookup(x => x.Type?.Name ?? "", x => new DumpObject(x.Type?.Name ?? "", x.Size, GetReferencedTypesNames(x).ToArray()));

    WriteTypesCsv(objectsPerType);

    WriteReferencesCsv(objectsPerType);
}

Console.WriteLine($"Run time: {processingStopwatch.Elapsed}");

void WriteReferencesCsv(ILookup<string, DumpObject> objectsPerType)
{
    var referencesStatistics = objectsPerType.SelectMany(objectsByType => objectsByType
                                                                            .SelectMany(x => x.ReferencedTypes)
                                                                            .GroupBy(referenceType => referenceType)
                                                                            .Select(z => new DumpReference(objectsByType.Key, z.Key, z.Count())));

    using var writer = new StreamWriter("References.csv");
    using CsvWriter csvWriter = new(writer, CultureInfo.InvariantCulture);
    csvWriter.WriteRecords(referencesStatistics);
}

static IEnumerable<string> GetReferencedTypesNames(ClrObject x)
{
    return x.EnumerateReferences().Select(y => y.Type?.Name ?? "");
}

static void WriteTypesCsv(ILookup<string, DumpObject> objectsPerType)
{
    var typesStatistics = objectsPerType.Select(x => new DumpType(x.Key, x.Count(), x.Sum(y => (decimal)y.Size)));

    using var writer = new StreamWriter("Types.csv");
    using CsvWriter csvWriter = new(writer, CultureInfo.InvariantCulture);
    csvWriter.WriteRecords(typesStatistics);
}

ClrRuntime CreateRuntime(DataTarget dt, string? dacPath)
{
    if (dt.ClrVersions.Length == 0)
    {
        throw new Exception("Clr Versions is empty");
    }

    return dacPath is not null ? dt.ClrVersions[0].CreateRuntime(dacPath) : dt.ClrVersions[0].CreateRuntime();
}