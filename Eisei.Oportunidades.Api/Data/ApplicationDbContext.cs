using Eisei.Oportunidades.Api.Data.Configurations;
using Eisei.Oportunidades.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Eisei.Oportunidades.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Oportunidad> Oportunidades => Set<Oportunidad>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OportunidadConfiguration).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
