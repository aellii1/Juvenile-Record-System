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

                // 2. Query the user and include Role navigation property
                var user = db.Users.Include(u => u.Role).FirstOrDefault(u => u.Username == username);

                if (user != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Found user: {user.Username}, Status: {user.AccountStatus}");

                    // 3. Check if account status is active (case-insensitive)
                    bool isActive = string.Equals(user.AccountStatus, "ACTIVE", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(user.AccountStatus, "Active", StringComparison.OrdinalIgnoreCase);

                    if (!isActive)
                    {
                        ModelState.AddModelError("", "Your account is inactive. Please contact the administrator.");
                        return View(model);
                    }

                    // 4. Verify password (plain match fallback or hashed)
                    bool passwordMatched = (user.PasswordHash == model.Password) ||
                                         PasswordHasher.VerifyPassword(model.Password, user.PasswordHash);

                    if (passwordMatched)
                    {
                        string roleName = user.Role?.RoleName ?? db.Roles.Find(user.RoleId)?.RoleName ?? string.Empty;

                        // Store Session Data
                        Session["UserId"] = user.UserId;
                        Session["Username"] = user.Username;
                        Session["FullName"] = !string.IsNullOrEmpty(user.FullName) ? user.FullName : user.Username;
                        Session["RoleId"] = user.RoleId;
                        Session["RoleName"] = roleName;

                        // Load persisted module permits from SQL Server (User.AllowedPermits)
                        if (user.AllowedPermits != null)
                        {
                            Session["AllowedPermits"] = user.AllowedPermits
                                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(p => p.Trim())
                                .ToArray();
                        }
                        else
                        {
                            bool isEnforcement = roleName.IndexOf("Enforcement", StringComparison.OrdinalIgnoreCase) >= 0;
                            Session["AllowedPermits"] = isEnforcement
                                ? new[] { "ScanBiometrics", "Authenticate", "CheckRecords", "ManageProfile" }
                                : new[] { "ScanBiometrics", "Authenticate", "CheckRecords", "ManageProfile", "ProcessPenalty", "JuvenileRecords" };
                        }

                        // Tell AdminController to display welcome toast
                        Session["ShowWelcomeMessage"] = true;

                        return RedirectUserByRole(roleName);
                    }
                }

                ModelState.AddModelError("", "Invalid username or password.");
            }

            return View(model);
        }

        // Helper Method for Role-Based Redirection
        private ActionResult RedirectUserByRole(string role)
        {
            string normalizedRole = (role ?? "").Trim().ToLower();

            switch (normalizedRole)
            {
                case "admin":
                case "administrator":
                    return RedirectToAction("Index", "Admin");

                case "enforcement":
                case "enforcement officer":
                case "officer":
                case "review officer":
                    return RedirectToAction("Index", "Enforcement");

                default:
                    Session.Clear();
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