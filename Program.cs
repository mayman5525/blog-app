using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Blog.Areas.Identity.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Blog.Data.Repository;
using Blog.Data.FileManager;
using Serilog;
using Microsoft.AspNetCore.Diagnostics;
using static System.Net.Mime.MediaTypeNames;

var builder = WebApplication.CreateBuilder(args);



var config = builder.Configuration;
var connectionString = config.GetConnectionString("BlogDbContextConnection");



Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information("Starting up");
builder.Host.UseSerilog((ctx, lc) => lc
    .WriteTo.Console()
    .ReadFrom.Configuration(ctx.Configuration));


builder.Services.AddDbContext<BlogDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDefaultIdentity<BlogUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<BlogDbContext>();

builder.Services.AddScoped<IRepository, Repository>();
builder.Services.AddScoped<IFileManager, FileManager>();
// Add services to the container.
builder.Services.AddControllersWithViews();




builder.Services.Configure<IdentityOptions>(options =>
{
    // Default User settings.
    options.User.AllowedUserNameCharacters =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_";
    options.User.RequireUniqueEmail = true;

});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.Cookie.Name = "Cookie";
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.LoginPath = "/Identity/Account/Login";
    options.ReturnUrlParameter = CookieAuthenticationDefaults.ReturnUrlParameter;
    options.SlidingExpiration = true;
});

builder.Services.Configure<PasswordHasherOptions>(option =>
{
    option.IterationCount = 12000;
});
builder.Services.AddAuthentication()
   .AddCookie()
   .AddFacebook(options =>
   {
       
       options.AppId = config["Authentication__Facebook__AppId"];
       options.AppSecret = config["Authentication__Facebook__AppSecret"];
       options.AccessDeniedPath = "/Identity/Account/AccessDenied";
   });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(exceptionHandlerApp =>
    {
        exceptionHandlerApp.Run(async context =>
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            // using static System.Net.Mime.MediaTypeNames;
            context.Response.ContentType = Text.Plain;

            await context.Response.WriteAsync("An exception was thrown.");

            var exceptionHandlerPathFeature =
                context.Features.Get<IExceptionHandlerPathFeature>();

            if (exceptionHandlerPathFeature?.Error is FileNotFoundException)
            {
                await context.Response.WriteAsync(" The file was not found.");
            }

            if (exceptionHandlerPathFeature?.Path == "/")
            {
                await context.Response.WriteAsync(" Page: Home.");
            }
        });
    });

    app.UseHsts();
}

var scope = app.Services.CreateScope();
try
{
    var ctx = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
    var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<BlogUser>>();
    var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    ctx.Database.EnsureCreated();

    var adminRole = new IdentityRole("Admin");
    var blogOwnerRole = new IdentityRole("BlogOwner");
    if (!ctx.Roles.Any())
    {
        roleMgr.CreateAsync(adminRole).GetAwaiter().GetResult();
        roleMgr.CreateAsync(blogOwnerRole).GetAwaiter().GetResult();

    }
    if (!ctx.Users.Any(u => u.UserName == "admin"))
    {
        //create an admin
        var adminUser = new BlogUser
        {
            UserName = "admin",
            Email = "admin@blog.com",
            FirstName = "SIKI",
            LastName = "MIKI",
            Gender = "Male",
            PlanType = "",
            DOB = new DateTime(2000, 1, 1)
        };
        var result = userMgr.CreateAsync(adminUser, "6#B9[*g,f=x[V+7t").GetAwaiter().GetResult();
        var code = await userMgr.GenerateEmailConfirmationTokenAsync(adminUser);
        var ConfirmationResult = await userMgr.ConfirmEmailAsync(adminUser, code);
        if (ConfirmationResult.Succeeded)
        {
            Console.WriteLine("Successfully Done");
        }
        else
        {
            Console.WriteLine("Something Went Wrong");
        }
        //add role to user
        userMgr.AddToRoleAsync(adminUser, adminRole.Name).GetAwaiter().GetResult();
    }
    if (!ctx.Users.Any(u => u.UserName == "Owner"))
    {
        //create an admin
        var OwnerUser = new BlogUser
        {
            UserName = "Owner",
            Email = "Owner@blog.com",
            FirstName = "SIKI",
            LastName = "MIKI",
            Gender = "Male",
            PlanType = "",
            DOB = new DateTime(2000, 1, 1)
        };
        var result = userMgr.CreateAsync(OwnerUser, "6#B9[*g,f=x[V+7t").GetAwaiter().GetResult();
        var code = await userMgr.GenerateEmailConfirmationTokenAsync(OwnerUser);
        var ConfirmationResult = await userMgr.ConfirmEmailAsync(OwnerUser, code);
        if (ConfirmationResult.Succeeded)
        {
            Console.WriteLine("Successfully Done");
        }
        else
        {
            Console.WriteLine("Something Went Wrong");
        }
        //add role to user
        userMgr.AddToRoleAsync(OwnerUser, blogOwnerRole.Name).GetAwaiter().GetResult();
    }

}
catch (Exception e)
{
    Console.WriteLine(e.Message);
}
app.UseStatusCodePagesWithReExecute("/StatusCode/{0}");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();;

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();
app.UseSerilogRequestLogging(
    options =>
    {
        options.MessageTemplate =
            "{RemoteIpAddress} {RequestScheme} {RequestHost} {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        options.EnrichDiagnosticContext = (
            diagnosticContext,
            httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
            diagnosticContext.Set("RemoteIpAddress", httpContext.Connection.RemoteIpAddress);
        };
    });
app.Run();
