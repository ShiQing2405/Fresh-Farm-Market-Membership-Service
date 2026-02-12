namespace Fresh_Farm_Market_Membership_Service.Services
{
    public interface IEmailService
    {
        Task SendPasswordResetEmailAsync(string toEmail, string resetLink);
        Task SendEmailAsync(string toEmail, string subject, string htmlMessage);
    }
}
