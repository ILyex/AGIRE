using Microsoft.AspNetCore.Identity;

namespace Hawdh.Portal.Data;

// Add profile data for application users by adding properties to the ApplicationUser class
public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string JobTitle { get; set; } = "";
    public string Rank { get; set; } = "";
    public bool ProfileCompleted { get; set; }
    public bool IsActive { get; set; } = true;
}

