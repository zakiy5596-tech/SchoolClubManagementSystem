using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public class Student
    {
        public int StudentId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey(nameof(UserId))]
        public ApplicationUser? User { get; set; }

        [Required, StringLength(30)]
        public string StudentNumber { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Phone { get; set; }

        public ICollection<ClubMembership> Memberships { get; set; } = new List<ClubMembership>();
    }
}
