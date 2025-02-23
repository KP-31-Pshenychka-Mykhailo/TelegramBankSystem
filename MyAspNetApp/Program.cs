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
string connectionStringDataBase = builder.Configuration.GetConnectionString("DefaultConnectionDataBase");

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

userRoutes.MapPost("/logIn", async (LoginDto dto) =>
{
    logger.LogInformation($"User {dto.UserId} attempting login...");

    var request = new UserRequest
    {
        UserID = dto.UserId,
        UserSNP = dto.UserSNP,
        UserPhoneNumber = dto.UserPhoneNumber,
        UserPassword = dto.UserPassword
    };


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

    return await userIdCheckHandler.HandleAsync(request);
});

userRoutes.MapPost("/signIn", async (SignInDto dto) =>
{
    logger.LogInformation($"User {dto.NewUserId} attempting sign in...");
    
    var request = new UserRequest
    {
        UserID = dto.NewUserId,
        UserOldID = dto.OldUserId,
        UserPassword = dto.UserPassword
    };

    var oldUserIDCheckHandler = new UserExistenceHandler(connectionStringDataBase);
    var passwordOldUserIDChecker = new PasswordExistenceHandler(connectionStringDataBase);
    var newUserIDCheckHandler = new UserUniquenessHandler(connectionStringDataBase);
    var updateUserInformation = new UpdateAccountInformation(connectionStringDataBase);

    oldUserIDCheckHandler
        .SetNext(passwordOldUserIDChecker)
        .SetNext(newUserIDCheckHandler)
        .SetNext(updateUserInformation);

    return await oldUserIDCheckHandler.HandleAsync(request);
});

userRoutes.MapPost("/accountRecovery", async (AccountRecoveryDto dto) =>
{
    logger.LogInformation($"User {dto.NewUserId} trying to recover account {dto.OldUserId}...");
    
    var request = new UserRequest
    {
        UserID = dto.NewUserId,
        UserOldID = dto.OldUserId,
        UserPhoneNumber = dto.UserPhoneNumber,
    };

    var newUserIdCheckHandler = new UserUniquenessHandler(connectionStringDataBase);
    var oldUserIdCheckHandler = new UserExistenceHandler(connectionStringDataBase);
    var phoneNumberValidationHandler = new PhoneNumberValidationHandler();
    var phoneNumberOldUserIDChecker = new PhoneExistenceHandler(connectionStringDataBase);
    var twoFactorAuthenticationHandler = new TwoFactorAuthenticationCodeSend(
        twilioAccountSid, twilioAuthToken, twilioPhoneNumber, connectionStringDataBase
    );

    newUserIdCheckHandler
        .SetNext(oldUserIdCheckHandler)
        .SetNext(phoneNumberValidationHandler)
        .SetNext(phoneNumberOldUserIDChecker)
        .SetNext(twoFactorAuthenticationHandler);

    return await newUserIdCheckHandler.HandleAsync(request);
});

userRoutes.MapPost("/updateInformation", async (UpdateAccountDto dto) =>
{
    logger.LogInformation($"User {dto.NewUserId} updating information from account {dto.OldUserId}...");
    
    var request = new UserRequest
    {
        UserID = dto.NewUserId,
        UserOldID = dto.OldUserId,
        UserCode = dto.UserCode
    };

    
    var userIdCheckHandler = new UserUniquenessHandler(connectionStringDataBase);
    var twoFactorAuthenticationCodeCheck = new TwoFactorAuthenticationCodeCheck(connectionStringDataBase);
    var updateInformationOfAccount = new UpdateAccountInformation(connectionStringDataBase);

    userIdCheckHandler
        .SetNext(twoFactorAuthenticationCodeCheck)
        .SetNext(updateInformationOfAccount);

    return await userIdCheckHandler.HandleAsync(request);
});


var transactionRoutes = app.MapGroup("/operationwithbalance");

transactionRoutes.MapPost("/transfer", async (TransferDto dto) =>
{
    logger.LogInformation($"User {dto.UserId} attempting transfer to {dto.RecipientId} with amount {dto.AmountOfMoney}...");
    
    var request = new UserRequestOperation
    {
        UserID = dto.UserId,
        UserAmountOfMoney = dto.AmountOfMoney,
        UserRecipientId = dto.RecipientId
    };

    var accountExistenceHandler = new AccountExistenceHandler(connectionStringDataBase);
    var balanceCheckHandler = new BalanceCheckHandler(connectionStringDataBase);
    var transferHandler = new TransferHandler(connectionStringDataBase);

    accountExistenceHandler
        .SetNext(balanceCheckHandler)
        .SetNext(transferHandler);

    return await accountExistenceHandler.HandleAsync(request);
});

transactionRoutes.MapPost("/replenishment", async (ReplenishmentDto dto) =>
{
    logger.LogInformation($"User {dto.UserId} attempting replenishment with amount {dto.AmountOfMoney}...");
    
    var request = new UserRequestOperation
    {
        UserID = dto.UserId,
        UserAmountOfMoney = dto.AmountOfMoney,
    };

    var accountExistenceHandler = new AccountExistenceHandler(connectionStringDataBase);
    var replenishmentHandler = new ReplenishmentHandler(connectionStringDataBase);

    accountExistenceHandler.SetNext(replenishmentHandler);

    return await accountExistenceHandler.HandleAsync(request);
});

transactionRoutes.MapPost("/withdrawal", async (WithdrawalDto dto) =>
{
    logger.LogInformation($"User {dto.UserId} attempting withdrawal with amount {dto.AmountOfMoney}...");
    
    var request = new UserRequestOperation
    {
        UserID = dto.UserId,
        UserAmountOfMoney = dto.AmountOfMoney,
    };

    var accountExistenceHandler = new AccountExistenceHandler(connectionStringDataBase);
    var balanceCheckHandler = new BalanceCheckHandler(connectionStringDataBase);
    var withdrawalHandler = new WithdrawalHandler(connectionStringDataBase);

    accountExistenceHandler
        .SetNext(balanceCheckHandler)
        .SetNext(withdrawalHandler);

    return await accountExistenceHandler.HandleAsync(request);
});

transactionRoutes.MapGet("/showInformation/{userID}", async (long userID) =>
{
    logger.LogInformation($"User {userID} requested account information...");


        var request = new UserRequestOperation
    {
        UserID = userID,
    };

    var showInformationHandler = new ShowInformationHandler(connectionStringDataBase);
    return await showInformationHandler.HandleAsync(request);
});



app.Run();

