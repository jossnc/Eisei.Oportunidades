namespace Eisei.Oportunidades.Api.DTOs;

public class OportunidadResponse
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Cliente { get; set; } = string.Empty;
    public decimal MontoEstimado { get; set; }
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaCreacionUtc { get; set; }
}
