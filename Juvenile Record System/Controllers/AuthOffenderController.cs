using System.Web.Mvc;
using Juvenile_Record_System.Helpers;

namespace Juvenile_Record_System.Controllers
{
    [AuthorizeRole("Enforcement Officer", "Admin", "Administrator")]
    public class AuthOffenderController : Controller
    {
        // GET: AuthOffender
        public ActionResult Index()
        {
            return View("~/Views/Enforcement/AuthOffender.cshtml");
        }
    }
}
