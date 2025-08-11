using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TuBarberiaAPI.Data;
using TuBarberiaAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// ▶ DB sencilla (sin wake-up/retentativas/timeout extra)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// Servicios
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<EmailService>();

// ▶ CORS: orígenes permitidos (sin credenciales)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
        policy
            .WithOrigins(
                "http://localhost:4200",
                "https://brilliant-travesseiro-dddd27.netlify.app",
                "https://calm-coast-04658b71e.1.azurestaticapps.net"
            )
            // Permite túneles de dev si los usas (ngrok)
            .SetIsOriginAllowed(origin =>
            {
                try
                {
                    var host = new Uri(origin).Host;
                    return host.EndsWith(".ngrok-free.app") || host.EndsWith(".ngrok.io");
                }
                catch { return false; }
            })
            .AllowAnyHeader()
            .AllowAnyMethod()
    );
});

// ▶ Auth JWT
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

// ▶ Pipeline (orden importa)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAngularApp");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
