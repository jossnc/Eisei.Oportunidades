using System.ComponentModel.DataAnnotations;
using Eisei.Oportunidades.Api.DTOs;
using Eisei.Oportunidades.Api.Entities;
using Eisei.Oportunidades.Api.Exceptions;
using Eisei.Oportunidades.Api.Repositories;

namespace Eisei.Oportunidades.Api.Services;

public class OportunidadService : IOportunidadService
{
    private static readonly HashSet<string> EstadosValidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "Nueva",
        "EnSeguimiento",
        "Ganada",
        "Perdida"
    };

    private static readonly Dictionary<string, HashSet<string>> TransicionesValidas = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Nueva"] = new(StringComparer.OrdinalIgnoreCase) { "EnSeguimiento" },
        ["EnSeguimiento"] = new(StringComparer.OrdinalIgnoreCase) { "Ganada", "Perdida" },
        ["Ganada"] = new(StringComparer.OrdinalIgnoreCase),
        ["Perdida"] = new(StringComparer.OrdinalIgnoreCase)
    };

    private readonly IOportunidadRepository _repository;

    public OportunidadService(IOportunidadRepository repository)
    {
        _repository = repository;
    }

    public async Task<OportunidadResponse> CrearAsync(CrearOportunidadRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        request.Normalize();

        var validationResults = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), validationResults, validateAllProperties: true);
        validationResults.AddRange(request.Validate());

        if (validationResults.Count > 0)
        {
            throw CreateRequestValidationException(validationResults);
        }

        var oportunidad = new Oportunidad
        {
            Nombre = request.Nombre.Trim(),
            Cliente = request.Cliente.Trim(),
            MontoEstimado = request.MontoEstimado,
            Estado = "Nueva",
            FechaCreacionUtc = DateTime.UtcNow
        };

        var id = await _repository.CrearAsync(oportunidad);
        var creada = await _repository.ObtenerPorIdAsync(id);

        return MapToResponse(creada ?? oportunidad);
    }

    public async Task<OportunidadResponse?> ObtenerPorIdAsync(int id)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "El identificador debe ser mayor que cero.");
        }

        var oportunidad = await _repository.ObtenerPorIdAsync(id);
        if (oportunidad is null)
        {
            throw new KeyNotFoundException($"No se encontró la oportunidad con Id {id}.");
        }

        return MapToResponse(oportunidad);
    }

    public async Task<List<OportunidadResponse>> ListarAsync(string? estado = null)
    {
        if (!string.IsNullOrWhiteSpace(estado))
        {
            var normalized = estado.Trim();
            if (!EstadosValidos.Contains(normalized))
            {
                throw CreateRequestValidationException(
                    new[] { new ValidationResult($"El estado '{normalized}' no es válido.", new[] { "estado" }) });
            }
        }

        var resultados = await _repository.ListarAsync(estado);
        return resultados.Select(MapToResponse).ToList();
    }

    public async Task<OportunidadResponse> ActualizarEstadoAsync(int id, ActualizarEstadoRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        request.Normalize();

        var validationResults = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), validationResults, validateAllProperties: true);
        if (validationResults.Count > 0)
        {
            throw CreateRequestValidationException(validationResults);
        }

        var nuevoEstado = request.Estado.Trim();
        var oportunidadActual = await _repository.ObtenerPorIdAsync(id)
            ?? throw new KeyNotFoundException($"No se encontró la oportunidad con Id {id}.");

        var estadoActual = oportunidadActual.Estado;

        if (string.Equals(nuevoEstado, estadoActual, StringComparison.OrdinalIgnoreCase))
        {
            return MapToResponse(oportunidadActual);
        }

        var transiciones = TransicionesValidas.TryGetValue(estadoActual, out var estadosPermitidos)
            ? estadosPermitidos
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!transiciones.Contains(nuevoEstado))
        {
            throw new ConflictException($"La transición de '{estadoActual}' a '{nuevoEstado}' no es válida.");
        }

        var filasAfectadas = await _repository.ActualizarEstadoAsync(id, estadoActual, nuevoEstado);
        if (filasAfectadas == 0)
        {
            throw new ConflictException($"La oportunidad con Id {id} fue modificada por otra operación.");
        }

        var oportunidadActualizada = await _repository.ObtenerPorIdAsync(id)
            ?? throw new KeyNotFoundException($"No se encontró la oportunidad con Id {id}.");

        return MapToResponse(oportunidadActualizada);
    }

    private static RequestValidationException CreateRequestValidationException(IEnumerable<ValidationResult> validationResults)
    {
        var errors = validationResults
            .SelectMany(x => (x.MemberNames.Any() ? x.MemberNames : new[] { "error" })
                .Select(member => new { Member = member, Message = x.ErrorMessage ?? "Error de validación." }))
            .GroupBy(x => x.Member)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Message).Distinct().ToArray());

        return new RequestValidationException(errors);
    }

    private static OportunidadResponse MapToResponse(Oportunidad oportunidad)
    {
        return new OportunidadResponse
        {
            Id = oportunidad.Id,
            Nombre = oportunidad.Nombre,
            Cliente = oportunidad.Cliente,
            MontoEstimado = oportunidad.MontoEstimado,
            Estado = oportunidad.Estado,
            FechaCreacionUtc = oportunidad.FechaCreacionUtc
        };
    }
}
