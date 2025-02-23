using Npgsql;
using Telegram.Bot;

namespace SendMessage
{
    public class TransactionMonitorService : BackgroundService
    {
        private readonly string _connectionString;
        private readonly TelegramBotClient _botClient;
        private readonly ILogger<TransactionMonitorService> _logger;

        public TransactionMonitorService(string connectionString, string botToken, ILogger<TransactionMonitorService> logger)
        {
            _connectionString = connectionString;
            _botClient = new TelegramBotClient(botToken);
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Transaction monitoring service started.");
            while (!stoppingToken.IsCancellationRequested)
            {
                await MonitorTransactions();
                await Task.Delay(10000, stoppingToken);
            }
        }

        private async Task MonitorTransactions()
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand("SELECT id, userid, fee, operation_type FROM transaction_fees WHERE date > NOW() - INTERVAL '10 seconds'", conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        long userId = reader.GetInt64(1);
                        decimal fee = reader.GetDecimal(2);
                        string operationType = reader.GetString(3);
                        await NotifyUser(userId, fee, operationType);
                    }
                }
            }
        }

        private async Task NotifyUser(long userId, decimal fee, string operationType)
        {
            string message = $"Your balance has changed. Operation: {operationType}, amount: {(fee*10)-fee}";
            long telegramChatId = await GetUserTelegramId(userId);
            if (telegramChatId != 0)
            {
                await _botClient.SendTextMessageAsync(telegramChatId, message);
            }
        }

        private async Task<long> GetUserTelegramId(long userId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand("SELECT userid FROM users WHERE userid = @userId", conn))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);
                    var result = await cmd.ExecuteScalarAsync();
                    return result != null ? Convert.ToInt64(result) : 0;
                }
            }
        }
    }
}