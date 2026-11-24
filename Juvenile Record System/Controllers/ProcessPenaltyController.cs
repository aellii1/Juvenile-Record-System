using System.Web.Mvc;
using Juvenile_Record_System.Helpers;

namespace Juvenile_Record_System.Controllers
{
    [AuthorizeRole("Enforcement Officer", "Review Officer", "Admin", "Administrator")]
    public class ProcessPenaltyController : Controller
    {
        public ActionResult Index()
        {
            return View("~/Views/Enforcement/ProcessPenalty.cshtml");
        }
    }
}