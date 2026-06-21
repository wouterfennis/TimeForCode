using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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

            app.UseSecurityHeaders();

            app.UseRateLimiter();

            app.UseRouting();

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