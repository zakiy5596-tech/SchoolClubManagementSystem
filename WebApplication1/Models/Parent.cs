using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public class Parent
    {
        public int ParentId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey(nameof(UserId))]
        public ApplicationUser? User { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        public ICollection<ParentStudent> Children { get; set; } = new List<ParentStudent>();
    }
}
