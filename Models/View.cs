using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Blog.Models
{

    public class View
    {
        [Key]
        [Column(Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [MaxLength(450)]
        public string UserId { get; set; }
       
        public Guid ArticleId { get; set; }

        [ForeignKey("ArticleId")]
        public Article Article { get; set; }
    }
}