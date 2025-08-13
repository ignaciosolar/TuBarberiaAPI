using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TuBarberiaAPI.Data;
using TuBarberiaAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// DB
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// Servicios
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<EmailService>();

// CORS (orígenes exactos; sin credenciales)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
        policy
            .WithOrigins(
                "http://localhost:4200",
                "https://brilliant-travesseiro-dddd27.netlify.app",
                "https://calm-coast-04658b71e.1.azurestaticapps.net"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
    );
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

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();                  // 👈 necesario con endpoint routing
app.UseCors("AllowAngularApp");    // 👈 CORS entre routing y auth
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
