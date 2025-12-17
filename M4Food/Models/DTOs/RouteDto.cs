namespace M4Food.Models.DTOs;

public class RouteDto
{
    public string FromLocation { get; set; } = string.Empty;
    public string ToLocation { get; set; } = string.Empty;
    public string? RouteData { get; set; }
    public double Distance { get; set; }
    public int Duration { get; set; }
}

