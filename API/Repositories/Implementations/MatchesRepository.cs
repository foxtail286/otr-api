using System.Diagnostics.CodeAnalysis;
using API.DTOs;
using API.Entities;
using API.Enums;
using API.Handlers.Interfaces;
using API.Repositories.Interfaces;
using API.Utilities;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace API.Repositories.Implementations;

[SuppressMessage("Performance", "CA1862:Use the \'StringComparison\' method overloads to perform case-insensitive string comparisons")]
[SuppressMessage("ReSharper", "SpecifyStringComparison")]
public class MatchesRepository(
    ILogger<MatchesRepository> logger,
    IMapper mapper,
    OtrContext context,
    IMatchDuplicateRepository matchDuplicateRepository,
    ICacheHandler cacheHandler
    ) : HistoryRepositoryBase<Match, MatchHistory>(context, mapper), IMatchesRepository, IUsesCache
{
    private readonly OtrContext _context = context;

    public override async Task<Match?> GetAsync(int id) =>
        // Get the match with all associated data
        await MatchBaseQuery(false).FirstOrDefaultAsync(x => x.Id == id);

    public async Task InvalidateCacheEntriesAsync()
    {
        await cacheHandler.OnMatchUpdateAsync();
    }

    // Suppression: This query will inherently produce a large number of records by including
    // games and match scores. The query itself is almost as efficient as possible (as far as we know)
    [SuppressMessage("ReSharper.DPA", "DPA0007: Large number of DB records")]
    public async Task<IEnumerable<Match>> GetAsync(int limit, int page, bool filterUnverified = true) =>
        await MatchBaseQuery(filterUnverified)
            // Include all MatchDTO navigational properties
            .Include(m => m.Tournament)
            .OrderBy(m => m.Id)
            // Set index to start of desired page
            .Skip(limit * page)
            // Take only next n entities
            .Take(limit)
            .ToListAsync();

    public async Task<IEnumerable<MatchSearchResultDTO>> SearchAsync(string name)
    {
        //_ is a wildcard character in psql so it needs to have an escape character added in front of it.
        name = name.Replace("_", @"\_");
        return await _context.Matches
            .AsNoTracking()
            .WhereVerified()
            .Where(x => EF.Functions.ILike(x.Name ?? string.Empty, $"%{name}%", @"\"))
            .Select(m => new MatchSearchResultDTO()
            {
                Id = m.Id,
                MatchId = m.MatchId,
                Name = m.Name
            })
            .Take(30)
            .ToListAsync();
    }

    public async Task<Match?> GetAsync(int id, bool filterInvalidMatches = true) =>
        await MatchBaseQuery(filterInvalidMatches).FirstOrDefaultAsync(x => x.Id == id);

    public async Task<Match?> GetByMatchIdAsync(long matchId) =>
        await _context
            .Matches.Include(x => x.Games)
            .ThenInclude(x => x.MatchScores)
            .Include(x => x.Tournament)
            .FirstOrDefaultAsync(x => x.MatchId == matchId);

    public async Task<IList<Match>> GetMatchesNeedingAutoCheckAsync(int limit = 10000) =>
        // We only want api processed matches because the verification checks require the data from the API
        await _context
            .Matches.Include(x => x.Games)
            .ThenInclude(x => x.MatchScores)
            .Include(x => x.Tournament)
            .Where(x => x.NeedsAutoCheck == true && x.IsApiProcessed == true)
            .Take(limit)
            .ToListAsync();

    public async Task<Match?> GetFirstMatchNeedingApiProcessingAsync() =>
        await _context
            .Matches.Include(x => x.Games)
            .ThenInclude(x => x.MatchScores)
            .Where(x => x.IsApiProcessed == false)
            .FirstOrDefaultAsync();

    public async Task<IEnumerable<Match>> GetAsync(IEnumerable<long> matchIds) =>
        await _context.Matches.Where(x => matchIds.Contains(x.MatchId)).ToListAsync();

    public async Task<Match?> UpdateVerificationStatusAsync(
        int id,
        MatchVerificationStatus status,
        MatchVerificationSource source,
        string? info = null,
        int? verifierId = null
    )
    {
        Match? match = await GetAsync(id);
        if (match is null)
        {
            logger.LogWarning("Match {id} not found (failed to update verification status)", id);
            return null;
        }

        match.VerificationStatus = status;
        match.VerificationSource = source;
        match.VerificationInfo = info;

        logger.LogInformation(
            "Updated verification status of match {MatchId} to {Status} (source: {Source}, info: {Info})",
            id,
            status,
            source,
            info
        );

        // TODO: nullable "verifierId" can be passed for this without checking once #228 is merged
        if (verifierId.HasValue)
        {
            await UpdateAsync(match, verifierId.Value);
        }
        else
        {
            await UpdateAsync(match);
        }

        return match;
    }

    public async Task<IEnumerable<Match>> GetPlayerMatchesAsync(
        long osuId,
        int mode,
        DateTime before,
        DateTime after
    )
    {
        return await _context
            .Matches.IncludeAllChildren()
            .WherePlayerParticipated(osuId)
            .WhereMode(mode)
            .Before(before)
            .After(after)
            .ToListAsync();
    }

    public async Task UpdateAsApiProcessed(Match match)
    {
        match.IsApiProcessed = true;
        await UpdateAsync(match);
    }

    public async Task SetRequireAutoCheckAsync(bool invalidOnly = true)
    {
        if (invalidOnly)
        {
            await _context
                .Matches.Where(x =>
                    x.VerificationStatus != MatchVerificationStatus.Verified
                    && x.VerificationStatus != MatchVerificationStatus.PreVerified
                )
                .ExecuteUpdateAsync(x => x.SetProperty(y => y.NeedsAutoCheck, true));
        }
        else
        {
            // Applies to all matches
            await _context.Matches.ExecuteUpdateAsync(x => x.SetProperty(y => y.NeedsAutoCheck, true));
        }
    }

    public async Task MergeDuplicatesAsync(int matchRootId)
    {
        Match root = await GetAsync(matchRootId) ?? throw new InvalidOperationException($"Failed to find corresponding match: {matchRootId}");
        if (root.IsApiProcessed != true)
        {
            throw new Exception("All matches must be API processed.");
        }

        if (root.Games.Count == 0)
        {
            throw new Exception("Root does not contain any games.");
        }

        var totalScores = root.Games.Select(x => x.MatchScores.Count).Sum();
        if (totalScores == 0)
        {
            throw new Exception("Root has no scores.");
        }

        var duplicateReferences = (await matchDuplicateRepository.GetDuplicatesAsync(matchRootId)).ToList();
        if (duplicateReferences.Count == 0)
        {
            throw new Exception("Match does not have any detected duplicates.");
        }

        var duplicateMatches = (await GetMatchesFromDuplicatesAsync(duplicateReferences)).ToList();

        foreach (Match? duplicate in duplicateMatches)
        {
            if (root.TournamentId != duplicate.TournamentId)
            {
                throw new Exception("Tournament ids must match");
            }

            if (duplicate.IsApiProcessed != true)
            {
                throw new Exception("All matches must be API processed.");
            }

            var satisfiesNameCheck = root.Name == duplicate.Name;
            var satisfiesOsuIdCheck = root.MatchId == duplicate.MatchId;
            if (!satisfiesNameCheck && !satisfiesOsuIdCheck)
            {
                throw new Exception(
                    "Failed to satisfy preconditions. Either the name is a mismatch or the match id is a mismatch from the root."
                );
            }
        }

        // The rootId will be used when reassigning game / score data.
        var rootId = root.Id;
        foreach (Match? duplicate in duplicateMatches)
        {
            // Reassign all of the games' matchid fields.
            foreach (Game game in duplicate.Games)
            {
                game.MatchId = rootId;
                _context.Games.Update(game);
            }

            await _context.SaveChangesAsync();

            // Delete the match.
            // We don't delete the duplicate item entry because we
            // want to preserve the merged match links. This gives us
            // the ability to say "this match X was merged from Y, Z, etc."
            await DeleteAsync(duplicate.Id);

            logger.LogInformation(
                "Updated {GamesCount} games in duplicate match {DuplicateId} to point to new root parent match {RootId}",
                duplicate.Games.Count,
                duplicate.Id,
                rootId
            );
        }
    }

    public async Task VerifyDuplicatesAsync(int matchRoot, int userId, bool confirmed)
    {
        IEnumerable<MatchDuplicate> duplicates = await matchDuplicateRepository.GetDuplicatesAsync(matchRoot);
        foreach (MatchDuplicate dupe in duplicates)
        {
            dupe.VerifiedBy = userId;
            dupe.VerifiedAsDuplicate = confirmed;

            await matchDuplicateRepository.UpdateAsync(dupe);
        }
    }

    public async Task<IEnumerable<IList<Match>>> GetDuplicateGroupsAsync()
    {
        // Fetch groups by MatchId, excluding matches present in MatchDuplicates and confirmed duplicates
        var duplicatesById = (
            await _context
                .Matches.Where(m => !_context.MatchDuplicates.Any(md => md.OsuMatchId == m.MatchId))
                .Where(m => !_context.MatchDuplicates.Any(md => md.VerifiedAsDuplicate == true))
                .GroupBy(m => new { m.TournamentId, m.MatchId })
                .ToListAsync()
        )
            .Select(g => new { Group = g, Count = g.Count() })
            .Where(g => g.Count > 1)
            .Select(g => g.Group.ToList()) // Convert each group to List<Match>
            .ToList();

        // Fetch groups by Name and start date, excluding matches present in MatchDuplicates
        var groupedByNameAndDate = await _context
            .Matches.Where(m =>
                m.Name != null
                && m.StartTime.HasValue
                && !_context.MatchDuplicates.Any(md => md.OsuMatchId == m.MatchId)
            )
            .GroupBy(m => new
            {
                m.TournamentId,
                m.Name,
                m.StartTime!.Value.Date
            })
            .ToListAsync();

        var duplicatesByName = groupedByNameAndDate
            .Select(g => new
            {
                Group = g.Where(m1 =>
                        g.Any(m2 =>
                            m1 != m2 && Math.Abs((m2.StartTime - m1.StartTime)!.Value.TotalHours) <= 2
                        )
                    )
                    .ToList(),
                Count = g.Count()
            })
            .Where(g => g.Group.Count > 1)
            .Select(x => x.Group)
            .ToList();

        return duplicatesById.Concat(duplicatesByName);
    }

    private async Task<IEnumerable<Match>> GetMatchesFromDuplicatesAsync(
        IEnumerable<MatchDuplicate> duplicates
    )
    {
        var ls = new List<Match>();
        foreach (MatchDuplicate dupe in duplicates)
        {
            Match? match = await GetByMatchIdAsync(dupe.OsuMatchId);
            if (match == null)
            {
                continue;
            }

            ls.Add(match);
        }

        return ls;
    }

    private IQueryable<Match> MatchBaseQuery(bool filterInvalidMatches)
    {
        if (!filterInvalidMatches)
        {
            return _context
                .Matches.Include(x => x.Games)
                .ThenInclude(x => x.MatchScores)
                .Include(x => x.Games)
                .ThenInclude(x => x.Beatmap);
        }

        return _context
            .Matches.WhereVerified()
            .Include(x => x.Games.Where(y => y.VerificationStatus == GameVerificationStatus.Verified))
            .ThenInclude(x => x.MatchScores.Where(y => y.IsValid == true))
            .Include(x => x.Games.Where(y => y.VerificationStatus == GameVerificationStatus.Verified))
            .ThenInclude(x => x.Beatmap);
    }
}
