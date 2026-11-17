using System.Linq.Expressions;
using Eisei.Oportunidades.Api.DTOs;
using Eisei.Oportunidades.Api.Entities;
using Eisei.Oportunidades.Api.Exceptions;
using Eisei.Oportunidades.Api.Repositories;
using Eisei.Oportunidades.Api.Services;
using Xunit;

namespace Eisei.Oportunidades.Api.Tests;

public class OportunidadServiceTests
{
    [Fact]
    public async Task CrearAsync_ConDatosValidos_DevuelveOportunidadConEstadoNueva()
    {
        var repository = new FakeOportunidadRepository();
        var service = new OportunidadService(repository);

        var request = new CrearOportunidadRequest
        {
            Nombre = "  Proyecto ABC  ",
            Cliente = "  Cliente XYZ  ",
            MontoEstimado = 1250.50m
        };

        var result = await service.CrearAsync(request);

        Assert.Equal("Proyecto ABC", result.Nombre);
        Assert.Equal("Cliente XYZ", result.Cliente);
        Assert.Equal(1250.50m, result.MontoEstimado);
        Assert.Equal("Nueva", result.Estado);
        Assert.True(result.Id > 0);
    }

    [Fact]
    public async Task ActualizarEstadoAsync_CuandoTransicionEsValida_ActualizaEstado()
    {
        var repository = new FakeOportunidadRepository(new Oportunidad
        {
            Id = 1,
            Nombre = "Proyecto",
            Cliente = "Cliente",
            MontoEstimado = 1000m,
            Estado = "Nueva",
            FechaCreacionUtc = DateTime.UtcNow
        });
        var service = new OportunidadService(repository);

        var request = new ActualizarEstadoRequest { Estado = "EnSeguimiento" };

        var result = await service.ActualizarEstadoAsync(1, request);

        Assert.Equal("EnSeguimiento", result.Estado);
    }

    [Fact]
    public async Task ActualizarEstadoAsync_CuandoEstadoEsIgual_DevuelveLaOportunidadSinModificarla()
    {
        var oportunidad = new Oportunidad
        {
            Id = 8,
            Nombre = "Proyecto",
            Cliente = "Cliente",
            MontoEstimado = 500m,
            Estado = "Ganada",
            FechaCreacionUtc = DateTime.UtcNow
        };
        var repository = new FakeOportunidadRepository(oportunidad);
        var service = new OportunidadService(repository);

        var result = await service.ActualizarEstadoAsync(8, new ActualizarEstadoRequest { Estado = "Ganada" });

        Assert.Equal("Ganada", result.Estado);
        Assert.Equal(500m, result.MontoEstimado);
    }

    [Fact]
    public async Task ActualizarEstadoAsync_CuandoTransicionNoPermitida_LanzaConflictException()
    {
        var repository = new FakeOportunidadRepository(new Oportunidad
        {
            Id = 2,
            Nombre = "Proyecto",
            Cliente = "Cliente",
            MontoEstimado = 300m,
            Estado = "Ganada",
            FechaCreacionUtc = DateTime.UtcNow
        });
        var service = new OportunidadService(repository);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            service.ActualizarEstadoAsync(2, new ActualizarEstadoRequest { Estado = "Nueva" }));

        Assert.Contains("transición", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ActualizarEstadoAsync_CuandoFilasAfectadasEsCero_LanzaConflictException()
    {
        var repository = new FakeOportunidadRepository(new Oportunidad
        {
            Id = 3,
            Nombre = "Proyecto",
            Cliente = "Cliente",
            MontoEstimado = 777m,
            Estado = "Nueva",
            FechaCreacionUtc = DateTime.UtcNow
        })
        {
            RowsAffectedOnUpdate = 0
        };
        var service = new OportunidadService(repository);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.ActualizarEstadoAsync(3, new ActualizarEstadoRequest { Estado = "EnSeguimiento" }));
    }

    [Fact]
    public async Task ObtenerPorIdAsync_ConIdInvalido_LanzaArgumentOutOfRangeException()
    {
        var repository = new FakeOportunidadRepository();
        var service = new OportunidadService(repository);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.ObtenerPorIdAsync(0));
    }

    private sealed class FakeOportunidadRepository : IOportunidadRepository
    {
        private readonly Dictionary<int, Oportunidad> _store;
        private int _nextId;

        public FakeOportunidadRepository(params Oportunidad[] oportunidades)
        {
            _store = new Dictionary<int, Oportunidad>();
            _nextId = 1;

            foreach (var oportunidad in oportunidades)
            {
                _store[oportunidad.Id] = oportunidad;
                _nextId = Math.Max(_nextId, oportunidad.Id + 1);
            }
        }

        public int RowsAffectedOnUpdate { get; set; } = 1;

        public Task<IReadOnlyList<Oportunidad>> GetByEstadoAsync(string estado, CancellationToken cancellationToken = default)
        {
            var result = _store.Values
                .Where(x => x.Estado.Equals(estado, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.FechaCreacionUtc)
                .ToList();

            return Task.FromResult<IReadOnlyList<Oportunidad>>(result);
        }

        public Task<int> CrearAsync(Oportunidad oportunidad, CancellationToken cancellationToken = default)
        {
            oportunidad.Id = _nextId++;
            _store[oportunidad.Id] = oportunidad;
            return Task.FromResult(oportunidad.Id);
        }

        public Task<Oportunidad?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default)
        {
            _store.TryGetValue(id, out var oportunidad);
            return Task.FromResult(oportunidad);
        }

        public Task<IReadOnlyList<Oportunidad>> ListarAsync(string? estado = null, CancellationToken cancellationToken = default)
        {
            var result = _store.Values
                .Where(x => string.IsNullOrWhiteSpace(estado) || x.Estado.Equals(estado, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.FechaCreacionUtc)
                .ToList();

            return Task.FromResult<IReadOnlyList<Oportunidad>>(result);
        }

        public Task<int> ActualizarEstadoAsync(int id, string estadoAnterior, string nuevoEstado, CancellationToken cancellationToken = default)
        {
            if (!_store.TryGetValue(id, out var oportunidad))
            {
                return Task.FromResult(0);
            }

            if (!string.Equals(oportunidad.Estado, estadoAnterior, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(0);
            }

            oportunidad.Estado = nuevoEstado;
            return Task.FromResult(RowsAffectedOnUpdate);
        }

        public Task<Oportunidad?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => ObtenerPorIdAsync(id, cancellationToken);

        public Task<IReadOnlyList<Oportunidad>> GetAllAsync(
            Expression<Func<Oportunidad, bool>>? predicate = null,
            Func<IQueryable<Oportunidad>, IOrderedQueryable<Oportunidad>>? orderBy = null,
            bool asNoTracking = true,
            CancellationToken cancellationToken = default)
        {
            IQueryable<Oportunidad> query = _store.Values.AsQueryable();

            if (predicate is not null)
            {
                query = query.Where(predicate);
            }

            if (orderBy is not null)
            {
                query = orderBy(query);
            }

            return Task.FromResult<IReadOnlyList<Oportunidad>>(query.ToList());
        }

        public Task AddAsync(Oportunidad entity, CancellationToken cancellationToken = default)
        {
            entity.Id = _nextId++;
            _store[entity.Id] = entity;
            return Task.CompletedTask;
        }

        public void Update(Oportunidad entity)
        {
            if (_store.ContainsKey(entity.Id))
            {
                _store[entity.Id] = entity;
            }
        }

        public void Delete(Oportunidad entity)
        {
            _store.Remove(entity.Id);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(1);
        }
    }
}
