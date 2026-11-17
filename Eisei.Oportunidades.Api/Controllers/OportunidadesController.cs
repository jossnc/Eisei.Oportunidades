using Eisei.Oportunidades.Api.DTOs;
using Eisei.Oportunidades.Api.Exceptions;
using Eisei.Oportunidades.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Eisei.Oportunidades.Api.Controllers;

[ApiController]
[Route("api/oportunidades")]
public class OportunidadesController : ControllerBase
{
    private static readonly HashSet<string> EstadosValidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "Nueva",
        "EnSeguimiento",
        "Ganada",
        "Perdida"
    };

    private readonly IOportunidadService _service;

    public OportunidadesController(IOportunidadService service)
    {
        _service = service;
    }

    [HttpPost]
    [Authorize(Policy = "Editor")]
    public async Task<IActionResult> Crear([FromBody] CrearOportunidadRequest request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var result = await _service.CrearAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "ConsultaEditor")]
    public async Task<IActionResult> GetById(int id)
    {
        if (id <= 0)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Bad request",
                Status = StatusCodes.Status400BadRequest,
                Detail = "El identificador debe ser mayor que cero."
            });
        }

        var result = await _service.ObtenerPorIdAsync(id);
        return result is null ? throw new KeyNotFoundException($"No se encontró la oportunidad con Id {id}.") : Ok(result);
    }

    [HttpGet]
    [Authorize(Policy = "ConsultaEditor")]
    public async Task<IActionResult> GetAll([FromQuery] string? estado)
    {
        if (!string.IsNullOrWhiteSpace(estado) && !EstadosValidos.Contains(estado.Trim()))
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["estado"] = [$"El estado '{estado.Trim()}' no es válido."]
            })
            {
                Title = "One or more validation errors occurred.",
                Status = StatusCodes.Status400BadRequest,
                Detail = $"El estado '{estado.Trim()}' no es válido.",
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
            });
        }

        var result = await _service.ListarAsync(estado);
        return Ok(result);
    }

    [HttpPatch("{id:int}/estado")]
    [Authorize(Policy = "Editor")]
    public async Task<IActionResult> ActualizarEstado(int id, [FromBody] ActualizarEstadoRequest request)
    {
        if (id <= 0)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Bad request",
                Status = StatusCodes.Status400BadRequest,
                Detail = "El identificador debe ser mayor que cero."
            });
        }

        if (request is null)
        {
            return BadRequest();
        }

        var result = await _service.ActualizarEstadoAsync(id, request);
        return Ok(result);
    }
}
