using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public enum MembershipStatus
    {
        Pending,
        Approved,
        Rejected
    }

    public class ClubMembership
    {
        [Key]
        public int MembershipId { get; set; }

        [Required]
        public int StudentId { get; set; }

        [ForeignKey(nameof(StudentId))]
        public Student? Student { get; set; }

        [Required]
        public int ClubId { get; set; }

        [ForeignKey(nameof(ClubId))]
        public Club? Club { get; set; }

        public DateTime? DateJoined { get; set; }

        public MembershipStatus Status { get; set; } = MembershipStatus.Pending;

        public DateTime RequestedAt { get; set; } = DateTime.Now;
    }
}
