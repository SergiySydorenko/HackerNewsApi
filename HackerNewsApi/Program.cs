using HackerNewsApi.Services;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add memory cache
builder.Services.AddMemoryCache();

// Configure typed HttpClient for Hacker News API
builder.Services.AddHttpClient("HackerNews", client =>
{
	client.BaseAddress = new Uri("https://hacker-news.firebaseio.com/v0/");
	client.Timeout = TimeSpan.FromSeconds(10);
});

// Register HackerNewsService with DI
builder.Services.AddScoped<IHackerNewsService, HackerNewsService>();

builder.Services.AddSwaggerGen(options =>
{
	var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
	options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}



app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();