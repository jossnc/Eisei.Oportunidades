using System.Data;
using Eisei.Oportunidades.Api.Data;
using Eisei.Oportunidades.Api.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Eisei.Oportunidades.Api.Repositories;

public class OportunidadRepository : Repository<Oportunidad>, IOportunidadRepository
{
    public OportunidadRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Oportunidad>> GetByEstadoAsync(string estado, CancellationToken cancellationToken = default)
    {
        return await ListarAsync(estado, cancellationToken);
    }

    public async Task<int> CrearAsync(Oportunidad oportunidad, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(oportunidad);

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.sp_CrearOportunidad", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(new SqlParameter("@Nombre", SqlDbType.NVarChar, 100)
        {
            Value = (object?)oportunidad.Nombre ?? string.Empty
        });
        command.Parameters.Add(new SqlParameter("@Cliente", SqlDbType.NVarChar, 120)
        {
            Value = (object?)oportunidad.Cliente ?? string.Empty
        });
        command.Parameters.Add(new SqlParameter("@MontoEstimado", SqlDbType.Decimal)
        {
            Precision = 18,
            Scale = 2,
            Value = oportunidad.MontoEstimado
        });
        command.Parameters.Add(new SqlParameter("@Estado", SqlDbType.NVarChar, 50)
        {
            Value = (object?)oportunidad.Estado ?? "Nueva"
        });
        command.Parameters.Add(new SqlParameter("@FechaCreacionUtc", SqlDbType.DateTime2)
        {
            Value = oportunidad.FechaCreacionUtc
        });

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public async Task<Oportunidad?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.sp_ObtenerOportunidadPorId", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int)
        {
            Value = id
        });

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.CloseConnection, cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return Map(reader);
    }

    public async Task<IReadOnlyList<Oportunidad>> ListarAsync(string? estado = null, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.sp_ListarOportunidades", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(new SqlParameter("@Estado", SqlDbType.NVarChar, 50)
        {
            Value = string.IsNullOrWhiteSpace(estado) ? DBNull.Value : estado.Trim()
        });

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.CloseConnection, cancellationToken);
        var resultados = new List<Oportunidad>();

        while (await reader.ReadAsync(cancellationToken))
        {
            resultados.Add(Map(reader));
        }

        return resultados;
    }

    public async Task<int> ActualizarEstadoAsync(int id, string estadoAnterior, string nuevoEstado, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.sp_ActualizarEstadoOportunidad", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int) { Value = id });
        command.Parameters.Add(new SqlParameter("@EstadoAnterior", SqlDbType.NVarChar, 50) { Value = estadoAnterior });
        command.Parameters.Add(new SqlParameter("@NuevoEstado", SqlDbType.NVarChar, 50) { Value = nuevoEstado });

        var filasAfectadas = new SqlParameter("@FilasAfectadas", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(filasAfectadas);

        await command.ExecuteNonQueryAsync(cancellationToken);
        return filasAfectadas.Value is DBNull ? 0 : Convert.ToInt32(filasAfectadas.Value);
    }

    private SqlConnection CreateConnection()
    {
        var connectionString = Context.Database.GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("No se encontró la cadena de conexión a SQL Server.");
        }

        return new SqlConnection(connectionString);
    }

    private static Oportunidad Map(SqlDataReader reader)
    {
        return new Oportunidad
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            Nombre = reader.GetString(reader.GetOrdinal("Nombre")),
            Cliente = reader.GetString(reader.GetOrdinal("Cliente")),
            MontoEstimado = reader.GetDecimal(reader.GetOrdinal("MontoEstimado")),
            Estado = reader.GetString(reader.GetOrdinal("Estado")),
            FechaCreacionUtc = reader.GetDateTime(reader.GetOrdinal("FechaCreacionUtc"))
        };
    }
}
