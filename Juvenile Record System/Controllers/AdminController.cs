using Juvenile_Record_System.Helpers;
using Juvenile_Record_System.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace Juvenile_Record_System.Controllers
{
    [AuthorizeRole("Admin")]
    public class AdminController : Controller
    {
        private JuvenileContext db = new JuvenileContext();
        private const string ActiveAccountStatus = "Active";
        private const string InactiveAccountStatus = "Inactive";

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

        // GET: Admin/Officers
        [HttpGet]
        public ActionResult Officers()
        {
            var officers = db.Users.Include("Role").ToList();

            return View(officers);
        }

        // GET: Admin/AdminModal
        [HttpGet]
        public ActionResult AdminModal(string actionType, int? id)
        {
            ViewBag.ActionType = actionType;

            if (actionType == "assignRole")
            {
                var officer = db.Users.Find(id);
                if (officer == null) return HttpNotFound();

                // Use a unique ViewBag name that doesn't match "RoleId"
                ViewBag.AvailableRoles = new SelectList(db.Roles, "RoleId", "RoleName", officer.RoleId);

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

            // Retrieve active officers to display and manage permits/roles
            var officers = db.Users
                             .Where(u => u.AccountStatus == "ACTIVE")
                             .ToList();

            return View(officers);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AssignRole(int userId, int roleId)
        {
            var officer = db.Users.Find(userId);
            if (officer != null)
            {
                officer.RoleId = roleId;
                db.SaveChanges();
                TempData["SuccessMessage"] = "Officer role updated successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "User not found.";
            }

            return RedirectToAction("Officers");
        }
    }
}

