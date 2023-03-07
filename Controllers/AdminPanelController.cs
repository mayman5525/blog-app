
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Blog.Models;
using Blog.Data.FileManager;
using Blog.Data.Repository;
using Microsoft.AspNetCore.Identity;
using Blog.Areas.Identity.Data;
using Microsoft.AspNetCore.Authorization;

namespace Blog.Controllers;


[Authorize(Roles = "Admin")]
public class AdminPanelController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly SignInManager<BlogUser> _signInManager;
    private readonly UserManager<BlogUser> _userManager;
    private IRepository _repo;
    private IFileManager _fileManager;

    public AdminPanelController(ILogger<HomeController> logger,
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

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        var UserId = await _userManager.GetUserIdAsync(user);
        var vm = _repo.AdminViewModel(UserId);
        return View(vm);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveUser(string UserId)
    {
        if (_repo.RemoveUser(UserId)){

            await _repo.SaveChangesAsync();
            TempData["Message"] = "success: Successfully removed";

            return RedirectToAction("Index");
        }
        TempData["Message"] = "warning: User does not exist";
        return RedirectToAction("Index");
    }
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GivePremium(string UserId)
    {
        if (_repo.GivePremium(UserId))
        {

            await _repo.SaveChangesAsync();
            TempData["Message"] = "success: Successfully gived";

            return RedirectToAction("Index");
        }
        TempData["Message"] = "warning: User does not exist";
        return RedirectToAction("Index");
    }
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GlobalPin(Guid ArticleId)
    {
        var cond = _repo.GlobalPin(ArticleId);
        if (cond == "Added")
        {

            await _repo.SaveChangesAsync();
            TempData["Message"] = "success: Successfully Added";

            return RedirectToAction("Index");
        }
        else if (cond == "Removed")
        {
            await _repo.SaveChangesAsync();
            TempData["Message"] = "success: Successfully Added";

            return RedirectToAction("Index");
        }
        TempData["Message"] = "warning: Article does not exist";
        return RedirectToAction("Index");
    }
}