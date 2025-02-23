using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Npgsql;
using System.Text.RegularExpressions;
using System.Security.Cryptography.X509Certificates;

namespace OperationWithBallance 
{
    public abstract class Handler
    {
        private Handler _nextHandler;

        public Handler SetNext(Handler nextHandler)
        {
            _nextHandler = nextHandler;
            return nextHandler;
        }

        public virtual async Task<object> HandleAsync(object request)
        {
            if (_nextHandler != null)
            {
                return await _nextHandler.HandleAsync(request);
            }

            return null;
        }
    }

    public class AccountExistenceHandler : Handler
    {
        private readonly string _connectionString;

        public AccountExistenceHandler(string connectionString)
        {
            _connectionString = connectionString;
        }

        public override async Task<object> HandleAsync(object request)
        {
            var (userID, _, recientID) = ((int, int, int))request;

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var checkAccountQuery = "SELECT COUNT(*) FROM users WHERE userid = @userid";
            using var checkAccountCommand = new NpgsqlCommand(checkAccountQuery, connection);
            checkAccountCommand.Parameters.AddWithValue("userid", userID);

            var accountExists = (long)await checkAccountCommand.ExecuteScalarAsync() > 0;
            if (!accountExists)
            {
                return Results.BadRequest("Аккаунт отправителя не найден.");
            }

            if (recientID != 0)
            {
                checkAccountCommand.Parameters["userid"].Value = recientID;
                accountExists = (long)await checkAccountCommand.ExecuteScalarAsync() > 0;
                if (!accountExists)
                {
                    return Results.BadRequest("Аккаунт получателя не найден.");
                }
            }

            return await base.HandleAsync(request);
        }
    }

    public class BalanceCheckHandler : Handler
    {
        private readonly string _connectionString;

        public BalanceCheckHandler(string connectionString)
        {
            _connectionString = connectionString;
        }

        public override async Task<object> HandleAsync(object request)
        {
            var (userID, amountOfMoney, _) = ((int, int, int))request;

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var checkBalanceQuery = "SELECT balance FROM users WHERE userid = @userid";
            using var checkBalanceCommand = new NpgsqlCommand(checkBalanceQuery, connection);
            checkBalanceCommand.Parameters.AddWithValue("userid", userID);

            var balance = (decimal)await checkBalanceCommand.ExecuteScalarAsync();
            if (balance < amountOfMoney)
            {
                return Results.BadRequest("Недостаточно средств на счету.");
            }

            return await base.HandleAsync(request);
        }
    }

    public class TransferHandler : Handler
    {
        private readonly string _connectionString;

        public TransferHandler(string connectionString)
        {
            _connectionString = connectionString;
        }

        public override async Task<object> HandleAsync(object request)
        {
            var (userID, amountOfMoney, recientID) = ((int, int, int))request;

            using var connection = new NpgsqlConnection(_connectionString);
            var fee = FeeHelper.CalculateFee(amountOfMoney);

            await connection.OpenAsync();

            var transferQuery = @"
            UPDATE users 
            SET balance = CASE 
                WHEN userid = @userid THEN balance - (@amount + @fee)
                WHEN userid = @recientid THEN balance + @amount 
            END 
            WHERE userid IN (@userid, @recientid)";

            using var transferCommand = new NpgsqlCommand(transferQuery, connection);
            transferCommand.Parameters.AddWithValue("userid", userID);
            transferCommand.Parameters.AddWithValue("recientid", recientID);
            transferCommand.Parameters.AddWithValue("amount", amountOfMoney);
            transferCommand.Parameters.AddWithValue("fee", fee);

            await transferCommand.ExecuteNonQueryAsync();

            await FeeHelper.LogTransactionFee(connection, userID, fee, "Transfer");

            return Results.Ok($"Перевод выполнен успешно. {fee}");
        }
    }

    public class ReplenishmentHandler : Handler
    {
        private readonly string _connectionString;

        public ReplenishmentHandler(string connectionString)
        {
            _connectionString = connectionString;
        }

        public override async Task<object> HandleAsync(object request)
        {
            var (userID, amountOfMoney, _) = ((int, int, int))request;

            using var connection = new NpgsqlConnection(_connectionString);
            var fee = FeeHelper.CalculateFee(amountOfMoney);

            await connection.OpenAsync();

            var replenishQuery = "UPDATE users SET balance = balance + @amount WHERE userid = @userid";
            using var replenishCommand = new NpgsqlCommand(replenishQuery, connection);
            replenishCommand.Parameters.AddWithValue("userid", userID);
            replenishCommand.Parameters.AddWithValue("amount", amountOfMoney);
            replenishCommand.Parameters.AddWithValue("fee", fee);

            await replenishCommand.ExecuteNonQueryAsync();
            await FeeHelper.LogTransactionFee(connection, userID, fee, "Replenishment");

            return Results.Ok($"Счет пополнен успешно.{fee}");
        }
    }

    public class WithdrawalHandler : Handler
    {
        private readonly string _connectionString;

        public WithdrawalHandler(string connectionString)
        {
            _connectionString = connectionString;
        }

        public override async Task<object> HandleAsync(object request)
        {
            

            var (userID, amountOfMoney, _) = ((int, int, int))request;
            var fee = FeeHelper.CalculateFee(amountOfMoney);

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var withdrawQuery = "UPDATE users SET balance = balance - (@amount + @fee) WHERE userid = @userid";
            using var withdrawCommand = new NpgsqlCommand(withdrawQuery, connection);
            withdrawCommand.Parameters.AddWithValue("userid", userID);
            withdrawCommand.Parameters.AddWithValue("amount", amountOfMoney);
            withdrawCommand.Parameters.AddWithValue("fee", fee);

            await withdrawCommand.ExecuteNonQueryAsync();

            await FeeHelper.LogTransactionFee(connection, userID, fee, "Withdrawal");
            return Results.Ok($"Снятие средств успешно выполнено.{fee}");
        }
    }

    public static class FeeHelper
    {
        public static decimal CalculateFee(decimal amount)
        {
            return amount * 0.01m; // 1% комиссии
        }

        public static async Task LogTransactionFee(NpgsqlConnection connection, int userID, decimal fee, string operationType)
        {
            var logFeeQuery = "INSERT INTO transaction_fees (userid, fee, operation_type, date) VALUES (@userid, @fee, @operationType, NOW())";
            using var logFeeCommand = new NpgsqlCommand(logFeeQuery, connection);
            logFeeCommand.Parameters.AddWithValue("userid", userID);
            logFeeCommand.Parameters.AddWithValue("fee", fee);
            logFeeCommand.Parameters.AddWithValue("operationType", operationType);

            await logFeeCommand.ExecuteNonQueryAsync();
        }
    }

    public class ShowInformationHandler : Handler
    {
        private readonly string _connectionString;

        public ShowInformationHandler(string connectionString)
        {
            _connectionString = connectionString;
        }

        public override async Task<object> HandleAsync(object request)
        {
            var userID = (int)request;

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var checkBalanceQuery = "SELECT balance FROM users WHERE userid = @userid";
            using var checkBalanceCommand = new NpgsqlCommand(checkBalanceQuery, connection);
            checkBalanceCommand.Parameters.AddWithValue("userid", userID);

            var balance = (decimal)await checkBalanceCommand.ExecuteScalarAsync();
            
            return Results.Ok($"Your ID : {userID} Cредств на счетуc: {balance}, ");
            

        }
    }


}