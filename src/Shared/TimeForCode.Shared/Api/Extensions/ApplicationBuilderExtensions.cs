using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;

namespace TimeForCode.Shared.Api.Extensions
{
    /// <summary>
    /// Extension methods for configuring the default API HTTP request pipeline.
    /// </summary>
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// Configures the standard API pipeline: exception page or HSTS, security headers,
        /// rate limiting, routing, authentication, authorisation, and mapped endpoints.
        /// </summary>
        public static IApplicationBuilder UseDefaultApiPipeline(this IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.Use(async (context, next) =>
            {
                try
                {
                    await next(context);
                }
                catch (ValidationException ex)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    context.Response.ContentType = "application/problem+json";
                    await context.Response.WriteAsJsonAsync(new ProblemDetails
                    {
                        Title = "Validation failed",
                        Detail = string.Join("; ", ex.Errors.Select(e => e.ErrorMessage)),
                        Status = StatusCodes.Status400BadRequest
                    });
                }
            });

            app.UseSecurityHeaders();

            app.UseRouting();

            app.UseRateLimiter();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapOpenApi();
                endpoints.MapScalarApiReference();
            });

            return app;
        }
    }
}