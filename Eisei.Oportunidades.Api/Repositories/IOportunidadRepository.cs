using Eisei.Oportunidades.Api.Entities;

namespace Eisei.Oportunidades.Api.Repositories;

public interface IOportunidadRepository : IRepository<Oportunidad>
{
    Task<IReadOnlyList<Oportunidad>> GetByEstadoAsync(string estado, CancellationToken cancellationToken = default);
    Task<int> CrearAsync(Oportunidad oportunidad, CancellationToken cancellationToken = default);
    Task<Oportunidad?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Oportunidad>> ListarAsync(string? estado = null, CancellationToken cancellationToken = default);
    Task<int> ActualizarEstadoAsync(int id, string estadoAnterior, string nuevoEstado, CancellationToken cancellationToken = default);
}
