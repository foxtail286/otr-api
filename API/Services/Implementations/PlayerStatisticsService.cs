using API.DTOs;
using API.Entities;
using API.Repositories.Interfaces;
using API.Services.Interfaces;

namespace API.Services.Implementations;

public class PlayerStatisticsService : IPlayerStatisticsService
{
	private readonly IPlayerRepository _playerRepository;
	private readonly IPlayerMatchStatisticsRepository _matchStatsRepository;
	private readonly IMatchRatingStatisticsRepository _ratingStatsRepository;
	private readonly IPlayerScoreStatsService _playerScoreStatsService;

	public PlayerStatisticsService(IPlayerRepository playerRepository, IPlayerMatchStatisticsRepository matchStatsRepository, 
		IMatchRatingStatisticsRepository ratingStatsRepository, IPlayerScoreStatsService playerScoreStatsService)
	{
		_playerRepository = playerRepository;
		_matchStatsRepository = matchStatsRepository;
		_ratingStatsRepository = ratingStatsRepository;
		_playerScoreStatsService = playerScoreStatsService;
	}
	
	public async Task<PlayerStatisticsDTO> GetAsync(long osuPlayerId, int mode, DateTime dateMin, DateTime dateMax)
	{
		int playerId = await _playerRepository.GetIdByOsuIdAsync(osuPlayerId);
		var matchStats = await GetMatchStatsAsync(playerId, mode, dateMin, dateMax);
		var scoreStats = await GetScoreStatsAsync(playerId, mode, dateMin, dateMax);
		
		return new PlayerStatisticsDTO(matchStats, scoreStats);
	}

	public async Task BatchInsertAsync(IEnumerable<PlayerMatchStatisticsDTO> postBody)
	{
		var items = new List<PlayerMatchStatistics>();

		foreach (var item in postBody)
		{
			var stats = new PlayerMatchStatistics
			{
				PlayerId = item.PlayerId,
				MatchId = item.MatchId,
				Won = item.Won,
				AverageScore = item.AverageScore,
				AverageMisses = item.AverageMisses,
				AverageAccuracy = item.AverageAccuracy,
				GamesPlayed = item.GamesPlayed,
				AveragePlacement = item.AveragePlacement,
				GamesWon = item.GamesWon,
				GamesLost = item.GamesLost,
				TeammateIds = item.TeammateIds,
				OpponentIds = item.OpponentIds
			};
			
			items.Add(stats);
		}
		
		await _matchStatsRepository.InsertAsync(items);
	}

	public async Task BatchInsertAsync(IEnumerable<MatchRatingStatisticsDTO> postBody)
	{
		var items = new List<MatchRatingStatistics>();
		foreach (var item in postBody)
		{
			var stats = new MatchRatingStatistics
			{
				PlayerId = item.PlayerId,
				MatchId = item.MatchId,
				MatchCost = item.MatchCost,
				RatingBefore = item.RatingBefore,
				RatingAfter = item.RatingAfter,
				RatingChange = item.RatingChange,
				VolatilityBefore = item.VolatilityBefore,
				VolatilityAfter = item.VolatilityAfter,
				VolatilityChange = item.VolatilityChange,
				GlobalRankBefore = item.GlobalRankBefore,
				GlobalRankAfter = item.GlobalRankAfter,
				GlobalRankChange =  item.GlobalRankChange,
				CountryRankBefore = item.CountryRankBefore,
				CountryRankAfter = item.CountryRankAfter,
				CountryRankChange = item.CountryRankChange,
				PercentileBefore = item.PercentileBefore,
				PercentileAfter = item.PercentileAfter,
				PercentileChange = item.PercentileChange,
				AverageTeammateRating = item.AverageTeammateRating,
				AverageOpponentRating = item.AverageOpponentRating
			};
			
			items.Add(stats);
		}

		await _ratingStatsRepository.InsertAsync(items);
	}

	public async Task TruncateAsync()
	{
		await _matchStatsRepository.TruncateAsync();
		await _ratingStatsRepository.TruncateAsync();
	}

	private async Task<AggregatePlayerMatchStatisticsDTO?> GetMatchStatsAsync(int id, int mode, DateTime dateMin, DateTime dateMax)
	{
		var matchStats = (await _matchStatsRepository.GetForPlayerAsync(id, mode, dateMin, dateMax)).ToList();
		var ratingStats = (await _ratingStatsRepository.GetForPlayerAsync(id, mode, dateMin, dateMax)).ToList();

		if (!matchStats.Any())
		{
			return null;
		}
		
		return new AggregatePlayerMatchStatisticsDTO
		{
			HighestRating = (int) ratingStats.Max(x => x.RatingAfter),
			HighestGlobalRank = ratingStats.Min(x => x.GlobalRankAfter),
			HighestCountryRank = ratingStats.Min(x => x.CountryRankAfter),
			HighestPercentile = ratingStats.Max(x => x.PercentileAfter),
			RatingGained = ratingStats.First().RatingAfter - ratingStats.Last().RatingAfter,
			GamesWon = matchStats.Sum(x => x.GamesWon),
			GamesLost = matchStats.Sum(x => x.GamesLost),
			GamesPlayed = matchStats.Sum(x => x.GamesPlayed),
			MatchesWon = matchStats.Count(x => x.Won),
			MatchesLost = matchStats.Count(x => !x.Won),
			AverageTeammateRating = ratingStats.Average(x => x.AverageTeammateRating),
			AverageOpponentRating = ratingStats.Average(x => x.AverageOpponentRating),
			BestWinStreak = GetHighestWinStreak(matchStats),
			MatchAverageScoreAggregate = matchStats.Average(x => x.AverageScore),
			MatchAverageAccuracyAggregate = matchStats.Average(x => x.AverageAccuracy),
			MatchAverageMissesAggregate = matchStats.Average(x => x.AverageMisses),
			AverageGamesPlayedAggregate = matchStats.Average(x => x.GamesPlayed),
			AveragePlacingAggregate = matchStats.Average(x => x.AveragePlacement),
			MostPlayedTeammateName = await _playerRepository.GetUsernameAsync(MostPlayedTeammateId(matchStats)),
			MostPlayedOpponentName = await _playerRepository.GetUsernameAsync(MostPlayedOpponentId(matchStats)),
			PeriodStart = dateMin,
			PeriodEnd = dateMax
		};
	}
	
	private int GetHighestWinStreak(IEnumerable<PlayerMatchStatistics> stats)
	{
		int highest = 0;
		int current = 0;

		foreach (var item in stats)
		{
			if (item.Won)
			{
				current++;
			}
			else
			{
				if (current > highest)
				{
					highest = current;
				}

				current = 0;
			}
		}

		return highest;
	}
	
	private int? MostPlayedTeammateId(IEnumerable<PlayerMatchStatistics> stats)
	{
		var teammates = stats.SelectMany(x => x.TeammateIds).GroupBy(x => x).Select(x => new { Id = x.Key, Count = x.Count() }).ToList();
		return teammates.Any() ? teammates.OrderByDescending(x => x.Count).First().Id : null;
	}
	
	private int? MostPlayedOpponentId(IEnumerable<PlayerMatchStatistics> stats)
	{
		var opponents = stats.SelectMany(x => x.OpponentIds).GroupBy(x => x).Select(x => new { Id = x.Key, Count = x.Count() }).ToList();
		return opponents.Any() ? opponents.OrderByDescending(x => x.Count).First().Id : null;
	}
	
	private async Task<PlayerScoreStatsDTO?> GetScoreStatsAsync(int id, int mode, DateTime dateMin, DateTime dateMax)
	{
		return await _playerScoreStatsService.GetScoreStatsAsync(id, mode, dateMin, dateMax);
	}
}