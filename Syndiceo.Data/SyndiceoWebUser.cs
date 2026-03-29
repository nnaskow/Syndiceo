using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace SyndiceoWeb.Areas.Identity.Data;

// Add profile data for application users by adding properties to the SyndiceoWebUser class
public class SyndiceoWebUser : IdentityUser
{
    [PersonalData]
    public string FirstName { get; set; }

    [PersonalData]
    public string LastName { get; set; }

    public bool IsApproved { get; set; } = false;

    public DateTime LastDiscussionsView { get; set; } = DateTime.Now;

}

