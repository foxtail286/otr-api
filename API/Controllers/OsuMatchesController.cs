using API.Entities;
using API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OsuMatchesController : CrudController<OsuMatch>
{
	private readonly ILogger<OsuMatchesController> _logger;
	private readonly IMultiplayerLinkService _service;

	public OsuMatchesController(ILogger<OsuMatchesController> logger, IMultiplayerLinkService service) : base(logger, service)
	{
		_logger = logger;
		_service = service;
	}

	[HttpPost("batch")]
	public async Task<string> PostAsync([FromBody] IEnumerable<OsuMatch> linkBatch)
	{
		/**
		 * FLOW:
		 *
		 * The user submits a batch of links to the front-end. They are looking to add new data
		 * to our database that will eventually count towards ratings.
		 *
		 * This post endpoint takes these links, validates them (i.e. checks for duplicates,
		 * whether the match titles align with osu! tournament naming conventions,
		 * amount of matches being submitted, etc.).
		 *
		 * Assuming we have a good batch, we will mark all of the new items as "PENDING".
		 * The API.Osu.Multiplayer.MultiplayerLobbyDataWorker service checks the database for pending links
		 * periodically and processes them automatically.
		 */

		// Check if any of the links already exist in the database
		var ret = linkBatch.ToList();
		var existing = (await _service.CheckExistingAsync(ret.Select(x => x.MatchId).ToList())).ToList();
		if (existing.Any())
		{
			// Remove existing links from the batch
			ret.RemoveAll(x => existing.Contains(x.MatchId));
			_logger.LogInformation("Removed {Count} existing links from the batch", existing.Count);
		}

		// Mark as pending for worker
		ret.ForEach(async x =>
		{
			x.VerificationStatus = VerificationStatus.PendingVerification;
			x.Created = DateTime.Now;
			x.Updated = DateTime.Now;

			await _service.CreateAsync(x);
		});

		_logger.LogInformation("Created {Count} new links", ret.Count);

		return JsonConvert.SerializeObject(ret);
	}
}