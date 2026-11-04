using Juvenile_Record_System.Helpers;
using Juvenile_Record_System.Models;
using System;
using System.Linq;
using System.Web.Mvc;

namespace Juvenile_Record_System.Controllers
{
    public class AuthenticationController : Controller
    {
        private JuvenileContext db = new JuvenileContext();

        // GET: Authentication/Login
        [HttpGet]
        public ActionResult Login()
        {
            // If already logged in, redirect based on active session role
            if (Session["RoleName"] != null)
            {
                return RedirectUserByRole(Session["RoleName"].ToString());
            }
            return View();
        }

        // POST: Authentication/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(Authentication model)
        {
            if (ModelState.IsValid)
            {
                // 1. Clean inputs
                var username = model.Username?.Trim();

                // 2. Query the user by username only first (to check if the user even exists)
                var user = db.Users.FirstOrDefault(u => u.Username == username);

                if (user != null)
                {
                    // Debug check: Print or evaluate account status and password hash
                    System.Diagnostics.Debug.WriteLine($"Found user: {user.Username}, Status: {user.AccountStatus}");

                    // Check if status is active (case-insensitive check to be safe)
                    bool isActive = string.Equals(user.AccountStatus, "ACTIVE", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(user.AccountStatus, "Active", StringComparison.OrdinalIgnoreCase);

                    if (!isActive)
                    {
                        ModelState.AddModelError("", "Your account is inactive. Please contact the administrator.");
                        return View(model);
                    }

                    // check password 
                    bool passwordMatched = (user.PasswordHash == model.Password) ||
                        PasswordHasher.VerifyPassword(model.Password, user.PasswordHash);

                    if (passwordMatched)
                    {
                        // Store Session Data
                        Session["UserId"] = user.UserId;
                        Session["Username"] = user.Username;
                        Session["FullName"] = !string.IsNullOrEmpty(user.FullName) ? user.FullName : user.Username;
                        Session["RoleId"] = user.RoleId;
                        Session["RoleName"] = user.Role?.RoleName;

                        // Tell AdminController to display welcome toast
                        Session["ShowWelcomeMessage"] = true;

                        return RedirectUserByRole(user.Role?.RoleName ?? "");
                    }
                }

                ModelState.AddModelError("", "Invalid username or password.");
            }

            return View(model);
        }

        // Helper Method for Role-Based Redirection
        private ActionResult RedirectUserByRole(string role)
        {
            switch (role.Trim().ToLower())
            {
                case "admin":
                    return RedirectToAction("Index", "Admin");

                case "enforcement":
                    return RedirectToAction("Index", "Enforcement");

                case "review":
                    return RedirectToAction("Index", "Review");

                default:
                    // Fallback route if role is unassigned or unknown
                    TempData["ErrorMessage"] = "Unauthorized role assignment.";
                    return RedirectToAction("Login", "Authentication");
            }
        }

        // GET: Authentication/Logout
        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            TempData["SuccessMessage"] = "You have been logged out successfully.";
            return RedirectToAction("Login", "Authentication");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}