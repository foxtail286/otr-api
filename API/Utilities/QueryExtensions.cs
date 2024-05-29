using API.Entities;
using API.Enums;
using API.Osu.Enums;
using Microsoft.EntityFrameworkCore;

namespace API.Utilities;

public static class QueryExtensions
{
    // Player
    public static IQueryable<Player> WhereOsuId(this IQueryable<Player> query, long osuId) =>
        query.AsQueryable().Where(x => x.OsuId == osuId);

    /// <summary>
    /// Filters a query for players with the given username
    /// </summary>
    public static IQueryable<Player> WhereUsername(this IQueryable<Player> query, string username, bool partialMatch)
    {
        //_ is a wildcard character in psql so it needs to have an escape character added in front of it.
        username = username.Replace("_", @"\_");
        var pattern = partialMatch
            ? $"%{username}%"
            : username;

        return query
            .AsQueryable()
            .Where(p =>
                p.Username != null
                && EF.Functions.ILike(p.Username ?? string.Empty, pattern, @"\")
            );
    }

    // Match
    public static IQueryable<Match> WhereVerified(this IQueryable<Match> query) =>
        query
            .AsQueryable()
            .Where(x =>
                x.VerificationStatus == MatchVerificationStatus.Verified
                && x.IsApiProcessed == true
                && x.NeedsAutoCheck == false
            );

    public static IQueryable<Match> After(this IQueryable<Match> query, DateTime after) =>
        query.AsQueryable().Where(x => x.StartTime > after);

    public static IQueryable<Match> Before(this IQueryable<Match> query, DateTime before) =>
        query.AsQueryable().Where(x => x.StartTime < before);

    public static IQueryable<Match> WhereMode(this IQueryable<Match> query, int mode) =>
        query.AsQueryable().Where(x => x.Tournament.Ruleset == mode);

    public static IQueryable<Match> IncludeAllChildren(this IQueryable<Match> query) =>
        query
            .AsQueryable()
            .Include(x => x.Games)
            .ThenInclude(x => x.MatchScores)
            .Include(x => x.Games)
            .ThenInclude(x => x.Beatmap);

    public static IQueryable<Match> WherePlayerParticipated(this IQueryable<Match> query, long osuPlayerId) =>
        query
            .AsQueryable()
            .Where(x => x.Games.Any(y => y.MatchScores.Any(z => z.Player.OsuId == osuPlayerId)));

    // Game
    public static IQueryable<Game> WhereVerified(this IQueryable<Game> query) =>
        query
            .AsQueryable()
            .Where(x =>
                x.VerificationStatus == (int)GameVerificationStatus.Verified && x.RejectionReason == null
            );

    public static IQueryable<Game> WhereTeamVs(this IQueryable<Game> query) =>
        query.AsQueryable().Where(x => x.TeamType == TeamType.TeamVs);

    public static IQueryable<Game> WhereHeadToHead(this IQueryable<Game> query) =>
        query.AsQueryable().Where(x => x.TeamType == TeamType.HeadToHead);

    public static IQueryable<Game> After(this IQueryable<Game> query, DateTime after) =>
        query.AsQueryable().Where(x => x.StartTime > after);

    /// <summary>
    ///  Returns all MatchScores where either the game or the player had the specified mods enabled
    /// </summary>
    /// <param name="query"></param>
    /// <param name="enabledMods"></param>
    /// <returns></returns>
    public static IQueryable<MatchScore> WhereMods(
        this IQueryable<MatchScore> query,
        Mods enabledMods
    )
    {
        return query
            .AsQueryable()
            .Where(x =>
                (x.Game.Mods != Mods.None && x.Game.Mods == enabledMods)
                || // Not using NF
                (x.EnabledMods != null && x.EnabledMods.Value == (int)enabledMods)
                || (x.Game.Mods != Mods.None && x.Game.Mods == (enabledMods | Mods.NoFail))
                || // Using NF
                (x.EnabledMods != null && x.EnabledMods.Value == (int)(enabledMods | Mods.NoFail))
            );
    }

    // MatchScore
    /// <summary>
    ///  Selects scores that are verified
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    public static IQueryable<MatchScore> WhereVerified(this IQueryable<MatchScore> query) =>
        query
            .AsQueryable()
            .Where(x =>
                x.IsValid != false
                && x.Game.Match.VerificationStatus == MatchVerificationStatus.Verified
                && x.Game.VerificationStatus == (int)GameVerificationStatus.Verified
            );

    /// <summary>
    ///  Selects all HeadToHead scores
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    public static IQueryable<MatchScore> WhereHeadToHead(this IQueryable<MatchScore> query) =>
        query.AsQueryable().Where(x => x.Game.TeamType == TeamType.HeadToHead);

    public static IQueryable<MatchScore> WhereNotHeadToHead(this IQueryable<MatchScore> query) =>
        query.AsQueryable().Where(x => x.Game.TeamType != TeamType.HeadToHead);

    /// <summary>
    ///  Selects all TeamVs match scores
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    public static IQueryable<MatchScore> WhereTeamVs(this IQueryable<MatchScore> query) =>
        query.AsQueryable().Where(x => x.Game.TeamType == TeamType.TeamVs);

    /// <summary>
    ///  Selects all match scores, other than the provided player's, that are on the opposite team as the provided player.
    ///  Excludes HeadToHead scores
    /// </summary>
    /// <param name="query"></param>
    /// <param name="osuPlayerId"></param>
    /// <returns></returns>
    public static IQueryable<MatchScore> WhereOpponent(this IQueryable<MatchScore> query, long osuPlayerId) =>
        query
            .AsQueryable()
            .Where(x =>
                x.Game.MatchScores.Any(y => y.Player.OsuId == osuPlayerId)
                && x.Player.OsuId != osuPlayerId
                && x.Team != x.Game.MatchScores.First(y => y.Player.OsuId == osuPlayerId).Team
            );

    /// <summary>
    ///  Selects all match scores, other than the provided player's, that are on the same team as the provided player. Excludes
    ///  HeadToHead scores
    /// </summary>
    /// <param name="query"></param>
    /// <param name="osuPlayerId"></param>
    /// <returns></returns>
    public static IQueryable<MatchScore> WhereTeammate(this IQueryable<MatchScore> query, long osuPlayerId) =>
        query
            .AsQueryable()
            .Where(x =>
                x.Game.MatchScores.Any(y => y.Player.OsuId == osuPlayerId)
                && x.Player.OsuId != osuPlayerId
                && x.Game.TeamType != TeamType.HeadToHead
                && x.Team == x.Game.MatchScores.First(y => y.Player.OsuId == osuPlayerId).Team
            );

    public static IQueryable<MatchScore> WhereDateRange(
        this IQueryable<MatchScore> query,
        DateTime dateMin,
        DateTime dateMax
    ) => query.AsQueryable().Where(x => x.Game.StartTime > dateMin && x.Game.StartTime < dateMax);

    /// <summary>
    /// Selects all MatchScores for a given ruleset (e.g. mania)
    /// </summary>
    public static IQueryable<MatchScore> WhereRuleset(this IQueryable<MatchScore> query, Ruleset ruleset) =>
        query.AsQueryable().Where(x => x.Game.Ruleset == ruleset);

    public static IQueryable<MatchScore> WhereOsuPlayerId(
        this IQueryable<MatchScore> query,
        long osuPlayerId
    ) => query.AsQueryable().Where(x => x.Player.OsuId == osuPlayerId);

    public static IQueryable<MatchScore> WherePlayerId(this IQueryable<MatchScore> query, int playerId) =>
        query.AsQueryable().Where(x => x.PlayerId == playerId);

    public static IQueryable<MatchScore> After(this IQueryable<MatchScore> query, DateTime after) =>
        query.AsQueryable().Where(x => x.Game.StartTime > after);

    // Rating
    public static IQueryable<BaseStats> WhereRuleset(this IQueryable<BaseStats> query, Ruleset ruleset) =>
        query.AsQueryable().Where(x => x.Ruleset == ruleset);

    public static IQueryable<BaseStats> WhereOsuPlayerId(
        this IQueryable<BaseStats> query,
        long osuPlayerId
    ) => query.AsQueryable().Where(x => x.Player.OsuId == osuPlayerId);

    public static IQueryable<BaseStats> OrderByRatingDescending(this IQueryable<BaseStats> query) =>
        query.AsQueryable().OrderByDescending(x => x.Rating);
}
