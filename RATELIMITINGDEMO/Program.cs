using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi;

namespace RATELIMITINGDEMO;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddScoped<RateLimitingMiddleware>();
        builder.Services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter(
                "FixedPolicy",
                config =>
                {
                    config.Window = TimeSpan.FromSeconds(50);
                    config.PermitLimit = 5;
                    config.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    config.QueueLimit = 0;
                }
            );
        });
        builder.Services.AddMemoryCache();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(
                "v1",
                new OpenApiInfo
                {
                    Version = "v1",
                    Title = "My API",
                    Description = "ASP.NET Core 10 Web API",
                    TermsOfService = new Uri("https://example.com/terms"),
                    Contact = new OpenApiContact
                    {
                        Name = "Example Contact",
                        Url = new Uri("https://example.com/contact"),
                    },
                    License = new OpenApiLicense
                    {
                        Name = "Example License",
                        Url = new Uri("https://example.com/license"),
                    },
                }
            );

            var xmlFilename =
                $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
        });

        builder.Services.AddControllers();
        builder.Services.AddOpenApi();

        var app = builder.Build();
        app.UseRateLimiter();
        //app.UseMiddleware<RateLimitingMiddleware>();
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
            options.RoutePrefix = "swagger";
        });
        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}
