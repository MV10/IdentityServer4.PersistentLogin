using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Site.Pages
{
    public class IndexModel : PageModel
    {
        private bool PersistentLoginAttempted = false;
        private const string PersistentLoginFlag = "persistent_login_attempt";

        public IActionResult OnGet()
        {
            // Always clean up an existing flag.
            bool FlagFound = false;
            if(!String.IsNullOrEmpty(TempData[PersistentLoginFlag] as string))
            {
                FlagFound = true;
                TempData.Remove(PersistentLoginFlag);
            }

            // Try to refresh a persistent login the first time an anonymous user hits the index page in this session
            if(!User.Identity.IsAuthenticated && !PersistentLoginAttempted)
            {
                PersistentLoginAttempted = true;
                // If there was a flag, this is the return-trip from a failed persistent login attempt.
                if(!FlagFound)
                {
                    // No flag was found. Create it, then begin the OIDC challenge flow.
                    TempData[PersistentLoginFlag] = PersistentLoginFlag;
                    return Challenge("persistent");
                }
            }
            return Page();
        }

    }
}
