using Eisei.Oportunidades.Api.Data;

namespace Eisei.Oportunidades.Api.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public UnitOfWork(ApplicationDbContext context, IOportunidadRepository oportunidades)
    {
        _context = context;
        Oportunidades = oportunidades;
    }

    public IOportunidadRepository Oportunidades { get; }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
