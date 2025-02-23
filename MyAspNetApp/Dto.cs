using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace Dto
{
    public interface IValidatable
    {
        public List<string> Validate();
    }

    public class LogInDto : IValidatable
    {
        [Required(ErrorMessage = "User ID is required.")]
        public long UserID { get; set; }
        
        [Required(ErrorMessage = "UserSNP is required.")]
        [MinLength(3, ErrorMessage = "UserSNP must be at least 3 characters long.")]
        public string UserSNP { get; set; }
        
        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Invalid phone number format.")]
        public string UserPhoneNumber { get; set; }
        
        [Required(ErrorMessage = "Password is required.")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
        public string UserPassword { get; set; }

        public List<string> Validate()
        {
            return ValidationHelper.ValidateModel(this);
        }
    }

    public class SignInDto : IValidatable
    {
        [Required(ErrorMessage = "New User ID is required.")]
        public long NewUserID { get; set; }
        
        [Required(ErrorMessage = "Old User ID is required.")]
        public string OldUserID { get; set; }
        
        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Invalid phone number format.")]
        public string UserPhoneNumber { get; set; }
        
        [Required(ErrorMessage = "Password is required.")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
        public string UserPassword { get; set; }

        public List<string> Validate()
        {
            return ValidationHelper.ValidateModel(this);
        }
    }

    public class AccountRecoveryDto : IValidatable
    {
        [Required(ErrorMessage = "New User ID is required.")]
        public long NewUserID { get; set; }
        
        [Required(ErrorMessage = "Old User ID is required.")]
        public string OldUserID { get; set; }
        
        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Invalid phone number format.")]
        public string UserPhoneNumber { get; set; }
        
        [Required(ErrorMessage = "Password is required.")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
        public string UserPassword { get; set; }

        public List<string> Validate()
        {
            return ValidationHelper.ValidateModel(this);
        }
    }

    public class TransferDto : IValidatable
    {
        [Required(ErrorMessage = "User ID is required.")]
        public long UserID { get; set; }
        
        [Required(ErrorMessage = "Amount is required.")]
        [Range(1, long.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        public long AmountOfMoney { get; set; }
        
        [Required(ErrorMessage = "Recipient ID is required.")]
        public long RecientID { get; set; }

        public List<string> Validate()
        {
            return ValidationHelper.ValidateModel(this);
        }
    }

    public class ReplenishmentDto : IValidatable
    {
        [Required(ErrorMessage = "User ID is required.")]
        public long UserID { get; set; }
        
        [Required(ErrorMessage = "Amount is required.")]
        [Range(1, long.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        public long AmountOfMoney { get; set; }

        public List<string> Validate()
        {
            return ValidationHelper.ValidateModel(this);
        }
    }

    public class WithdrawalDto : IValidatable
    {
        [Required(ErrorMessage = "User ID is required.")]
        public long UserID { get; set; }
        
        [Required(ErrorMessage = "Amount is required.")]
        [Range(1, long.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        public long AmountOfMoney { get; set; }

        public List<string> Validate()
        {
            return ValidationHelper.ValidateModel(this);
        }
    }

    public class ShowInformationDto : IValidatable
    {
        [Required(ErrorMessage = "User ID is required.")]
        public long UserID { get; set; }

        public List<string> Validate()
        {
            return ValidationHelper.ValidateModel(this);
        }
    }

    public static class ValidationHelper
    {
        public static List<string> ValidateModel(object obj)
        {
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(obj);
            Validator.TryValidateObject(obj, validationContext, validationResults, true);
            return validationResults.Select(vr => vr.ErrorMessage).ToList();
        }
    }

}
