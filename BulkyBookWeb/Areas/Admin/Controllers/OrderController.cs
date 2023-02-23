using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Diagnostics;
using System.Security.Claims;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class OrderController : Controller
    {
        public readonly IUnitOfWork _unitOfWork;
        [BindProperty]
        public OrderVM OrderVM { get; set; }
        public OrderController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Details(int orderId)
        {
            OrderVM = new OrderVM()
            {
                OrderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == orderId, includeProperties: "ApplicationUser"),
                OrderDetails = _unitOfWork.OrderDetail.GetAll(u => u.OrderId == orderId, includeProperties: "Product")
            };
            return View(OrderVM);
        }
        [ActionName("Details")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Details_Pay_Now(int orderId)
        {

            OrderVM.OrderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == OrderVM.OrderHeader.Id, includeProperties: "ApplicationUser");
            OrderVM.OrderDetails = _unitOfWork.OrderDetail.GetAll(u => u.OrderId == OrderVM.OrderHeader.Id, includeProperties: "Product");

            //Stripe Settings
            var domain = "https://localhost:44393/";
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card", },
                LineItems = new List<SessionLineItemOptions>(),

                Mode = "payment",
                SuccessUrl = domain + $"Admin/Order/PaymentConfirmation?orderHeaderId={OrderVM.OrderHeader.Id}",
                CancelUrl = domain + $"Admin/Order/Details?orderId={OrderVM.OrderHeader.Id}",
            };
            foreach (var item in OrderVM.OrderDetails)
            {
                var sessionLineItem = new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)item.Price,
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Product.Title,
                        },
                    },
                    Quantity = item.Count,
                };
                options.LineItems.Add(sessionLineItem);
            }
            var service = new SessionService();
            Session session = service.Create(options);
            _unitOfWork.OrderHeader.UpdateStripePaymentID(OrderVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
            _unitOfWork.Save();
            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);
        }

        public IActionResult PaymentConfirmation(int orderHeaderId)
        {
            OrderHeader orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == orderHeaderId);
            if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId);
                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _unitOfWork.OrderHeader.UpdateStripePaymentID(orderHeaderId, orderHeader.SessionId, session.PaymentIntentId);
                    _unitOfWork.OrderHeader.UpdateStatus(orderHeaderId, orderHeader.OrderStatus, SD.PaymentStatusApproved);
                    _unitOfWork.Save();
                }
            }
      
            return View(orderHeaderId);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateOrderDetails()
        {
            var OrderHeaderFromDb= _unitOfWork.OrderHeader.GetFirstOrDefault(u=>u.Id==OrderVM.OrderHeader.Id,tracked:false);
            OrderHeaderFromDb.Name = OrderVM.OrderHeader.Name;
            OrderHeaderFromDb.PhoneNumber = OrderVM.OrderHeader.PhoneNumber;
            OrderHeaderFromDb.StreetAddress = OrderVM.OrderHeader.StreetAddress;
            OrderHeaderFromDb.City = OrderVM.OrderHeader.City;
            OrderHeaderFromDb.State =  OrderVM.OrderHeader.State;
            OrderHeaderFromDb.PostalCode = OrderVM.OrderHeader.PostalCode;
            if(OrderVM.OrderHeader.Carrier != null)
            {
                OrderHeaderFromDb.Carrier = OrderVM.OrderHeader.Carrier;
            }
            if (OrderVM.OrderHeader.TrackingNumber != null)
            {
                OrderHeaderFromDb.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
            }
            _unitOfWork.OrderHeader.Update(OrderHeaderFromDb);
            _unitOfWork.Save();
            TempData["success"] = "Order Details Updated Successfully";
            return RedirectToAction("Details", "Order", new { orderId = OrderHeaderFromDb.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult StartProcessing()
        {
            _unitOfWork.OrderHeader.UpdateStatus(OrderVM.OrderHeader.Id, SD.StatusInProcess);
            _unitOfWork.Save();
            TempData["success"] = "Order In process Successfully";
            return RedirectToAction("Details", "Order", new { orderId = OrderVM.OrderHeader.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ShipOrder()
        {
            var OrderHeaderFromDb = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == OrderVM.OrderHeader.Id, tracked: false);
            OrderHeaderFromDb.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
            OrderHeaderFromDb.Carrier = OrderVM.OrderHeader.Carrier;
            OrderHeaderFromDb.OrderStatus = SD.StatusShipped;
            OrderHeaderFromDb.ShippingDate = DateTime.Now;
            if (OrderHeaderFromDb.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                OrderHeaderFromDb.PaymentDueDate = DateTime.Now.AddDays(30);
            }
            _unitOfWork.OrderHeader.Update(OrderHeaderFromDb);
            _unitOfWork.Save();
            TempData["success"] = "Order Shipped Successfully";
            return RedirectToAction("Details", "Order", new { orderId = OrderHeaderFromDb.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult CancelOrder()
        {
            var OrderHeaderFromDb = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == OrderVM.OrderHeader.Id, tracked: false);
            if (OrderHeaderFromDb.PaymentStatus == SD.PaymentStatusApproved)
            {
                var options = new RefundCreateOptions
                {
                    Reason = RefundReasons.RequestedByCustomer,
                    PaymentIntent = OrderHeaderFromDb.PaymentIntentId
                };
                var service = new RefundService();
                Refund refund = service.Create(options);
                _unitOfWork.OrderHeader.UpdateStatus(OrderHeaderFromDb.Id, SD.StatusCancelled, SD.StatusRefunded);
            }

            else
            {
                _unitOfWork.OrderHeader.UpdateStatus(OrderHeaderFromDb.Id, SD.StatusCancelled, SD.StatusCancelled);
            }

            _unitOfWork.Save();
            TempData["success"] = "Order Cancelled Successfully";
            return RedirectToAction("Details", "Order", new { orderId = OrderVM.OrderHeader.Id });
        }

        #region API Calls
        [HttpGet]
        public IActionResult GetAll(string status)
        {
            IEnumerable<OrderHeader> orderHeaders;

            if (User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
            {
                orderHeaders = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser");
            }
            else
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
                orderHeaders = _unitOfWork.OrderHeader.GetAll(u=>u.ApplicationUserId == claim.Value,includeProperties: "ApplicationUser");
            }
            switch (status)
            {
                case "pending":
                    orderHeaders = orderHeaders.Where(u => u.PaymentStatus == SD.PaymentStatusDelayedPayment);
                    break;

                case "inprocess":
                    orderHeaders = orderHeaders.Where(u => u.OrderStatus == SD.StatusInProcess);
                    break;

                case "completed":
                    orderHeaders = orderHeaders.Where(u => u.OrderStatus == SD.StatusShipped);
                    break;

                case "approved":
                    orderHeaders = orderHeaders.Where(u => u.OrderStatus == SD.StatusApproved);
                    break;

                default:
                    break;
            }
            return Json(new {data = orderHeaders});
        }

        #endregion
    }
}
