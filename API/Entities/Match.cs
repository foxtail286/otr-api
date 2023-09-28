﻿using API.Enums;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Entities;

[Table("matches")]
[Index("MatchId", Name = "osumatches_matchid", IsUnique = true)]
public class Match
{
	[Key]
	[Column("id")]
	public int Id { get; set; }
	[Column("match_id")]
	public long MatchId { get; set; }
	[Column("name")]
	public string? Name { get; set; }
	[Column("abbreviation")]
	public string? Abbreviation { get; set; }
	[Column("forum")]
	public string? Forum { get; set; }
	[Column("tournament_name")]
	public string? TournamentName { get; set; }
	/// <summary>
	/// The "best skill" rank allowed in a tournament. Open is 1.
	/// If a tournament is #7500-35000, this would be 7500.
	/// </summary>
	[Column("rank_range_lower_bound")]
	public int? RankRangeLowerBound { get; set; }
	/// <summary>
	/// The amount of players that are supposed to be playing in this match, per team.
	/// </summary>
	[Column("team_size")]
	public int? TeamSize { get; set; }
	/// <summary>
	/// The play mode: mania, osu!, taiko, catch
	/// </summary>
	[Column("mode")]
	public int? Mode { get; set; }
	[Column("start_time", TypeName = "timestamp with time zone")]
	public DateTime? StartTime { get; set; }
	[Column("created", TypeName = "timestamp with time zone")]
	public DateTime Created { get; set; }
	[Column("updated", TypeName = "timestamp with time zone")]
	public DateTime? Updated { get; set; }
	[Column("end_time", TypeName = "timestamp with time zone")]
	public DateTime? EndTime { get; set; }
	[Column("verification_info")]
	public string? VerificationInfo { get; set; }
	[Column("verification_source")]
	public int? VerificationSource { get; set; }
	[Column("verification_status")]
	public int? VerificationStatus { get; set; }
	[Column("submitted_by_user")]
	public int? SubmitterUserId { get; set; }
	public User? SubmittedBy { get; set; }	
	[InverseProperty("Match")]
	public virtual ICollection<Game> Games { get; set; } = new List<Game>();
	[InverseProperty("Match")]
	public virtual ICollection<RatingHistory> RatingHistories { get; set; } = new List<RatingHistory>();
	[NotMapped]
	public MatchVerificationSource? VerificationSourceEnum
	{
		get
		{
			if (VerificationSource != null)
			{
				return (MatchVerificationSource)VerificationSource;
			}

			return null;
		}
	}
	[NotMapped]
	public MatchVerificationStatus? VerificationStatusEnum
	{
		get
		{
			if (VerificationStatus != null)
			{
				return (MatchVerificationStatus)VerificationStatus;
			}

			return null;
		}
	}
}