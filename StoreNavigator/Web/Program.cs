using Web.Services;

namespace Web;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        // Register CORS policy
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowViteDevClient", policy =>
            {
                policy.WithOrigins("http://localhost:5174")
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        // Load .env file (optional: specify path if not root)
        DotNetEnv.Env.Load();

        // Add configuration from environment to IConfiguration
        builder.Configuration.AddEnvironmentVariables();
        // Bind the OpenAI section to a strongly typed class
        builder.Services.Configure<OpenAiOptions>(builder.Configuration.GetSection("OpenAi"));
        
        var configuration = builder.Configuration;

        // Add services to the container.
        builder.Services.AddScoped<QuestionService>();
        builder.Services.AddHttpClient<QuestionService>();
        
        builder.Services.AddScoped<AudioService>();
        builder.Services.AddHttpClient<AudioService>();
        
        builder.Services.AddControllers();
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        var app = builder.Build();
        
        // Use CORS middleware
        app.UseCors("AllowViteDevClient");
        
        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();

            Console.WriteLine(configuration.GetSection("OpenAI").Value);
            app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "NavigatorProject"));
        }

        app.MapControllers();

        app.Run();
    }
}