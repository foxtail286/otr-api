using API.Entities;

namespace APITests.SeedData;

public static class SeedData
{
	public static BaseStats GetBaseStats() => SeededBaseStats.Get();
	public static Beatmap GetBeatmap() => SeededBeatmap.Get();
	public static Config GetConfig() => SeededConfig.Get();
	public static Game GetGame() => SeededGame.Get();
	public static Match GetMatch() => SeededMatch.Get();
	public static MatchRatingStats GetMatchRatingStats() => SeededMatchRatingStats.Get();
	public static MatchScore GetMatchScore() => SeededMatchScore.Get();
	public static Player GetPlayer() => SeededPlayer.Get();
	public static PlayerMatchStats GetPlayerMatchStats() => SeededPlayerMatchStats.Get();
	public static Tournament GetTournament() => SeededTournament.Get();
	public static User GetUser() => SeededUser.Get();
}