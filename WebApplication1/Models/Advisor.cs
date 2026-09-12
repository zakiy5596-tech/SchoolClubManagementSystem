using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public class Advisor
    {
        public int AdvisorId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey(nameof(UserId))]
        public ApplicationUser? User { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        public ICollection<Club> Clubs { get; set; } = new List<Club>();
    }
}
