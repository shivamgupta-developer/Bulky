using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles =SD.Role_Admin)]
    public class CategoryController : Controller
    {
        private readonly IUnitOfWork _db;
        public CategoryController(IUnitOfWork db)
        {
            _db = db;
        }
        public IActionResult Index()
        {
            IEnumerable<Category> objCategoryList = _db.Category.GetAll();
            return View(objCategoryList);
        }

        //GET
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Category obj)
        {
            if (obj.Name == obj.DisplayOrder.ToString())
            {
                ModelState.AddModelError("Name", "The Name and Display Order cannot be same");
            }
            if (ModelState.IsValid)
            {
                _db.Category.Add(obj);
                _db.Save();
                TempData["Success"] = "Category Created Successfully";
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
            var editDetailObj = _db.Category.GetFirstOrDefault(u => u.Id == id);
            if (editDetailObj == null)
            {
                return NotFound();
            }
            return View(editDetailObj);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Category obj)
        {
            if (obj.Name == obj.DisplayOrder.ToString())
            {
                ModelState.AddModelError("Name", "The Name and Display Order cannot be same");
            }
            if (ModelState.IsValid)
            {
                _db.Category.Update(obj);
                _db.Save();
                TempData["Success"] = "Category Updated Successfully";
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
            //var deleteDetailObj = _db.Categories.Find(id);
            var deleteDetailObj = _db.Category.GetFirstOrDefault(u => u.Id == id);
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
            var obj = _db.Category.GetFirstOrDefault(u => u.Id == id);
            if (obj == null)
            {
                return NotFound();
            }
            _db.Category.Delete(obj);
            _db.Save();
            TempData["Success"] = "Category Deleted Successfully";
            return RedirectToAction("Index");
        }
    }
}
