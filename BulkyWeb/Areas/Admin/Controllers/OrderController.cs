using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Diagnostics;
using System.Security.Claims;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class OrderController : Controller
    {

        private readonly IUnitOfWork _unitOfWork;
        [BindProperty]
        public OrderVM OrderVM { get; set; }

        private readonly IEmailSender _emailSender;


        public OrderController(IUnitOfWork unitOfWork, IEmailSender emailSender)
        {
            _unitOfWork = unitOfWork;
            _emailSender = emailSender;
        }



        public IActionResult Index()
        {
            return View();
        }




        public IActionResult Details(int orderId)
        {

            OrderVM = new()
            {
                OrderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderId, includeProperties: "ApplicationUser"),
                OrderDetail = _unitOfWork.OrderDetail.GetAll(u => u.OrderHeaderId == orderId, includeProperties: "Product")
            };


            return View(OrderVM);
        }


        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult UpdateOrderDetail()
        {

            var orderHeaderFromDb = _unitOfWork.OrderHeader.Get(u => u.Id == OrderVM.OrderHeader.Id);

            if (orderHeaderFromDb is null)
            {
                TempData["Error"] = "Order Details Faild To Update";
                return RedirectToAction(nameof(Details), new { orderId = orderHeaderFromDb.Id });
            }

            try
            {
                orderHeaderFromDb.Name = OrderVM.OrderHeader.Name;
                orderHeaderFromDb.PhoneNumber = OrderVM.OrderHeader.PhoneNumber;
                orderHeaderFromDb.StreetAddress = OrderVM.OrderHeader.StreetAddress;
                orderHeaderFromDb.City = OrderVM.OrderHeader.City;
                orderHeaderFromDb.State = OrderVM.OrderHeader.State;
                orderHeaderFromDb.PostalCode = OrderVM.OrderHeader.PostalCode;

                if (!string.IsNullOrEmpty(OrderVM.OrderHeader.Carrier))
                {
                    orderHeaderFromDb.Carrier = OrderVM.OrderHeader.Carrier;
                }

                if (!string.IsNullOrEmpty(OrderVM.OrderHeader.TrackingNumber))
                {
                    orderHeaderFromDb.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
                }

                _unitOfWork.OrderHeader.Update(orderHeaderFromDb);
                _unitOfWork.Save();

                TempData["Success"] = "Order Details Updated Successfully";

                return RedirectToAction(nameof(Details), new { orderId = orderHeaderFromDb.Id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Order Details Faild To Update {ex.Message}";
                return RedirectToAction(nameof(Details), new { orderId = orderHeaderFromDb.Id });
            }


        }



        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult StartProcessing()
        {
            try
            {
                _unitOfWork.OrderHeader.UpdateStatus(OrderVM.OrderHeader.Id, SD.StatusInProcess);
                _unitOfWork.Save();

                TempData["Success"] = "Order Details Updated Successfully";

                return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });

            }
            catch (Exception e)
            {
                TempData["Error"] = $"Order Details Faild To Update {e.Message}";
                return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });

            }
        }



        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult ShipOrder()
        {

            var orderHeaderFromDb = _unitOfWork.OrderHeader.Get(u => u.Id == OrderVM.OrderHeader.Id);

            if (orderHeaderFromDb is not null)
            {

                try
                {

                    orderHeaderFromDb.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
                    orderHeaderFromDb.Carrier = OrderVM.OrderHeader.Carrier;
                    orderHeaderFromDb.OrderStatus = SD.StatusShipped;
                    orderHeaderFromDb.ShippingDate = DateTime.Now;

                    if (orderHeaderFromDb.PaymentStatus == SD.PaymentStatusDelayedPayment)
                    {
                        orderHeaderFromDb.PaymentDueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(30));
                    }

                    _unitOfWork.OrderHeader.Update(orderHeaderFromDb);
                    _unitOfWork.Save();

                    TempData["Success"] = "Order Details Updated Successfully";

                    _emailSender.SendEmailAsync(OrderVM.OrderHeader.ApplicationUser.Email, "Order Status - Bulky Book", $"<p>Your Order Has Been Shipped - {OrderVM.OrderHeader.Id}</p>");


                    return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });

                }
                catch (Exception e)
                {
                    TempData["Error"] = $"Order Details Faild To Update {e.Message}";
                    return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });

                }


            }
            TempData["Error"] = $"Order Details Faild To Update";
            return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });
        }





        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult CancelOrder()
        {

            try
            {
                var orderHeaderFromDb = _unitOfWork.OrderHeader.Get(u => u.Id == OrderVM.OrderHeader.Id);

                if (orderHeaderFromDb is not null)
                {
                    if (orderHeaderFromDb.PaymentStatus == SD.PaymentStatusApprovrd)
                    {
                        var options = new RefundCreateOptions
                        {
                            Reason = RefundReasons.RequestedByCustomer,
                            PaymentIntent = orderHeaderFromDb.PaymentIntentId
                        };

                        var service = new RefundService();
                        Refund refund = service.Create(options);

                        _unitOfWork.OrderHeader.UpdateStatus(orderHeaderFromDb.Id, SD.StatusCancelled, SD.StatusRefunded);
                    }
                    else
                    {
                        _unitOfWork.OrderHeader.UpdateStatus(orderHeaderFromDb.Id, SD.StatusCancelled, SD.StatusCancelled);
                    }
                    _unitOfWork.Save();

                    TempData["Success"] = "Order Cancelled Successfully";

                    return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });


                }
            }
            catch (Exception e)
            {


                TempData["Error"] = $"Order Faild To Canceld {e.Message}";
                return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });

            }
            TempData["Error"] = $"Order Faild To Canceld";
            return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });

        }


        [ActionName("Details")]
        [HttpPost]
        public IActionResult Details_PAY_NOW()
        {
            try
            {

                OrderVM.OrderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == OrderVM.OrderHeader.Id, includeProperties: "ApplicationUser");
                OrderVM.OrderDetail = _unitOfWork.OrderDetail.GetAll(u => u.OrderHeaderId == OrderVM.OrderHeader.Id, includeProperties: "Product");

                // Stripe Logic

                var domin = "https://localhost:44304/";

                var options = new Stripe.Checkout.SessionCreateOptions
                {
                    SuccessUrl = domin + $"admin/order/PaymentConfirmation?orderHeaderId={OrderVM.OrderHeader.Id}",
                    CancelUrl = domin + $"admin/order/details?orderId={OrderVM.OrderHeader.Id}",
                    LineItems = new List<SessionLineItemOptions>(),
                    Mode = "payment"
                };


                foreach (var item in OrderVM.OrderDetail)
                {
                    var sessionLineItem = new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(item.Price * 100), // Convert From Doller To  سنت امريكي Penny  ( $20.50 ====> 2050 Penny)
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = item.Product.Title
                            }
                        },
                        Quantity = item.Count
                    };
                    options.LineItems.Add(sessionLineItem);
                }

                var service = new SessionService();
                var session = service.Create(options);

                _unitOfWork.OrderHeader.UpdateStripePaymentID(OrderVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
                _unitOfWork.Save();

                Response.Headers.Add("Location", session.Url);

                return new StatusCodeResult(303);

            }
            catch (Exception e)
            {

                TempData["Error"] = $"Payment Faild {e.Message}";
                return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });
            }

        }






        public IActionResult PaymentConfirmation(int orderHeaderId)
        {

            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderHeaderId);

            if (orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment)
            {
                // This is an Order By Company

                var service = new SessionService();

                var session = service.Get(orderHeader.SessionId);

                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _unitOfWork.OrderHeader.UpdateStripePaymentID(orderHeaderId, session.Id, session.PaymentIntentId);
                    _unitOfWork.OrderHeader.UpdateStatus(orderHeaderId, orderHeader.OrderStatus, SD.PaymentStatusApprovrd);
                    _unitOfWork.Save();

                }
            }


            return View(orderHeaderId);
        }








        #region API Calls
        [HttpGet]
        public IActionResult GetAll(string status)
        {
            IEnumerable<OrderHeader> model = null;

            if (User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
            {
                model = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser").ToList();
            }
            else
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

                if (userId is not null)
                {
                    model = _unitOfWork.OrderHeader.GetAll(u => u.ApplicationUserId == userId, includeProperties: "ApplicationUser");

                }




            }

            switch (status)
            {

                case "pending":
                    model = model.Where(o => o.PaymentStatus == SD.PaymentStatusPending);
                    break;

                case "inprocess":
                    model = model.Where(o => o.OrderStatus == SD.StatusInProcess);
                    break;

                case "completed":
                    model = model.Where(o => o.OrderStatus == SD.StatusShipped);
                    break;

                case "approved":
                    model = model.Where(o => o.OrderStatus == SD.StatusApproved);
                    break;

                default:
                    break;

            }
            return Json(new { data = model });

        }



        #endregion




    }
}
