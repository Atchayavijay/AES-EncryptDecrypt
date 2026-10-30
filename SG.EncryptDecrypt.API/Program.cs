using SG.EncryptDecrypt.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<JsonCryptoService>();

var app = builder.Build();

// Enable Swagger for testing
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.MapControllers();

// Root endpoint
app.MapGet("/", () => Results.Ok(new
{
    Status = "Success",
    Message = "Encrypt Decrypt API is running",
    Version = "1.0"
}));

app.Run();