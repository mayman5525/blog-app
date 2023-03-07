using Blog.Areas.Identity.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Blog.Models
{
    public class Comment
    {
        [Key]
        [Column(Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid CommentId { get; set; }
        [Required]

        public Guid ArticleId { get; set; }

        [Required]

        public Guid ParentId { get; set; }


        [Required]
        public string Message { get; set; }

        [MaxLength(450)]

        public string AuthorId { get; set; }

        [Range(0, 2)]
        public int level { get; set; }

        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd,hh:mm tt}", ApplyFormatInEditMode = true)]
        public DateTime Created { get; set; } = DateTime.Now;
        [ForeignKey("AuthorId")]
        public virtual BlogUser Creator { get; set; }
        [ForeignKey("ArticleId")]
        public virtual Article Article { get; set; }
        [ForeignKey("ParentId")]
        public virtual ICollection<Comment>? SubComments { get; set; }
        


    }
}