using BG3.SaveEditor.Core.Models;
using LSLib.LS;
using LSLib.LS.Enums;
using System.Diagnostics;

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

                // Strategy 1: Search template/item nodes for gold (legacy XML-style saves)
                FindGoldItems(resource, state.GoldItems);

                // Strategy 2: Parse NewAge ScratchBuffer for ECS gold data (modern saves)
                if (state.GoldItems.Count == 0)
                {
                    Console.WriteLine("[DEBUG] No gold found in template nodes, trying ScratchBuffer...");
                    TryParseScratchBufferGold(resource, state);
                }

                break;
            }
        }

        return state;
    }

    /// <summary>
    /// Try to extract gold info from the NewAge ScratchBuffer (LSMF binary blob).
    /// Modern BG3 saves store inventory/gold in the ECS component system.
    /// </summary>
    private void TryParseScratchBufferGold(Resource resource, SaveState state)
    {
        try
        {
            if (!resource.Regions.TryGetValue("NewAge", out var newAgeRegion))
                return;

            if (!newAgeRegion.Attributes.TryGetValue("NewAge", out var attr) ||
                attr.Value is not byte[] bytes)
                return;

            Console.WriteLine($"[DEBUG] ScratchBuffer found: {bytes.Length:N0} bytes");

            var parser = new ScratchBufferParser();
            var sbData = parser.Parse(bytes);

            Console.WriteLine($"[DEBUG] Parsed {sbData.Components.Count} components, {sbData.StackEntries.Count} stack entries with qty > 1");

            // Stack entries with large quantities are likely gold
            // In BG3, gold is typically the largest quantity stackable item
            foreach (var entry in sbData.StackEntries.OrderByDescending(e => e.Quantity))
            {
                state.GoldItems.Add(new GoldItem
                {
                    Handle = $"entity_{entry.EntityIndex}",
                    TemplateName = entry.Quantity > 100 ? "LOOT_Gold_A (ScratchBuffer)" : $"StackEntry_{entry.EntityIndex}",
                    Amount = (int)Math.Min(entry.Quantity, int.MaxValue),
                    Location = "NewAge ScratchBuffer"
                });
            }

            if (state.GoldItems.Count > 0)
            {
                Console.WriteLine($"[DEBUG] Found {state.GoldItems.Count} potential gold entries from ScratchBuffer, total: {state.TotalGold}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] ScratchBuffer parse failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Modify gold amount in save
    /// </summary>
    public async Task<string> SetGoldAsync(string savePath, int newAmount, string? outputPath = null)
    {
        if (newAmount < 0 || newAmount > 999_999_999)
        {
            throw new ArgumentException("Gold amount must be between 0 and 999,999,999");
        }

        // Create backup first
        var backupPath = CreateBackup(savePath);
        Console.WriteLine($"[DEBUG] Backup created at: {backupPath}");

        // Set output path
        outputPath ??= savePath.Replace(".lsv", "_modified.lsv");

        // Extract save to temp directory
        var tempDir = Path.Combine(Path.GetTempPath(), "BG3SaveEditor", Guid.NewGuid().ToString());
        var extractDir = Path.Combine(tempDir, "extracted");
        Directory.CreateDirectory(extractDir);

        try
        {
            // Step 1: Extract the save using Divine.exe
            Console.WriteLine("[DEBUG] Extracting save...");
            await ExecuteDivineAsync(new[] { "-g", "bg3", "-a", "extract-package", "-s", savePath, "-d", extractDir });

            // Step 2: Read and modify Globals.lsf
            var globalsLsf = Path.Combine(extractDir, "Globals.lsf");
            if (!File.Exists(globalsLsf))
            {
                throw new FileNotFoundException("Globals.lsf not found in save");
            }

            Console.WriteLine("[DEBUG] Reading Globals.lsf...");
            Resource resource;
            using (var stream = File.OpenRead(globalsLsf))
            {
                var lsfReader = new LSFReader(stream);
                resource = lsfReader.Read();
            }

            // Modify gold items
            int modifiedCount = ModifyGoldInResource(resource, newAmount);
            Console.WriteLine($"[DEBUG] Modified {modifiedCount} gold items");

            if (modifiedCount == 0)
            {
                throw new InvalidOperationException("No gold items found to modify in save file");
            }

            // Write modified LSF back
            Console.WriteLine("[DEBUG] Writing modified Globals.lsf...");
            using (var stream = File.Create(globalsLsf))
            {
                var lsfWriter = new LSFWriter(stream);
                lsfWriter.Write(resource);
            }

            // Step 3: Repack the save using Divine.exe
            Console.WriteLine($"[DEBUG] Repacking save to: {outputPath}");
            await ExecuteDivineAsync(new[] { "-g", "bg3", "-a", "create-package", "-s", extractDir, "-d", outputPath, "-c", "lz4" });

            Console.WriteLine($"[DEBUG] Save modified successfully");
            return outputPath;
        }
        finally
        {
            // Cleanup temp directory
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
            catch { /* Ignore cleanup errors */ }
        }
    }

    /// <summary>
    /// Execute Divine.exe with the given arguments
    /// </summary>
    private async Task ExecuteDivineAsync(string[] args)
    {
        var divinePath = GetDivinePath();

        var startInfo = new ProcessStartInfo
        {
            FileName = divinePath,
            Arguments = string.Join(" ", args.Select(a => a.Contains(' ') ? $"\"{a}\"" : a)),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        Console.WriteLine($"[DEBUG] Executing: {startInfo.FileName} {startInfo.Arguments}");

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new Exception($"Divine.exe failed: {error}\n{output}");
        }

        Console.WriteLine($"[DEBUG] Divine.exe output: {output}");
    }

    /// <summary>
    /// Get the path to Divine.exe
    /// </summary>
    private string GetDivinePath()
    {
        // Look for Divine.exe in multiple locations (published layout, dev layout, etc.)
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        var possiblePaths = new[]
        {
            // Published self-contained: tools/ subfolder next to BG3SaveEditor.exe
            Path.Combine(basePath, "tools", "Divine.exe"),
            // Dev build: navigate up from bin/Debug/net8.0 to backend/lib/...
            Path.Combine(basePath, "..\\..\\..\\..\\..\\backend\\lib\\lslib\\Packed\\Tools\\Divine.exe"),
            Path.Combine(basePath, "..\\..\\..\\..\\lib\\lslib\\Packed\\Tools\\Divine.exe"),
            // Workspace root tools/ folder
            Path.Combine(basePath, "..\\..\\..\\..\\..\\tools\\lslib\\Packed\\Tools\\Divine.exe"),
            Path.Combine(basePath, "lib\\lslib\\Packed\\Tools\\Divine.exe"),
        };

        foreach (var path in possiblePaths)
        {
            var fullPath = Path.GetFullPath(path);
            if (File.Exists(fullPath))
            {
                Console.WriteLine($"[DEBUG] Found Divine.exe at: {fullPath}");
                return fullPath;
            }
        }

        throw new FileNotFoundException("Divine.exe not found. Please ensure LSLib is installed in the lib folder.");
    }

    /// <summary>
    /// Create a timestamped backup of a save file
    /// </summary>
    public string CreateBackup(string savePath)
    {
        if (!File.Exists(savePath))
        {
            throw new FileNotFoundException("Save file not found", savePath);
        }

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var fileName = Path.GetFileNameWithoutExtension(savePath);
        var directory = Path.GetDirectoryName(savePath) ?? "";

        var backupPath = Path.Combine(directory, $"{fileName}_backup_{timestamp}.lsv");
        File.Copy(savePath, backupPath);

        Console.WriteLine($"[DEBUG] Backup created: {backupPath}");
        return backupPath;
    }

    private int ModifyGoldInResource(Resource resource, int newAmount)
    {
        int count = 0;
        bool isFirstGold = true;

        // Search in Templates region
        if (resource.Regions.TryGetValue("Templates", out var templatesRegion))
        {
            count += ModifyGoldInNode(templatesRegion, newAmount, ref isFirstGold);
        }

        // Search in Items region
        if (resource.Regions.TryGetValue("Items", out var itemsRegion))
        {
            count += ModifyGoldInNode(itemsRegion, newAmount, ref isFirstGold);
        }

        // Search in all other regions that might contain inventory
        foreach (var region in resource.Regions)
        {
            if (region.Key != "Templates" && region.Key != "Items")
            {
                count += ModifyGoldInNode(region.Value, newAmount, ref isFirstGold);
            }
        }

        return count;
    }

    private int ModifyGoldInNode(Node node, int newAmount, ref bool isFirstGold)
    {
        int count = 0;

        // Check if this node is a gold item
        var templateName = GetAttributeValue<string>(node, "MapKey") ??
                           GetAttributeValue<string>(node, "Stats") ??
                           GetAttributeValue<string>(node, "ItemName");

        if (templateName != null &&
            (templateName.Contains("Gold", StringComparison.OrdinalIgnoreCase) ||
             templateName.Contains("LOOT_Gold", StringComparison.OrdinalIgnoreCase)))
        {
            // Modify the amount
            if (isFirstGold)
            {
                // Set first gold item to the full amount
                SetAttributeValue(node, "Amount", newAmount);
                SetAttributeValue(node, "StackAmount", newAmount);
                isFirstGold = false;
                Console.WriteLine($"[DEBUG] Set first gold item to {newAmount}");
            }
            else
            {
                // Set additional gold items to 1 (or remove them)
                SetAttributeValue(node, "Amount", 1);
                SetAttributeValue(node, "StackAmount", 1);
                Console.WriteLine($"[DEBUG] Set additional gold item to 1");
            }
            count++;
        }

        // Recurse into children
        if (node.Children != null)
        {
            foreach (var childList in node.Children.Values)
            {
                foreach (var child in childList)
                {
                    count += ModifyGoldInNode(child, newAmount, ref isFirstGold);
                }
            }
        }

        return count;
    }

    private void SetAttributeValue<T>(Node node, string name, T value)
    {
        if (node.Attributes.ContainsKey(name))
        {
            node.Attributes[name].Value = value;
        }
        // Note: LSLib requires attributes to exist - we only modify existing ones
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
