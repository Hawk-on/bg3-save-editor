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
