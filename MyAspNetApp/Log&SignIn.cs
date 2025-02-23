using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using System.Text.RegularExpressions; 
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace LogSignIn 
{
//                             <----LogIn_Method---->                             //
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

public  class UserUniquenessHandler : Handler
{
    private readonly string _connectionString;

    public UserUniquenessHandler(string connectionString)
    {
        _connectionString = connectionString;
    }

    public override async Task<object> HandleAsync(object request)
    {
        var (userID, userSNP, userPhoneNumber, userPassword) = ((long, string, string, string))request;

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var checkUserQuery = "SELECT COUNT(*) FROM users WHERE userid = @userid";
        using var checkUserCommand = new NpgsqlCommand(checkUserQuery, connection);
        checkUserCommand.Parameters.AddWithValue("userid", userID);

        var userExists = (long)await checkUserCommand.ExecuteScalarAsync() > 0;
        if (userExists)
        {
            return Results.BadRequest("UserID уже зарегистрирован.");
        }

        return await base.HandleAsync(request);
    }
}

public class SnpValidationHandler : Handler
{
    public override async Task<object> HandleAsync(object request)
    {
        var (userID, userSNP, userPhoneNumber, userPassword) = ((long, string, string, string))request;

            // Проверка ФИО для русского и латинского алфавита
        bool isValidSNP = Regex.IsMatch(userSNP, @"^[А-ЯЁ][а-яё]+ [А-ЯЁ][а-яё]+ [А-ЯЁ][а-яё]+$") ||
                          Regex.IsMatch(userSNP, @"^[A-Z][a-z]+ [A-Z][a-z]+ [A-Z][a-z]+$");

        if (!isValidSNP)
        {
            return Results.BadRequest("ФИО должно содержать три слова с большой буквы, разделенные пробелами.");
        }

        // Если все проверки пройдены, вызываем базовый метод
        return await base.HandleAsync(request);
    }
}

public class PhoneNumberValidationHandler : Handler
{
    public override async Task<object> HandleAsync(object request)
    {
        var (userID, userSNP, userPhoneNumber, userPassword) = ((long, string, string, string))request;

        userPhoneNumber = userPhoneNumber.StartsWith("+38") ? userPhoneNumber[3..] : userPhoneNumber;//
        if (userPhoneNumber.Length != 10 || !Regex.IsMatch(userPhoneNumber, @"^\d{10}$"))
        {
            return Results.BadRequest("Номер телефона должен содержать ровно 10 цифр.");
        }

        return await base.HandleAsync(request);
    }
}

public class PasswordValidationHandler : Handler
{
    public override async Task<object> HandleAsync(object request)
    {
        var (userID, userSNP, userPhoneNumber, userPassword) = ((long, string, string, string))request;

        if (userPassword.Length < 9 || !Regex.IsMatch(userPassword, @"[0-9]") || !Regex.IsMatch(userPassword, @"[\W_]") )
        {
            return Results.BadRequest("Пароль должен быть длиной не менее 9 символов и содержать хотя бы одну цифру и один символ.");
        }

        return await base.HandleAsync(request);
    }
}

public class UserRegistrationHandler : Handler
{
    private readonly string _connectionString;

    public UserRegistrationHandler(string connectionString)
    {
        _connectionString = connectionString;
    }

    public override async Task<object> HandleAsync(object request)
    {
        var (userID, userSNP, userPhoneNumber, userPassword) = ((long, string, string, string))request;

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        userPhoneNumber = userPhoneNumber.StartsWith("+38") ? userPhoneNumber.Substring(3) : userPhoneNumber; //<----!---->//

        var insertQuery = @"INSERT INTO users (userid, fullname, phonenumber, passwordhash) VALUES (@userid, @fullname, @phonenumber, @passwordhash)";
        using var insertCommand = new NpgsqlCommand(insertQuery, connection);
        insertCommand.Parameters.AddWithValue("userid", userID);
        insertCommand.Parameters.AddWithValue("fullname", userSNP);
        insertCommand.Parameters.AddWithValue("phonenumber", userPhoneNumber);
        insertCommand.Parameters.AddWithValue("passwordhash", userPassword);

        await insertCommand.ExecuteNonQueryAsync();

        return Results.Ok("Регистрация успешна!");
    }
}


//                        <----Recovery&SignIn_Method---->                        //
public class UserExistenceHandler : Handler
{
    private readonly string _connectionString;

    public UserExistenceHandler(string connectionString)
    {
        _connectionString = connectionString;
    }

    public override async Task<object> HandleAsync(object request)
    {
        var (newUserID, oldUserID,  userPhoneNumber, userPassword) = ((long, string, string, string))request;

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        

        var checkOldUserQuery = "SELECT COUNT(*) FROM users WHERE userid = @userid";
        using var checkOldUserCommand = new NpgsqlCommand(checkOldUserQuery, connection);
        checkOldUserCommand.Parameters.AddWithValue("userid", Convert.ToInt64(oldUserID));

        var oldUserExists = (long)await checkOldUserCommand.ExecuteScalarAsync() > 0;
            if (!oldUserExists)
            {
                return Results.BadRequest("Старый UserID не найден.");
            }

        return await base.HandleAsync(request);
    }
}

public class PasswordExistenceHandler : Handler
{
    private readonly string _connectionString;

    public PasswordExistenceHandler(string connectionString)
    {
        _connectionString = connectionString;
    }

    public override async Task<object> HandleAsync(object request)
    {
        var (newUserID, oldUserID,  userPhoneNumber, userPassword) = ((long, string, string, string))request;

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var checkPasswordQuery = "SELECT passwordhash FROM users WHERE userid = @userid";
        using var checkPasswordCommand = new NpgsqlCommand(checkPasswordQuery, connection);
        checkPasswordCommand.Parameters.AddWithValue("userid", Convert.ToInt64(oldUserID));

        var storedPassword = (string)await checkPasswordCommand.ExecuteScalarAsync();
            if (storedPassword != userPassword)
            {
                return Results.BadRequest("Неверный пароль для старого UserID.");
            }

        return await base.HandleAsync(request);
    }
}

public class PhoneExistenceHandler : Handler
{
    private readonly string _connectionString;

    public PhoneExistenceHandler(string connectionString)
    {
        _connectionString = connectionString;
    }

    public override async Task<object> HandleAsync(object request)
    {
        var (newUserID, oldUserID,  userPhoneNumber, userPassword) = ((long, string, string, string))request;

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

   

        var checkPhoneQuery = "SELECT phonenumber FROM users WHERE userid = @userid";
        using var checkPhoneCommand = new NpgsqlCommand(checkPhoneQuery, connection);
        checkPhoneCommand.Parameters.AddWithValue("userid", Convert.ToInt64(oldUserID));

        var storedPhoneNumber = (string)await checkPhoneCommand.ExecuteScalarAsync();

       userPhoneNumber = userPhoneNumber.StartsWith("+38") ? userPhoneNumber.Substring(3) : userPhoneNumber; //<----!---->//

            if (storedPhoneNumber != userPhoneNumber)
            {
                return Results.BadRequest("Номер телефона не совпадает.");
            }

        return await base.HandleAsync(request);
       
    }
}

public class TwoFactorAuthenticationCodeSend: Handler
{

        private readonly string _connectionString;
        private readonly string _twilioAccountSid;
        private readonly string _twilioAuthToken;
        private readonly string _twilioPhoneNumber;

        public TwoFactorAuthenticationCodeSend( string twilioAccountSid, string twilioAuthToken, string twilioPhoneNumber, string connectionString)
        {
            _twilioAccountSid = twilioAccountSid;
            _twilioAuthToken = twilioAuthToken;
            _twilioPhoneNumber = twilioPhoneNumber;
            _connectionString = connectionString;
        }

    public override async Task<object> HandleAsync(object request)
    {
        var (newUserID, oldUserID,  userPhoneNumber, userPassword) = ((long, string, string, string))request;

        var randomCode = new Random().Next(100000, 999999);

        userPhoneNumber = userPhoneNumber.StartsWith("+38") ? userPhoneNumber.Substring(3) : userPhoneNumber;//<----!---->//

            TwilioClient.Init(_twilioAccountSid, _twilioAuthToken);
            var message = await MessageResource.CreateAsync(
                to: new PhoneNumber($"+38{userPhoneNumber}"),
                from: new PhoneNumber(_twilioPhoneNumber),
                body: $"Ваш код для восстановления аккаунта: {randomCode}"
            );
           
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var insertQuery = @"INSERT INTO twofactorauthenticationcodes (userid, codes, created_at) VALUES (@userid, @codes, NOW())";
        using var insertCommand = new NpgsqlCommand(insertQuery, connection);
        insertCommand.Parameters.AddWithValue("userid", Convert.ToInt64(oldUserID));
        insertCommand.Parameters.AddWithValue("codes", randomCode);

        await insertCommand.ExecuteNonQueryAsync();

        return Results.Ok(randomCode);
    }
}

public class TwoFactorAuthenticationCodeCheck : Handler
{
    private readonly string _connectionString;

    public TwoFactorAuthenticationCodeCheck(string connectionString)
    {
        _connectionString = connectionString;
    }

    public override async Task<object> HandleAsync(object request)
    {
        var (newUserID, oldUserID,  userCode, _) = ((long, string, string, string))request;

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var checkCodeQuery = "SELECT COUNT(*) FROM twofactorauthenticationcodes WHERE userid = @userid AND codes = @code";
        using var checkCodeCommand = new NpgsqlCommand(checkCodeQuery, connection);
        checkCodeCommand.Parameters.AddWithValue("userid", Convert.ToInt64(oldUserID));
        checkCodeCommand.Parameters.AddWithValue("code", userCode);

        var isCodeValid = (long)await checkCodeCommand.ExecuteScalarAsync() > 0;
        if (!isCodeValid)
        {
            return Results.BadRequest("Неверный код двухфакторной аутентификации.");
        }

        return await base.HandleAsync(request);
    }
}

public class UpdateAccountInformation : Handler
{
    private readonly string _connectionString;

    public UpdateAccountInformation(string connectionString)
    {
        _connectionString = connectionString;
    }

    public override async Task<object> HandleAsync(object request)
    {
        var (newUserID, oldUserID,  userPhoneNumber, userPassword) = ((long, string, string, string))request;

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        
        var updateQuery = @"UPDATE users SET userid = @newUserID WHERE userid = @oldUserID";
        using var updateCommand = new NpgsqlCommand(updateQuery, connection);
        updateCommand.Parameters.AddWithValue("newUserID", newUserID);
        updateCommand.Parameters.AddWithValue("oldUserID", Convert.ToInt64(oldUserID));

        var rowsAffected = await updateCommand.ExecuteNonQueryAsync();

        if (rowsAffected > 0)
        {
            return Results.Ok("Информация успешно обновлена. Вход совершен");
        }

        return Results.BadRequest("Ошибка при обновлении информации.");
    }
}

}
