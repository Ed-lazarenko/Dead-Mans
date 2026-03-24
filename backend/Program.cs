using backend.Api.Auth;
using backend.Api.Contracts;
using backend.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var isDevelopment = builder.Environment.IsDevelopment();
builder.Configuration
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// Add services to the container.
builder.Services.AddControllers();
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
builder.Services.AddScoped<IUserRoleService, UserRoleService>();
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
builder.Services.AddHttpClient<ITwitchAuthService, TwitchAuthService>();

// OpenAPI / Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Dead-Mans API",
        Version = "v1",
        Description = "Backend для панели Dead-Mans (лоадауты, лидеры, модификаторы и т.д.)"
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Dead-Mans API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();

app.UseCors("Default");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static Task WriteErrorResponseAsync(HttpResponse response, int statusCode, string message)
{
    response.StatusCode = statusCode;
    return response.WriteAsJsonAsync(new ErrorResponse(message));
}

public partial class Program;
