﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using API.Osu.Enums;

namespace API.Entities;

/// <summary>
/// Represents a collection of rating stats for a given player and ruleset, generated by the processor
/// </summary>
[Table("base_stats")]
[SuppressMessage("ReSharper", "ClassWithVirtualMembersNeverInherited.Global")]
[SuppressMessage("ReSharper", "EntityFramework.ModelValidation.CircularDependency")]
[SuppressMessage("ReSharper", "PropertyCanBeMadeInitOnly.Global")]
public class BaseStats
{
    /// <summary>
    /// Primary key
    /// </summary>
    [Key]
    [Column("id")]
    public int Id { get; set; }

    /// <summary>
    /// The id of the player described by the base stat
    /// </summary>
    [Column("player_id")]
    public int PlayerId { get; set; }

    /// <summary>
    /// The ruleset the base stat was generated for
    /// </summary>
    [Column("ruleset")]
    public Ruleset Ruleset { get; set; }

    /// <summary>
    /// The rating of the player for the given ruleset
    /// </summary>
    [Column("rating")]
    public double Rating { get; set; }

    /// <summary>
    /// The rating volatility of the player for the given ruleset
    /// </summary>
    [Column("volatility")]
    public double Volatility { get; set; }

    /// <summary>
    /// The rating percentile of the player for the given ruleset
    /// </summary>
    [Column("percentile")]
    public double Percentile { get; set; }

    /// <summary>
    /// The global rank of the player for the given ruleset
    /// </summary>
    [Column("global_rank")]
    public int GlobalRank { get; set; }

    /// <summary>
    /// The country rank of the player for the given ruleset
    /// </summary>
    [Column("country_rank")]
    public int CountryRank { get; set; }

    /// <summary>
    /// The average match cost of the player for the given ruleset
    /// </summary>
    [Column("match_cost_average")]
    public double MatchCostAverage { get; set; }

    /// <summary>
    /// Date the entity was created
    /// </summary>
    [Column("created", TypeName = "timestamp with time zone")]
    public DateTime Created { get; set; }

    /// <summary>
    /// Date of the last update to the entity
    /// </summary>
    [Column("updated", TypeName = "timestamp with time zone")]
    public DateTime? Updated { get; set; }

    /// <summary>
    /// The player described by the base stat
    /// </summary>
    [ForeignKey("PlayerId")]
    [InverseProperty("Ratings")]
    public virtual Player Player { get; set; } = null!;
}
