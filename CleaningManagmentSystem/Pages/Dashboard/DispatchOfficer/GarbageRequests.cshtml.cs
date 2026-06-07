using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;

namespace CleaningManagmentSystem.Pages.Dashboard.DispatchOfficer
{
    public class GarbageRequestsModel : PageModel
    {
        public IActionResult OnGet()
        {
            return RedirectToPage("/Dashboard/DispatchOfficer/TransportRequests");
        }
    }
}
