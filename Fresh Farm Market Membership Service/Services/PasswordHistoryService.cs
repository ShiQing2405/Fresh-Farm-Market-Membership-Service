using Fresh_Farm_Market_Membership_Service.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Fresh_Farm_Market_Membership_Service.Services
{
    public interface IPasswordHistoryService
    {
        Task<bool> IsPasswordReusedAsync(string userId, string newPassword);
        Task AddPasswordHistoryAsync(string userId, string passwordHash);
    }

    public class PasswordHistoryService : IPasswordHistoryService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<ApplicationUser> _passwordHasher;
        private const int MaxPasswordHistory = 2;

        public PasswordHistoryService(ApplicationDbContext context, IPasswordHasher<ApplicationUser> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public async Task<bool> IsPasswordReusedAsync(string userId, string newPassword)
        {
            var passwordHistories = await _context.PasswordHistories
                .Where(ph => ph.UserId == userId)
                .OrderByDescending(ph => ph.CreatedAt)
                .Take(MaxPasswordHistory)
                .ToListAsync();

            foreach (var history in passwordHistories)
            {
                var result = _passwordHasher.VerifyHashedPassword(
                    null!, 
                    history.PasswordHash, 
                    newPassword
                );

                if (result == PasswordVerificationResult.Success || 
                    result == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    return true;
                }
            }

            return false;
        }

        public async Task AddPasswordHistoryAsync(string userId, string passwordHash)
        {
            var passwordHistory = new PasswordHistory
            {
                UserId = userId,
                PasswordHash = passwordHash,
                CreatedAt = DateTime.UtcNow
            };

            _context.PasswordHistories.Add(passwordHistory);
            await _context.SaveChangesAsync();
        }
    }
}
