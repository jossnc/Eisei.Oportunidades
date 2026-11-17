# Eisei Oportunidades API

API REST para gestionar oportunidades comerciales con autenticación JWT, autorización por roles y acceso a SQL Server mediante stored procedures.

## Objetivo

Esta solución implementa un backend en ASP.NET Core .NET 8 para registrar, consultar y actualizar oportunidades comerciales siguiendo las reglas de negocio descritas en la prueba técnica.

## Requisitos

- .NET SDK 8.0
- SQL Server 2019+ o SQL Server LocalDB/Express
- PowerShell, bash o terminal con acceso a `dotnet`
- Opcional: SQL Server Management Studio (SSMS) o `sqlcmd`

## Versión de SDK

- .NET SDK: 8.0

## Estructura del repositorio

- `Eisei.Oportunidades.Api/` — proyecto Web API
- `Eisei.Oportunidades.Api.Tests/` — pruebas unitarias y de integración
- `database/init.sql` — creación de la base de datos y de los stored procedures
- `README.md` — documentación de ejecución y uso

## Regla de negocio implementada

Los estados permitidos son:

- `Nueva`
- `EnSeguimiento`
- `Ganada`
- `Perdida`

Transiciones permitidas:

- `Nueva -> EnSeguimiento`
- `EnSeguimiento -> Ganada`
- `EnSeguimiento -> Perdida`

Reglas:

- Toda oportunidad nace en estado `Nueva`.
- Si se intenta repetir el estado actual, la API devuelve la oportunidad sin modificarla.
- Si la transición no está permitida, la API devuelve `409 Conflict`.
- Las operaciones de escritura se hacen de forma atómica en SQL Server y validan el estado persistido antes de aplicar el cambio.

## Base de datos

### 1) Crear la base de datos y los procedimientos almacenados

Ejecuta el script incluido en la raíz del repositorio:

```bash
sqlcmd -S localhost -d master -i database/init.sql
```

O desde SSMS:

```sql
:r database/init.sql
```

El script crea la base de datos `EiseiOportunidadesDb`, la tabla `dbo.Oportunidades` y estos procedimientos:

- `dbo.sp_CrearOportunidad`
- `dbo.sp_ObtenerOportunidadPorId`
- `dbo.sp_ListarOportunidades`
- `dbo.sp_ActualizarEstadoOportunidad`

## Configuración

### appsettings.json

El proyecto usa la configuración de conexión a SQL Server y los valores JWT.

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=EiseiOportunidadesDb;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "JwtSettings": {
    "Secret": "<clave-secreta-muy-larga>",
    "Issuer": "EiseiApi",
    "Audience": "EiseiClients"
  }
}
```

### User Secrets (recomendado para desarrollo local)

```bash
dotnet user-secrets init

dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=EiseiOportunidadesDb;Trusted_Connection=True;TrustServerCertificate=True;"
dotnet user-secrets set "JwtSettings:Secret" "<clave-secreta-muy-larga>"
dotnet user-secrets set "JwtSettings:Issuer" "EiseiApi"
dotnet user-secrets set "JwtSettings:Audience" "EiseiClients"
```

También se pueden usar variables de entorno:

```bash
setx JwtSettings__Secret "<clave-secreta-muy-larga>"
setx JwtSettings__Issuer "EiseiApi"
setx JwtSettings__Audience "EiseiClients"
```

Importante:

- La clave JWT no se debe hardcodear en código.
- En entorno de desarrollo local, se recomienda `dotnet user-secrets`.

## Ejecutar la API

Desde la raíz del repositorio:

```bash
dotnet restore
dotnet build
dotnet run --project Eisei.Oportunidades.Api
```

La API queda levantada en:

- `http://localhost:5000`
- `https://localhost:5001`

Swagger disponible en:

- `https://localhost:5001/swagger`

## Seguridad JWT y roles

La API expone autenticación JWT Bearer y autoriza por roles.

Roles soportados:

- `Consulta` — acceso de lectura
- `Editor` — lectura + alta + cambio de estado

Se valida:

- firma del token
- issuer
- audience
- expiración

### Generar token de prueba local

La API incluye un endpoint de login para generar tokens de prueba:

```http
POST /api/auth/login
Content-Type: application/json
```

Ejemplos:

```bash
curl -X POST "https://localhost:5001/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"usuario":"juan","role":"Consulta"}'
```

```bash
curl -X POST "https://localhost:5001/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"usuario":"ana","role":"Editor"}'
```

Respuesta esperada:

```json
{
  "token": "<jwt>",
  "usuario": "juan",
  "role": "Consulta"
}
```

## Endpoints

### 1) Crear oportunidad

Ruta:

```http
POST /api/oportunidades
```

Permisos:

- `Editor`

Ejemplo:

```bash
curl -X POST "https://localhost:5001/api/oportunidades" \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "nombre": "Proyecto ABC",
    "cliente": "Cliente XYZ",
    "montoEstimado": 1250.50
  }'
```

### 2) Obtener oportunidad por Id

Ruta:

```http
GET /api/oportunidades/{id}
```

Permisos:

- `Consulta`
- `Editor`

Ejemplo:

```bash
curl -X GET "https://localhost:5001/api/oportunidades/1" \
  -H "Authorization: Bearer <token>"
```

### 3) Listar oportunidades con filtro opcional

Ruta:

```http
GET /api/oportunidades?estado=EnSeguimiento
```

Permisos:

- `Consulta`
- `Editor`

Ejemplo:

```bash
curl -X GET "https://localhost:5001/api/oportunidades?estado=Nueva" \
  -H "Authorization: Bearer <token>"
```

### 4) Actualizar estado

Ruta:

```http
PATCH /api/oportunidades/{id}/estado
```

Permisos:

- `Editor`

Ejemplo:

```bash
curl -X PATCH "https://localhost:5001/api/oportunidades/1/estado" \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"estado":"EnSeguimiento"}'
```

## Validaciones y manejo de errores

La API devuelve `ProblemDetails` o `ValidationProblemDetails` con campos identificados para:

- `400 Bad Request`
- `404 Not Found`
- `409 Conflict`
- `500 Internal Server Error`

Validaciones principales:

- `nombre`: obligatorio, máximo 100 caracteres tras limpiar espacios extremos
- `cliente`: obligatorio, máximo 120 caracteres tras limpiar espacios extremos
- `montoEstimado`: decimal > 0, máximo 2 decimales y 16 dígitos enteros
- `estado`: debe ser uno de `Nueva`, `EnSeguimiento`, `Ganada`, `Perdida`
- `id`: debe ser positivo

## Acceso a datos

La capa de acceso a datos se implementa mediante stored procedures y ADO.NET con `SqlCommand`.

Operaciones:

- alta
- consulta por Id
- listado
- cambio de estado

El cambio de estado es atómico y solo se aplica si el estado persistido sigue siendo el mismo que se validó al inicio de la operación. Si ha cambiado por otra operación concurrente, se devuelve `409 Conflict`.

## Pruebas automatizadas

Se incluyen pruebas unitarias para transiciones válidas/rechazadas y pruebas de integración para casos clave.

Ejecutar todas las pruebas:

```bash
dotnet test
```

## Checklist de cumplimiento

Señalamos el estado de cada requisito principal del enunciado.

- Arquitectura Web API en ASP.NET Core: cumplido
- Separación por capas / contratos / servicios / repositorio: cumplido
- Configuración con SQL Server y JWT: cumplido
- OpenAPI/Swagger: cumplido
- Script SQL con tabla y stored procedures: cumplido
- ADO.NET con procedimientos almacenados tipados: cumplido
- Actualización atómica con validación de estado previo: cumplido
- Endpoints REST para crear, consultar, listar y actualizar: cumplido
- 400/404/409/500 con `ProblemDetails`: cumplido
- JWT Bearer y roles `Consulta`/`Editor`: cumplido
- 401 y 403: cumplido
- README con configuración y ejemplos: cumplido
- pruebas unitarias: cumplido
- pruebas de integración: incluidas en el proyecto; su ejecución final depende del entorno SQL dispuesto para la ejecución local real

## Nota final de entrega

La solución queda preparada para ser ejecutada localmente desde un repositorio vacío siguiendo el README, el script SQL y la configuración de `User Secrets`/variables de entorno. La parte operativa está documentada y preparada para uso local y validación funcional.
