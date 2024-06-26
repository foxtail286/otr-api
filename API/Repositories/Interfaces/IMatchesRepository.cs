using API.DTOs;
using API.Entities;
using API.Enums;

namespace API.Repositories.Interfaces;

public interface IMatchesRepository : IHistoryRepository<Match, MatchHistory>
{
    Task<Match?> GetAsync(int id, bool filterInvalidMatches = true);

    /// <summary>
    /// Gets a paged list of matches
    /// </summary>
    /// <remarks>
    /// Matches are ordered by primary key. All navigational properties required for <see cref="MatchDTO"/> are included
    /// </remarks>
    /// <param name="limit">Amount of matches to return. Functions as the "page size"</param>
    /// <param name="page">Which block of matches to return</param>
    /// <param name="filterUnverified">If unverified matches should be excluded from the results</param>
    /// <returns>A list of matches of size <paramref name="limit"/> indexed by <paramref name="page"/></returns>
    Task<IEnumerable<Match>> GetAsync(int limit, int page, bool filterUnverified = true);

    Task<IEnumerable<Match>> GetAsync(IEnumerable<long> matchIds);
    Task<Match?> GetByMatchIdAsync(long matchId);
    Task<IEnumerable<MatchSearchResultDTO>> SearchAsync(string name);
    Task<IList<Match>> GetMatchesNeedingAutoCheckAsync(int limit = 10000);
    Task<Match?> GetFirstMatchNeedingApiProcessingAsync();

    /// <summary>
    /// Updates the verification status of a match for the given id
    /// </summary>
    /// <param name="id">Id of the match</param>
    /// <param name="verificationStatus">New verification status to assign</param>
    /// <param name="verificationSource">New verification source to assign</param>
    /// <param name="info">Optional verification info</param>
    /// <param name="verifierId">Optional user id to attribute the update to</param>
    /// <returns>An updated match, or null if not found</returns>
    Task<Match?> UpdateVerificationStatusAsync(
        int id,
        MatchVerificationStatus status,
        MatchVerificationSource source,
        string? info = null,
        int? verifierId = null
    );
    Task<IEnumerable<Match>> GetPlayerMatchesAsync(long osuId, int mode, DateTime before, DateTime after);
    Task UpdateAsApiProcessed(Match match);
    Task SetRequireAutoCheckAsync(bool invalidOnly = true);

    /// <summary>
    ///  Marks all duplicate matches of the <see cref="matchRootId" /> as duplicates. All game and score data from all of the
    ///  matches
    ///  will be moved to reference the <see cref="root" /> match. All duplicate osu match ids will then
    ///  be stored as a <see cref="MatchDuplicate" /> and deleted.
    ///  The root and the duplicates both must all have either a matching title, matching osu! id, or both.
    /// </summary>
    /// <param name="matchRootId">The id of the match that all duplicate data will be moved to</param>
    /// <exception cref="Exception">
    ///  Thrown if the matches are not from the same tournament or if any of the
    ///  matches have not been fully processed.
    ///  Thrown if any of the matches in <see cref="duplicates" /> are missing game or score data, or if
    ///  the start time of the root is not the earliest start time of both the root and all <see cref="duplicates" />, or if
    ///  the
    ///  duplicates fail to match the root's title or osu id.
    /// </exception>
    /// <returns></returns>
    Task MergeDuplicatesAsync(int matchRootId);

    Task VerifyDuplicatesAsync(int matchRoot, int userId, bool confirmed);

    /// <summary>
    ///  Returns all collections of duplicate matches present in the table.
    ///  Each collection represents a group of matches that are duplicates.
    ///  Use the <see cref="MergeDuplicatesAsync" /> method after identifying the
    ///  root match from the collection to merge the data.
    ///  Duplicates are any matches that have identical tournament ids AND either or both
    ///  of the following:
    ///  1. The MatchId properties are identical
    ///  2. The Name properties are identical
    /// </summary>
    /// <returns>A list of duplicate collections</returns>
    Task<IEnumerable<IList<Match>>> GetDuplicateGroupsAsync();
}
