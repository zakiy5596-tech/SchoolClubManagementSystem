using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models.ViewModels
{
    public class ParentFormViewModel : IValidatableObject
    {
        public int? ParentId { get; set; }

        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Status is required.")]
        public UserStatus Status { get; set; } = UserStatus.Active;

        [DataType(DataType.Password)]
        public string? Password { get; set; }

        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        public string? ConfirmPassword { get; set; }

        public List<int> SelectedStudentIds { get; set; } = new();

        public List<Student> AllStudents { get; set; } = new();

        public bool IsEdit => ParentId.HasValue;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!IsEdit && string.IsNullOrWhiteSpace(Password))
            {
                yield return new ValidationResult("Password is required.", new[] { nameof(Password) });
            }
            else if (!string.IsNullOrEmpty(Password) && Password.Length < 6)
            {
                yield return new ValidationResult("Password must be at least 6 characters.", new[] { nameof(Password) });
            }
        }
    }
}
