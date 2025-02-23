using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using System.Text.RegularExpressions;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Recovery
{
    public class AccountRecoveryController
    {
        private readonly string _connectionString;
        private readonly string _twilioAccountSid;
        private readonly string _twilioAuthToken;
        private readonly string _twilioPhoneNumber;

        public AccountRecoveryController(string connectionString, string twilioAccountSid, string twilioAuthToken, string twilioPhoneNumber)
        {
            _connectionString = connectionString;
            _twilioAccountSid = twilioAccountSid;
            _twilioAuthToken = twilioAuthToken;
            _twilioPhoneNumber = twilioPhoneNumber;
        }

        public async Task<object> RecoverAccount(int newUserID, int oldUserID, string userPhoneNumber, string userPassword)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            // Проверка существования старого ID
            var checkOldUserQuery = "SELECT COUNT(*) FROM users WHERE userid = @userid";
            using var checkOldUserCommand = new NpgsqlCommand(checkOldUserQuery, connection);
            checkOldUserCommand.Parameters.AddWithValue("userid", oldUserID);

            var oldUserExists = (long)await checkOldUserCommand.ExecuteScalarAsync() > 0;
            if (!oldUserExists)
            {
                return Results.BadRequest("Старый UserID не найден.");
            }

            // Проверка пароля для старого ID
            var checkPasswordQuery = "SELECT passwordhash FROM users WHERE userid = @userid";
            using var checkPasswordCommand = new NpgsqlCommand(checkPasswordQuery, connection);
            checkPasswordCommand.Parameters.AddWithValue("userid", oldUserID);

            var storedPassword = (string)await checkPasswordCommand.ExecuteScalarAsync();
            if (storedPassword != userPassword)
            {
                return Results.BadRequest("Неверный пароль для старого UserID.");
            }

            // Проверка номера телефона для старого ID
            var checkPhoneQuery = "SELECT phonenumber FROM users WHERE userid = @userid";
            using var checkPhoneCommand = new NpgsqlCommand(checkPhoneQuery, connection);
            checkPhoneCommand.Parameters.AddWithValue("userid", oldUserID);

            var storedPhoneNumber = (string)await checkPhoneCommand.ExecuteScalarAsync();
            if (storedPhoneNumber != userPhoneNumber)
            {
                return Results.BadRequest("Номер телефона не совпадает.");
            }

            // Генерация случайного кода
            var randomCode = new Random().Next(100000, 999999).ToString();

            // Отправка кода через Twilio
            TwilioClient.Init(_twilioAccountSid, _twilioAuthToken);
            var message = await MessageResource.CreateAsync(
                to: new PhoneNumber($"+38{userPhoneNumber}"),
                from: new PhoneNumber(_twilioPhoneNumber),
                body: $"Ваш код для восстановления аккаунта: {randomCode}"
            );

            return Results.Ok(randomCode);
        }

        public async Task<object> UpdateAccountInformation(int oldUserID, int newUserID)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            // Перезапись информации
            var updateQuery = @"UPDATE users SET userid = @newUserID WHERE userid = @oldUserID";
            using var updateCommand = new NpgsqlCommand(updateQuery, connection);
            updateCommand.Parameters.AddWithValue("newUserID", newUserID);
            updateCommand.Parameters.AddWithValue("oldUserID", oldUserID);

            var rowsAffected = await updateCommand.ExecuteNonQueryAsync();

            if (rowsAffected > 0)
            {
                return Results.Ok("Информация успешно обновлена.");
            }

            return Results.BadRequest("Ошибка при обновлении информации.");
        }
    }
}
