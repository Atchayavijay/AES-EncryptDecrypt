var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var apiUrl = builder.Configuration["CryptoApi:BaseUrl"]
    ?? throw new InvalidOperationException(
        "CryptoApi:BaseUrl is missing.");

builder.Services.AddHttpClient("CryptoApi", client =>
{
    client.BaseAddress = new Uri(apiUrl);
    client.Timeout = TimeSpan.FromSeconds(120);
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.MapControllers();

app.MapGet("/", () => Results.Ok(new
{
    Status = "Success",
    Message = "Encrypt Decrypt IAPI is running"
}));

app.Run();