using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using System.Text.RegularExpressions; 

namespace LogSignIn 
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

public  class UserUniquenessHandler : Handler
{
    private readonly string _connectionString;

    public UserUniquenessHandler(string connectionString)
    {
        _connectionString = connectionString;
    }

    public override async Task<object> HandleAsync(object request)
    {
        var (userID, userSNP, userPhoneNumber, userPassword) = ((int, string, string, string))request;

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
        var (userID, userSNP, userPhoneNumber, userPassword) = ((int, string, string, string))request;

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
        var (userID, userSNP, userPhoneNumber, userPassword) = ((int, string, string, string))request;

        userPhoneNumber = userPhoneNumber.StartsWith("+38") ? userPhoneNumber[3..] : userPhoneNumber;
        if (userPhoneNumber.Length != 10 || !Regex.IsMatch(userPhoneNumber, @"^\d{10}$"))
        {
            return Results.BadRequest("Номер телефона должен содержать ровно 10 цифр (без +38).");
        }

        return await base.HandleAsync(request);
    }
}

public class PasswordValidationHandler : Handler
{
    public override async Task<object> HandleAsync(object request)
    {
        var (userID, userSNP, userPhoneNumber, userPassword) = ((int, string, string, string))request;

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
        var (userID, userSNP, userPhoneNumber, userPassword) = ((int, string, string, string))request;

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

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

public class UserExistenceHandler : Handler
{
    private readonly string _connectionString;

    public UserExistenceHandler(string connectionString)
    {
        _connectionString = connectionString;
    }

    public override async Task<object> HandleAsync(object request)
    {
        var (userID, userPassword) = ((int, string))request;

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var checkUserQuery = "SELECT COUNT(*) FROM users WHERE userid = @userid";
        using var checkUserCommand = new NpgsqlCommand(checkUserQuery, connection);
        checkUserCommand.Parameters.AddWithValue("userid", userID);

        var userExists = (long)await checkUserCommand.ExecuteScalarAsync() > 0;
        if (!userExists)
        {
            return Results.BadRequest("Пользователь с таким ID не найден.");
        }

        return await base.HandleAsync(request);
    }
}

public class PasswordCheckHandler : Handler
{
    private readonly string _connectionString;

    public PasswordCheckHandler(string connectionString)
    {
        _connectionString = connectionString;
    }

    public override async Task<object> HandleAsync(object request)
    {
        var (userID, userPassword) = ((int, string))request;

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var getPasswordQuery = "SELECT passwordhash FROM users WHERE userid = @userid";
        using var getPasswordCommand = new NpgsqlCommand(getPasswordQuery, connection);
        getPasswordCommand.Parameters.AddWithValue("userid", userID);

        var storedPassword = (string)await getPasswordCommand.ExecuteScalarAsync();

        if (storedPassword != userPassword)
        {
            return Results.BadRequest("Неверный пароль.");
        }

        return Results.Ok("Аутентификация успешна!");
    }
}


}

