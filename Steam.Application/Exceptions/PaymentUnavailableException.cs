namespace Steam.Application.Exceptions;

public class PaymentUnavailableException : Exception
{
    public PaymentUnavailableException() : base("Card payments are not available. No payment was taken and no paid games were added.") { }
}
