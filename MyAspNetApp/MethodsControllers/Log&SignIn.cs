using Dto;
using Npgsql;
using System.Text.RegularExpressions; 
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace LogSignIn 
{
        public class UserRequest
    {
        public long UserID { get; set; }
        public long UserOldID { get; set; }
        public string UserSNP { get; set; }
        public string UserPhoneNumber { get; set; } 
        public string UserPassword { get; set; }
        public string UserCode { get; set; }
    }
//                             <----LogIn_Method---->                             //
public abstract class Handler
{
    private Handler _nextHandler;

    public Handler SetNext(Handler nextHandler)
    {
        _nextHandler = nextHandler;
        return nextHandler;
    }

    public virtual async Task<object> HandleAsync(UserRequest request)
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

    public override async Task<object> HandleAsync(UserRequest request)
    {
        System.Console.WriteLine(128);
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var checkUserQuery = "SELECT COUNT(*) FROM users WHERE userid = @userid";
        using var checkUserCommand = new NpgsqlCommand(checkUserQuery, connection);
        checkUserCommand.Parameters.AddWithValue("userid", request.UserID);

        var userExists = (long)await checkUserCommand.ExecuteScalarAsync() > 0;

        if (request.UserID == request.UserOldID && userExists)
        {
            return await base.HandleAsync(request);
        }
        else if (userExists)
        {
            return Results.BadRequest("UserID is already registered.");
        }

        return await base.HandleAsync(request);
    }

        internal async Task HandleAsync(UpdateAccountDto dto)
        {
            throw new NotImplementedException();
        }
    }

public class SnpValidationHandler : Handler
{
    public override async Task<object> HandleAsync(UserRequest request)
    {

            // Проверка ФИО для русского и латинского алфавита
        bool isValidSNP = Regex.IsMatch(request.UserSNP, @"^[А-ЯЁ][а-яё]+ [А-ЯЁ][а-яё]+ [А-ЯЁ][а-яё]+$") ||
                          Regex.IsMatch(request.UserSNP, @"^[A-Z][a-z]+ [A-Z][a-z]+ [A-Z][a-z]+$");

        if (!isValidSNP)
        {
            return Results.BadRequest("Full name must contain three capitalized words separated by spaces.");
        }

        // Если все проверки пройдены, вызываем базовый метод
        return await base.HandleAsync(request);
    }
}

public class PhoneNumberValidationHandler : Handler
{
    public override async Task<object> HandleAsync(UserRequest request)
    {
       

        request.UserPhoneNumber = request.UserPhoneNumber.StartsWith("+38") ? request.UserPhoneNumber[3..] : request.UserPhoneNumber;
        if (request.UserPhoneNumber.Length != 10 || !Regex.IsMatch(request.UserPhoneNumber, @"^\d{10}$"))
        {
            return Results.BadRequest("Phone number must contain exactly 10 digits.");
        }

        return await base.HandleAsync(request);
    }
}

public class PasswordValidationHandler : Handler
{
    public override async Task<object> HandleAsync(UserRequest request)
    {

        if (request.UserPassword.Length < 9 || !Regex.IsMatch(request.UserPassword, @"[0-9]") || !Regex.IsMatch(request.UserPassword, @"[\W_]") )
        {
            return Results.BadRequest("Password must be at least 9 characters long and contain at least one number and one special character.");
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

    public override async Task<object> HandleAsync(UserRequest request)
    {


        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        request.UserPhoneNumber = request.UserPhoneNumber.StartsWith("+38") ? request.UserPhoneNumber.Substring(3) : request.UserPhoneNumber; //<----!---->//

        var insertQuery = @"INSERT INTO users (userid, fullname, phonenumber, passwordhash) VALUES (@userid, @fullname, @phonenumber, @passwordhash)";
        using var insertCommand = new NpgsqlCommand(insertQuery, connection);
        insertCommand.Parameters.AddWithValue("userid", request.UserID);
        insertCommand.Parameters.AddWithValue("fullname", request.UserSNP);
        insertCommand.Parameters.AddWithValue("phonenumber", request.UserPhoneNumber);
        insertCommand.Parameters.AddWithValue("passwordhash", request.UserPassword);

        await insertCommand.ExecuteNonQueryAsync();

        return Results.Ok("Registration successful!");
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

    public override async Task<object> HandleAsync(UserRequest request)
    {

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        

        var checkOldUserQuery = "SELECT COUNT(*) FROM users WHERE userid = @userid";
        using var checkOldUserCommand = new NpgsqlCommand(checkOldUserQuery, connection);
        checkOldUserCommand.Parameters.AddWithValue("userid", request.UserOldID);

        var oldUserExists = (long)await checkOldUserCommand.ExecuteScalarAsync() > 0;
            if (!oldUserExists)
            {
                return Results.BadRequest("Old UserID not found.");
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

    public override async Task<object> HandleAsync(UserRequest request)
    {

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var checkPasswordQuery = "SELECT passwordhash FROM users WHERE userid = @userid";
        using var checkPasswordCommand = new NpgsqlCommand(checkPasswordQuery, connection);
        checkPasswordCommand.Parameters.AddWithValue("userid", request.UserOldID);

        var storedPassword = (string)await checkPasswordCommand.ExecuteScalarAsync();
            if (storedPassword != request.UserPassword)
            {
                return Results.BadRequest("Incorrect password for old UserID.");
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

    public override async Task<object> HandleAsync(UserRequest request)
    {

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

   

        var checkPhoneQuery = "SELECT phonenumber FROM users WHERE userid = @userid";
        using var checkPhoneCommand = new NpgsqlCommand(checkPhoneQuery, connection);
        checkPhoneCommand.Parameters.AddWithValue("userid", request.UserOldID);

        var storedPhoneNumber = (string)await checkPhoneCommand.ExecuteScalarAsync();

       request.UserPhoneNumber = request.UserPhoneNumber.StartsWith("+38") ? request.UserPhoneNumber.Substring(3) : request.UserPhoneNumber; //<----!---->//

            if (storedPhoneNumber != request.UserPhoneNumber)
            {
                return Results.BadRequest("Phone number does not match.");
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

    public override async Task<object> HandleAsync(UserRequest request)
    {

        var randomCode = new Random().Next(100000, 999999);

        request.UserPhoneNumber = request.UserPhoneNumber.StartsWith("+38") ? request.UserPhoneNumber.Substring(3) : request.UserPhoneNumber;//<----!---->//

            TwilioClient.Init(_twilioAccountSid, _twilioAuthToken);
            var message = await MessageResource.CreateAsync(
                to: new PhoneNumber($"+38{request.UserPhoneNumber}"),
                from: new PhoneNumber(_twilioPhoneNumber),
                body: $"Your account recovery code is: {randomCode}"
            );
           
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var insertQuery = @"INSERT INTO twofactorauthenticationcodes (userid, codes, created_at) VALUES (@userid, @codes, NOW())";
        using var insertCommand = new NpgsqlCommand(insertQuery, connection);
        insertCommand.Parameters.AddWithValue("userid", request.UserOldID);
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

    public override async Task<object> HandleAsync(UserRequest request)
    {

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var checkCodeQuery = "SELECT COUNT(*) FROM twofactorauthenticationcodes WHERE userid = @userid AND codes = @code";
        using var checkCodeCommand = new NpgsqlCommand(checkCodeQuery, connection);
        checkCodeCommand.Parameters.AddWithValue("userid", request.UserOldID);
        checkCodeCommand.Parameters.AddWithValue("code", request.UserCode);

        var isCodeValid = (long)await checkCodeCommand.ExecuteScalarAsync() > 0;
        if (!isCodeValid)
        {
            return Results.BadRequest("Invalid two-factor authentication code.");
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

    public override async Task<object> HandleAsync(UserRequest request)
    {

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        
        var updateQuery = @"UPDATE users SET userid = @newUserID WHERE userid = @oldUserID";
        using var updateCommand = new NpgsqlCommand(updateQuery, connection);
        updateCommand.Parameters.AddWithValue("newUserID", request.UserID);
        updateCommand.Parameters.AddWithValue("oldUserID",request.UserOldID);

        var rowsAffected = await updateCommand.ExecuteNonQueryAsync();

        if (rowsAffected > 0)
        {
            return Results.Ok("Information successfully updated. Login completed");
        }

        return Results.BadRequest("Error updating information.");
    }
}

}