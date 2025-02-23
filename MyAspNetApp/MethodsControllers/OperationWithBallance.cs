using Npgsql;


namespace OperationWithBallance 
{
    public class UserRequestOperation
    {
        public long UserID { get; set; }
        public long UserAmountOfMoney { get; set; }
        public long UserRecipientId { get; set; }
    }

    public abstract class Handler
    {
        private Handler _nextHandler;

        public Handler SetNext(Handler nextHandler)
        {
            _nextHandler = nextHandler;
            return nextHandler;
        }

        public virtual async Task<object> HandleAsync(UserRequestOperation request)
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

        public override async Task<object> HandleAsync( UserRequestOperation request)
        {

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var checkAccountQuery = "SELECT COUNT(*) FROM users WHERE userid = @userid";
            using var checkAccountCommand = new NpgsqlCommand(checkAccountQuery, connection);
            checkAccountCommand.Parameters.AddWithValue("userid", request.UserID);

            var accountExists = (long)await checkAccountCommand.ExecuteScalarAsync() > 0;
            if (!accountExists)
            {
                return Results.BadRequest("Sender's account not found.");
            }

            if (request.UserRecipientId != 0)
            {
                checkAccountCommand.Parameters["userid"].Value = request.UserRecipientId;
                accountExists = (long)await checkAccountCommand.ExecuteScalarAsync() > 0;
                if (!accountExists)
                {
                    return Results.BadRequest("Recipient's account not found.");
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

        public override async Task<object> HandleAsync(UserRequestOperation request)
        {

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var checkBalanceQuery = "SELECT balance FROM users WHERE userid = @userid";
            using var checkBalanceCommand = new NpgsqlCommand(checkBalanceQuery, connection);
            checkBalanceCommand.Parameters.AddWithValue("userid", request.UserID);

            var balance = (decimal)await checkBalanceCommand.ExecuteScalarAsync();
            if (balance < request.UserAmountOfMoney)
            {
                return Results.BadRequest("Insufficient funds in the account.");
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

        public override async Task<object> HandleAsync(UserRequestOperation request)
        {

            using var connection = new NpgsqlConnection(_connectionString);
            var fee = FeeHelper.CalculateFee(request.UserAmountOfMoney);

            await connection.OpenAsync();

            var transferQuery = @"
            UPDATE users 
            SET balance = CASE 
                WHEN userid = @userid THEN balance - (@amount + @fee)
                WHEN userid = @recientid THEN balance + @amount 
            END 
            WHERE userid IN (@userid, @recientid)";

            using var transferCommand = new NpgsqlCommand(transferQuery, connection);
            transferCommand.Parameters.AddWithValue("userid", request.UserID);
            transferCommand.Parameters.AddWithValue("recientid", request.UserRecipientId);
            transferCommand.Parameters.AddWithValue("amount", request.UserAmountOfMoney);
            transferCommand.Parameters.AddWithValue("fee", fee);

            await transferCommand.ExecuteNonQueryAsync();

            await FeeHelper.LogTransactionFee(connection, request.UserID, fee, "Transfer");

            return Results.Ok($"Transfer completed successfully. {request.UserAmountOfMoney-fee}");
        }
    }

    public class ReplenishmentHandler : Handler
    {
        private readonly string _connectionString;

        public ReplenishmentHandler(string connectionString)
        {
            _connectionString = connectionString;
        }

        public override async Task<object> HandleAsync(UserRequestOperation request)
        {

            using var connection = new NpgsqlConnection(_connectionString);
            var fee = FeeHelper.CalculateFee(request.UserAmountOfMoney);

            await connection.OpenAsync();

            var replenishQuery = "UPDATE users SET balance = balance + (@amount-@fee) WHERE userid = @userid";
            using var replenishCommand = new NpgsqlCommand(replenishQuery, connection);
            replenishCommand.Parameters.AddWithValue("userid", request.UserID);
            replenishCommand.Parameters.AddWithValue("amount", request.UserAmountOfMoney);
            replenishCommand.Parameters.AddWithValue("fee", fee);

            await replenishCommand.ExecuteNonQueryAsync();
            await FeeHelper.LogTransactionFee(connection, request.UserID, fee, "Replenishment");

            return Results.Ok($"Account replenished successfully. {request.UserAmountOfMoney-fee}");
        }
    }

    public class WithdrawalHandler : Handler
    {
        private readonly string _connectionString;

        public WithdrawalHandler(string connectionString)
        {
            _connectionString = connectionString;
        }

        public override async Task<object> HandleAsync(UserRequestOperation request)
        {
            var fee = FeeHelper.CalculateFee(request.UserAmountOfMoney);

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var withdrawQuery = "UPDATE users SET balance = balance - (@amount + @fee) WHERE userid = @userid";
            using var withdrawCommand = new NpgsqlCommand(withdrawQuery, connection);
            withdrawCommand.Parameters.AddWithValue("userid", request.UserID);
            withdrawCommand.Parameters.AddWithValue("amount", request.UserAmountOfMoney);
            withdrawCommand.Parameters.AddWithValue("fee", fee);

            await withdrawCommand.ExecuteNonQueryAsync();

            await FeeHelper.LogTransactionFee(connection, request.UserID, fee, "Withdrawal");
            return Results.Ok($"Withdrawal completed successfully. {request.UserAmountOfMoney-fee}");
        }
    }

    public static class FeeHelper
    {
        public static decimal CalculateFee(decimal amount)
        {
            return amount * 0.01m; // 1% комиссии
        }

        public static async Task LogTransactionFee(NpgsqlConnection connection, long userID, decimal fee, string operationType)
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

        public override async Task<object> HandleAsync(UserRequestOperation request)
        {

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var checkBalanceQuery = "SELECT balance FROM users WHERE userid = @userid";
            using var checkBalanceCommand = new NpgsqlCommand(checkBalanceQuery, connection);
            checkBalanceCommand.Parameters.AddWithValue("userid", request.UserID);

            var balance = (decimal)await checkBalanceCommand.ExecuteScalarAsync();
            
            return Results.Ok($"Your ID : {request.UserID}; Funds in the account: {balance}");
            

        }
    }

}