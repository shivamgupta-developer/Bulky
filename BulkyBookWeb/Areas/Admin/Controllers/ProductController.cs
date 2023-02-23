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
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _db;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public ProductController(IUnitOfWork db, IWebHostEnvironment webHostEnvironment)
        {
            _db = db;
            _webHostEnvironment = webHostEnvironment;
        }
        public IActionResult Index()
        {
            IEnumerable<Product> objProductList = _db.Product.GetAll();
            return View(objProductList);
        }

        //GET
        public IActionResult Upsert(int? id)
        {
            Product product = new();
            ProductVM productVM = new ProductVM()
            {
                Product = new(),
                CategoryList = _db.Category.GetAll().Select(
                u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                }),

                CoverTypeList = _db.CoverType.GetAll().Select(
                  u => new SelectListItem
                  {
                      Text = u.Name,
                      Value = u.Id.ToString()
                  })
            };
            

            if (id == null || id == 0)
            {
                //create product
                //ViewBag.CategoryList = CategoryList;
                //ViewData["CoverTypeList"] = CoverTypeList;
                return View(productVM);
            }
            else
            {
                //update product
               productVM.Product = _db.Product.GetFirstOrDefault(u => u.Id == id);
                return View(productVM);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(ProductVM obj, IFormFile file)
        {
            if (ModelState.IsValid)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                if (file != null)
                {
                    string fileName = Guid.NewGuid().ToString();
                    var path = Path.Combine(wwwRootPath, @"images\products");
                    var extension = Path.GetExtension(file.FileName);
                    if (obj.Product.ImageURL != null)
                    {
                        var rootPath = Path.Combine(wwwRootPath, obj.Product.ImageURL.TrimStart('\\'));
                        if(System.IO.File.Exists(rootPath))
                        {
                            System.IO.File.Delete(rootPath);
                        }
                    }

                    using(var fileStreams = new FileStream(Path.Combine(path, fileName + extension), FileMode.Create))
                    {
                        file.CopyTo(fileStreams);
                    }
                    obj.Product.ImageURL = @"\images\products\" + fileName + extension;
                }
                if (obj.Product.Id == 0)
                {
                    _db.Product.Add(obj.Product);
                }
                else
                {
                    _db.Product.update(obj.Product);
                }
                _db.Save();
                if (obj.Product.Id == 0)
                {
                    TempData["Success"] = "Product Created Successfully";
                }
                else
                {
                    TempData["Success"] = "Product Updated Successfully";
                }
                return RedirectToAction("Index");
            }
            return View(obj);
        }
       
        #region API CALLS
        [HttpGet]
        public IActionResult GetAll()
        {
            var productList = _db.Product.GetAll(includeProperties:"Category,CoverType");
            return Json(new { data = productList });
        }

        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var obj = _db.Product.GetFirstOrDefault(u => u.Id == id);
            if (obj == null)
            {
                return Json(new { success = false, message = "Error while Deleting" });
            }
            var rootPath = Path.Combine(_webHostEnvironment.WebRootPath, obj.ImageURL.TrimStart('\\'));
            if (System.IO.File.Exists(rootPath))
            {
                System.IO.File.Delete(rootPath);
            }
            _db.Product.Delete(obj);
            _db.Save();
            return Json(new { success = true, message = "Data Deleted Successfully" });
            //TempData["Success"] = "Product Deleted Successfully";
           // return RedirectToAction("Index");
        }
        #endregion
    }
}
