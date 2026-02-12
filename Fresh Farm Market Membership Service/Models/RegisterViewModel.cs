namespace Fresh_Farm_Market_Membership_Service.Models
{
    using System.ComponentModel.DataAnnotations;
    using Microsoft.AspNetCore.Http;

    public class RegisterViewModel
    {
        [Required]
        [Display(Name = "Full Name")]
        [RegularExpression(@"^[A-Za-z\s]+$", ErrorMessage = "Full Name can only contain letters and spaces.")]
        public string FullName { get; set; }

        [Required]
        [Display(Name = "Credit Card No")]
        [RegularExpression(@"^\d{12,19}$", ErrorMessage = "Credit Card No must be 12-19 digits.")]
        public string CreditCardNo { get; set; }

        [Required]
        [RegularExpression(@"^(Male|Female|Other)$", ErrorMessage = "Invalid gender.")]
        public string Gender { get; set; }

        [Required]
        [Display(Name = "Mobile No")]
        [RegularExpression(@"^\d{8,15}$", ErrorMessage = "Mobile No must be numeric and 8-15 digits.")]
        public string MobileNo { get; set; }

        [Required]
        [Display(Name = "Delivery Address")]
        [StringLength(100, ErrorMessage = "Delivery Address is too long.")]
        [RegularExpression(@"^[^<>]*$", ErrorMessage = "Delivery Address cannot contain angle brackets.")]
        public string DeliveryAddress { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email address")]
        public string Email { get; set; }

        [Required]
        [MinLength(12)]
        [Display(Name = "Password")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z\d]).{12,}$", ErrorMessage = "Password must be strong.")]
        public string Password { get; set; }

        [Required]
        [Display(Name = "Confirm Password")]
        [Compare("Password")]
        public string ConfirmPassword { get; set; }

        [Required]
        [Display(Name = "Photo (.JPG only)")]
        public IFormFile Photo { get; set; }

        [Display(Name = "About Me")]
        [StringLength(500, ErrorMessage = "About Me is too long.")]
        // No regex: allow all special characters, but sanitize/encode on output
        public string AboutMe { get; set; }
    }
}
