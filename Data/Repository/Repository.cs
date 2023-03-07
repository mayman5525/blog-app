using Blog.Areas.Identity.Data;
using Blog.Models;
using Blog.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;


namespace Blog.Data.Repository
{
    public class Repository : IRepository
    {
        private static BlogDbContext _ctx;
        private readonly UserManager<BlogUser> _userManager;

        public Repository(BlogDbContext ctx,
            
            UserManager<BlogUser> userManager)
        {
            _ctx = ctx;

            _userManager = userManager;
        }
        

        public void AddArticle(Article article)
        {
            _ctx.Articles.Add(article);
        }



        public IndexViewModel GetIndexViewModel(
            int pageNumber,
            string category,
            string search,
            string UserId)
        {
            
            

            var query = _ctx.Articles
                .OrderBy(a => a.Created)
                .AsNoTracking()
                .AsQueryable();

            if (!String.IsNullOrEmpty(category))
                query = query.Where(a => a.Categories.ToLower().Contains(category.ToLower() + ','));
                                                        

            if (!String.IsNullOrEmpty(search))
                query = query.Where(x => EF.Functions.Like(x.Title, $"%{search}%")
                                    || EF.Functions.Like(x.Content, $"%{search}%")
                                    || EF.Functions.Like(x.Description, $"%{search}%"));
            int pageSize = 5;
            int articlesCount = query.Count();
            int pageCount = (int)Math.Ceiling((double)articlesCount / pageSize);
            if (pageNumber < 1)
            {
                pageNumber = 1;
            }
            if (pageNumber > pageCount)
            {
                pageNumber = pageCount;
            }
            
            int skipAmount = pageSize * (pageNumber - 1);
            

            var indexViewModel = new IndexViewModel
            {
                Genres = GetGenres(),
                PageNumber = pageNumber,
                PageCount = pageCount,
                NextPage = pageNumber < pageCount,
                PreviousPage = pageNumber > 1,
                Pages = GetPageNumbers(pageNumber, pageCount),
                Category = category,
                Search = search,
                RecommendedArticles = GetRecommenedArticles(),
                
                PinnedArticles = GetPinnedArticles(UserId),
                CategoriesCount = GetCategoriesCount()

            };
            if (query.ToList().Count() > pageSize )
            {
                indexViewModel.Articles = (List<FrontArticleView>)query
                    .Select(x => new FrontArticleView
                    {
                        Id = x.ArticleId,
                        Title = x.Title,
                        Description = x.Description,
                        CreatedDate = x.Created,
                        CommentsCount = GetCommentsCount(x.ArticleId),
                        userProfile = GetUserProfile(x.AuthorId)
                    })
                    .Skip(skipAmount)
                    .Take(pageSize)
                    .ToList();
            }
            else
            {
                indexViewModel.Articles = (List<FrontArticleView>)query
                    .Select(x => new FrontArticleView
                    {
                        Id = x.ArticleId,
                        Title = x.Title,
                        Description = x.Description,
                        CreatedDate = x.Created,
                        CommentsCount = GetCommentsCount(x.ArticleId),
                        userProfile = GetUserProfile(x.AuthorId)
                    })
                    .ToList();

            }
            return indexViewModel;
        }

        private List<CatagoryCountViewModel> GetCategoriesCount()
        {
            var categories = String.Join(',',_ctx.Articles
                                .Select(x => x.Categories)
                                .ToList())
                                .Split(',')
                                .Distinct();
            var CategoriesCount = new List<CatagoryCountViewModel>();
            foreach (var category in categories)
            {
                var categorycount = new CatagoryCountViewModel
                {
                    Category = category,
                    Count = GetCategoryCount(category)
                };
                CategoriesCount.Add(categorycount);
            }
            return CategoriesCount;
        }

        private int GetCategoryCount(string category)
        {
            return _ctx.Articles
                        .Where(a => a.Categories.ToLower().Contains(category.ToLower()+','))
                        .ToList()
                        .Count();
        }

        private IEnumerable<FrontArticleView> GetPinnedArticles(string UserId)
        {
            var AdminPinnedArticles = _ctx.Articles
                .Where(x => x.Pinned)
                .Select(x => new FrontArticleView
                {
                    Id = x.ArticleId,
                    Title = x.Title,
                    Description = x.Description
                }).ToList();

            var UserPinnedArticles = new List<FrontArticleView>();
            if (UserId != null)
            {
                var UserPinnedArticlesId = _ctx.PinnedArticles
                                            .Where(x => x.UserId == UserId)
                                            .Select(i => new PinnedArticles {
                                                ArticleId = i.ArticleId   
                                            }).ToList();
                foreach (var UserPinnedArticle in UserPinnedArticlesId)
                {
                    UserPinnedArticles.Add(GetFrontArticleViewById(UserPinnedArticle.ArticleId));
                }
            }

            var PinnedArticles = AdminPinnedArticles.Union(UserPinnedArticles).ToList();
            return PinnedArticles;
        }

        private IEnumerable<FrontArticleView> GetRecommenedArticles()
        {
           var RecommendedArticlesId = _ctx.RecommendedBy
                                .GroupBy(e => e.ArticleId)
                                .Select(i => new 
                                { 
                                    ArticleId = i.Key,
                                    Count = i.Count()
                                })
                                .Where(a => a.Count >= _userManager.Users.Count()/2)
                                .ToList();
            var RecommendedArticles = new List<FrontArticleView>();
            foreach (var article in RecommendedArticlesId)
            {
                RecommendedArticles.Add(GetFrontArticleViewById(article.ArticleId));
            }
            return RecommendedArticles;
        }

        private static FrontArticleView GetFrontArticleViewById(Guid articleId)
        {
            return (FrontArticleView)_ctx.Articles
                .Where(a => a.ArticleId == articleId)
                .Select(x => new FrontArticleView
                {
                    Id = x.ArticleId,
                    Title = x.Title,
                    Description = x.Description,
                    CreatedDate = x.Created,
                    CommentsCount = GetCommentsCount(x.ArticleId),
                    userProfile = GetUserProfile(x.AuthorId),
                    ViewsCount = GetArticleViews(x.ArticleId),
                    LikeCount = GetArticleLikes(x.ArticleId)
                }).FirstOrDefault();
        }

        private static int GetCommentsCount(Guid articleId)
        {
            
            int count = _ctx.Comments.Where(a => a.ArticleId == articleId).Count();
            return count;
        }

        private List<int> GetPageNumbers(int pageNumber, int pageCount)
        {
            var pageNumbers = new List<int>();
            if (pageCount < 10)
            {
                for (int i = 1; i <= pageCount; i++)
                {
                    pageNumbers.Add(i);
                }
            }
            else
            {
                if (pageNumber + 3 < pageCount & pageNumber - 3 > 1)
                {
                    pageNumbers.Add(1);
                    for (int i = pageNumber - 3; i < pageNumber + 4; i++)
                    {
                        pageNumbers.Add(i);
                    }
                    pageNumbers.Add(pageCount);
                }
                else if (pageNumber + 3 > pageCount & pageNumber - 3 > 1)
                {
                    pageNumbers.Add(1);
                    for (int i = pageCount - 7; i < pageCount + 1; i++)
                    {
                        pageNumbers.Add(i);
                    }

                }
                else
                {

                    for (int i = 1; i < 9; i++)
                    {
                        pageNumbers.Add(i);
                    }
                    pageNumbers.Add(pageCount);
                }
            }
            return pageNumbers;
        }

        public void RemoveArticle(Guid id)
        {
                _ctx.Articles.Remove(GetArticle(id));
        }

        public bool UpdateArticle(Article article)
        {
            if (GetArticle(article.ArticleId) != null)
            {
                _ctx.Articles.Update(article);
                return true;
            }
            return false;
        }

        public async Task<bool> SaveChangesAsync()
        {
            if (await _ctx.SaveChangesAsync() > 0)
            {
                return true;
            }
            return false;
        }

        public string AddComment(Comment comment)
        {
            if (GetArticle(comment.ArticleId) != null)
            {
                if (comment.ParentId == Guid.Empty)
                {
                    comment.level = 0;
                }
                else if(GetComment(comment.ParentId) != null )
                {
                    comment.level = GetCommentlevelByID(comment.ParentId) + 1;
                    if (comment.level >= 3) { return "levelLemit"; }
                }
                else
                {
                    return "parentNotFound";
                }
                _ctx.Comments.Add(comment);
                return "success";
            }
            return "articleNotFound";
        }

        public ArticleViewModel GetArticleViewModel(Guid id, string UserId)
        {
            var ArticleViewModel = new ArticleViewModel();
            ArticleViewModel.Genres = GetGenres();
            ArticleViewModel.Article = GetArticle(id);
            if(ArticleViewModel.Article != null) 
            { 
                ArticleViewModel.ArticleLikes = GetArticleLikes(id);
                ArticleViewModel.ArticleViews = GetArticleViews(id);
                ArticleViewModel.Author = GetUserProfile(ArticleViewModel.Article.AuthorId);
                ArticleViewModel.SideBarArticles = GetSideBarArticles(ArticleViewModel.Article.GenreName);

                ArticleViewModel.MainComments = GetComments(id);

                ArticleViewModel.isPinned = GetPinnedArticles(UserId).Any(a => a.Id == id);
                ArticleViewModel.isLiked = _ctx.ArticleLikes.Any(a => a.Id == id & a.UserId ==UserId);
            }
            else
            {
                ArticleViewModel.NotFound = true;
            }
            return ArticleViewModel;
        }

        private static int GetArticleViews(Guid id)
        {
            return _ctx.Views
                    .Where(a => a.ArticleId == id).ToList().Count();
        }

        private List<string> GetGenres()
        {
            return _ctx.Articles
                .Select(a => a.GenreName)
                .Distinct()
                .ToList();
        }

        private static int GetArticleLikes(Guid id)
        {
            return _ctx.ArticleLikes
                    .Where(a => a.ArticleId == id).Count();
        }
        private int GetCommentLikes(Guid id)
        {
            return _ctx.CommentLikes
                    .Where(a => a.CommentId == id).Count();
        }

        

        private List<CommentViewModel>? GetComments(Guid id)

        {
            var CommentsViewModdel = new List<CommentViewModel>();
            Console.WriteLine(id);
            var comments = _ctx.Comments


 

                .Where(a => a.ArticleId == id)

                .ToList();
            
            var level0Comments = comments.Where(c => c.level == 0);
            foreach (var comment in level0Comments)
            {
                CommentsViewModdel.Add(commentToViewComment(comment, comments.Except(level0Comments).ToList()));
            }


            return CommentsViewModdel;
        }

        private CommentViewModel commentToViewComment(Comment comment, List<Comment> comments)
        {   
            var nextLevelComments = comments.Where(c => c.level == comment.level + 1);
            var commentViewModel = new CommentViewModel();
            commentViewModel.Comment = comment;
            commentViewModel.Creator = GetUserProfile(comment.AuthorId);
            commentViewModel.CommentLikes = GetCommentLikes(comment.CommentId);
            commentViewModel.SubComments = new List<CommentViewModel>();
            if (comment.level < 3)
            {
                foreach (var nextlevelcomment in nextLevelComments)
                {
                    
                    commentViewModel.SubComments.Add(commentToViewComment(nextlevelcomment, comments.Except(nextLevelComments).ToList()));
                }
            }
            return commentViewModel;
        }

        public Article? GetArticle(Guid id)
        {
            return _ctx.Articles
                        .Where(a => a.ArticleId == id)
                        .FirstOrDefault();
        }
        private static UserProfile GetUserProfile(string id)
        {
            var user = _ctx.Users
                    .Where(u => u.Id == id)
                    .FirstOrDefault();
            UserProfile userProfile = new UserProfile
            {
                UserId = user.Id,
                ProfilePicture = user.ProfilePicture ,
                UserName = user.UserName ,
                PlanType = user.PlanType,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Gender = user.Gender,
                DOB = user.DOB
            };
            return userProfile;
        }
        private List<FrontArticleView> GetSideBarArticles(string genreName)
        {
            return _ctx.Articles
                    .Where(a => a.GenreName == genreName)
                    .Select(x => new FrontArticleView
                    {
                        Id = x.ArticleId,
                        Title = x.Title,
                        Description = x.Description
                    }).ToList();
        }

        public bool AddView(Guid ArticleId, string UserId)
        {
            if (GetArticle(ArticleId) != null)
            {
                var view = _ctx.Views.Where(e => e.ArticleId == ArticleId & e.UserId == UserId)
                    .FirstOrDefault();

                if (view == null)
                {
                    _ctx.Views.Add(new View {Id = Guid.NewGuid() , ArticleId = ArticleId, UserId = UserId });
                    return true;
                }
                
            }
            return false;
        }

        public bool AddArticleLike(Guid ArticleId, string UserId)
        {
            if (GetArticle(ArticleId) != null)
            {
                var ArticleLike = _ctx.ArticleLikes
                    .Where(e => e.ArticleId == ArticleId & e.UserId == UserId)
                    .FirstOrDefault();
                if (ArticleLike == null)
                {
                    ArticleLike = new ArticleLike
                    {
                        Id = Guid.NewGuid(),
                        ArticleId = ArticleId,
                        UserId = UserId
                    };
                    _ctx.ArticleLikes.Add(ArticleLike);
                    return true;
                }
                else
                {
                    _ctx.ArticleLikes.Remove(ArticleLike);
                    return true;
                }
                
            }
            return false;
        }

        public bool AddCommentLike(Guid CommentId, string UserId)
        {

            if (GetComment(CommentId) != null)
            {
                var CommentLike = _ctx.CommentLikes
                    .Where(e => e.CommentId == CommentId & e.UserId == UserId)
                    .FirstOrDefault();
                if (CommentLike == null)
                {
                     CommentLike = new CommentLike {
                        Id= Guid.NewGuid(),
                        CommentId = CommentId,
                        UserId = UserId };
                    _ctx.CommentLikes.Add(CommentLike);
                    return true;
                }
                else
                {
                    _ctx.CommentLikes.Remove(CommentLike);
                    return true;
                }
                
            }
            return false;
        }

        public Comment? GetComment(Guid commentId)
        {
            return _ctx.Comments
                .FirstOrDefault(e => e.CommentId == commentId);
        }

        public bool IsAllowedToPost(string UserId)
        {
            var MonthlyPostedArticles = _ctx.Articles
                                            .Where(a => a.AuthorId == UserId & a.Created.ToString("MM/yyyy") == DateTime.Now.ToString("MM/yyyy"))
                                            .Count();
            return MonthlyPostedArticles < 2;
            
        }

        public Guid? GetFirstArticleIdByGenre(string Genre)
        {
            var article = _ctx.Articles
                .Where(a => a.GenreName == Genre)
                .OrderBy(a => a.Created)
                .ToList()[0];
            return article.ArticleId;
        }

        private int GetCommentlevelByID(Guid id)
        {
            var level = _ctx.Comments
                .Where(c => c.CommentId == id)
                .Select(c => c.level)
                .First();
            return level;
        }

        public Guid GetArticleId(Guid CommentId)
        {
            return GetComment(CommentId).ArticleId;
        }

        public void RemoveComment(Guid id)
        {
            var subcomments = _ctx.Comments.Where(c => c.ParentId == id).ToList();
            _ctx.Comments.Remove(GetComment(id));
            foreach(var subcomment in subcomments)
            {
                _ctx.Comments.Remove(subcomment);
            }
        }

        public bool Recommend(Guid ArticleId, string UserId)
        {
            var recommendedBy = _ctx.RecommendedBy
                 .Where(e => e.ArticleId == ArticleId & e.UserId == UserId)
                 .FirstOrDefault(); ;
            if (recommendedBy == null)
            {
                recommendedBy = new RecommendedBy
                {
                    Id = Guid.NewGuid(),
                    ArticleId = ArticleId,
                    UserId = UserId

                };
                _ctx.RecommendedBy.Add(recommendedBy);
                return true;
            }
            else
            {
                _ctx.RecommendedBy.Remove(recommendedBy);
            }
            return false;
        }

        public AdminViewModel AdminViewModel(string UserId)
        {

            var AdminViewModel =  new AdminViewModel {
                UsersCount = GetUsersCount(),
                ArticleCount = GetArticleCount(),
                MostInteractionsArticleViewModel = GetMostLikedArticles(),
                userProfile = GetUserProfile(UserId),
                UserRequestedPremium = GetUserRequestedPremium()
            };
            return AdminViewModel;
        }

        private List<UserProfile> GetUserRequestedPremium()
        {
            return (List<UserProfile>)_ctx.Users
                .Where(a => a.RequestedPremium)
                .Select(user => new UserProfile
                {
                    UserId = user.Id,
                    ProfilePicture = user.ProfilePicture,
                    UserName = user.UserName,
                    PlanType = user.PlanType,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Gender = user.Gender,
                    DOB = user.DOB
                });

            
        }

        public bool RequestPremium(string UserId)
        {
            var user = _ctx.Users
                .Where(u => u.Id == UserId)
                .FirstOrDefault();
            if (user != null)
            {
                user.RequestedPremium = true;
                _ctx.Users.Update(user);
                return true;
            }
            return false;   
            
                

         
        }

        public bool GivePremium(string UserId)
        {
            var user = _ctx.Users
                .Where(u => u.Id == UserId)
                .FirstOrDefault();
            if (user != null)
            {
                user.PlanType = "Premium";
                user.RequestedPremium = false;
                _ctx.Users.Update(user);
                
                return true;
            }
            return false;
        }

        public bool RemoveUser(string UserId)
        {
            var user = _ctx.Users
                .Where(u => u.Id == UserId)
                .FirstOrDefault();
            if (user != null)
            {
                _ctx.Users.Remove(user);
                return true;
            }
            return false;
        }

        public string LocalPin(string UserId, Guid ArticleId)
        {

            if (GetArticle(ArticleId) != null)
            {
                var ArticlePin = _ctx.PinnedArticles
                    .Where(e => e.ArticleId == ArticleId & e.UserId == UserId)
                    .FirstOrDefault();
                if (ArticlePin == null)
                {
                    ArticlePin = new PinnedArticles
                    {
                        Id = Guid.NewGuid(),
                        ArticleId = ArticleId,
                        UserId = UserId
                    };
                    _ctx.PinnedArticles.Add(ArticlePin);
                    return "Added";
                }
                else
                {
                    _ctx.PinnedArticles.Remove(ArticlePin);
                    return "Removed";
                }

            }
            return "Error"; 
        }

        public string GlobalPin(Guid ArticleId)
        {
            var article = GetArticle(ArticleId);
            if (article != null)
            {
                article.Pinned = !article.Pinned;
                _ctx.Articles.Update(article);
                return article.Pinned ? "Adderd" :"Removed";
            }
            return "Error";
        }

        private int GetUsersCount()
        {
            return _ctx.Users.Count();
        }

        private int GetArticleCount()
        {
            return _ctx.ArticleLikes.Count();
        }

        private MostInteractionsArticleViewModel GetMostLikedArticles()
        {
            var ArticlesId = _ctx.Articles
                .Select(a => a.ArticleId).ToList();
            var Articles = new List<FrontArticleView>();
            foreach (var ArticleId in ArticlesId)
            {
                Articles.Add(GetFrontArticleViewById(ArticleId));
            }
            var MostInteractions = new MostInteractionsArticleViewModel
            {
                MostLikedArticles = Articles
                .OrderByDescending(a => a.LikeCount)
                .Take(3),
                MostCommentedArticles = Articles
                .OrderByDescending(a => a.CommentsCount)
                .Take(3),
                MostViewedArticles = Articles
                .OrderByDescending(a => a.ViewsCount)
                .Take(3)
            };

            return MostInteractions;

        }

    }
}