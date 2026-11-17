using Eisei.Oportunidades.Api.DTOs;
using Eisei.Oportunidades.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Eisei.Oportunidades.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ITokenService _tokenService;

    public AuthController(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Usuario) || string.IsNullOrWhiteSpace(request.Role))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Bad request",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Usuario y role son obligatorios."
            });
        }

        var role = request.Role.Trim();
        if (role != "Consulta" && role != "Editor")
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Bad request",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Role inválido. Usa Consulta o Editor."
            });
        }

        var token = _tokenService.GenerateToken(request.Usuario.Trim(), role);

        return Ok(new
        {
            token,
            usuario = request.Usuario.Trim(),
            role
        });
    }
}
