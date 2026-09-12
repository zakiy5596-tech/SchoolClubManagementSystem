using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models.ViewModels
{
    public class ProfileViewModel
    {
        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(150)]
        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public string? StudentNumber { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }
    }
}
