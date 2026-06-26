using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using EGC_Ticketing_System.Validation;

namespace EGC_Ticketing_System.Middleware
{
    public class ValidationExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public ValidationExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (BusinessValidationException ex)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.Response.ContentType = "application/json";

                var response = new
                {
                    success = false,
                    message = "Validation failed.",
                    errors = ex.Errors
                };

                await context.Response.WriteAsJsonAsync(response);
            }
        }
    }
}
