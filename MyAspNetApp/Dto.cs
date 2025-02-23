
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
    public string OldUserId { get; set; }
    public string UserPhoneNumber { get; set; }
    public string UserPassword { get; set; }
}

public class AccountRecoveryDto
{
    public long NewUserId { get; set; }
    public string OldUserId { get; set; }
    public string UserPhoneNumber { get; set; }
    public string UserPassword { get; set; }
}

public class UpdateAccountDto
{
    public long NewUserId { get; set; }
    public string OldUserId { get; set; }
    public string UserCode { get; set; }
    public string UserPassword { get; set; }
}


public class TransferDto
{
    public long UserId { get; set; }
    public long AmountOfMoney { get; set; }
    public long RecipientId { get; set; }
}

public class ReplenishmentDto
{
    public long UserId { get; set; }
    public long AmountOfMoney { get; set; }
}

public class WithdrawalDto
{
    public long UserId { get; set; }
    public long AmountOfMoney { get; set; }
}
}

