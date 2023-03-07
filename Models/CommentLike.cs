using Blog.Areas.Identity.Data;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Blog.Models
{
    
    public class CommentLike
    {
        [Key]
        [Column(Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        [MaxLength(450)]
        public string UserId { get; set; }
        
        public Guid CommentId { get; set; }

        [ForeignKey("CommentId")]
        public Comment Comment { get; set; }

    }
}