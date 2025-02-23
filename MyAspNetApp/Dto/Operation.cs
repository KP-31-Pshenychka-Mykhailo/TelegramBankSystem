
namespace Dto
{
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