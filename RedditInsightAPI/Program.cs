global using RedditInsightAPI.Services.RedditInsightService;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IRedditInsightService, RedditInsightService>();

builder.Services.AddCors(options => options.AddPolicy(name: "RedditInsightAngular", policy =>
{
    policy.WithOrigins(
        "http://localhost:4200",
        "https://redditinsight.netlify.app",
        "https://redditinsight.pro"
        ).AllowAnyMethod().AllowAnyHeader();
}));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("RedditInsightAngular");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
