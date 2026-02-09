using BG3.SaveEditor.Core.Models;
using LSLib.LS;
using LSLib.LS.Enums;

namespace BG3.SaveEditor.Core.Services;

/// <summary>
/// Service for loading and modifying BG3 save files using LSLib
/// </summary>
public class SaveService
{
    private string? _currentSavePath;
    private string? _tempExtractPath;

    /// <summary>
    /// Load a save file and return basic info
    /// </summary>
    public async Task<SaveInfo> LoadSaveAsync(string savePath)
    {
        _currentSavePath = savePath;
        _tempExtractPath = Path.Combine(Path.GetTempPath(), "BG3SaveEditor", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempExtractPath);

        Console.WriteLine($"[DEBUG] Loading save from: '{savePath}'");
        if (!File.Exists(savePath))
        {
            Console.WriteLine($"[DEBUG] File NOT FOUND at path: '{savePath}'");
        }
        else 
        {
             Console.WriteLine($"[DEBUG] File exists. Size: {new FileInfo(savePath).Length} bytes");
        }

        // Use LSLib to read the package (static method)
        var reader = new PackageReader();
        Console.WriteLine("[DEBUG] Created PackageReader");
        var package = reader.Read(savePath);
        Console.WriteLine($"[DEBUG] Package read successfully. Files: {package.Files.Count}");

        var info = new SaveInfo
        {
            FilePath = savePath,
            SaveName = Path.GetFileNameWithoutExtension(savePath)
        };


        // Find and extract SaveInfo.json for metadata
        foreach (var file in package.Files)
        {
            if (file.Name.Equals("SaveInfo.json", StringComparison.OrdinalIgnoreCase))
            {
                var extractPath = Path.Combine(_tempExtractPath, file.Name);
                
                // Extract to temp file
                using (var stream = file.CreateContentReader())
                using (var fileStream = File.Create(extractPath))
                {
                    await stream.CopyToAsync(fileStream);
                }
                
                // Parse SaveInfo.json (stream is now closed)
                var json = await File.ReadAllTextAsync(extractPath);
                info = ParseSaveInfo(json, info);
                break;
            }
        }

        return info;
    }

    /// <summary>
    /// Get gold information from the loaded save
    /// </summary>
    public async Task<SaveState> GetSaveStateAsync(string savePath)
    {
        var info = await LoadSaveAsync(savePath);
        var state = new SaveState { Info = info };

        var reader = new PackageReader();
        var package = reader.Read(savePath);

        // Extract and parse Globals.lsf for gold data
        foreach (var file in package.Files)
        {
            if (file.Name.Equals("Globals.lsf", StringComparison.OrdinalIgnoreCase))
            {
                using var stream = file.CreateContentReader();
                using var memStream = new MemoryStream();
                await stream.CopyToAsync(memStream);
                memStream.Position = 0;

                // Use LSLib to parse LSF to resource tree
                var lsfReader = new LSFReader(memStream);
                var resource = lsfReader.Read();

                // Search for gold items in the resource tree
                FindGoldItems(resource, state.GoldItems);
                break;
            }
        }

        return state;
    }

    /// <summary>
    /// Modify gold amount in save
    /// </summary>
    public async Task<bool> SetGoldAsync(string savePath, int newAmount, string outputPath)
    {
        // TODO: Implement gold modification using LSLib's write capabilities
        // This will involve:
        // 1. Reading the package
        // 2. Finding gold entries in Globals.lsf
        // 3. Modifying the values
        // 4. Writing back to a new package
        await Task.CompletedTask;
        throw new NotImplementedException("Gold modification coming soon");
    }

    private SaveInfo ParseSaveInfo(string json, SaveInfo info)
    {
        // Simple JSON parsing without bringing in more dependencies
        // Look for key fields
        if (json.Contains("\"PartyLevel\""))
        {
            var match = System.Text.RegularExpressions.Regex.Match(json, "\"PartyLevel\"\\s*:\\s*(\\d+)");
            if (match.Success) info.PartyLevel = int.Parse(match.Groups[1].Value);
        }

        if (json.Contains("\"Party\""))
        {
            // Extract first party member name
            var match = System.Text.RegularExpressions.Regex.Match(json, "\"Name\"\\s*:\\s*\"([^\"]+)\"");
            if (match.Success) info.PlayerName = match.Groups[1].Value;
        }

        if (json.Contains("\"RealPlayTime\""))
        {
            var match = System.Text.RegularExpressions.Regex.Match(json, "\"RealPlayTime\"\\s*:\\s*(\\d+(?:\\.\\d+)?)");
            if (match.Success) info.PlayTime = TimeSpan.FromSeconds(double.Parse(match.Groups[1].Value));
        }

        return info;
    }

    private void FindGoldItems(Resource resource, List<GoldItem> items)
    {
        // Traverse the resource tree looking for gold items
        if (resource.Regions.TryGetValue("Templates", out var templatesRegion))
        {
            SearchNodeForGold(templatesRegion, items, "Templates");
        }

        if (resource.Regions.TryGetValue("Items", out var itemsRegion))
        {
            SearchNodeForGold(itemsRegion, items, "Items");
        }

        // Also check for inventory data
        foreach (var region in resource.Regions)
        {
            if (region.Key.Contains("Inventory") || region.Key.Contains("Item"))
            {
                SearchNodeForGold(region.Value, items, region.Key);
            }
        }
    }

    private void SearchNodeForGold(Node node, List<GoldItem> items, string location)
    {
        // Check if this node contains gold
        var templateName = GetAttributeValue<string>(node, "MapKey") ?? 
                           GetAttributeValue<string>(node, "Stats") ??
                           GetAttributeValue<string>(node, "ItemName");

        if (templateName != null && 
            (templateName.Contains("Gold", StringComparison.OrdinalIgnoreCase) ||
             templateName.Contains("LOOT_Gold", StringComparison.OrdinalIgnoreCase)))
        {
            var amount = GetAttributeValue<int?>(node, "Amount") ?? 
                        GetAttributeValue<int?>(node, "StackAmount") ?? 1;
            
            var handle = GetAttributeValue<string>(node, "Handle") ?? 
                        GetAttributeValue<string>(node, "UUID") ?? "";

            items.Add(new GoldItem
            {
                Handle = handle,
                TemplateName = templateName,
                Amount = amount,
                Location = location
            });
        }

        // Recurse into children
        if (node.Children != null)
        {
            foreach (var childList in node.Children.Values)
            {
                foreach (var child in childList)
                {
                    SearchNodeForGold(child, items, location);
                }
            }
        }
    }

    private T? GetAttributeValue<T>(Node node, string name)
    {
        if (node.Attributes.TryGetValue(name, out var attr))
        {
            if (attr.Value is T val)
                return val;
            
            // Try conversion
            try
            {
                return (T)Convert.ChangeType(attr.Value, typeof(T));
            }
            catch
            {
                return default;
            }
        }
        return default;
    }

    public void Cleanup()
    {
        if (_tempExtractPath != null && Directory.Exists(_tempExtractPath))
        {
            try { Directory.Delete(_tempExtractPath, true); }
            catch { /* Ignore cleanup errors */ }
        }
    }
}
