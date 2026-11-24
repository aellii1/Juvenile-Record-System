using Juvenile_Record_System.Helpers;
using Juvenile_Record_System.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Juvenile_Record_System.Controllers
{
    [AuthorizeRole("Admin")]
    public class AdminController : Controller
    {
        private JuvenileContext db = new JuvenileContext();
        private const string ActiveAccountStatus = "Active";
        private const string InactiveAccountStatus = "Inactive";

        private static readonly string[] AllModules =
        {
            "ScanBiometrics",
            "Authenticate",
            "CheckRecords",
            "ManageProfile",
            "ProcessPenalty",
            "JuvenileRecords"
        };

        private static readonly string[] DefaultEnforcementModules =
        {
            "ScanBiometrics",
            "Authenticate",
            "CheckRecords",
            "ManageProfile"
        };

        // GET: Admin/Index
        public ActionResult Index(string searchString)
        {
            // 1. Welcome Toast Logic
            if (Session["ShowWelcomeMessage"] != null && (bool)Session["ShowWelcomeMessage"] == true)
            {
                string displayName = Session["FullName"]?.ToString() ?? Session["Username"]?.ToString() ?? "Administrator";
                TempData["SuccessMessage"] = "Welcome back, " + displayName + "!";

                Session["ShowWelcomeMessage"] = false;
            }

            // 2. Fetch Users for Dashboard Metrics & Management Table
            var users = db.Users.Include("Role").AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                users = users.Where(u => u.FullName.Contains(searchString) || u.Username.Contains(searchString));
            }

            ViewBag.CurrentFilter = searchString;

            return View(users.ToList());
        }

        // Helper to ensure layout profile variables exist across all Admin actions
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);

            if (Session["UserId"] != null)
            {
                int rawUserId = Convert.ToInt32(Session["UserId"]);

                // Format to U-001, U-002, etc. (3-digit padding)
                ViewBag.FormattedUserId = $"U-{rawUserId:D3}";

                ViewBag.AdminFullName = Session["FullName"]?.ToString() ?? Session["Username"]?.ToString() ?? "Administrator";
                ViewBag.AdminUsername = Session["Username"]?.ToString() ?? "Admin";
                ViewBag.AdminRole = Session["RoleName"]?.ToString() ?? "Administrator";

                string name = ViewBag.AdminFullName;
                ViewBag.AdminInitial = !string.IsNullOrEmpty(name) ? name.Trim().Substring(0, 1).ToUpper() : "A";
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private Dictionary<int, string[]> GetPermitMap()
        {
            return HttpRuntime.Cache["UserPermitMap"] as Dictionary<int, string[]>
                   ?? new Dictionary<int, string[]>();
        }

        private void SavePermitMap(Dictionary<int, string[]> permitMap)
        {
            HttpRuntime.Cache["UserPermitMap"] = permitMap;
        }

        private string[] ResolvePermitsForUser(User user, Dictionary<int, string[]> permitMap)
        {
            string roleName = user?.Role?.RoleName ?? string.Empty;
            bool isAdmin = roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                           roleName.Equals("Administrator", StringComparison.OrdinalIgnoreCase);

            if (isAdmin)
            {
                return new[] { "All Modules (Admin)" };
            }

            if (user != null && permitMap != null && permitMap.TryGetValue(user.UserId, out string[] savedPermits))
            {
                return savedPermits ?? Array.Empty<string>();
            }

            bool isEnforcement = roleName.IndexOf("Enforcement", StringComparison.OrdinalIgnoreCase) >= 0;
            return isEnforcement ? DefaultEnforcementModules : AllModules;
        }

        // GET: Admin/Officers
        [HttpGet]
        public ActionResult Officers()
        {
            var officers = db.Users.Include("Role").ToList();
            var userPermitMap = GetPermitMap();
            var displayMap = new Dictionary<int, string[]>();

            foreach (var officer in officers)
            {
                var resolved = ResolvePermitsForUser(officer, userPermitMap);
                if (resolved.Length == 1 && resolved[0] == "All Modules (Admin)")
                {
                    displayMap[officer.UserId] = resolved;
                }
                else
                {
                    displayMap[officer.UserId] = resolved.Length > 0 ? resolved : new[] { "No Modules" };
                }
            }

            ViewBag.OfficerPermitsMap = displayMap;
            return View(officers);
        }

        // GET: Admin/AdminModal
        [HttpGet]
        public ActionResult AdminModal(string actionType, int? id)
        {
            ViewBag.ActionType = actionType;

            if (actionType == "assignRole")
            {
                var officer = db.Users.Include("Role").FirstOrDefault(u => u.UserId == id);
                if (officer == null) return HttpNotFound();

                ViewBag.AvailableRoles = new SelectList(db.Roles, "RoleId", "RoleName", officer.RoleId);

                var permitMap = GetPermitMap();
                string[] currentPermits;

                if (permitMap.TryGetValue(officer.UserId, out string[] savedPermits))
                {
                    currentPermits = savedPermits ?? Array.Empty<string>();
                }
                else
                {
                    // Use the same permit source that Officer Details uses instead of resetting to all 4 defaults
                    currentPermits = ParseUserPermits(officer) ?? Array.Empty<string>();
                }

                ViewBag.SelectedPermits = currentPermits;
                return PartialView("AdminModal", officer);
            }

            if (actionType == "create")
            {
                return PartialView("AdminModal", new User());
            }
            else if (actionType == "edit" && id.HasValue)
            {
                var user = db.Users.Find(id.Value);
                if (user == null) return HttpNotFound();
                return PartialView("AdminModal", user);
            }
            else if (actionType == "details" && id.HasValue)
            {
                var user = db.Users.Include("Role").FirstOrDefault(u => u.UserId == id.Value);
                if (user == null) return HttpNotFound();

                var userPermitMap = GetPermitMap();
                ViewBag.OfficerPermits = ResolvePermitsForUser(user, userPermitMap);
                ViewBag.OfficerPermits = ParseUserPermits(user);

                return PartialView("AdminModal", user);
            }
            else if (actionType == "delete" && id.HasValue)
            {
                var user = db.Users.Find(id.Value);
                if (user == null) return HttpNotFound();
                return PartialView("AdminModal", user);
            }

            return HttpNotFound();
        }

        // POST: Admin/CreateUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateUser(User user)
        {
            if (ModelState.ContainsKey("RoleId"))
            {
                ModelState["RoleId"].Errors.Clear();
            }

            user.FullName = user.FullName?.Trim();
            user.Username = user.Username?.Trim();
            user.Email = string.IsNullOrWhiteSpace(user.Email) ? null : user.Email.Trim();
            user.PhoneNumber = string.IsNullOrWhiteSpace(user.PhoneNumber) ? null : user.PhoneNumber.Trim();
            user.HomeAddress = string.IsNullOrWhiteSpace(user.HomeAddress) ? null : user.HomeAddress.Trim();

            if (!string.IsNullOrEmpty(user.PhoneNumber) && !user.PhoneNumber.All(char.IsDigit))
            {
                ModelState.AddModelError("PhoneNumber", "Phone number must contain numbers only.");
            }

            if (!string.IsNullOrEmpty(user.Username) && db.Users.Any(u => u.Username == user.Username))
            {
                ModelState.AddModelError("Username", "Username is already taken.");
            }

            user.RoleId = 2;
            user.CreatedAt = DateTime.Now;

            if (string.IsNullOrWhiteSpace(user.AccountStatus))
            {
                user.AccountStatus = ActiveAccountStatus;
            }
            else if (string.Equals(user.AccountStatus.Trim(), ActiveAccountStatus, StringComparison.OrdinalIgnoreCase))
            {
                user.AccountStatus = ActiveAccountStatus;
            }
            else if (string.Equals(user.AccountStatus.Trim(), InactiveAccountStatus, StringComparison.OrdinalIgnoreCase))
            {
                user.AccountStatus = InactiveAccountStatus;
            }
            else
            {
                ModelState.AddModelError("AccountStatus", "Account status must be Active or Inactive.");
            }

            if (ModelState.IsValid)
            {
                if (!string.IsNullOrEmpty(user.PasswordHash))
                {
                    user.PasswordHash = PasswordHasher.HashPassword(user.PasswordHash);
                }

                try
                {
                    db.Users.Add(user);
                    db.SaveChanges();

                    TempData["SuccessMessage"] = "Officer account successfully created!";
                    return RedirectToAction("Officers");
                }
                catch (System.Data.Entity.Infrastructure.DbUpdateException ex)
                {
                    var sqlError = ex.InnerException?.InnerException?.Message ?? ex.InnerException?.Message ?? ex.Message;
                    ModelState.AddModelError("", "Database Error: " + sqlError);
                }
                catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
                {
                    foreach (var validationErrors in dbEx.EntityValidationErrors)
                    {
                        foreach (var validationError in validationErrors.ValidationErrors)
                        {
                            ModelState.AddModelError("", $"{validationError.PropertyName}: {validationError.ErrorMessage}");
                        }
                    }
                }
            }

            ViewBag.ActionType = "create";
            return PartialView("AdminModal", user);
        }

        // POST: Admin/EditUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditUser(User user)
        {
            // If password wasn't changed, remove it from validation so it doesn't fail
            if (string.IsNullOrEmpty(user.PasswordHash))
            {
                ModelState.Remove("PasswordHash");
            }

            // Prevent RoleId and UserId validation failures since they come from hidden/default states
            if (ModelState.ContainsKey("RoleId"))
            {
                ModelState["RoleId"].Errors.Clear();
            }
            if (ModelState.ContainsKey("UserId"))
            {
                ModelState["UserId"].Errors.Clear();
            }

            var trimmedPhone = string.IsNullOrWhiteSpace(user.PhoneNumber) ? null : user.PhoneNumber.Trim();
            if (!string.IsNullOrEmpty(trimmedPhone) && !trimmedPhone.All(char.IsDigit))
            {
                ModelState.AddModelError("PhoneNumber", "Phone number must contain numbers only.");
            }

            var accountStatus = (user.AccountStatus ?? string.Empty).Trim();

            if (string.Equals(accountStatus, ActiveAccountStatus, StringComparison.OrdinalIgnoreCase))
            {
                user.AccountStatus = ActiveAccountStatus;
            }
            else if (string.Equals(accountStatus, InactiveAccountStatus, StringComparison.OrdinalIgnoreCase))
            {
                user.AccountStatus = InactiveAccountStatus;
            }
            else
            {
                ModelState.AddModelError("AccountStatus", "Invalid account status.");
            }

            if (ModelState.IsValid)
            {
                var existingUser = db.Users.Find(user.UserId);
                if (existingUser != null)
                {
                    existingUser.FullName = user.FullName?.Trim();
                    existingUser.Username = user.Username?.Trim();
                    existingUser.AccountStatus = user.AccountStatus;
                    existingUser.Email = string.IsNullOrWhiteSpace(user.Email) ? null : user.Email.Trim();
                    existingUser.PhoneNumber = trimmedPhone;
                    existingUser.Birthdate = user.Birthdate;
                    existingUser.HomeAddress = string.IsNullOrWhiteSpace(user.HomeAddress) ? null : user.HomeAddress.Trim();

                    if (!string.IsNullOrEmpty(user.PasswordHash))
                    {
                        existingUser.PasswordHash = PasswordHasher.HashPassword(user.PasswordHash);
                    }

                    try
                    {
                        db.SaveChanges();
                        TempData["SuccessMessage"] = "Officer account successfully updated!";
                        return RedirectToAction("Officers");
                    }
                    catch (System.Data.Entity.Infrastructure.DbUpdateException ex)
                    {
                        var sqlError = ex.InnerException?.InnerException?.Message ?? ex.InnerException?.Message ?? ex.Message;
                        ModelState.AddModelError("", "Database Error: " + sqlError);
                    }
                    catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
                    {
                        foreach (var validationErrors in dbEx.EntityValidationErrors)
                        {
                            foreach (var validationError in validationErrors.ValidationErrors)
                            {
                                ModelState.AddModelError("", $"{validationError.PropertyName}: {validationError.ErrorMessage}");
                            }
                        }
                    }
                }
                else
                {
                    return HttpNotFound();
                }
            }

            ViewBag.ActionType = "edit";
            return PartialView("AdminModal", user);
        }

        // GET: Admin/DeleteUser
        public ActionResult DeleteUser(int id)
        {
            var user = db.Users.Find(id);
            if (user != null)
            {
                db.Users.Remove(user);
                db.SaveChanges();
                TempData["SuccessMessage"] = "Officer account successfully deleted!";
            }

            return RedirectToAction("Officers");
        }

        // GET: Admin/OfficerPermits
        public ActionResult OfficerPermits()
        {
            ViewBag.ActivePage = "Officer Permits";

            var officers = db.Users
                             .Include("Role")
                             .Where(u => u.AccountStatus == "ACTIVE" || u.AccountStatus == ActiveAccountStatus)
                             .ToList();

            var userPermitMap = GetPermitMap();
            var displayMap = new Dictionary<int, string[]>();
            foreach (var officer in officers)
            {
                displayMap[officer.UserId] = ResolvePermitsForUser(officer, userPermitMap);
            }
            ViewBag.OfficerPermitsMap = displayMap;

            return View(officers);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AssignRole(int userId, int roleId, string[] selectedPermits = null)
        {
            var officer = db.Users.Find(userId);
            if (officer != null)
            {
                officer.RoleId = roleId;

                var role = db.Roles.Find(roleId);
                string roleName = role?.RoleName ?? string.Empty;
                bool isAdmin = roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                               roleName.Equals("Administrator", StringComparison.OrdinalIgnoreCase);
                bool isEnforcement = roleName.IndexOf("Enforcement", StringComparison.OrdinalIgnoreCase) >= 0;

                string[] finalPermits;
                if (isAdmin)
                {
                    finalPermits = AllModules;
                }
                else if (isEnforcement)
                {
                    finalPermits = (selectedPermits ?? Array.Empty<string>())
                        .Where(p => !string.Equals(p, "ProcessPenalty", StringComparison.OrdinalIgnoreCase) &&
                                    !string.Equals(p, "JuvenileRecords", StringComparison.OrdinalIgnoreCase))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToArray();
                }
                else
                {
                    finalPermits = (selectedPermits ?? AllModules)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToArray();
                }

                // Save comma-separated permits directly into SQL Server
                officer.AllowedPermits = string.Join(",", finalPermits);
                db.SaveChanges();

                if (Session["UserId"] != null && Convert.ToInt32(Session["UserId"]) == userId)
                {
                    Session["AllowedPermits"] = finalPermits;
                }

                TempData["SuccessMessage"] = "Officer role and permits updated successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "User not found.";
            }

            string refererPath = Request.UrlReferrer?.AbsolutePath ?? string.Empty;
            if (refererPath.IndexOf("OfficerPermits", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return RedirectToAction("OfficerPermits");
            }

            return RedirectToAction("Officers");
        }

        private string[] ParseUserPermits(User user)
        {
            string roleName = user?.Role?.RoleName ?? string.Empty;
            bool isAdmin = roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                           roleName.Equals("Administrator", StringComparison.OrdinalIgnoreCase);

            if (isAdmin)
            {
                return new[] { "All Modules (Admin)" };
            }

            if (user != null && user.AllowedPermits != null)
            {
                return user.AllowedPermits
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim())
                    .ToArray();
            }

            // Fallback only if AllowedPermits has never been saved yet (NULL in DB)
            bool isEnforcement = roleName.IndexOf("Enforcement", StringComparison.OrdinalIgnoreCase) >= 0;
            return isEnforcement ? DefaultEnforcementModules : AllModules;
        }
    }
}