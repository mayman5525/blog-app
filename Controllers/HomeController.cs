using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Blog.Models;
using Blog.Data.FileManager;
using Blog.Data.Repository;
using Microsoft.AspNetCore.Identity;
using Blog.Areas.Identity.Data;
using Microsoft.AspNetCore.Authorization;

namespace Blog.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly SignInManager<BlogUser> _signInManager;
    private readonly UserManager<BlogUser> _userManager;
    private IRepository _repo;
    private IFileManager _fileManager;

    public HomeController(ILogger<HomeController> logger,
        SignInManager<BlogUser> signInManager,
        UserManager<BlogUser> userManager,
        IRepository repo,
        IFileManager fileManager)
    {
        _logger = logger;
        _signInManager = signInManager;
        _userManager = userManager;
        _repo = repo;
        _fileManager = fileManager;
    }

    public IActionResult Index(int pageNumber, string category, string search)
    {
        
        category = System.Net.WebUtility.HtmlEncode(category);
        search = System.Net.WebUtility.HtmlEncode(search);
        var vm = _repo.GetIndexViewModel(pageNumber, category, search, _userManager.GetUserId(User));
        return View(vm);
    }
    [Authorize]
    public async Task<IActionResult> Article(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }
        Guid Id = id.Value;
        var user = await _userManager.GetUserAsync(User);
        var UserId = await _userManager.GetUserIdAsync(user);
        _repo.AddView(Id, UserId);
        await _repo.SaveChangesAsync();
        var vm = _repo.GetArticleViewModel(Id,UserId);
        if (vm.NotFound)
        {
            return NotFound();
        }
        return View(vm);
    }
    public async Task<IActionResult> AddArticle()
    {
        var user = await _userManager.GetUserAsync(User);
        var UserId = await _userManager.GetUserIdAsync(user);
        if (User.IsInRole("BlogOwner") | User.IsInRole("Admin") | user.PlanType == "Premium")
        {
            return View();
        }
        TempData["Message"] = "warning: Only premiums who can post an article";
        return RedirectToAction("Index");
    }
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddArticle(string Title
                                                ,string GenreName
                                                ,string Categories
                                                ,string Level
                                                ,string Description
                                                ,string Content)
    {
        Article article = new Article
        {
            Title = Title,
            GenreName = GenreName,
            Categories = Categories,
            Level = Level,
            Description = Description,
            Content = Content
        };
        var user = await _userManager.GetUserAsync(User);
        var UserId = await _userManager.GetUserIdAsync(user);
        if (User.IsInRole("BlogOwner") | User.IsInRole("Admin"))
        {
            await PostArticle(article, UserId);
            return RedirectToAction("Article", new { id = article.ArticleId });
        }
        if (user.PlanType == "Premium")
        {
            if (_repo.IsAllowedToPost(UserId))
            {
                await PostArticle(article, UserId);
                return RedirectToAction("Article", new { id = article.ArticleId });
            }
            else
            {
                TempData["Message"] = "warning: You have exceeded your monthly allowed articles";
                return RedirectToAction("Index");
            }
        }
        else
        {
            TempData["Message"] = "warning: Only premiums who can post an article";
            return RedirectToAction("Index");
        }
    }

    private async Task PostArticle(Article article, string UserId)
    {
        article.ArticleId = Guid.NewGuid();
        article.Title = System.Net.WebUtility.HtmlEncode(article.Title);
        article.Description = System.Net.WebUtility.HtmlEncode(article.Description);
        article.GenreName = System.Net.WebUtility.HtmlEncode(article.GenreName);
        article.Created = DateTime.Now;
        article.LastUpdated = DateTime.Now;
        article.Pinned = false;
        article.Recommended = false;
        article.Categories = System.Net.WebUtility.HtmlEncode(article.Categories);
        article.Content = System.Net.WebUtility.HtmlEncode(article.Content);
        article.Level = System.Net.WebUtility.HtmlEncode(article.Level);
        article.AuthorId = UserId;
        _repo.AddArticle(article);
        await _repo.SaveChangesAsync();
        TempData["Message"] = "success: Successfully Created a new article";

    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Comment([Bind("ArticleId,ParentId,Message")] Comment comment)
    {

        comment.Message = System.Net.WebUtility.HtmlEncode(comment.Message);
        comment.CommentId = Guid.NewGuid();
        comment.Created = DateTime.Now;
        if (String.IsNullOrEmpty(comment.Message))
        {
            TempData["Message"] = "warning: Empty comments are not allowed";
            return RedirectToAction("Article", new { id = comment.ArticleId });
        }
        var user = await _userManager.GetUserAsync(User);
        comment.AuthorId = await _userManager.GetUserIdAsync(user);
        var addCommend = _repo.AddComment(comment);
        if (addCommend == "success")
        {
            await _repo.SaveChangesAsync();
            TempData["Message"] = "success: Successfully Added The comment";
            return RedirectToAction("Article", new { id = comment.ArticleId });
        }
        else if(addCommend == "parentNotFound")
        {
            TempData["Message"] = "warning: You are trying to add a subcomment to non existanct comment";
            return RedirectToAction("Article", new { id = comment.ArticleId });
        }
        else if(addCommend == "articleNotFound")
        {
            TempData["Message"] = "warning: You are trying to add a comment to non existanct article";
            return RedirectToAction("Index");
        }
        else
        {
            TempData["Message"] = "warning: The main comment reached its subcomment limit";
            TempData["Message"] = "warning: The main comment reached its subcomment limit";
            return RedirectToAction("Article", new { id = comment.ArticleId });
        }
        
        

        
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> CommentLike(Guid CommentId)
    {

        var user = await _userManager.GetUserAsync(User);
        var UserId = await _userManager.GetUserIdAsync(user);
        
        if (_repo.AddCommentLike(CommentId, UserId))
        {
            await _repo.SaveChangesAsync();
            return RedirectToAction("Article", new { id = _repo.GetArticleId(CommentId) });
        }
        TempData["Message"] = "warning: The comment you are trying to like is not exist";
        return RedirectToAction("Index");

    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> ArticlePin(Guid ArticleId)
    {

        var user = await _userManager.GetUserAsync(User);
        var UserId = await _userManager.GetUserIdAsync(user);

        if (_repo.LocalPin(UserId, ArticleId) == "Added")
        {
            await _repo.SaveChangesAsync();
            TempData["Message"] = "success: Successfully pinned";
            return RedirectToAction("Article", new { id = ArticleId });
        }
        else if(_repo.LocalPin(UserId, ArticleId) == "Removed")
        {
            await _repo.SaveChangesAsync();
            TempData["Message"] = "success: Successfully unpinned";
            return RedirectToAction("Article", new { id = ArticleId });
        }
        TempData["Message"] = "warning: The Article you are trying to pin is not exist";
        return RedirectToAction("Index");

    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> ArticleLike(Guid ArticleId)
    {
        var user = await _userManager.GetUserAsync(User);
        var UserId = await _userManager.GetUserIdAsync(user);

        if (_repo.AddArticleLike(ArticleId, UserId))
        {
            await _repo.SaveChangesAsync();
           
            return RedirectToAction("Article", new { id = ArticleId });
        }
        TempData["Message"] = "warning: The article you are trying to like is not exist";
        return RedirectToAction("Index");
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Genre(string GenreName)
    {
        if (GenreName != null)
        {
            var id = _repo.GetFirstArticleIdByGenre(GenreName);
            if (id != null)
            {

                return RedirectToAction("Article", new {id = id});
            }
            return NotFound();
        }
        return NotFound();
    }
    

    public async Task<IActionResult> UpdateArticleAsync(Guid id)
    {
        var article = _repo.GetArticle(id);
        if (article != null )
        {
            var user = await _userManager.GetUserAsync(User);
            var UserId = await _userManager.GetUserIdAsync(user);
            if (article.AuthorId == UserId & User.IsInRole("BlogOwner") | User.IsInRole("Admin"))
            {
                return View(article);
            }
            TempData["Message"] = "warning: You are not authorized to Update this article";
            return RedirectToAction("Article", new { id = id });
        }
        return NotFound();
    }
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateArticle(Guid id,[Bind("ArticleId"
                                                    , "Title"
                                                    , "GenreName"
                                                    , "Categories"
                                                    , "Level"
                                                    , "Description"
                                                    ,"Content")] Article article)
    {
        if (id != article.ArticleId)
        {
            return NotFound();
        }

            var user = await _userManager.GetUserAsync(User);
            var UserId = await _userManager.GetUserIdAsync(user);
            var Article = _repo.GetArticle(article.ArticleId);
            if (Article != null)
            {
                if (Article.AuthorId == UserId | User.IsInRole("BlogOwner") | User.IsInRole("Admin"))
                {
                    Article.Title = System.Net.WebUtility.HtmlEncode(article.Title);
                    Article.Description = System.Net.WebUtility.HtmlEncode(article.Description);
                    Article.GenreName = System.Net.WebUtility.HtmlEncode(article.GenreName);
                    Article.Categories = System.Net.WebUtility.HtmlEncode(article.Categories);
                    Article.Content = System.Net.WebUtility.HtmlEncode(article.Content);
                    Article.Level = System.Net.WebUtility.HtmlEncode(article.Level);
                    _repo.UpdateArticle(Article);
                    await _repo.SaveChangesAsync();
                    TempData["Message"] = "success: Successfully updeated ";
                    return RedirectToAction("Article", new { id = article.ArticleId });
                }
                else
                {
                    TempData["Message"] = "warning: You are not authorized to update this article ";
                    return RedirectToAction("Article", new { id = article.ArticleId });
                }
           
            }
            else
            {
                return NotFound();
            }

        
    }
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveArticle(Guid id)
    {
        
        var user = await _userManager.GetUserAsync(User);
        var UserId = await _userManager.GetUserIdAsync(user);
        var article = _repo.GetArticle(id);
        if (article != null)
        {
            if (article.AuthorId == UserId | User.IsInRole("BlogOwner") | User.IsInRole("Admin"))
            {
                _repo.RemoveArticle(id);
                await _repo.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            else
            {
                TempData["Message"] = "warning: You are not authorized to remove this article ";
                return RedirectToAction("Article", new { id = article.ArticleId });
            }
        }
        else
        {
            return NotFound();
        }
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveComment(Guid id)
    {
        
        var user = await _userManager.GetUserAsync(User);
        var UserId = await _userManager.GetUserIdAsync(user);
        var comment = _repo.GetComment(id);
        
        if (comment != null)
        {
            if (comment.AuthorId == UserId | User.IsInRole("BlogOwner") | User.IsInRole("Admin"))
            {
                _repo.RemoveComment(id);
                await _repo.SaveChangesAsync();
                return RedirectToAction("Article", new { id = comment.ArticleId } );
            }
            else
            {
                TempData["Message"] = "warning: You are not authorized to remove this comment ";
                return RedirectToAction("Article", new { id = comment.ArticleId });
            }
        }
        else
        {
            return NotFound();
        }
    }
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Recommend(Guid ArticleId)
    {
        var user = await _userManager.GetUserAsync(User);
        var UserId = await _userManager.GetUserIdAsync(user);

        if (_repo.Recommend(ArticleId, UserId))
        {
            await _repo.SaveChangesAsync();
            return RedirectToAction("Article", new { id = ArticleId });
        }

        TempData["Message"] = "warning: The article you are trying to like is not exist";
        return RedirectToAction("Index");
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequstPremium()
    {
        var user = await _userManager.GetUserAsync(User);
        var userRequestedPremium = user.RequestedPremium;
        if (user.PlanType != "Premium")
        {
            if (!userRequestedPremium)
            {
                var UserId = await _userManager.GetUserIdAsync(user);
                _repo.RequestPremium(UserId);
                await _repo.SaveChangesAsync();
                TempData["Message"] = "success: Your request will be processed as soon as possible";
                return RedirectToAction("Index");
            }
            else
            {
                TempData["Message"] = "success: Your have sent a request before. Please be patient";
                return RedirectToAction("Index");
            }

        }
        else
        {
            TempData["Message"] = "warning: Your are already premium";
            return RedirectToAction("Index");
        }

    }
        public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
