using LSLib.LS;
using System.Text;

const int BASE_OFFSET = 48; // LSMF header size

string path = @"C:\Users\Hawkon\AppData\Local\Larian Studios\Baldur's Gate 3\PlayerProfiles\Public\Savegames\Story\Gale-71212614022__Wilderness - 2h 14m\Wilderness - 2h 14m.lsv";

Console.OutputEncoding = Encoding.UTF8;
Console.WriteLine("=== LSMF NEWAGE PARSER v2 ===\n");

var reader = new PackageReader();
var package = reader.Read(path);

var packFile = package.Files.FirstOrDefault(f => f.Name.Equals("Globals.lsf", StringComparison.OrdinalIgnoreCase));
if (packFile == null) { Console.WriteLine("Globals.lsf not found!"); return; }

using var stream = packFile.CreateContentReader();
using var mem = new MemoryStream();
stream.CopyTo(mem);
mem.Position = 0;

var res = new LSFReader(mem).Read();

if (!res.Regions.TryGetValue("NewAge", out var newAgeRegion) ||
    !newAgeRegion.Attributes.TryGetValue("NewAge", out var attr) ||
    attr.Value is not byte[] bytes)
{
    Console.WriteLine("No NewAge ScratchBuffer found!");
    return;
}

Console.WriteLine($"ScratchBuffer: {bytes.Length:N0} bytes ({bytes.Length / 1024.0 / 1024.0:F2} MB)\n");

// ========================================================
// PARSE LSMF HEADER (48 bytes)
// ========================================================
using var br = new BinaryReader(new MemoryStream(bytes));

var magic = Encoding.ASCII.GetString(br.ReadBytes(4));
if (magic != "LSMF") { Console.WriteLine($"Bad magic: {magic}"); return; }

var version = br.ReadUInt32();
var hash = br.ReadUInt64();
var blockOffset = br.ReadInt64();  // Offset to start of index block
var blockSize = br.ReadInt64();    // Total size of index block
var nameBlockSize = br.ReadInt32(); // Size of name strings block
var numberOfComponents = br.ReadUInt16();
var unknown1 = br.ReadUInt16();
var unknown2 = br.ReadUInt64();

Console.WriteLine($"Version: 0x{version:X8}");
Console.WriteLine($"Hash: 0x{hash:X16}");
Console.WriteLine($"Block offset: 0x{blockOffset:X} ({blockOffset})");
Console.WriteLine($"Block size: 0x{blockSize:X} ({blockSize})");
Console.WriteLine($"Name block size: {nameBlockSize}");
Console.WriteLine($"Components: {numberOfComponents}");
Console.WriteLine($"Unknown1: {unknown1}, Unknown2: {unknown2}");

// ========================================================
// PARSE COMPONENT ENTRIES
// ========================================================
// Name block starts at: blockOffset + BASE_OFFSET
// ComponentEntry array starts at: blockOffset + nameBlockSize + BASE_OFFSET
long componentEntryStart = blockOffset + nameBlockSize + BASE_OFFSET;

Console.WriteLine($"\nName block at: 0x{(blockOffset + BASE_OFFSET):X}");
Console.WriteLine($"ComponentEntry array at: 0x{componentEntryStart:X}");
Console.WriteLine($"ComponentEntry array end: 0x{(componentEntryStart + numberOfComponents * 48):X}");
Console.WriteLine($"File size: 0x{bytes.Length:X}\n");

var components = new List<ComponentInfo>();

// Debug: dump first 3 component entries raw
Console.WriteLine($"\nDEBUG: First 3 raw component entries at 0x{componentEntryStart:X}:");
for (int d = 0; d < 3; d++)
{
    long entryOff = componentEntryStart + d * 48;
    Console.Write($"  [{d}] ");
    for (int b = 0; b < 48; b++)
        Console.Write($"{bytes[entryOff + b]:X2} ");
    Console.WriteLine();
    // Parse fields
    long no = BitConverter.ToInt64(bytes, (int)entryOff);
    long ns = BitConverter.ToInt64(bytes, (int)entryOff + 8);
    ulong h = BitConverter.ToUInt64(bytes, (int)entryOff + 16);
    int ss = BitConverter.ToInt32(bytes, (int)entryOff + 24);
    int v = BitConverter.ToInt32(bytes, (int)entryOff + 28);
    long ne = BitConverter.ToInt64(bytes, (int)entryOff + 32);
    long co = BitConverter.ToInt64(bytes, (int)entryOff + 40);
    Console.WriteLine($"       nameOff={no} nameSize={ns} hash=0x{h:X} structSize={ss} ver={v} numElems={ne} compOff=0x{co:X}");
    // Try multiple name resolution strategies
    long abs1 = blockOffset + no + BASE_OFFSET;
    long abs2 = no + BASE_OFFSET;
    long abs3 = no;
    // Name table is at blockOffset + BASE_OFFSET = nameBlockStart
    long nameBlockStart = blockOffset + BASE_OFFSET;
    long abs4 = nameBlockStart + no;
    Console.WriteLine($"       Name try1 (blockOff+nameOff+BASE): @0x{abs1:X} = \"{SafeStr(bytes, abs1, ns)}\"");
    Console.WriteLine($"       Name try2 (nameOff+BASE):          @0x{abs2:X} = \"{SafeStr(bytes, abs2, ns)}\"");
    Console.WriteLine($"       Name try3 (nameOff):               @0x{abs3:X} = \"{SafeStr(bytes, abs3, ns)}\"");
    Console.WriteLine($"       Name try4 (nameBlockStart+nameOff):@0x{abs4:X} = \"{SafeStr(bytes, abs4, ns)}\"");
}

br.BaseStream.Position = componentEntryStart;
for (int i = 0; i < numberOfComponents; i++)
{
    var nameOffset = br.ReadInt64();
    var nameSizeRaw = br.ReadInt64();
    var nameLen = (int)(nameSizeRaw & 0xFFFFFFFF); // Low 32 bits = actual name length
    var nameExtra = (int)(nameSizeRaw >> 32);       // High 32 bits = unknown field
    var compHash = br.ReadUInt64();
    var structureSize = br.ReadInt32();
    var compVersion = br.ReadInt32();
    var numberOfElements = br.ReadInt64();
    var componentOffset = br.ReadInt64();

    // Resolve name: absolute = blockOffset + nameOffset + BASE_OFFSET
    string name = "";
    long absNamePos = blockOffset + nameOffset + BASE_OFFSET;
    if (absNamePos >= 0 && absNamePos + nameLen <= bytes.Length && nameLen > 0 && nameLen < 500)
    {
        name = Encoding.UTF8.GetString(bytes, (int)absNamePos, nameLen);
    }

    components.Add(new ComponentInfo
    {
        Index = i,
        Name = name,
        NameOffset = nameOffset,
        Hash = compHash,
        StructureSize = structureSize,
        Version = compVersion,
        NumberOfElements = numberOfElements,
        ComponentOffset = componentOffset,
        AbsoluteDataOffset = componentOffset + BASE_OFFSET
    });
}

// ========================================================
// PRINT ALL COMPONENTS
// ========================================================
Console.WriteLine($"=== ALL {components.Count} COMPONENTS ===\n");
foreach (var c in components)
{
    Console.WriteLine($"  [{c.Index:D3}] {c.Name,-60} elemSize={c.StructureSize,6}  count={c.NumberOfElements,6}  dataOff=0x{c.AbsoluteDataOffset:X8}  totalBytes={c.StructureSize * c.NumberOfElements,10}");
}

// ========================================================
// FIND INTERESTING COMPONENTS
// ========================================================
Console.WriteLine("\n=== INTERESTING COMPONENTS ===\n");

var interestingNames = new[]
{
    "inventory", "Stack", "Container", "Gold",
    "Stats", "Health", "Experience", "Level",
    "Value", "Armor", "BaseHp",
    "Camp", "Supply", "TotalSupplies",
    "core.v0.Level", "core.v0.EntityId"
};

foreach (var c in components)
{
    if (interestingNames.Any(n => c.Name.Contains(n, StringComparison.OrdinalIgnoreCase)))
    {
        Console.WriteLine($"  [{c.Index:D3}] {c.Name}");
        Console.WriteLine($"         structSize={c.StructureSize} count={c.NumberOfElements} dataOff=0x{c.AbsoluteDataOffset:X}");

        // Dump first few elements as hex
        if (c.NumberOfElements > 0 && c.AbsoluteDataOffset + c.StructureSize <= bytes.Length)
        {
            int dumpCount = (int)Math.Min(3, c.NumberOfElements);
            for (int e = 0; e < dumpCount; e++)
            {
                long elemOffset = c.AbsoluteDataOffset + (long)e * c.StructureSize;
                if (elemOffset + c.StructureSize > bytes.Length) break;

                Console.Write($"         [{e}] @0x{elemOffset:X}: ");
                int dumpBytes = Math.Min(c.StructureSize, 64);
                for (int b = 0; b < dumpBytes; b++)
                    Console.Write($"{bytes[elemOffset + b]:X2} ");
                if (c.StructureSize > 64) Console.Write("...");
                Console.WriteLine();
            }
        }
        Console.WriteLine();
    }
}

// ========================================================
// DEEP DIVE: game.inventory.v0.StackEntry
// ========================================================
var stackEntryComp = components.FirstOrDefault(c => c.Name == "game.inventory.v0.StackEntry");
if (stackEntryComp != null)
{
    Console.WriteLine($"=== STACK ENTRY DEEP DIVE ===");
    Console.WriteLine($"StructSize={stackEntryComp.StructureSize} Count={stackEntryComp.NumberOfElements}\n");

    // StackEntry from bg3se: { uint16_t EntityIndex, uint32_t Quantity }
    // Might be 6 or 8 bytes depending on padding
    Console.WriteLine("All StackEntry elements (EntityIndex, Quantity):");
    for (int e = 0; e < stackEntryComp.NumberOfElements; e++)
    {
        long off = stackEntryComp.AbsoluteDataOffset + (long)e * stackEntryComp.StructureSize;
        if (off + stackEntryComp.StructureSize > bytes.Length) break;

        if (stackEntryComp.StructureSize == 6)
        {
            ushort entityIndex = BitConverter.ToUInt16(bytes, (int)off);
            uint quantity = BitConverter.ToUInt32(bytes, (int)off + 2);
            if (quantity > 1) // Only show non-trivial quantities
                Console.WriteLine($"  [{e}] EntityIndex={entityIndex} Quantity={quantity}");
        }
        else if (stackEntryComp.StructureSize == 8)
        {
            // Try different field layouts
            ushort entityIndex = BitConverter.ToUInt16(bytes, (int)off);
            // Skip 2 bytes padding
            uint quantity = BitConverter.ToUInt32(bytes, (int)off + 4);
            if (quantity > 1)
                Console.WriteLine($"  [{e}] EntityIndex={entityIndex} Qty(off+4)={quantity}");
            // Also try contiguous
            uint qty2 = BitConverter.ToUInt32(bytes, (int)off + 2);
            if (qty2 > 1 && qty2 != quantity)
                Console.WriteLine($"        Qty(off+2)={qty2}");
        }
        else
        {
            // Unknown size - dump hex
            Console.Write($"  [{e}] ");
            for (int b = 0; b < stackEntryComp.StructureSize; b++)
                Console.Write($"{bytes[off + b]:X2} ");
            Console.WriteLine();
        }
    }
}

// ========================================================
// DEEP DIVE: game.inventory.v0.Stack
// ========================================================
var stackComp = components.FirstOrDefault(c => c.Name == "game.inventory.v0.Stack");
if (stackComp != null)
{
    Console.WriteLine($"\n=== STACK COMPONENT DEEP DIVE ===");
    Console.WriteLine($"StructSize={stackComp.StructureSize} Count={stackComp.NumberOfElements}\n");

    // StackComponent: { Array<EntityHandle> Elements, Array<StackEntry> Entries }
    // Array is likely { u64 startOffset, u64 endOffset } = 16 bytes per array
    // Total: 32 bytes
    for (int e = 0; e < Math.Min(10, (int)stackComp.NumberOfElements); e++)
    {
        long off = stackComp.AbsoluteDataOffset + (long)e * stackComp.StructureSize;
        if (off + stackComp.StructureSize > bytes.Length) break;

        Console.Write($"  [{e}] @0x{off:X}: ");
        for (int b = 0; b < Math.Min(stackComp.StructureSize, 64); b++)
            Console.Write($"{bytes[off + b]:X2} ");
        Console.WriteLine();

        // Try parsing as two offset pairs
        if (stackComp.StructureSize >= 32)
        {
            long elemStart = BitConverter.ToInt64(bytes, (int)off);
            long elemEnd = BitConverter.ToInt64(bytes, (int)off + 8);
            long entryStart = BitConverter.ToInt64(bytes, (int)off + 16);
            long entryEnd = BitConverter.ToInt64(bytes, (int)off + 24);

            long elemCount = 0;
            long entryCount = 0;
            if (elemEnd > elemStart && elemStart >= 0) elemCount = (elemEnd - elemStart) / 16; // EntityHandle = GUID = 16 bytes
            if (entryEnd > entryStart && entryStart >= 0)
            {
                // StackEntry size from the component info
                int seSize = stackEntryComp?.StructureSize ?? 8;
                entryCount = (entryEnd - entryStart) / seSize;
            }

            Console.WriteLine($"       Elements: [{elemStart:X} - {elemEnd:X}] count~{elemCount}");
            Console.WriteLine($"       Entries:  [{entryStart:X} - {entryEnd:X}] count~{entryCount}");
        }
    }
}

// ========================================================
// DEEP DIVE: game.stats.v3.StatsComponent
// ========================================================
var statsComp = components.FirstOrDefault(c => c.Name == "game.stats.v3.StatsComponent");
if (statsComp != null)
{
    Console.WriteLine($"\n=== STATS COMPONENT DEEP DIVE ===");
    Console.WriteLine($"StructSize={statsComp.StructureSize} Count={statsComp.NumberOfElements}\n");

    // StatsComponent from bg3se:
    // int InitiativeBonus (4)
    // int[7] Abilities (28) - STR, DEX, CON, INT, WIS, CHA, ?
    // int[7] AbilityModifiers (28)
    // int[18] Skills (72)
    // int ProficiencyBonus (4)
    // AbilityId SpellCastingAbility (4?)
    // ... more fields

    for (int e = 0; e < Math.Min(10, (int)statsComp.NumberOfElements); e++)
    {
        long off = statsComp.AbsoluteDataOffset + (long)e * statsComp.StructureSize;
        if (off + statsComp.StructureSize > bytes.Length) break;

        Console.WriteLine($"  [{e}] @0x{off:X}:");

        // Try reading ability scores
        int initiative = BitConverter.ToInt32(bytes, (int)off);
        Console.Write($"       Initiative={initiative} Abilities=[");
        for (int a = 0; a < 7; a++)
        {
            int val = BitConverter.ToInt32(bytes, (int)off + 4 + a * 4);
            Console.Write($"{val}");
            if (a < 6) Console.Write(", ");
        }
        Console.Write("] Modifiers=[");
        for (int a = 0; a < 7; a++)
        {
            int val = BitConverter.ToInt32(bytes, (int)off + 32 + a * 4);
            Console.Write($"{val}");
            if (a < 6) Console.Write(", ");
        }
        Console.WriteLine("]");

        // Skills
        Console.Write($"       Skills=[");
        for (int s = 0; s < 18; s++)
        {
            int val = BitConverter.ToInt32(bytes, (int)off + 60 + s * 4);
            Console.Write($"{val}");
            if (s < 17) Console.Write(", ");
        }
        Console.WriteLine("]");

        int profBonus = BitConverter.ToInt32(bytes, (int)off + 132);
        Console.WriteLine($"       ProficiencyBonus={profBonus}");
    }
}

// ========================================================
// DEEP DIVE: game.stats.v0.HealthComponent
// ========================================================
var healthComp = components.FirstOrDefault(c => c.Name == "game.stats.v0.HealthComponent");
if (healthComp != null)
{
    Console.WriteLine($"\n=== HEALTH COMPONENT DEEP DIVE ===");
    Console.WriteLine($"StructSize={healthComp.StructureSize} Count={healthComp.NumberOfElements}\n");

    // HealthComponent: { int Hp, int MaxHp, int TempHp, int MaxTempHp, GUID field, bool IsInvulnerable }
    for (int e = 0; e < Math.Min(20, (int)healthComp.NumberOfElements); e++)
    {
        long off = healthComp.AbsoluteDataOffset + (long)e * healthComp.StructureSize;
        if (off + healthComp.StructureSize > bytes.Length) break;

        int hp = BitConverter.ToInt32(bytes, (int)off);
        int maxHp = BitConverter.ToInt32(bytes, (int)off + 4);
        int tempHp = BitConverter.ToInt32(bytes, (int)off + 8);
        int maxTempHp = BitConverter.ToInt32(bytes, (int)off + 12);

        // Only show entities with reasonable HP values
        if (hp > 0 && hp <= 500 && maxHp > 0 && maxHp <= 500)
            Console.WriteLine($"  [{e}] HP={hp}/{maxHp} TempHP={tempHp}/{maxTempHp}");
    }
}

// ========================================================
// DEEP DIVE: game.experience.v0.ExperienceComponent
// ========================================================
var xpComp = components.FirstOrDefault(c => c.Name == "game.experience.v0.ExperienceComponent");
if (xpComp != null)
{
    Console.WriteLine($"\n=== EXPERIENCE COMPONENT DEEP DIVE ===");
    Console.WriteLine($"StructSize={xpComp.StructureSize} Count={xpComp.NumberOfElements}\n");

    // ExperienceComponent: { int CurrentLevelXP, int NextLevelXP, int TotalXP, u8 flag }
    for (int e = 0; e < Math.Min(20, (int)xpComp.NumberOfElements); e++)
    {
        long off = xpComp.AbsoluteDataOffset + (long)e * xpComp.StructureSize;
        if (off + xpComp.StructureSize > bytes.Length) break;

        int currentXP = BitConverter.ToInt32(bytes, (int)off);
        int nextLevelXP = BitConverter.ToInt32(bytes, (int)off + 4);
        int totalXP = BitConverter.ToInt32(bytes, (int)off + 8);

        if (totalXP > 0 && totalXP < 10000000)
            Console.WriteLine($"  [{e}] CurrentLevelXP={currentXP} NextLevelXP={nextLevelXP} TotalXP={totalXP}");
    }
}

// ========================================================
// DEEP DIVE: game.camp.v0.TotalSuppliesComponent
// ========================================================
var suppliesComp = components.FirstOrDefault(c => c.Name == "game.camp.v0.TotalSuppliesComponent");
if (suppliesComp != null)
{
    Console.WriteLine($"\n=== TOTAL SUPPLIES COMPONENT ===");
    Console.WriteLine($"StructSize={suppliesComp.StructureSize} Count={suppliesComp.NumberOfElements}\n");

    for (int e = 0; e < Math.Min(5, (int)suppliesComp.NumberOfElements); e++)
    {
        long off = suppliesComp.AbsoluteDataOffset + (long)e * suppliesComp.StructureSize;
        if (off + suppliesComp.StructureSize > bytes.Length) break;

        Console.Write($"  [{e}] @0x{off:X}: ");
        for (int b = 0; b < Math.Min(suppliesComp.StructureSize, 32); b++)
            Console.Write($"{bytes[off + b]:X2} ");
        Console.WriteLine();

        // Try reading as int32
        if (suppliesComp.StructureSize >= 4)
        {
            int val = BitConverter.ToInt32(bytes, (int)off);
            Console.WriteLine($"       AsInt32={val}");
        }
    }
}

Console.WriteLine("\n=== ANALYSIS COMPLETE ===");

// ========================================================
// Data class
// ========================================================
static string SafeStr(byte[] data, long offset, long length)
{
    if (offset < 0 || offset + length > data.Length || length <= 0 || length > 500) return "(out of range)";
    var s = Encoding.UTF8.GetString(data, (int)offset, (int)length);
    // Check if it looks like valid text
    if (s.Any(c => c < 32 && c != '\n' && c != '\r' && c != '\t')) return "(binary data)";
    return s.Length > 80 ? s[..80] + "..." : s;
}

class ComponentInfo
{
    public int Index;
    public string Name = "";
    public long NameOffset;
    public ulong Hash;
    public int StructureSize;
    public int Version;
    public long NumberOfElements;
    public long ComponentOffset;
    public long AbsoluteDataOffset;
}
