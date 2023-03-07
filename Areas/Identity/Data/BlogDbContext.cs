using Blog.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Blog.Areas.Identity.Data;

public class BlogDbContext : IdentityDbContext<BlogUser>
{
    public BlogDbContext(DbContextOptions<BlogDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // Customize the ASP.NET Identity model and override the defaults if needed.
        // For example, you can rename the ASP.NET Identity table names and more.
        // Add your customizations after calling base.OnModelCreating(builder);
        

    }
    public DbSet<Article> Articles { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<ArticleLike> ArticleLikes { get; set; }
    
    public DbSet<CommentLike> CommentLikes { get; set; }

    public DbSet<View> Views { get; set; }
    public DbSet<RecommendedBy> RecommendedBy { get; set; }
    public DbSet<PinnedArticles> PinnedArticles { get; set; }


}