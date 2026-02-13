using BG3.SaveEditor.Core.Services;
using LSLib.LS;
using LSLib.LS.Enums;
using System.IO;

var service = new SaveService();
string path = @"C:\Users\Hawkon\AppData\Local\Larian Studios\Baldur's Gate 3\PlayerProfiles\Public\Savegames\Story\Gale-71212614022__Wilderness - 2h 14m\Wilderness - 2h 14m.lsv";

Console.WriteLine($"Testing load save: {path}");

try
{
    var info = await service.GetSaveStateAsync(path);
    Console.WriteLine($"Found {info.TotalGold} gold (Expected ~195)");

    // Inspect resource structure
    var reader = new PackageReader();
    var package = reader.Read(path);
    var globals = package.Files.FirstOrDefault(f => f.Name.Contains("Globals.lsf"));
    if (globals != null)
    {
        using var stream = globals.CreateContentReader();
        using var mem = new MemoryStream();
        stream.CopyTo(mem);
        mem.Position = 0;
        
        var res = new LSLib.LS.LSFReader(mem).Read();
        Console.WriteLine("\nRegions found:");
        foreach (var region in res.Regions)
        {
            Console.WriteLine($"- {region.Key}");
            if (region.Key == "NewAge")
            {
                var node = region.Value;
                Console.WriteLine($"  Attributes: {string.Join(", ", node.Attributes.Keys)}");
                if (node.Attributes.TryGetValue("ScratchBuffer", out var attr))
                {
                    var bytes = (byte[])attr.Value;
                    Console.WriteLine($"  ScratchBuffer size: {bytes.Length} bytes");
                }
            }
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"ERROR: {ex}");
}
