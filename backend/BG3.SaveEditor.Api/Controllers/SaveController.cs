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
            var outputPath = request.OutputPath ?? request.Path;
            await _saveService.SetGoldAsync(request.Path, request.Amount, outputPath);
            return Ok(new { success = true, outputPath });
        }
        catch (NotImplementedException)
        {
            return StatusCode(501, "Gold modification not yet implemented");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Failed to modify gold: {ex.Message}");
        }
    }
}

public record LoadRequest(string Path);
public record SetGoldRequest(string Path, int Amount, string? OutputPath = null);
