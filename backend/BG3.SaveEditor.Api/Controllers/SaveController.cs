using BG3.SaveEditor.Core.Models;
using BG3.SaveEditor.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace BG3.SaveEditor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SaveController : ControllerBase
{
    private readonly SaveService _saveService;

    public SaveController()
    {
        _saveService = new SaveService();
    }

    /// <summary>
    /// Load a save file and return its state including gold info
    /// </summary>
    [HttpPost("load")]
    public async Task<ActionResult<SaveState>> LoadSave([FromBody] LoadRequest request)
    {
        if (string.IsNullOrEmpty(request.Path) || !System.IO.File.Exists(request.Path))
        {
            return BadRequest("Invalid save file path");
        }

        try
        {
            var state = await _saveService.GetSaveStateAsync(request.Path);
            return Ok(state);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Failed to load save: {ex.ToString()}");
        }
    }

    /// <summary>
    /// Modify gold in a save file
    /// </summary>
    [HttpPost("gold")]
    public async Task<ActionResult> SetGold([FromBody] SetGoldRequest request)
    {
        if (string.IsNullOrEmpty(request.Path) || !System.IO.File.Exists(request.Path))
        {
            return BadRequest("Invalid save file path");
        }

        try
        {
            var outputPath = await _saveService.SetGoldAsync(request.Path, request.Amount, request.OutputPath);
            return Ok(new { success = true, outputPath });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Failed to modify gold: {ex.Message}");
        }
    }

    /// <summary>
    /// List save files from a directory
    /// </summary>
    [HttpPost("list")]
    public ActionResult<List<SaveFileInfo>> ListSaves([FromBody] ListSavesRequest request)
    {
        if (string.IsNullOrEmpty(request.Path) || !Directory.Exists(request.Path))
        {
            return BadRequest("Invalid save directory path");
        }

        try
        {
            var saves = new List<SaveFileInfo>();
            var files = Directory.GetFiles(request.Path, "*.lsv", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                saves.Add(new SaveFileInfo
                {
                    Path = file,
                    Name = Path.GetFileNameWithoutExtension(file),
                    Size = fileInfo.Length,
                    Modified = fileInfo.LastWriteTime
                });
            }

            // Sort by modified date (most recent first)
            saves = saves.OrderByDescending(s => s.Modified).ToList();

            return Ok(saves);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Failed to list saves: {ex.Message}");
        }
    }

    /// <summary>
    /// Create a backup of a save file
    /// </summary>
    [HttpPost("backup")]
    public ActionResult CreateBackup([FromBody] BackupRequest request)
    {
        if (string.IsNullOrEmpty(request.Path) || !System.IO.File.Exists(request.Path))
        {
            return BadRequest("Invalid save file path");
        }

        try
        {
            var backupPath = _saveService.CreateBackup(request.Path);
            return Ok(new { success = true, backupPath });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Failed to create backup: {ex.Message}");
        }
    }
}

public record LoadRequest(string Path);
public record SetGoldRequest(string Path, int Amount, string? OutputPath = null);
public record ListSavesRequest(string Path);
public record BackupRequest(string Path);

public class SaveFileInfo
{
    public string Path { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTime Modified { get; set; }
}
