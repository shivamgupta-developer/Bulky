using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace BulkyBookWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly IUnitOfWork _db;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public HomeController(IUnitOfWork db, IWebHostEnvironment webHostEnvironment)
        {
            _db = db;
            _webHostEnvironment = webHostEnvironment;
        }
        public IActionResult Index()
        {
            IEnumerable<Product> objProductList = _db.Product.GetAll(includeProperties: "Category,CoverType");
            return View(objProductList);
        }

        public IActionResult Details(int productId)
        {
            ShoppingCart cartObj = new()
            {
                Count = 1,
                ProductId= productId,
                Product = _db.Product.GetFirstOrDefault(u => u.Id == productId, includeProperties: "Category,CoverType")
            };     
            return View(cartObj);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult Details(ShoppingCart shoppingCart)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            shoppingCart.ApplicationUserId = claim.Value;
            ShoppingCart cartobj = _db.ShoppingCart.GetFirstOrDefault(u => u.ApplicationUserId == claim.Value && u.ProductId == shoppingCart.ProductId);
            if (cartobj == null)
            {
                _db.ShoppingCart.Add(shoppingCart);
                _db.Save();
                HttpContext.Session.SetInt32(SD.SessionCart, _db.ShoppingCart.GetAll(u => u.ApplicationUserId == claim.Value).ToList().Count);
            }
            else
            {
                _db.ShoppingCart.IncrementCount(cartobj, shoppingCart.Count);
                _db.Save();
            }
            return RedirectToAction(nameof(Index));
        }
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}