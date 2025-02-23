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
using SendMessage;
using Dto;

var builder = WebApplication.CreateBuilder(args);

//<!---SendMessage---!>//
builder.Services.AddHostedService(sp => new TransactionMonitorService(
    builder.Configuration.GetConnectionString("DefaultConnectionDataBase"), 
    builder.Configuration.GetConnectionString("DefaultConnectionTelegramBot"),
    sp.GetRequiredService<ILogger<TransactionMonitorService>>()
));

builder.Host.UseSerilog((context, configuration) => 
configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
string connectionStringDataBase = builder.Configuration.GetConnectionString("DefaultConnectionDataBase");//database

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
userRoutes.MapGet("/logIn", async (long userID, string userSNP, string userPhoneNumber, string userPassword) =>
{
    logger.LogInformation($"User {userID} attempting login...");

    var userIdCheckHandler = new UserUniquenessHandler(connectionStringDataBase);
    var snpValidationHandler = new SnpValidationHandler();
    var phoneNumberValidationHandler = new PhoneNumberValidationHandler();
    var passwordValidationHandler = new PasswordValidationHandler();
    var userRegistrationHandler = new UserRegistrationHandler(connectionStringDataBase);

    userIdCheckHandler
        .SetNext(snpValidationHandler)
        .SetNext(phoneNumberValidationHandler)
        .SetNext(passwordValidationHandler)
        .SetNext(userRegistrationHandler);

    return await userIdCheckHandler.HandleAsync((userID, userSNP, userPhoneNumber, userPassword));
});
userRoutes.MapGet("/signIn/{newUserID}/{oldUserID}/{userPhoneNumber}/{userPassword}", async (long newUserID, string oldUserID, string userPhoneNumber, string userPassword) =>
{
    logger.LogInformation($"User {newUserID} attempting sign in...");
    var oldUserIDCheckHandler = new UserExistenceHandler(connectionStringDataBase);
    var passwordOldUserIDChecker = new PasswordExistenceHandler(connectionStringDataBase);
    var newUserIDCheckHandler = new UserUniquenessHandler(connectionStringDataBase);
    var updateUserInformation = new UpdateAccountInformation(connectionStringDataBase);

    oldUserIDCheckHandler
            .SetNext(passwordOldUserIDChecker)
            .SetNext(newUserIDCheckHandler)
            .SetNext(updateUserInformation);

    return await oldUserIDCheckHandler.HandleAsync((newUserID, oldUserID, userPhoneNumber, userPassword));
});
userRoutes.MapGet("/accountrecovery/{newUserID}/{oldUserID}/{userPhoneNumber}/{userPassword}", async (long newUserID, string oldUserID, string userPhoneNumber, string userPassword) => //password
{
    logger.LogInformation($"User {newUserID} try recovery account {oldUserID}...");
    var newUserIdCheckHandler = new UserUniquenessHandler(connectionStringDataBase);
    var oldUserIdCheckHandler = new UserExistenceHandler(connectionStringDataBase);
    var phoneNumberValidationHandler = new PhoneNumberValidationHandler();
    var phoneNumberOldUserIDChecker = new PhoneExistenceHandler(connectionStringDataBase);
    var twoFactorAuthenticationHandler = new TwoFactorAuthenticationCodeSend(twilioAccountSid, twilioAuthToken, twilioPhoneNumber, connectionStringDataBase);

    newUserIdCheckHandler
            .SetNext(oldUserIdCheckHandler)
            .SetNext(phoneNumberValidationHandler)
            .SetNext(phoneNumberOldUserIDChecker)
            .SetNext(twoFactorAuthenticationHandler);

    return await newUserIdCheckHandler.HandleAsync((newUserID, oldUserID, userPhoneNumber, userPassword));
});
userRoutes.MapGet("/updateinformation_account/{newUserID}/{oldUserID}/{userCode}/{userPassword}", async (long newUserID, string oldUserID, string userCode, string userPassword) => 
{
    logger.LogInformation($"User {newUserID} update information from account {oldUserID}...");
    var userIdCheckHandler = new UserUniquenessHandler(connectionStringDataBase);
    var twoFactorAuthenticationCodeCheck = new TwoFactorAuthenticationCodeCheck(connectionStringDataBase);
    var updateInformationOfAccount = new UpdateAccountInformation(connectionStringDataBase);

    userIdCheckHandler
        .SetNext(twoFactorAuthenticationCodeCheck)
        .SetNext(updateInformationOfAccount);
    return await userIdCheckHandler.HandleAsync((newUserID, oldUserID, userCode, userPassword));
});


var trancsactionRoutes = app.MapGroup("/operationwithballance");
trancsactionRoutes.MapGet("/transfer/{userID}/{amountOfMoney}/{recientID}", async (long userID, long amountOfMoney, long recientID) =>
{
    logger.LogInformation($"User {userID} attempting transfer to {recientID} with amount {amountOfMoney}...");
    var accountExistenceHandler = new AccountExistenceHandler(connectionStringDataBase);
    var balanceCheckHandler = new BalanceCheckHandler(connectionStringDataBase);
    var transferHandler = new TransferHandler(connectionStringDataBase);

    accountExistenceHandler
        .SetNext(balanceCheckHandler)
        .SetNext(transferHandler);

    return await accountExistenceHandler.HandleAsync((userID, amountOfMoney, recientID));
});
trancsactionRoutes.MapGet("/replenishment/{userID}/{amountOfMoney}", async (long userID, long amountOfMoney) =>
{
    logger.LogInformation($"User {userID} attempting replenishment with amount {amountOfMoney}...");
    var accountExistenceHandler = new AccountExistenceHandler(connectionStringDataBase);
    var replenishmentHandler = new ReplenishmentHandler(connectionStringDataBase);

    accountExistenceHandler.SetNext(replenishmentHandler);

    return await accountExistenceHandler.HandleAsync((userID, amountOfMoney, Convert.ToInt64(null)));
});
trancsactionRoutes.MapGet("/withdrawl/{userID}/{amountOfMoney}", async (long userID, long amountOfMoney) =>
{
    logger.LogInformation($"User {userID} attempting withdrawal with amount {amountOfMoney}...");
    var accountExistenceHandler = new AccountExistenceHandler(connectionStringDataBase);
    var balanceCheckHandler = new BalanceCheckHandler(connectionStringDataBase);
    var withdrawalHandler = new WithdrawalHandler(connectionStringDataBase);

    accountExistenceHandler
        .SetNext(balanceCheckHandler)
        .SetNext(withdrawalHandler);

    return await accountExistenceHandler.HandleAsync((userID, amountOfMoney, Convert.ToInt64(null)));
});
trancsactionRoutes.MapGet("/showinformation/{userID}", async (long userID) =>
{
    logger.LogInformation($"User {userID} send reques for showing information...");
    var showInformationHandler = new ShowInformationHandler(connectionStringDataBase);

    return await showInformationHandler.HandleAsync((userID));
});


app.Run();

