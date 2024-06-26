﻿namespace API.Entities.Interfaces;

public interface IUpdateableEntity : IEntity
{
    /// <summary>
    /// Date of the last update to the entity
    /// </summary>
    public DateTime? Updated { get; set; }
}
