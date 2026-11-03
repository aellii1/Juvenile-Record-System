using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Juvenile_Record_System.Models;

public class JuvenilesController : Controller
{
    private JuvenileContext db = new JuvenileContext();

    // GET: Juveniles (List & Search matching GUI specs)
    public ActionResult Index(string searchString)
    {
        var juveniles = db.Juveniles.AsQueryable();

        if (!String.IsNullOrEmpty(searchString))
        {
            juveniles = juveniles.Where(j => j.FullName.Contains(searchString) || j.JuvenileId.ToString().Contains(searchString));
        }

        return View(juveniles.ToList());
    }

    // GET: Juveniles/Create (Register new juvenile profile)
    public ActionResult Create()
    {
        return View();
    }

    // POST: Juveniles/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Create([Bind(Include = "FullName,BirthDate,Address")] Juvenile juvenile, string guardianName, string relationship, string contactNo)
    {
        if (ModelState.IsValid)
        {
            // Set default creation audit metadata (e.g., created by logged-in user ID 1 for now)
            juvenile.CreatedBy = 1;
            juvenile.CreatedAt = DateTime.Now;
            juvenile.RecordStatus = "Active";

            db.Juveniles.Add(juvenile);
            db.SaveChanges();

            // Save Guardian details linked to this juvenile
            if (!String.IsNullOrEmpty(guardianName))
            {
                Guardian guardian = new Guardian
                {
                    JuvenileId = juvenile.JuvenileId,
                    FullName = guardianName,
                    Relationship = relationship,
                    ContactNo = contactNo
                };
                db.Guardians.Add(guardian);
                db.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        return View(juvenile);
    }

    // GET: Juveniles/Edit/5 (Update existing profile)
    public ActionResult Edit(int? id)
    {
        if (id == null)
        {
            return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        }
        Juvenile juvenile = db.Juveniles.Find(id);
        if (juvenile == null)
        {
            return HttpNotFound();
        }
        return View(juvenile);
    }

    // POST: Juveniles/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Edit([Bind(Include = "JuvenileId,FullName,BirthDate,Address,RecordStatus,CreatedBy,CreatedAt")] Juvenile juvenile)
    {
        if (ModelState.IsValid)
        {
            juvenile.UpdatedAt = DateTime.Now;
            juvenile.UpdatedBy = 1; // Current user ID placeholder
            db.Entry(juvenile).State = EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("Index");
        }
        return View(juvenile);
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