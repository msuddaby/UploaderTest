using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using UploaderTest.Context;
using UploaderTest.Helpers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.



builder.Services.AddAzureClients(cb =>
{
    cb.AddBlobServiceClient(builder.Configuration["BlobStorageProdConnection"]);
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration["ConnectionString:DefaultConnection"]);
});

builder.Services.AddSingleton<VideoUploadQueue>();
builder.Services.AddHostedService<IndexVideoBackgroundService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
