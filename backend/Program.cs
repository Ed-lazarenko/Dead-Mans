using backend.Api.Contracts;
using backend.Infrastructure.Auth;
using backend.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
var isDevelopment = builder.Environment.IsDevelopment();
builder.Configuration
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// Add services to the container.
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        );
    });
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "dm_auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = isDevelopment
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.SlidingExpiration = true;
        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = async context =>
            {
                if (context.Request.Path.StartsWithSegments("/api") || context.Request.Path.StartsWithSegments("/auth"))
                {
                    await WriteErrorResponseAsync(
                        context.Response,
                        StatusCodes.Status401Unauthorized,
                        "Authentication is required."
                    );
                    return;
                }

                context.Response.Redirect(context.RedirectUri);
            },
            OnRedirectToAccessDenied = async context =>
            {
                if (context.Request.Path.StartsWithSegments("/api") || context.Request.Path.StartsWithSegments("/auth"))
                {
                    await WriteErrorResponseAsync(
                        context.Response,
                        StatusCodes.Status403Forbidden,
                        "You do not have access to this resource."
                    );
                    return;
                }

                context.Response.Redirect(context.RedirectUri);
            }
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddDeadMansInfrastructure(builder.Configuration);
builder.Services.AddDeadMansCors(builder.Configuration);
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});
builder.Services
    .AddOptions<TwitchAuthOptions>()
    .Bind(builder.Configuration.GetSection(TwitchAuthOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(
        options => options.Scopes.Length > 0,
        "TwitchAuth:Scopes must contain at least one scope."
    )
    .ValidateOnStart();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/openapi/deadmans.v1.yaml", "Dead-Mans API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();

app.UseCors("Default");

app.UseAuthentication();
app.UseAuthorization();

app.MapGet(
    "/openapi/deadmans.v1.yaml",
    () => Results.File(
        Path.Combine(app.Environment.ContentRootPath, "openapi", "deadmans.v1.yaml"),
        "application/yaml"
    )
);
app.MapControllers();

app.Run();

static Task WriteErrorResponseAsync(HttpResponse response, int statusCode, string message)
{
    response.StatusCode = statusCode;
    return response.WriteAsJsonAsync(new ErrorResponse(message));
}

public partial class Program;
