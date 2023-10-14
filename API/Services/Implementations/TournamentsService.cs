using API.Entities;
using API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Services.Implementations;

public class TournamentsService : ServiceBase<Tournament>, ITournamentsService
{
	private readonly ILogger<TournamentsService> _logger;
	private readonly OtrContext _context;
	private readonly IMatchesService _matchesService;
	public TournamentsService(ILogger<TournamentsService> logger, OtrContext context, IMatchesService matchesService) : base(logger, context)
	{
		_logger = logger;
		_context = context;
		_matchesService = matchesService;
	}

	public async Task<Tournament?> GetAsync(string name) => await _context.Tournaments.FirstOrDefaultAsync(x => x.Name.ToLower() == name.ToLower());

	public async Task PopulateAndLinkAsync()
	{
		var matches = await MatchesWithoutTournamentAsync();
		foreach (var match in matches)
		{
			var associatedTournament = await AssociatedTournament(match);

			if (associatedTournament == null)
			{
				associatedTournament = await CreateFromMatchDataAsync(match);

				if (associatedTournament != null)
				{
					_logger.LogInformation("Created tournament {TournamentName} ({TournamentId})", associatedTournament?.Name, associatedTournament?.Id);
				}
			}
			
			if(associatedTournament == null)
			{
				_logger.LogError("Could not create tournament from match {MatchId}", match.MatchId);
				continue;
			}
			
			var updated = LinkTournamentToMatch(associatedTournament, match);

			await _matchesService.UpdateAsync(updated);
			_logger.LogInformation("Linked tournament {TournamentName} ({TournamentId}) to match {MatchId}", associatedTournament.Name, associatedTournament.Id, match.MatchId);
		}
	}

	private async Task<IList<Match>> MatchesWithoutTournamentAsync()
	{
		return await _context.Matches.Where(x => x.TournamentId == null && x.TournamentName != null && x.Abbreviation != null && x.Mode != null && x.RankRangeLowerBound != null && x.TeamSize != null)
		                     .ToListAsync();
	}

	private async Task<Tournament?> AssociatedTournament(Match match)
	{
		if (match.Abbreviation == null || match.TournamentName == null)
		{
			return null;
		}
		
		return await _context.Tournaments
		                     .FirstOrDefaultAsync(x =>
			x.Name.ToLower() == match.TournamentName.ToLower() && x.Abbreviation.ToLower() == match.Abbreviation.ToLower());
	}

	private Match LinkTournamentToMatch(Tournament t, Match m)
	{
		if (t.Id == 0)
		{
			throw new ArgumentException("Tournament must be saved to the database before it can be linked to a match.");
		}
		
		m.TournamentId = t.Id;
		return m;
	}

	private async Task<Tournament?> CreateFromMatchDataAsync(Match m)
	{
		if (m.TournamentName == null || m.Abbreviation == null || m.Mode == null || m.RankRangeLowerBound == null || m.TeamSize == null)
		{
			return null;
		}
		
		var existing = await GetAsync(m.TournamentName);
		if (existing != null)
		{
			return existing;
		}

		return await CreateAsync(new Tournament
		{
			Name = m.TournamentName,
			Abbreviation = m.Abbreviation,
			ForumUrl = m.Forum ?? string.Empty,
			Mode = m.Mode.Value,
			RankRangeLowerBound = m.RankRangeLowerBound.Value,
			TeamSize = m.TeamSize.Value
		});
	}
}