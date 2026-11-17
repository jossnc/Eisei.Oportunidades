namespace Eisei.Oportunidades.Api.Entities;

public class Oportunidad
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Cliente { get; set; } = string.Empty;
    public decimal MontoEstimado { get; set; }
    public string Estado { get; set; } = "Nueva";
    public DateTime FechaCreacionUtc { get; set; }
}
