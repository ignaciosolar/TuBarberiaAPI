using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TuBarberiaAPI.Data;
using TuBarberiaAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// 🟢 Conexión a SQL Server con resiliencia y timeout
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql =>
        {
            sql.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null
            );
            sql.CommandTimeout(120); // segundos
        }
    )
);

// Servicios
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<EmailService>();

// 🟢 CORS: orígenes fijos + dominios dinámicos (ej. ngrok)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins(
                "http://localhost:4200",
                "https://brilliant-travesseiro-dddd27.netlify.app",
                "https://calm-coast-04658b71e.1.azurestaticapps.net"
            )
            .SetIsOriginAllowed(origin =>
            {
                // Permite *.ngrok-free.app y *.ngrok.io si los usas
                try
                {
                    var host = new Uri(origin).Host;
                    return host.EndsWith(".ngrok-free.app") || host.EndsWith(".ngrok.io");
                }
                catch { return false; }
            })
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // 🟢 Añadido para permitir cookies y headers de autorización
    });
});

// Auth JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!);
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

// MVC + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 🟢 IMPORTANTE: Orden correcto del middleware - CORS debe ir PRIMERO
app.UseCors("AllowAngularApp");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
