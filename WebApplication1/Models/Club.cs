using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public class Club
    {
        public int ClubId { get; set; }

        [Required, StringLength(150)]
        public string ClubName { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required, StringLength(80)]
        public string Category { get; set; } = string.Empty;

        [Required]
        public int AdvisorId { get; set; }

        [ForeignKey(nameof(AdvisorId))]
        public Advisor? Advisor { get; set; }

        public DateTime DateCreated { get; set; } = DateTime.Now;

        public ICollection<ClubMembership> Memberships { get; set; } = new List<ClubMembership>();
        public ICollection<Event> Events { get; set; } = new List<Event>();
    }
}
