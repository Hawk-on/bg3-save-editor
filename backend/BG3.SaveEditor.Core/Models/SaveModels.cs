namespace BG3.SaveEditor.Core.Models;

/// <summary>
/// Represents basic save file information
/// </summary>
public class SaveInfo
{
    public string FilePath { get; set; } = string.Empty;
    public string SaveName { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public int PartyLevel { get; set; }
    public TimeSpan PlayTime { get; set; }
    public DateTime SaveDate { get; set; }
    public int Gold { get; set; }
}

/// <summary>
/// Represents a gold item in the save
/// </summary>
public class GoldItem
{
    public string Handle { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public int Amount { get; set; }
    public string Location { get; set; } = string.Empty; // e.g., "Inventory", "Container", "World"
}

/// <summary>
/// Detailed save state with editable values
/// </summary>
public class SaveState
{
    public SaveInfo Info { get; set; } = new();
    public List<GoldItem> GoldItems { get; set; } = new();
    public int TotalGold => GoldItems.Sum(g => g.Amount);
}

// ============================================================
// ScratchBuffer / LSMF models (NewAge ECS component data)
// ============================================================

/// <summary>
/// Parsed LSMF ScratchBuffer data from the NewAge region
/// </summary>
public class ScratchBufferData
{
    public uint Version { get; set; }
    public long TotalSize { get; set; }
    public int ComponentCount { get; set; }
    public List<ComponentEntry> Components { get; set; } = new();
    public List<StackEntryData> StackEntries { get; set; } = new();
    public List<HealthData> HealthEntries { get; set; } = new();
    public List<ExperienceData> ExperienceEntries { get; set; } = new();
}

/// <summary>
/// A single ECS component type from the LSMF index
/// </summary>
public class ComponentEntry
{
    public int Index { get; set; }
    public string Name { get; set; } = string.Empty;
    public int StructureSize { get; set; }
    public int Version { get; set; }
    public long NumberOfElements { get; set; }
    public long AbsoluteDataOffset { get; set; }
}

/// <summary>
/// An inventory stack entry with quantity > 1
/// </summary>
public class StackEntryData
{
    public ushort EntityIndex { get; set; }
    public uint Quantity { get; set; }
}

/// <summary>
/// Health data for an entity
/// </summary>
public class HealthData
{
    public int EntityIndex { get; set; }
    public int Hp { get; set; }
    public int MaxHp { get; set; }
}

/// <summary>
/// Experience data for an entity
/// </summary>
public class ExperienceData
{
    public int EntityIndex { get; set; }
    public int CurrentLevelXP { get; set; }
    public int NextLevelXP { get; set; }
    public int TotalXP { get; set; }
}
