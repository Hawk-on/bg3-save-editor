using System.Text;
using BG3.SaveEditor.Core.Models;

namespace BG3.SaveEditor.Core.Services;

/// <summary>
/// Parses the LSMF ScratchBuffer (NewAge region) from BG3 save files.
/// This binary blob contains the ECS component data for all game entities.
/// </summary>
public class ScratchBufferParser
{
    private const int LSMF_HEADER_SIZE = 48;
    private const int COMPONENT_ENTRY_SIZE = 48;

    /// <summary>
    /// Parse the LSMF header and component index from a ScratchBuffer byte array.
    /// </summary>
    public ScratchBufferData Parse(byte[] bytes)
    {
        if (bytes.Length < LSMF_HEADER_SIZE)
            throw new InvalidOperationException("ScratchBuffer too small for LSMF header");

        using var br = new BinaryReader(new MemoryStream(bytes));

        // Parse 48-byte header
        var magic = Encoding.ASCII.GetString(br.ReadBytes(4));
        if (magic != "LSMF")
            throw new InvalidOperationException($"Invalid LSMF magic: {magic}");

        var version = br.ReadUInt32();
        var hash = br.ReadUInt64();
        var blockOffset = br.ReadInt64();
        var blockSize = br.ReadInt64();
        var nameBlockSize = br.ReadInt32();
        var numberOfComponents = br.ReadUInt16();
        br.ReadUInt16(); // unknown1
        br.ReadUInt64(); // unknown2

        var data = new ScratchBufferData
        {
            Version = version,
            TotalSize = bytes.Length,
            ComponentCount = numberOfComponents
        };

        // Parse component entries
        long componentEntryStart = blockOffset + nameBlockSize + LSMF_HEADER_SIZE;
        br.BaseStream.Position = componentEntryStart;

        for (int i = 0; i < numberOfComponents; i++)
        {
            var nameOffset = br.ReadInt64();
            var nameSizeRaw = br.ReadInt64();
            var nameLen = (int)(nameSizeRaw & 0xFFFFFFFF);
            br.ReadUInt64(); // hash
            var structureSize = br.ReadInt32();
            var compVersion = br.ReadInt32();
            var numberOfElements = br.ReadInt64();
            var componentOffset = br.ReadInt64();

            // Resolve name
            string name = "";
            long absNamePos = blockOffset + nameOffset + LSMF_HEADER_SIZE;
            if (absNamePos >= 0 && absNamePos + nameLen <= bytes.Length && nameLen > 0 && nameLen < 500)
            {
                name = Encoding.UTF8.GetString(bytes, (int)absNamePos, nameLen);
            }

            data.Components.Add(new ComponentEntry
            {
                Index = i,
                Name = name,
                StructureSize = structureSize,
                Version = compVersion,
                NumberOfElements = numberOfElements,
                AbsoluteDataOffset = componentOffset + LSMF_HEADER_SIZE
            });
        }

        // Extract gold/inventory data from parsed components
        ExtractGoldData(bytes, data);

        return data;
    }

    /// <summary>
    /// Try to find gold-related data from inventory stack components.
    /// Uses StackEntry (quantity data) cross-referenced with item template names.
    /// </summary>
    private void ExtractGoldData(byte[] bytes, ScratchBufferData data)
    {
        // Look for StackEntry to find items with quantity > 1 (gold stacks)
        var stackEntry = data.Components.FirstOrDefault(c =>
            c.Name == "game.inventory.v0.StackEntry");

        if (stackEntry != null && stackEntry.NumberOfElements > 0)
        {
            Console.WriteLine($"[DEBUG] Found StackEntry: structSize={stackEntry.StructureSize}, count={stackEntry.NumberOfElements}");

            for (long e = 0; e < stackEntry.NumberOfElements; e++)
            {
                long off = stackEntry.AbsoluteDataOffset + e * stackEntry.StructureSize;
                if (off + stackEntry.StructureSize > bytes.Length) break;

                uint quantity = 0;
                ushort entityIndex = 0;

                if (stackEntry.StructureSize == 6)
                {
                    entityIndex = BitConverter.ToUInt16(bytes, (int)off);
                    quantity = BitConverter.ToUInt32(bytes, (int)off + 2);
                }
                else if (stackEntry.StructureSize == 8)
                {
                    entityIndex = BitConverter.ToUInt16(bytes, (int)off);
                    quantity = BitConverter.ToUInt32(bytes, (int)off + 4);
                    // Also try contiguous layout
                    var qty2 = BitConverter.ToUInt32(bytes, (int)off + 2);
                    if (qty2 > 1 && quantity <= 1) quantity = qty2;
                }

                if (quantity > 1 && quantity < 10_000_000)
                {
                    data.StackEntries.Add(new StackEntryData
                    {
                        EntityIndex = entityIndex,
                        Quantity = quantity
                    });
                }
            }

            Console.WriteLine($"[DEBUG] Found {data.StackEntries.Count} stack entries with quantity > 1");
        }

        // Extract health data
        var healthComp = data.Components.FirstOrDefault(c =>
            c.Name == "game.stats.v0.HealthComponent");
        if (healthComp != null)
        {
            for (long e = 0; e < Math.Min(50, healthComp.NumberOfElements); e++)
            {
                long off = healthComp.AbsoluteDataOffset + e * healthComp.StructureSize;
                if (off + healthComp.StructureSize > bytes.Length) break;

                int hp = BitConverter.ToInt32(bytes, (int)off);
                int maxHp = BitConverter.ToInt32(bytes, (int)off + 4);

                if (hp > 0 && hp <= 500 && maxHp > 0 && maxHp <= 500)
                {
                    data.HealthEntries.Add(new HealthData
                    {
                        EntityIndex = (int)e,
                        Hp = hp,
                        MaxHp = maxHp
                    });
                }
            }
        }

        // Extract experience data
        var xpComp = data.Components.FirstOrDefault(c =>
            c.Name == "game.experience.v0.ExperienceComponent");
        if (xpComp != null)
        {
            for (long e = 0; e < Math.Min(50, xpComp.NumberOfElements); e++)
            {
                long off = xpComp.AbsoluteDataOffset + e * xpComp.StructureSize;
                if (off + xpComp.StructureSize > bytes.Length) break;

                int currentXP = BitConverter.ToInt32(bytes, (int)off);
                int nextLevelXP = BitConverter.ToInt32(bytes, (int)off + 4);
                int totalXP = BitConverter.ToInt32(bytes, (int)off + 8);

                if (totalXP > 0 && totalXP < 10_000_000)
                {
                    data.ExperienceEntries.Add(new ExperienceData
                    {
                        EntityIndex = (int)e,
                        CurrentLevelXP = currentXP,
                        NextLevelXP = nextLevelXP,
                        TotalXP = totalXP
                    });
                }
            }
        }
    }
}
