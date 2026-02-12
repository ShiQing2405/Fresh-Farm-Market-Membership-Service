using Microsoft.AspNetCore.Identity;

namespace Fresh_Farm_Market_Membership_Service.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public string EncryptedCreditCardNo { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string MobileNo { get; set; } = string.Empty;
        public string DeliveryAddress { get; set; } = string.Empty;
        public string? PhotoPath { get; set; }
        public string? AboutMe { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastPasswordChangedDate { get; set; }
        public DateTime? PasswordExpiryDate { get; set; }
    }
}
