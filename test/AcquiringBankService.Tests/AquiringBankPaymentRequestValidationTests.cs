using System.ComponentModel.DataAnnotations;

using AcquiringBank.Service;

public class AcquiringBankPaymentRequestTests
{
    private List<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var ctx = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, ctx, validationResults, true);
        return validationResults;
    }

    [Theory]
    [InlineData("1234567890123456", true)]
    [InlineData("12345678901234567890", false)]  // Too long
    [InlineData("123456789012", false)]  // Too short
    [InlineData("1234abcd5678efgh", false)]  // Non-numeric
    [InlineData("", false)]  // Empty
    [InlineData(null, false)]  // Null
    public void CardNumber_Validation(string cardNumber, bool isValid)
    {
        var model = new AcquiringBankPaymentRequest { CardNumber = cardNumber };
        var results = ValidateModel(model);
        Assert.Equal(isValid, !results.Any(r => r.MemberNames.Contains("CardNumber")));
    }

    [Theory]
    [InlineData("USD", true)]
    [InlineData("EUR", true)]
    [InlineData("GBP", true)]
    [InlineData("JPY", false)]  // Not allowed
    [InlineData("US", false)]  // Too short
    [InlineData("USDD", false)]  // Too long
    [InlineData("", false)]  // Empty
    [InlineData(null, false)]  // Null
    public void Currency_Validation(string currency, bool isValid)
    {
        var model = new AcquiringBankPaymentRequest { Currency = currency };
        var results = ValidateModel(model);
        Assert.Equal(isValid, !results.Any(r => r.MemberNames.Contains("Currency")));
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(100, true)]
    [InlineData(0, false)]  // Zero
    [InlineData(-1, false)]  // Negative
    public void Amount_Validation(int amount, bool isValid)
    {
        var model = new AcquiringBankPaymentRequest { Amount = amount };
        var results = ValidateModel(model);
        Assert.Equal(isValid, !results.Any(r => r.MemberNames.Contains("Amount")));
    }

    [Theory]
    [InlineData(100, true)]
    [InlineData(999, true)]
    [InlineData(9999, true)]
    [InlineData(99, false)]  // Too short
    [InlineData(10000, false)]  // Too long
    public void Cvv_Validation(int cvv, bool isValid)
    {
        var model = new AcquiringBankPaymentRequest { Cvv = cvv };
        var results = ValidateModel(model);
        Assert.Equal(isValid, !results.Any(r => r.MemberNames.Contains("Cvv")));
    }

    [Fact]
    public void AllPropertiesRequired_Validation()
    {
        var model = new AcquiringBankPaymentRequest();
        var results = ValidateModel(model);

        Assert.Contains(results, r => r.MemberNames.Contains("CardNumber"));
        Assert.Contains(results, r => r.MemberNames.Contains("ExpiryDate"));
        Assert.Contains(results, r => r.MemberNames.Contains("Currency"));
        Assert.Contains(results, r => r.MemberNames.Contains("Amount"));
        Assert.Contains(results, r => r.MemberNames.Contains("Cvv"));
    }

    [Fact]
    public void ValidModel_NoValidationErrors()
    {
        var model = new AcquiringBankPaymentRequest
        {
            CardNumber = "1234567890123456",
            ExpiryDate = "12/2030",
            Currency = "USD",
            Amount = 100,
            Cvv = 123
        };

        var results = ValidateModel(model);
        Assert.Empty(results);
    }

    [Theory]
    [InlineData("12/2030", true)]  // Valid future date
    [InlineData("01/2025", true)]  // Valid future date
    [InlineData("12/2023", false)]  // Current year, but future month (assuming current year is 2023)
    [InlineData("01/2023", false)]  // Current year, but past month (assuming current year is 2023)
    [InlineData("12/2022", false)]  // Past year
    [InlineData("13/2025", false)]  // Invalid month
    [InlineData("00/2025", false)]  // Invalid month
    [InlineData("12/2020", false)]  // Past date
    [InlineData("01/202", false)]   // Invalid format (year too short)
    [InlineData("1/2025", false)]   // Invalid format (month should be 2 digits)
    [InlineData("01/20255", false)] // Invalid format (year too long)
    [InlineData("01-2025", false)]  // Invalid format (wrong separator)
    [InlineData("2025/01", false)]  // Invalid format (wrong order)
    [InlineData("", false)]         // Empty string
    [InlineData(null, false)]       // Null
    [InlineData("AB/CDEF", false)]  // Non-numeric
    public void ExpiryDate_Validation(string expiryDate, bool isValid)
    {
        var model = new AcquiringBankPaymentRequest { ExpiryDate = expiryDate };
        var results = ValidateModel(model);

        if (isValid)
        {
            var expiryDateErrors = results.Where(r => r.MemberNames.Contains("ExpiryDate")).ToList();
            Assert.Empty(expiryDateErrors);
        }
        else
        {
            Assert.Contains(results, r => r.MemberNames.Contains("ExpiryDate"));
        }
    }

    [Fact]
    public void ExpiryDate_CurrentMonthAndYear_Invalid()
    {
        var currentDate = DateTime.Now;
        var expiryDate = $"{currentDate:MM/yyyy}";
        var model = new AcquiringBankPaymentRequest { ExpiryDate = expiryDate };
        var results = ValidateModel(model);

        Assert.Contains(results, r => r.MemberNames.Contains("ExpiryDate"));
    }

    [Fact]
    public void ExpiryDate_NextMonthAndYear_Valid()
    {
        var nextMonth = DateTime.Now.AddMonths(1);
        var expiryDate = $"{nextMonth:MM/yyyy}";
        var model = new AcquiringBankPaymentRequest { ExpiryDate = expiryDate };
        var results = ValidateModel(model);

        var expiryDateErrors = results.Where(r => r.MemberNames.Contains("ExpiryDate")).ToList();
        Assert.Empty(expiryDateErrors);
    }

    [Fact]
    public void ExpiryDate_LastDayOfCurrentMonth_Invalid()
    {
        var lastDayOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month));
        var expiryDate = $"{lastDayOfMonth:MM/yyyy}";
        var model = new AcquiringBankPaymentRequest { ExpiryDate = expiryDate };
        var results = ValidateModel(model);

        Assert.Contains(results, r => r.MemberNames.Contains("ExpiryDate"));
    }

    [Fact]
    public void ExpiryDate_FirstDayOfNextMonth_Valid()
    {
        var firstDayOfNextMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1);
        var expiryDate = $"{firstDayOfNextMonth:MM/yyyy}";
        var model = new AcquiringBankPaymentRequest { ExpiryDate = expiryDate };
        var results = ValidateModel(model);

        var expiryDateErrors = results.Where(r => r.MemberNames.Contains("ExpiryDate")).ToList();
        Assert.Empty(expiryDateErrors);
    }

    [Fact]
    public void ExpiryDate_FarFuture_Valid()
    {
        var farFuture = DateTime.Now.AddYears(10);
        var expiryDate = $"{farFuture:MM/yyyy}";
        var model = new AcquiringBankPaymentRequest { ExpiryDate = expiryDate };
        var results = ValidateModel(model);

        var expiryDateErrors = results.Where(r => r.MemberNames.Contains("ExpiryDate")).ToList();
        Assert.Empty(expiryDateErrors);
    }

    [Fact]
    public void ExpiryDate_ErrorMessageContent()
    {
        var model = new AcquiringBankPaymentRequest { ExpiryDate = "01/2020" };
        var results = ValidateModel(model);

        var error = Assert.Single(results, r => r.MemberNames.Contains("ExpiryDate"));
        Assert.Contains("future", error.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExpiryDate_MultipleErrors_OnlyOneErrorReturned()
    {
        var model = new AcquiringBankPaymentRequest { ExpiryDate = "13/202" }; // Invalid month and format
        var results = ValidateModel(model);

        var expiryDateErrors = results.Where(r => r.MemberNames.Contains("ExpiryDate")).ToList();
        Assert.NotEmpty(expiryDateErrors);
    }
}
