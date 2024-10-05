using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace AcquiringBank.Service;

public class ExpiryDateInFutureValidationAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        string expiryDateStr = value?.ToString();
        if (string.IsNullOrEmpty(expiryDateStr))
        {
            return new ValidationResult(ErrorMessage ?? "Expiry date is required.",
                new[] { validationContext.MemberName });
        }

        if (DateTime.TryParseExact(expiryDateStr, "MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var expiryDate))
        {
            // Get the first day of the expiry month
            var firstDayOfExpiryMonth = new DateTime(expiryDate.Year, expiryDate.Month, 1);

            if (firstDayOfExpiryMonth < DateTime.Now)
            {
                return new ValidationResult(ErrorMessage ?? "Expiry date must be in the future.",
                    new[] { validationContext.MemberName });
            }

            return ValidationResult.Success;
        }

        return new ValidationResult(ErrorMessage ?? "Invalid expiry date format. Use MM/yyyy.",
            new[] { validationContext.MemberName });
    }
}
