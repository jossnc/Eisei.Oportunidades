using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Eisei.Oportunidades.Api.DTOs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Eisei.Oportunidades.Api.Tests;

public class ApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ApiIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task PostYGet_ConTokenEditorYConsulta_Retorna200Y201()
    {
        var editorToken = await LoginAsync("Editor");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", editorToken);

        var createRequest = new CrearOportunidadRequest
        {
            Nombre = "Proyecto ABC",
            Cliente = "Cliente XYZ",
            MontoEstimado = 1250.50m
        };

        var createResponse = await _client.PostAsJsonAsync("/api/oportunidades", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<OportunidadResponse>();
        Assert.NotNull(created);
        Assert.NotEqual(0, created!.Id);

        var consultaToken = await LoginAsync("Consulta");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", consultaToken);

        var getResponse = await _client.GetAsync($"/api/oportunidades/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var item = await getResponse.Content.ReadFromJsonAsync<OportunidadResponse>();
        Assert.NotNull(item);
        Assert.Equal(created.Id, item!.Id);
    }

    [Fact]
    public async Task Post_ConDatosInvalidos_Retorna400ValidationProblemDetails()
    {
        var token = await LoginAsync("Editor");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CrearOportunidadRequest
        {
            Nombre = "",
            Cliente = "",
            MontoEstimado = 0m
        };

        var response = await _client.PostAsJsonAsync("/api/oportunidades", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsDto>();
        Assert.NotNull(problem);
        Assert.True(problem!.Errors.ContainsKey("nombre") || problem.Errors.ContainsKey("cliente") || problem.Errors.ContainsKey("montoEstimado"));
    }

    [Fact]
    public async Task Post_SinToken_Retorna401Unauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var request = new CrearOportunidadRequest
        {
            Nombre = "Proyecto",
            Cliente = "Cliente",
            MontoEstimado = 100m
        };

        var response = await _client.PostAsJsonAsync("/api/oportunidades", request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_ConRolConsulta_Retorna403Forbidden()
    {
        var token = await LoginAsync("Consulta");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CrearOportunidadRequest
        {
            Nombre = "Proyecto",
            Cliente = "Cliente",
            MontoEstimado = 100m
        };

        var response = await _client.PostAsJsonAsync("/api/oportunidades", request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<string> LoginAsync(string role)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Usuario = "test-user",
            Role = role
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(payload);
        return payload!.Token;
    }

    private sealed class ValidationProblemDetailsDto
    {
        public Dictionary<string, string[]> Errors { get; set; } = new();
    }

    private sealed class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
    }
}

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((context, configBuilder) =>
        {
            var inMemory = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=EiseiOportunidadesDb;Trusted_Connection=True;TrustServerCertificate=True;",
                    ["JwtSettings:Secret"] = "IntegrationTestSecretKey1234567890!",
                    ["JwtSettings:Issuer"] = "EiseiApi",
                    ["JwtSettings:Audience"] = "EiseiClients"
                })
                .Build();

            configBuilder.AddConfiguration(inMemory);
        });
    }
}
