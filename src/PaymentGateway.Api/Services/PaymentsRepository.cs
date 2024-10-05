using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Services;

public interface IPaymentsRepository
{
    void Add(PostPaymentResponse payment);
    Task<PostPaymentResponse?> GetAsync(Guid id);
}

public class PaymentsRepository : IPaymentsRepository
{
    private readonly List<PostPaymentResponse> Payments = new();

    public void Add(PostPaymentResponse payment)
    {
        if (payment == null)
        {
            throw new ArgumentNullException(nameof(payment));
        }
        Payments.Add(payment);
    }

    public Task<PostPaymentResponse?> GetAsync(Guid id)
    {
        var payment = Payments.FirstOrDefault(p => p.Id == id);
        return Task.FromResult(payment);
    }
}