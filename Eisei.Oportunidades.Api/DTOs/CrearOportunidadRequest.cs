using System.ComponentModel.DataAnnotations;

namespace Eisei.Oportunidades.Api.DTOs;

public class CrearOportunidadRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [StringLength(120, MinimumLength = 1)]
    public string Cliente { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ErrorMessage = "El monto estimado debe ser mayor que cero.")]
    public decimal MontoEstimado { get; set; }

    public void Normalize()
    {
        Nombre = Nombre?.Trim() ?? string.Empty;
        Cliente = Cliente?.Trim() ?? string.Empty;
    }

    public IEnumerable<ValidationResult> Validate()
    {
        var results = new List<ValidationResult>();

        if (string.IsNullOrWhiteSpace(Nombre) || Nombre.Trim().Length == 0)
        {
            results.Add(new ValidationResult("El nombre es obligatorio.", new[] { nameof(Nombre) }));
        }
        else if (Nombre.Trim().Length > 100)
        {
            results.Add(new ValidationResult("El nombre no puede exceder 100 caracteres.", new[] { nameof(Nombre) }));
        }

        if (string.IsNullOrWhiteSpace(Cliente) || Cliente.Trim().Length == 0)
        {
            results.Add(new ValidationResult("El cliente es obligatorio.", new[] { nameof(Cliente) }));
        }
        else if (Cliente.Trim().Length > 120)
        {
            results.Add(new ValidationResult("El cliente no puede exceder 120 caracteres.", new[] { nameof(Cliente) }));
        }

        if (MontoEstimado <= 0)
        {
            results.Add(new ValidationResult("El monto estimado debe ser mayor que cero.", new[] { nameof(MontoEstimado) }));
        }

        if (MontoEstimado != decimal.Round(MontoEstimado, 2, MidpointRounding.AwayFromZero))
        {
            results.Add(new ValidationResult("El monto estimado admite máximo 2 decimales.", new[] { nameof(MontoEstimado) }));
        }

        var entero = decimal.Truncate(MontoEstimado);
        if (entero.ToString("F0").Length > 16)
        {
            results.Add(new ValidationResult("El monto estimado admite máximo 16 dígitos enteros.", new[] { nameof(MontoEstimado) }));
        }

        return results;
    }
}
