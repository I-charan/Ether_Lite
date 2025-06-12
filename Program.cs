using Nethereum.Web3;
using Ether_Lite.Services.Extensions;
using Ether_Lite.Services.Interface;
using Ether_Lite.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register Web3 and Wallet Services
ConfigureWeb3Services(builder);
builder.Services.AddHttpClient(); // ✅ ADD THIS LINE
builder.Services.AddScoped<IWalletInfoService, WalletInfoService>();
builder.Services.AddScoped<IWalletBalService, WalletInfoService>();

// CORS setup
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

builder.Services.AddWalletServices();
var app = builder.Build();
// Add this after builder is created


// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();

// Enable serving static files
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

app.Run();

void ConfigureWeb3Services(WebApplicationBuilder webBuilder)
{
    // Register Web3 as singleton for the default Ethereum connection
    var alchemyUrl = webBuilder.Configuration["Ethereum:AlchemyUrl"];
    if (!string.IsNullOrEmpty(alchemyUrl))
    {
        webBuilder.Services.AddSingleton<Web3>(_ => new Web3(alchemyUrl));
    }

    // Register the wallet info service (uses multiple Web3 clients internally)
    webBuilder.Services.AddWalletServices();
}