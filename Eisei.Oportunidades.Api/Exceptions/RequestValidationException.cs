using System.Collections.Generic;

namespace Eisei.Oportunidades.Api.Exceptions;

public class RequestValidationException : Exception
{
    public RequestValidationException(IDictionary<string, string[]> errors)
        : base("Se han encontrado errores de validación.")
    {
        Errors = errors ?? throw new ArgumentNullException(nameof(errors));
    }

    public IDictionary<string, string[]> Errors { get; }
}
