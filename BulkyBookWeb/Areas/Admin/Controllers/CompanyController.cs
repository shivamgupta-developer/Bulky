using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CompanyController : Controller
    {
        private readonly IUnitOfWork _db;
        public CompanyController(IUnitOfWork db)
        {
            _db = db;
        }
        public IActionResult Index()
        {
            IEnumerable<Company> objCompanyList = _db.Company.GetAll();
            return View(objCompanyList);
        }

        //GET
        public IActionResult Upsert(int? id)
        {
            Company company = new();

            if (id == null || id == 0)
            {
                //create product
                //ViewBag.CategoryList = CategoryList;
                //ViewData["CoverTypeList"] = CoverTypeList;
                return View(company);
            }
            else
            {
                //update product
                company = _db.Company.GetFirstOrDefault(u => u.Id == id);
                return View(company);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(Company obj)
        {
            if (ModelState.IsValid)
            {

                if (obj.Id == 0)
                {
                    _db.Company.Add(obj);
                    TempData["Success"] = "Company Created Successfully";
                }
                else
                {
                    _db.Company.Update(obj);
                    TempData["Success"] = "Company Updated Successfully";
                }
                _db.Save();
                return RedirectToAction("Index");
            }
            return View(obj);
        }

        #region API CALLS
        [HttpGet]
        public IActionResult GetAll()
        {
            var companyList = _db.Company.GetAll();
            return Json(new { data = companyList });
        }

        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var obj = _db.Company.GetFirstOrDefault(u => u.Id == id);

            _db.Company.Delete(obj);
            _db.Save();
            return Json(new { success = true, message = "Data Deleted Successfully" });
            //TempData["Success"] = "Product Deleted Successfully";
            // return RedirectToAction("Index");
        }
        #endregion
    }
}
