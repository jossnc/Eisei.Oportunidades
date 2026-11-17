namespace Eisei.Oportunidades.Api.Repositories;

public interface IUnitOfWork
{
    IOportunidadRepository Oportunidades { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
