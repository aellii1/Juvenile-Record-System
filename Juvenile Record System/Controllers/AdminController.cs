using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using Juvenile_Record_System.Models;

public class AdminController : Controller
{
    private JuvenileContext db = new JuvenileContext();
    private const string ActiveAccountStatus = "Active";
    private const string InactiveAccountStatus = "Inactive";

    // GET: Admin/Index 
    public ActionResult Index(string searchString)
    {
        var users = db.Users.Include("Role").AsQueryable();

        if (!string.IsNullOrEmpty(searchString))
        {
            users = users.Where(u => u.FullName.Contains(searchString) || u.Username.Contains(searchString));
        }

        ViewBag.CurrentFilter = searchString;
        ViewBag.Roles = new SelectList(db.Roles, "RoleId", "RoleName");

        return View(users.ToList());
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
            db.Users.Add(user);
            db.SaveChanges();

            TempData["SuccessMessage"] = "Officer account successfully created!";
            return RedirectToAction("Officers");
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

        var accountStatus = (user.AccountStatus ?? string.Empty).Trim();

        // Standardize to uppercase for database compatibility
        if (string.Equals(accountStatus, "Active", StringComparison.OrdinalIgnoreCase) || string.Equals(accountStatus, "ACTIVE", StringComparison.OrdinalIgnoreCase))
        {
            user.AccountStatus = "ACTIVE";
        }
        else if (string.Equals(accountStatus, "Inactive", StringComparison.OrdinalIgnoreCase) || string.Equals(accountStatus, "INACTIVE", StringComparison.OrdinalIgnoreCase))
        {
            user.AccountStatus = "INACTIVE";
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
                existingUser.FullName = user.FullName;
                existingUser.Username = user.Username;
                existingUser.AccountStatus = user.AccountStatus.ToUpper(); 

                if (!string.IsNullOrEmpty(user.PasswordHash))
                {
                    existingUser.PasswordHash = user.PasswordHash;
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
            }
            else
            {
                return HttpNotFound();
            }
        }

        try
        {
            db.SaveChanges();
            TempData["SuccessMessage"] = "Officer account successfully updated!";
            return RedirectToAction("Officers");
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
        catch (Exception ex)
        {
            var sqlError = ex.InnerException?.InnerException?.Message ?? ex.InnerException?.Message ?? ex.Message;
            ModelState.AddModelError("", "Database Error: " + sqlError);
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
            db.Entry(officer).State = System.Data.Entity.EntityState.Modified;
            db.SaveChanges();

            TempData["SuccessMessage"] = "Officer permit/role updated successfully!";

            return RedirectToAction("OfficerPermits");
        }

        return HttpNotFound();
    }

}

