namespace Eisei.Oportunidades.Api.Services;

public interface ITokenService
{
    string GenerateToken(string usuario, string role);
}
