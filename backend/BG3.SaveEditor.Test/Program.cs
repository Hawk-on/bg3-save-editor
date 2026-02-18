using BG3.SaveEditor.Core.Services;
using LSLib.LS;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;
Console.WriteLine("=== BG3 Save Editor — Integration Tests ===\n");

string savePath = @"C:\Users\Hawkon\AppData\Local\Larian Studios\Baldur's Gate 3\PlayerProfiles\Public\Savegames\Story\Gale-71212614022__Wilderness - 2h 14m\Wilderness - 2h 14m.lsv";

int passed = 0;
int failed = 0;

// ========================================================
// TEST 1: SaveService.LoadSaveAsync — basic metadata
// ========================================================
await RunTest("SaveService.LoadSaveAsync returns metadata", async () =>
{
    var service = new SaveService();
    var info = await service.LoadSaveAsync(savePath);

    Assert(info != null, "SaveInfo should not be null");
    Assert(!string.IsNullOrEmpty(info!.FilePath), "FilePath should be set");
    Assert(!string.IsNullOrEmpty(info.SaveName), "SaveName should be set");
    Console.WriteLine($"  SaveName={info.SaveName}, Player={info.PlayerName}, Level={info.PartyLevel}");
});

// ========================================================
// TEST 2: SaveService.GetSaveStateAsync — gold detection
// ========================================================
await RunTest("SaveService.GetSaveStateAsync returns gold data", async () =>
{
    var service = new SaveService();
    var state = await service.GetSaveStateAsync(savePath);

    Assert(state != null, "SaveState should not be null");
    Assert(state!.Info != null, "SaveState.Info should not be null");
    Console.WriteLine($"  GoldItems found: {state.GoldItems.Count}, TotalGold: {state.TotalGold}");

    foreach (var item in state.GoldItems.Take(5))
    {
        Console.WriteLine($"    {item.TemplateName}: {item.Amount} ({item.Location})");
    }
});

// ========================================================
// TEST 3: ScratchBufferParser — LSMF header parsing
// ========================================================
await RunTest("ScratchBufferParser parses LSMF header", async () =>
{
    var bytes = await ExtractScratchBuffer(savePath);
    Assert(bytes != null, "ScratchBuffer bytes should not be null");
    Assert(bytes!.Length > 48, $"ScratchBuffer should be > 48 bytes, got {bytes.Length}");

    var parser = new ScratchBufferParser();
    var data = parser.Parse(bytes);

    Assert(data.Version > 0, $"Version should be > 0, got {data.Version}");
    Assert(data.ComponentCount > 0, $"ComponentCount should be > 0, got {data.ComponentCount}");
    Assert(data.Components.Count == data.ComponentCount, $"Parsed {data.Components.Count} components but header says {data.ComponentCount}");
    Console.WriteLine($"  LSMF v0x{data.Version:X}, {data.TotalSize:N0} bytes, {data.ComponentCount} components");
});

// ========================================================
// TEST 4: ScratchBufferParser — component names resolve
// ========================================================
await RunTest("ScratchBufferParser resolves component names", async () =>
{
    var bytes = await ExtractScratchBuffer(savePath);
    var parser = new ScratchBufferParser();
    var data = parser.Parse(bytes);

    var namedCount = data.Components.Count(c => !string.IsNullOrEmpty(c.Name));
    Assert(namedCount > 0, "At least some components should have resolved names");
    Console.WriteLine($"  {namedCount}/{data.Components.Count} components have names");

    // Print first 10 component names
    foreach (var c in data.Components.Take(10))
    {
        Console.WriteLine($"    [{c.Index:D3}] {c.Name} (size={c.StructureSize}, count={c.NumberOfElements})");
    }
});

// ========================================================
// TEST 5: ScratchBufferParser — finds known components
// ========================================================
await RunTest("ScratchBufferParser finds expected ECS components", async () =>
{
    var bytes = await ExtractScratchBuffer(savePath);
    var parser = new ScratchBufferParser();
    var data = parser.Parse(bytes);

    var expectedComponents = new[]
    {
        "game.inventory.v0.StackEntry",
        "game.stats.v0.HealthComponent",
        "game.experience.v0.ExperienceComponent",
    };

    foreach (var expected in expectedComponents)
    {
        var found = data.Components.Any(c => c.Name == expected);
        Assert(found, $"Should find component '{expected}'");
        if (found)
        {
            var comp = data.Components.First(c => c.Name == expected);
            Console.WriteLine($"  ✅ {expected}: size={comp.StructureSize}, count={comp.NumberOfElements}");
        }
    }
});

// ========================================================
// TEST 6: ScratchBufferParser — stack entries (gold candidates)
// ========================================================
await RunTest("ScratchBufferParser extracts stack entries with qty > 1", async () =>
{
    var bytes = await ExtractScratchBuffer(savePath);
    var parser = new ScratchBufferParser();
    var data = parser.Parse(bytes);

    Console.WriteLine($"  Stack entries with qty > 1: {data.StackEntries.Count}");
    foreach (var entry in data.StackEntries.OrderByDescending(e => e.Quantity).Take(10))
    {
        Console.WriteLine($"    Entity {entry.EntityIndex}: qty={entry.Quantity}");
    }

    // At minimum we'd expect some items with quantity (arrows, gold, etc.)
    // Don't hard-assert count since it depends on save state
});

// ========================================================
// TEST 7: ScratchBufferParser — health data
// ========================================================
await RunTest("ScratchBufferParser extracts health data", async () =>
{
    var bytes = await ExtractScratchBuffer(savePath);
    var parser = new ScratchBufferParser();
    var data = parser.Parse(bytes);

    Console.WriteLine($"  Entities with HP (1-500): {data.HealthEntries.Count}");
    foreach (var h in data.HealthEntries.Take(10))
    {
        Console.WriteLine($"    Entity {h.EntityIndex}: HP={h.Hp}/{h.MaxHp}");
    }

    Assert(data.HealthEntries.Count > 0, "Should find at least one entity with valid HP");
});

// ========================================================
// TEST 8: ScratchBufferParser — experience data
// ========================================================
await RunTest("ScratchBufferParser extracts experience data", async () =>
{
    var bytes = await ExtractScratchBuffer(savePath);
    var parser = new ScratchBufferParser();
    var data = parser.Parse(bytes);

    Console.WriteLine($"  Entities with XP: {data.ExperienceEntries.Count}");
    foreach (var xp in data.ExperienceEntries.Take(10))
    {
        Console.WriteLine($"    Entity {xp.EntityIndex}: currentXP={xp.CurrentLevelXP}, nextLevel={xp.NextLevelXP}, total={xp.TotalXP}");
    }

    Assert(data.ExperienceEntries.Count > 0, "Should find at least one entity with XP data");
});

// ========================================================
// TEST 9: ScratchBufferParser — rejects invalid data
// ========================================================
await RunTest("ScratchBufferParser rejects invalid data", async () =>
{
    var parser = new ScratchBufferParser();

    // Buffer with wrong magic but large enough for header
    try
    {
        parser.Parse(new byte[64]); // 64 bytes, but starts with 0x00 not "LSMF"
        Assert(false, "Should throw on bad magic");
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("magic"))
    {
        Console.WriteLine($"  ✅ Correctly rejected bad magic: {ex.Message}");
    }

    // Buffer too small for LSMF header (< 48 bytes)
    try
    {
        parser.Parse(new byte[10]);
        Assert(false, "Should throw on too-small buffer");
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("too small"))
    {
        Console.WriteLine($"  ✅ Correctly rejected too-small buffer: {ex.Message}");
    }

    await Task.CompletedTask;
});

// ========================================================
// TEST 10: SaveService.CreateBackup
// ========================================================
await RunTest("SaveService.CreateBackup creates a copy", async () =>
{
    var service = new SaveService();
    var backupPath = service.CreateBackup(savePath);

    Assert(File.Exists(backupPath), $"Backup file should exist at {backupPath}");
    Assert(new FileInfo(backupPath).Length == new FileInfo(savePath).Length, "Backup should be same size");
    Console.WriteLine($"  Backup created: {Path.GetFileName(backupPath)}");

    // Clean up
    File.Delete(backupPath);
    Console.WriteLine("  Cleaned up backup file");
    await Task.CompletedTask;
});

// ========================================================
// RESULTS
// ========================================================
Console.WriteLine($"\n{"".PadRight(50, '=')}");
Console.WriteLine($"  RESULTS: {passed} passed, {failed} failed, {passed + failed} total");
Console.WriteLine($"{"".PadRight(50, '=')}");

return failed > 0 ? 1 : 0;

// ========================================================
// Helpers
// ========================================================

async Task RunTest(string name, Func<Task> test)
{
    Console.WriteLine($"TEST: {name}");
    try
    {
        await test();
        passed++;
        Console.WriteLine($"  ✅ PASSED\n");
    }
    catch (Exception ex)
    {
        failed++;
        Console.WriteLine($"  ❌ FAILED: {ex.Message}\n");
    }
}

void Assert(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

async Task<byte[]> ExtractScratchBuffer(string path)
{
    var reader = new PackageReader();
    var package = reader.Read(path);
    var packFile = package.Files.FirstOrDefault(f =>
        f.Name.Equals("Globals.lsf", StringComparison.OrdinalIgnoreCase));
    if (packFile == null) throw new Exception("Globals.lsf not found");

    using var stream = packFile.CreateContentReader();
    using var mem = new MemoryStream();
    await stream.CopyToAsync(mem);
    mem.Position = 0;

    var res = new LSFReader(mem).Read();

    if (!res.Regions.TryGetValue("NewAge", out var newAgeRegion) ||
        !newAgeRegion.Attributes.TryGetValue("NewAge", out var attr) ||
        attr.Value is not byte[] bytes)
    {
        throw new Exception("NewAge ScratchBuffer not found in save");
    }

    return bytes;
}
