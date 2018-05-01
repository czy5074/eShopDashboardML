using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.eShopOnContainers.WebDashboardRazor.Models;

namespace eShopDashboard.Pages
{
    public class IndexModel : PageModel
    {
        public void OnGet()
        {
            ViewData.SetSelectedMenu(SelectedMenu.Splash);


        }
    }
}