using API.DTOs;
using API.Services.Interfaces;
using API.Utilities;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[ApiVersion(1)]
[EnableCors]
[Route("api/v{version:apiVersion}/[controller]")]
public class PlayersController(IPlayerService playerService) : Controller
{
    [HttpGet("all")]
    [Authorize(Roles = OtrClaims.System)]
    public async Task<IActionResult> GetAllAsync()
    {
        IEnumerable<PlayerDTO> players = await playerService.GetAllAsync();
        return Ok(players);
    }

    [HttpGet("{key}/info")]
    [Authorize(Roles = $"{OtrClaims.User}, {OtrClaims.Client}")]
    [EndpointSummary("Get player info by versatile search")]
    [EndpointDescription("Get player info searching first by id, then osuId, then username")]
    public async Task<ActionResult<PlayerInfoDTO?>> GetAsync(string key)
    {
        PlayerInfoDTO? info = await playerService.GetVersatileAsync(key);

        if (info == null)
        {
            return NotFound($"User with key {key} does not exist");
        }

        return info;
    }

    [HttpGet("ranks/all")]
    [Authorize(Roles = OtrClaims.System)]
    public async Task<ActionResult<IEnumerable<PlayerRanksDTO>>> GetAllRanksAsync()
    {
        IEnumerable<PlayerRanksDTO> ranks = await playerService.GetAllRanksAsync();
        return Ok(ranks);
    }

    [HttpGet("id-mapping")]
    [Authorize(Roles = OtrClaims.System)]
    public async Task<ActionResult<IEnumerable<PlayerIdMappingDTO>>> GetIdMappingAsync()
    {
        IEnumerable<PlayerIdMappingDTO> mapping = await playerService.GetIdMappingAsync();
        return Ok(mapping);
    }

    [HttpGet("country-mapping")]
    [Authorize(Roles = OtrClaims.System)]
    [ProducesResponseType<IEnumerable<PlayerCountryMappingDTO>>(StatusCodes.Status200OK)]
    [EndpointSummary(
        "Returns a list of PlayerCountryMappingDTOs that have a player's id and their country tag."
    )]
    public async Task<IActionResult> GetCountryMappingAsync()
    {
        IEnumerable<PlayerCountryMappingDTO> mapping = await playerService.GetCountryMappingAsync();
        return Ok(mapping);
    }
}
