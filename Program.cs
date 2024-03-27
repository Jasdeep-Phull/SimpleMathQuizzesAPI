using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpleMathQuizzesAPI.Services;
using SimpleMathQuizzesAPI.Data;
using SimpleMathQuizzesAPI.SwaggerCustomisation;
using SimpleMathQuizzesAPI.Entities;


/* CORS is not set up on the SPA or the API. The SPA and API were tested with CORS disabled.
 * Tested on chrome with CORS disabled using: 'chrome.exe --disable-web-security --user-data-dir=~/chromeTemp'
 * (from folder: 'C:\Program Files\Google\Chrome\Application')
 */


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// add problem details
builder.Services.AddProblemDetails();


// get connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");


// add EmailLogger to the service container
builder.Services.AddTransient<IEmailSender<User>, EmailLogger<User>>();


// add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    // this app uses a Postgresql database, and uses the NpgSql EF Core provider
    options.UseNpgsql(connectionString));


// adds identity api endpoints to the API
// identity api endpoints with bearer token authentication is not suitable for a real application
builder.Services.AddIdentityApiEndpoints<User>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;

    options.User.RequireUniqueEmail = true;

    options.Lockout.MaxFailedAccessAttempts = 3;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(2);
})
    .AddDefaultTokenProviders() // ?
    .AddEntityFrameworkStores<ApplicationDbContext>();


builder.Services.AddControllers();


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
    options.OperationFilter<CustomHeaderSwaggerAttribute>());


// add authentication
builder.Services.AddAuthentication();

builder.Services.AddAuthorizationBuilder()
    /* this policy ensures that only authorized users can access (read, edit, delete) quizzes
     * currently the only authorized user for a quiz is the quiz creator
     */
    .AddPolicy("CanAccessQuiz", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.Requirements.Add(new CanAccessQuizRequirement());
    });
// add authorization handler for the "CanAccessQuiz" requirement to the service container
builder.Services.AddScoped<IAuthorizationHandler, IsQuizCreatorAuthorizationHandler>();


// This method of adding timestamps is deprecated, but it is a quick and easy way to add a timestamp to logs
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options =>
{
    options.TimestampFormat = "[dd/MM/yy HH:mm:ss:fff]";
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapSwagger().RequireAuthorization(); // ?
}
else
{
    app.UseExceptionHandler(exceptionHandlerApp
    => exceptionHandlerApp.Run(async context
        => await Results.Problem()
                     .ExecuteAsync(context)));
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapGroup("/account")
    .MapIdentityApi<User>(); // prepends "/account" to all identity endpoints


// from https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-api-authorization?view=aspnetcore-8.0#log-out
app.MapPost("/account/logout", async (SignInManager<User> signInManager,
    [FromBody] object emptyJson) =>
{
    if (emptyJson != null)
    {
        await signInManager.SignOutAsync();
        return Results.Ok();
    }
    return Results.Unauthorized();
})
// .WithOpenApi()
.RequireAuthorization();


app.MapControllers();

app.Run();
