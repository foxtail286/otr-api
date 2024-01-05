namespace API.DTOs;

public class MatchDTO
{
	public int Id { get; set; }
	public long MatchId { get; set; }
	public string? Name { get; set; }
	public DateTime? StartTime { get; set; }
	public DateTime? EndTime { get; set; }
	public ICollection<GameDTO> Games { get; set; } = new List<GameDTO>();
}