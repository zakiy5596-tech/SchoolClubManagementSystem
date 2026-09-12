using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models.ViewModels
{
    public class ClubFormViewModel
    {
        public int? ClubId { get; set; }

        [Required(ErrorMessage = "Club name is required.")]
        [StringLength(150)]
        public string ClubName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required.")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category is required.")]
        [StringLength(80)]
        public string Category { get; set; } = string.Empty;

        [Required(ErrorMessage = "Advisor is required.")]
        public int AdvisorId { get; set; }

        public List<Advisor> Advisors { get; set; } = new();

        public bool IsEdit => ClubId.HasValue;
    }
}
