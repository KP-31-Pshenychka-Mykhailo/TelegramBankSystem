using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using System.Text.RegularExpressions;
using System.Security.Cryptography.X509Certificates;

using Npgsql;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

using LogSignIn;
using OperationWithBallance;
using Serilog;

var builder = WebApplication.CreateBuilder(args);


builder.Host.UseSerilog((context, configuration) => 
configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
string connectionString = builder.Configuration.GetConnectionString("DefaultConnection");//database


var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Application started successfully.");


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var userRoutes = app.MapGroup("/user");
userRoutes.MapGet("/logIn/{userID}/{userSNP}/{userPhoneNumber}/{userPassword}", async (int userID, string userSNP, string userPhoneNumber, string userPassword) =>
{
    logger.LogInformation($"User {userID} attempting login...");

    var userIdCheckHandler = new UserUniquenessHandler(connectionString);
    var snpValidationHandler = new SnpValidationHandler();
    var phoneNumberValidationHandler = new PhoneNumberValidationHandler();
    var passwordValidationHandler = new PasswordValidationHandler();
    var userRegistrationHandler = new UserRegistrationHandler(connectionString);

    userIdCheckHandler
        .SetNext(snpValidationHandler)
        .SetNext(phoneNumberValidationHandler)
        .SetNext(passwordValidationHandler)
        .SetNext(userRegistrationHandler);

    return await userIdCheckHandler.HandleAsync((userID, userSNP, userPhoneNumber, userPassword));
});
userRoutes.MapGet("/signIn/{userID}/{userPassword}", async (int userID, string userPassword) =>
{
    logger.LogInformation($"User {userID} attempting sign in...");
    var userIdCheckHandler = new UserExistenceHandler(connectionString);
    var passwordValidationHandler = new PasswordCheckHandler(connectionString);

    userIdCheckHandler.SetNext(passwordValidationHandler);

    return await userIdCheckHandler.HandleAsync((userID, userPassword));
});
userRoutes.MapGet("/accountrecovery/{newUserID}/{oldUserID}/{userPhoneNumber}/{userPassword}", async (int newUserID, int oldUserID, string userPhoneNumber, string userPassword) =>
{
    logger.LogInformation($"User {newUserID} try recovery account {oldUserID}...");

    var userIdCheckHandlerNew = new UserUniquenessHandler(connectionString);
    var userIdCheckHandlerOld = new UserExistenceHandler(connectionString);

});




var trancsactionRoutes = app.MapGroup("/transaction");
trancsactionRoutes.MapGet("/transfer/{userID}/{amountOfMoney}/{recientID}", async (int userID, int amountOfMoney, int recientID) =>
{
    logger.LogInformation($"User {userID} attempting transfer to {recientID} with amount {amountOfMoney}...");
    var accountExistenceHandler = new AccountExistenceHandler(connectionString);
    var balanceCheckHandler = new BalanceCheckHandler(connectionString);
    var transferHandler = new TransferHandler(connectionString);

    accountExistenceHandler
        .SetNext(balanceCheckHandler)
        .SetNext(transferHandler);

    return await accountExistenceHandler.HandleAsync((userID, amountOfMoney, recientID));
});
trancsactionRoutes.MapGet("/replenishment/{userID}/{amountOfMoney}", async (int userID, int amountOfMoney) =>
{
    logger.LogInformation($"User {userID} attempting replenishment with amount {amountOfMoney}...");
    var accountExistenceHandler = new AccountExistenceHandler(connectionString);
    var replenishmentHandler = new ReplenishmentHandler(connectionString);

    accountExistenceHandler.SetNext(replenishmentHandler);

    return await accountExistenceHandler.HandleAsync((userID, amountOfMoney, 0));
});
trancsactionRoutes.MapGet("/withdrawl/{userID}/{amountOfMoney}", async (int userID, int amountOfMoney) =>
{
    logger.LogInformation($"User {userID} attempting withdrawal with amount {amountOfMoney}...");
    var accountExistenceHandler = new AccountExistenceHandler(connectionString);
    var balanceCheckHandler = new BalanceCheckHandler(connectionString);
    var withdrawalHandler = new WithdrawalHandler(connectionString);

    accountExistenceHandler
        .SetNext(balanceCheckHandler)
        .SetNext(withdrawalHandler);

    return await accountExistenceHandler.HandleAsync((userID, amountOfMoney, 0));
});


app.Run();