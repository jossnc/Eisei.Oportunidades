using System.ComponentModel.DataAnnotations;

namespace Eisei.Oportunidades.Api.DTOs;

public class ActualizarEstadoRequest
{
    [Required]
    [RegularExpression("^(Nueva|EnSeguimiento|Ganada|Perdida)$", ErrorMessage = "El estado no es válido.")]
    public string Estado { get; set; } = string.Empty;

    public void Normalize()
    {
        Estado = Estado?.Trim() ?? string.Empty;
    }
}
