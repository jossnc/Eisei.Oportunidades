IF DB_ID(N'EiseiOportunidadesDb') IS NULL
BEGIN
    CREATE DATABASE EiseiOportunidadesDb;
END;
GO

USE EiseiOportunidadesDb;
GO

IF OBJECT_ID(N'dbo.Oportunidades', N'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.Oportunidades;
END;
GO

CREATE TABLE dbo.Oportunidades (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Nombre NVARCHAR(100) NOT NULL,
    Cliente NVARCHAR(120) NOT NULL,
    MontoEstimado DECIMAL(18,2) NOT NULL,
    Estado NVARCHAR(50) NOT NULL CONSTRAINT DF_Oportunidades_Estado DEFAULT 'Nueva',
    FechaCreacionUtc DATETIME2 NOT NULL CONSTRAINT DF_Oportunidades_FechaCreacionUtc DEFAULT SYSUTCDATETIME()
);
GO

IF OBJECT_ID(N'dbo.sp_CrearOportunidad', N'P') IS NOT NULL
BEGIN
    DROP PROCEDURE dbo.sp_CrearOportunidad;
END;
GO
CREATE PROCEDURE dbo.sp_CrearOportunidad
    @Nombre NVARCHAR(100),
    @Cliente NVARCHAR(120),
    @MontoEstimado DECIMAL(18,2),
    @Estado NVARCHAR(50),
    @FechaCreacionUtc DATETIME2
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Oportunidades (Nombre, Cliente, MontoEstimado, Estado, FechaCreacionUtc)
    VALUES (@Nombre, @Cliente, @MontoEstimado, @Estado, @FechaCreacionUtc);

    SELECT CAST(SCOPE_IDENTITY() AS INT);
END;
GO

IF OBJECT_ID(N'dbo.sp_ObtenerOportunidadPorId', N'P') IS NOT NULL
BEGIN
    DROP PROCEDURE dbo.sp_ObtenerOportunidadPorId;
END;
GO
CREATE PROCEDURE dbo.sp_ObtenerOportunidadPorId
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        Id,
        Nombre,
        Cliente,
        MontoEstimado,
        Estado,
        FechaCreacionUtc
    FROM dbo.Oportunidades
    WHERE Id = @Id;
END;
GO

IF OBJECT_ID(N'dbo.sp_ListarOportunidades', N'P') IS NOT NULL
BEGIN
    DROP PROCEDURE dbo.sp_ListarOportunidades;
END;
GO
CREATE PROCEDURE dbo.sp_ListarOportunidades
    @Estado NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        Id,
        Nombre,
        Cliente,
        MontoEstimado,
        Estado,
        FechaCreacionUtc
    FROM dbo.Oportunidades
    WHERE @Estado IS NULL OR Estado = @Estado
    ORDER BY FechaCreacionUtc DESC;
END;
GO

IF OBJECT_ID(N'dbo.sp_ActualizarEstadoOportunidad', N'P') IS NOT NULL
BEGIN
    DROP PROCEDURE dbo.sp_ActualizarEstadoOportunidad;
END;
GO
CREATE PROCEDURE dbo.sp_ActualizarEstadoOportunidad
    @Id INT,
    @EstadoAnterior NVARCHAR(50),
    @NuevoEstado NVARCHAR(50),
    @FilasAfectadas INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Oportunidades
    SET Estado = @NuevoEstado
    WHERE Id = @Id AND Estado = @EstadoAnterior;

    SET @FilasAfectadas = @@ROWCOUNT;
END;
GO
