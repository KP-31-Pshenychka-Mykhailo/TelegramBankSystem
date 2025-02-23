using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using System.Text.RegularExpressions;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using Npgsql;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using Serilog;

using LogSignIn;
using OperationWithBallance;


var builder = WebApplication.CreateBuilder(args);


builder.Host.UseSerilog((context, configuration) => 
configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
string connectionString = builder.Configuration.GetConnectionString("DefaultConnection");//database

var twilioConfig = builder.Configuration.GetSection("Twilio");

string twilioAccountSid = twilioConfig["AccountSid"];
string twilioAuthToken = twilioConfig["AuthToken"];
string twilioPhoneNumber = twilioConfig["PhoneNumber"];

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
userRoutes.MapGet("/signIn/{newUserID}/{oldUserID}/{userPhoneNumber}/{userPassword}", async (int newUserID, string oldUserID, string userPhoneNumber, string userPassword) =>
{
    logger.LogInformation($"User {newUserID} attempting sign in...");
    var oldUserIDCheckHandler = new UserExistenceHandler(connectionString);
    var passwordOldUserIDChecker = new PasswordExistenceHandler(connectionString);
    var newUserIDCheckHandler = new UserUniquenessHandler(connectionString);
    var updateUserInformation = new UpdateAccountInformation(connectionString);

    oldUserIDCheckHandler
            .SetNext(passwordOldUserIDChecker)
            .SetNext(newUserIDCheckHandler)
            .SetNext(updateUserInformation);

    return await oldUserIDCheckHandler.HandleAsync((newUserID, oldUserID, userPhoneNumber, userPassword));
});
userRoutes.MapGet("/accountrecovery/{newUserID}/{oldUserID}/{userPhoneNumber}/{userPassword}", async (int newUserID, string oldUserID, string userPhoneNumber, string userPassword) => //oldUserID to int in methods
{
    logger.LogInformation($"User {newUserID} try recovery account {oldUserID}...");
    var newUserIdCheckHandler = new UserUniquenessHandler(connectionString);
    var oldUserIdCheckHandler = new UserExistenceHandler(connectionString);
    var phoneNumberValidationHandler = new PhoneNumberValidationHandler();
    var phoneNumberOldUserIDChecker = new PhoneExistenceHandler(connectionString);
    var twoFactorAuthenticationHandler = new TwoFactorAuthenticationCodeSend(twilioAccountSid, twilioAuthToken, twilioPhoneNumber, connectionString);

    newUserIdCheckHandler
            .SetNext(oldUserIdCheckHandler)
            .SetNext(phoneNumberValidationHandler)
            .SetNext(phoneNumberOldUserIDChecker)
            .SetNext(twoFactorAuthenticationHandler);

    return await newUserIdCheckHandler.HandleAsync((newUserID, oldUserID, userPhoneNumber, userPassword));
});
userRoutes.MapGet("/updateinformation_account/{newUserID}/{oldUserID}/{userCode}/{userPassword}", async (int newUserID, string oldUserID, string userCode, string userPassword) => //oldUserID to int in methods
{
    logger.LogInformation($"User {newUserID} update information from account {oldUserID}...");
    var userIdCheckHandler = new UserUniquenessHandler(connectionString);
    var twoFactorAuthenticationCodeCheck = new TwoFactorAuthenticationCodeCheck(connectionString);
    var updateInformationOfAccount = new UpdateAccountInformation(connectionString);

    userIdCheckHandler
        .SetNext(twoFactorAuthenticationCodeCheck)
        .SetNext(updateInformationOfAccount);
    return await userIdCheckHandler.HandleAsync((newUserID, oldUserID, userCode, userPassword));
});


var trancsactionRoutes = app.MapGroup("/operationwithballance");
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
trancsactionRoutes.MapGet("/showinformation/{userID}", async (int userID) =>
{
    logger.LogInformation($"User {userID} send request to show ballance...");
    var showInformationHandler = new ShowInformationHandler(connectionString);

    return await showInformationHandler.HandleAsync((userID));
});


app.Run();