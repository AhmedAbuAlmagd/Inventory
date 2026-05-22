using SmartInventory.API.Middleware;

namespace SmartInventory.API.Extensions;

public static class ApplicationBuilderExtensions
{
    public static WebApplication UseSmartInventoryPipeline(this WebApplication app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseMiddleware<ExceptionMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseStaticFiles();
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Smart Inventory API v1");
                c.DocumentTitle = "Smart Inventory API — Swagger";
                c.InjectJavascript("/swagger-ui/auth-refresh.js");
                c.InjectJavascript("/swagger-ui/custom-ui.js");
                c.ConfigObject.PersistAuthorization = true;
            });
        }
        else
        {
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseCors("AngularApp");

        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();

        app.MapControllers();

        return app;
    }
}
