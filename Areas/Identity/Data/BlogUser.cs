using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
namespace Blog.Areas.Identity.Data

{
    public class BlogUser : IdentityUser
    {


        [PersonalData]
        public String PlanType { get; set; }

        [PersonalData]
        public String FirstName { get; set; }
        
        [PersonalData]
        public String LastName { get; set; }
        [PersonalData]
        public String Gender { get; set; }

        [PersonalData]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime DOB { get; set; }
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd,hh:mm tt}", ApplyFormatInEditMode = true)]
        public DateTime LastSeen { get; set; }
        public byte[]? ProfilePicture { get; set; }

        public bool RequestedPremium { get; set; }





    }

}
