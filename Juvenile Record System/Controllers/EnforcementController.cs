using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using Juvenile_Record_System.Models;
using Juvenile_Record_System.Helpers;

namespace Juvenile_Record_System.Controllers
{
    // Restrict access to Enforcement Officers and Admins
    [AuthorizeRole("Enforcement Officer", "Enforcement", "Officer", "Review Officer")]
    public class EnforcementController : Controller
    {
        private JuvenileContext db = new JuvenileContext();

        // GET: Enforcement/Index 
        public ActionResult Index()
        {
            return View();
        }

        // Module 1: Scan Face ID & Biometrics
        public ActionResult ScanBiometrics()
        {
            return View();
        }

        // Module 2: Authenticate Offender
        public ActionResult AuthOffender()
        {
            return View();
        }

        // Module 3: Check Offender Records
        public ActionResult CheckRecords(string templateId)
        {
            return View();
        }

        // Module 4: Manage Offender Profile (Create New or Update Existing)
        public ActionResult ManageProfile()
        {
            return View();
        }

        // Action Filter to automatically populate profile variables for the layout header/sidebar
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);

            if (Session["UserId"] != null)
            {
                int rawUserId = Convert.ToInt32(Session["UserId"]);
                ViewBag.FormattedUserId = $"U-{rawUserId:D3}";

                string fullName = Session["FullName"]?.ToString() ?? Session["Username"]?.ToString() ?? "Enforcement Officer";
                ViewBag.OfficerFullName = fullName;
                ViewBag.OfficerUsername = Session["Username"]?.ToString() ?? "Officer";
                ViewBag.OfficerRole = Session["RoleName"]?.ToString() ?? "Enforcement Officer";

                // Generate initials for the avatar circle
                ViewBag.OfficerInitial = !string.IsNullOrEmpty(fullName) ? fullName.Trim().Substring(0, 1).ToUpper() : "E";
            }
        }
    }
}