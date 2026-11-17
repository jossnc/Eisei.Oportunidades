using Eisei.Oportunidades.Api.DTOs;
using Eisei.Oportunidades.Api.Entities;

namespace Eisei.Oportunidades.Api.Services;

public interface IOportunidadService
{
    Task<OportunidadResponse> CrearAsync(CrearOportunidadRequest request);
    Task<OportunidadResponse?> ObtenerPorIdAsync(int id);
    Task<List<OportunidadResponse>> ListarAsync(string? estado = null);
    Task<OportunidadResponse> ActualizarEstadoAsync(int id, ActualizarEstadoRequest request);
}
