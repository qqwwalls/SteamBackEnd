using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Steam.Api.MiddleWares;
using Steam.Application.Interfaces.Repository;
using Steam.Application.Interfaces.Services;
using Steam.Application.Mapping;
using Steam.Application.Services;
using Steam.Application.Validators;
using Steam.Infrastructure.Configuration;
using Steam.Infrastructure.Data;
using Steam.Infrastructure.Repositories;
using Steam.Infrastructure.Services;
using System.Text;
using Steam.Application.Configuration;

namespace Steam.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddDbContext<SteamDbContext>(options =>
            {
                if (builder.Configuration.GetValue<bool>("UseSqliteDatabase"))
                    options.UseSqlite(builder.Configuration.GetConnectionString("SqliteConnection"));
                else
                    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServerConnection"));
            });


            builder.Services.AddOptions<JwtSettings>()
                .Bind(builder.Configuration.GetSection("JwtSettings"))
                .Validate(settings => Encoding.UTF8.GetByteCount(settings.Key) >= 32,
                    "JwtSettings:Key must contain at least 32 bytes.")
                .Validate(settings => !string.IsNullOrWhiteSpace(settings.Issuer) &&
                    !string.IsNullOrWhiteSpace(settings.Audience) &&
                    settings.AccessTokenLifetimeMinutes > 0 && settings.RefreshTokenLifetimeDays > 0,
                    "JWT issuer, audience and positive token lifetimes are required.")
                .Validate(settings => builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing") ||
                    !settings.Key.StartsWith("development_only_"),
                    "Set a private JwtSettings:Key before deployment.")
                .ValidateOnStart();

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
            builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
                .Configure<IOptions<JwtSettings>>((options, settings) =>
                {
                    var jwtSettings = settings.Value;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtSettings.Issuer,
                        ValidAudience = jwtSettings.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
                        ClockSkew = TimeSpan.FromSeconds(30)
                    };
                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = async context =>
                        {
                            var id = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                            var db = context.HttpContext.RequestServices.GetRequiredService<SteamDbContext>();
                            if (!Guid.TryParse(id, out var userId) ||
                                !await db.Users.AnyAsync(u => u.Id == userId && u.IsActive, context.HttpContext.RequestAborted))
                                context.Fail("User is not active.");
                        }
                    };
                });
            builder.Services.AddAuthorization();
            builder.Services.Configure<CheckoutSettings>(builder.Configuration.GetSection("Checkout"));
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddCors(options => options.AddPolicy("AllowFrontend", policy =>
                policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                    ?? ["http://localhost:5173", "http://localhost:5174", "http://localhost:3000"])
                    .AllowAnyHeader().AllowAnyMethod()));

            builder.Services.AddControllers();
            builder.Services.AddFluentValidationAutoValidation();
            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Steam API",
                    Version = "v1"
                });

                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Description = "Enter JWT token"
                });
                options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                });
            });

            //===============================REPOSITORY===============================

            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IWishlistRepository, WishlistRepository>();
            builder.Services.AddScoped<ILibraryRepository, LibraryRepository>();
            builder.Services.AddScoped<IGameRepository, GameRepository>();
            builder.Services.AddScoped<IGenreRepository, GenreRepository>();
            builder.Services.AddScoped<ITagRepository, TagRepository>();
            builder.Services.AddScoped<IProviderRepository, ProviderRepository>();
            builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            builder.Services.AddScoped<IAuthRepository, AuthRepository>();
            builder.Services.AddScoped<IMediaRepository, MediaRepository>();
            builder.Services.AddScoped<ISystemRequirementsRepository, SystemRequirementsRepository>();
            builder.Services.AddScoped<IAchievementRepository, AchievementRepository>();

            //===============================SERVICES===============================
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IJWTService, JWTService>();
            builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
            builder.Services.AddScoped<IWishlistService, WishlistService>();
            builder.Services.AddScoped<ILibraryService, LibraryService>();

            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IGameService, GameService>();
            builder.Services.AddScoped<IGameAccessService, GameAccessService>();
            builder.Services.AddScoped<IMediaService, MediaService>();
            builder.Services.AddScoped<ISystemRequirementsService, SystemRequirementsService>();
            builder.Services.AddScoped<IAchievementService, AchievementService>();
            builder.Services.AddScoped<IGenreService, GenreService>();
            builder.Services.AddScoped<ITagService, TagService>();
            builder.Services.AddScoped<IProviderService, ProviderService>();


            builder.Services.AddAutoMapper(_ => { }, typeof(SteamProfile).Assembly);
            builder.Services.AddValidatorsFromAssemblyContaining<GameCreateValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<MediaCreateValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<SystemRequirementsCreateValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<UserCreateValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<AchievementCreateValidator>();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SteamDbContext>();
                if (builder.Configuration.GetValue<bool>("UseSqliteDatabase"))
                    db.Database.EnsureCreated();
                else if (builder.Configuration.GetValue<bool>("ApplyMigrations"))
                    db.Database.Migrate();
                if (app.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("SeedDemoData"))
                    DemoDataInitializer.InitializeAsync(db).GetAwaiter().GetResult();
            }

            app.UseMiddleware<ApiExceptionMiddleware>();
            app.UseMiddleware<RequestTimerMiddleware>();
            app.UseMiddleware<CancellationTokenHandleMiddleware>();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
                app.UseHttpsRedirection();

            app.UseCors("AllowFrontend");
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseStaticFiles();

            app.MapControllers();
            app.UseSwagger();
            app.UseSwaggerUI();

            app.Run();
        }
    }
}
