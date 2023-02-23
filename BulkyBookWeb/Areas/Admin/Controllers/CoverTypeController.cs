using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CoverTypeController : Controller
    {
        private readonly IUnitOfWork _db;
        public CoverTypeController(IUnitOfWork db)
        {
            _db = db;
        }
        public IActionResult Index()
        {
            IEnumerable<CoverType> objCoverTypeList = _db.CoverType.GetAll();
            return View(objCoverTypeList);
        }

        //GET
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(CoverType obj)
        {
            if (ModelState.IsValid)
            {
                _db.CoverType.Add(obj);
                _db.Save();
                TempData["Success"] = "Cover Type Created Successfully";
                return RedirectToAction("Index");
            }
            return View(obj);
        }

        //GET
        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            //var editDetailObj = _db.Categories.Find(id);
            var editDetailObj = _db.CoverType.GetFirstOrDefault(u => u.Id == id);
            if (editDetailObj == null)
            {
                return NotFound();
            }
            return View(editDetailObj);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(CoverType obj)
        {
            if (ModelState.IsValid)
            {
                _db.CoverType.Update(obj);
                _db.Save();
                TempData["Success"] = "Cover Type Updated Successfully";
                return RedirectToAction("Index");
            }
            return View(obj);
        }

        //GET
        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            var deleteDetailObj = _db.CoverType.GetFirstOrDefault(u => u.Id == id);
            if (deleteDetailObj == null)
            {
                return NotFound();
            }
            return View(deleteDetailObj);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePost(int? id)
        {
            var obj = _db.CoverType.GetFirstOrDefault(u => u.Id == id);
            if (obj == null)
            {
                return NotFound();
            }
            _db.CoverType.Delete(obj);
            _db.Save();
            TempData["Success"] = "Cover Type Deleted Successfully";
            return RedirectToAction("Index");
        }
    }
}
