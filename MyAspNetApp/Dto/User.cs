namespace Dto
{
    public class LoginDto
    {
        public long UserId { get; set; }
        public string UserSNP { get; set; }
        public string UserPhoneNumber { get; set; }
        public string UserPassword { get; set; }
    }

    public class SignInDto
    {
        public long NewUserId { get; set; }
        public long OldUserId { get; set; }
        public string UserPassword { get; set; }
    }

    public class AccountRecoveryDto
    {
        public long NewUserId { get; set; }
        public long OldUserId { get; set; }
        public string UserPhoneNumber { get; set; }
    }

    public class UpdateAccountDto
    {
        public long NewUserId { get; set; }
        public long OldUserId { get; set; }
        public string UserCode { get; set; }
    }
}