using Cortex.Data;
using Cortex.Middlewares;
using Cortex.Models;
using Cortex.Repositories;
using Cortex.Repositories.Interfaces;
using Cortex.Services;
using Cortex.Services.Factories;
using Cortex.Services.Interfaces;
using Cortex.Services.Strategies;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(StorageClient.Create());

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

Npgsql.NpgsqlConnection.GlobalTypeMapper.EnableDynamicJson();

builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsqlOptions => npgsqlOptions.UseVector()
            )
        );

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Key"]!)),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

builder.Services.AddCors(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.AddPolicy("Development", policy =>
        {
            policy.WithOrigins(
                    "http://localhost:3000",
                    "http://localhost:5173",
                    "http://localhost:5174" 
                )
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    }
    else
    {
        options.AddPolicy("Production", policy =>
        {
            policy.WithOrigins("https://seudominio.com")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    }
});

builder.Services.Configure<GeminiConfiguration>(
    builder.Configuration.GetSection(GeminiConfiguration.SectionName));

//Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAnalysisRepository, AnalysisRepository>();
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();

//User
builder.Services.AddScoped<IUserService, UserService>();

//Analysis
builder.Services.AddScoped<IAnalysisService, AnalysisService>();
builder.Services.AddScoped<IAnalysisOrchestrator, AnalysisOrchestrator>();

//Documents
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IDocumentProcessingEmbeddingsService, DocumentProcessingEmbeddingsService>();
builder.Services.AddScoped<IFileStorageService, GcsFileStorageService>();

//Strategies
builder.Services.AddScoped<IDocumentProcessingStrategy, TxtDocumentProcessingStrategy>();
builder.Services.AddScoped<IDocumentProcessingStrategy, PdfDocumentProcessingStrategy>();

//Chunks
builder.Services.AddScoped<IChunkRepository, ChunkRepository>();
builder.Services.AddScoped<IChunkService, ChunkService>();

//Embeddings
builder.Services.AddScoped<IEmbeddingService, EmbeddingService>();

//Stages
builder.Services.AddScoped<PreAnalysisStageService>();
builder.Services.AddScoped<ExplorationOfMaterialStageService>();
builder.Services.AddScoped<InferenceConclusionStageService>();
builder.Services.AddScoped<StageStrategyFactory>();
builder.Services.AddScoped<IStageRepository, StageRepository>();


//Gemini
builder.Services.AddScoped<IGeminiService, GeminiService.Api.Services.Implementations.GeminiService>();
builder.Services.AddHttpClient<EmbeddingService>();

//Indexes
builder.Services.AddScoped<IIndexRepository, IndexRepository>();

//Indexes References
builder.Services.AddScoped<IIndexReferenceRepository, IndexReferenceRepository>();

//Indicator
builder.Services.AddScoped<IIndicatorRepository, IndicatorRepository>();
builder.Services.AddScoped<IIndicatorService, IndicatorService>();


// Logging
builder.Services.AddLogging();

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseCors("Development");
}
else
{
    app.UseCors("Production");
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
