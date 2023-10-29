using API.Utilities;

namespace API.DTOs;

/// <summary>
/// General stats that are current and not time specific, i.e. current rating, volatility, tier 
/// </summary>
public class BaseStatsDTO
{
	public BaseStatsDTO(double rating, double volatility, int mode, double percentile,
		int matchesPlayed, double winRate, int highestGlobalRank, int globalRank, int countryRank)
	{
		Rating = rating;
		Volatility = volatility;
		Mode = mode;
		Percentile = percentile;
		MatchesPlayed = matchesPlayed;
		WinRate = winRate;
		HighestGlobalRank = highestGlobalRank;
		GlobalRank = globalRank;
		CountryRank = countryRank;
	}
	
	public double Rating { get; set; }
	public double Volatility { get; set; }
	public int Mode { get; set; }
	public double Percentile { get; set; }
	public int MatchesPlayed { get; set; }
	public double WinRate { get; set; }
	public int HighestGlobalRank { get; set; }
	public int GlobalRank { get; set; }
	public int CountryRank { get; set; }

	public string Tier => RatingUtils.GetTier((int) Rating);
}