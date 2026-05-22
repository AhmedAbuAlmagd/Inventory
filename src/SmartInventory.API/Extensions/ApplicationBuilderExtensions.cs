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
                c.DocumentTitle = "Smart Inventory";
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Smart Inventory API v1");
                c.RoutePrefix = string.Empty;
                c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
                c.DefaultModelsExpandDepth(1);
                c.DisplayRequestDuration();
                c.EnableDeepLinking();
                c.InjectJavascript("/swagger-ui/auth-refresh.js");
                c.InjectJavascript("/swagger-ui/custom-ui.js");
                c.HeadContent = """
                    <style>
                        .swagger-ui .topbar { background-color: #1a1a2e; }
                        .swagger-ui .topbar .download-url-wrapper .select-label select { border-color: #e94560; }
                        .swagger-ui .info .title { color: #1a1a2e; font-size: 2.2em; }
                        .swagger-ui .info .description p { font-size: 1.05em; line-height: 1.6; }
                        .swagger-ui .opblock.opblock-get .opblock-summary-method { background: #0f3460; }
                        .swagger-ui .opblock.opblock-post .opblock-summary-method { background: #16813d; }
                        .swagger-ui .opblock.opblock-put .opblock-summary-method { background: #e67e22; }
                        .swagger-ui .opblock.opblock-patch .opblock-summary-method { background: #e94560; }
                        .swagger-ui .opblock.opblock-delete .opblock-summary-method { background: #c0392b; }
                        .swagger-ui .opblock .opblock-summary-method { border-radius: 6px; font-weight: 700; }
                        .swagger-ui .btn.execute { background-color: #0f3460; border-color: #0f3460; }
                        .swagger-ui .btn.execute:hover { background-color: #e94560; border-color: #e94560; }
                    </style>
                    """;
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
