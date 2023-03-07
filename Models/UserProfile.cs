namespace Blog.Models;
public class UserProfile
{
    public string UserId { get; set; }
    public byte[]? ProfilePicture { get; set; }

    public string  UserName { get; set; }
    public String PlanType { get; set; }

    public String FirstName { get; set; }


    public String LastName { get; set; }
    public String Email { get; set; }

    public String Gender { get; set; }


    public DateTime DOB { get; set; }
}