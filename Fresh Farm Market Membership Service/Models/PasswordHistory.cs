namespace Fresh_Farm_Market_Membership_Service.Models
{
    public class PasswordHistory
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
