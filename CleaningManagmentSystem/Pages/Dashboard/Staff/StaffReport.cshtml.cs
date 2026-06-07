using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CleaningManagmentSystem.Pages.Dashboard.Staff
{
    public class StaffReportModel : PageModel
    {
        public IActionResult OnGet()
        {
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
            {
                return RedirectToPage("/Login");
            }
            return Page();
        }
    }
}
