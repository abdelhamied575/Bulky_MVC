﻿using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models.Models;
using Bulky.Models.Models.Identity;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using System.Security.Claims;

namespace BulkyWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        [BindProperty]
        public ShoppingCartVM ShoppingCartVM { get; set; }

        private readonly IEmailSender _emailSender;

        public CartController(IUnitOfWork unitOfWork, IEmailSender emailSender)
        {
            _unitOfWork = unitOfWork;
            _emailSender = emailSender;
        }


        public IActionResult Index()
        {

            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            ShoppingCartVM = new()
            {
                ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId,
                                                                    includeProperties: "Product"),
                OrderHeader = new()
            };

            foreach (var cart in ShoppingCartVM.ShoppingCartList)
            {
                cart.Price = GetPriceBasedOnQuantity(cart);
                ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }

            return View(ShoppingCartVM);
        }





        public IActionResult Summary()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            ShoppingCartVM = new()
            {
                ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId,
                                                                    includeProperties: "Product"),
                OrderHeader = new()
            };

            ShoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);

            ShoppingCartVM.OrderHeader.Name = ShoppingCartVM.OrderHeader.ApplicationUser.Name;
            ShoppingCartVM.OrderHeader.PhoneNumber = ShoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
            ShoppingCartVM.OrderHeader.StreetAddress = ShoppingCartVM.OrderHeader.ApplicationUser.StreetAddress;
            ShoppingCartVM.OrderHeader.City = ShoppingCartVM.OrderHeader.ApplicationUser.City;
            ShoppingCartVM.OrderHeader.State = ShoppingCartVM.OrderHeader.ApplicationUser.State;
            ShoppingCartVM.OrderHeader.PostalCode = ShoppingCartVM.OrderHeader.ApplicationUser.PostalCode;


            foreach (var cart in ShoppingCartVM.ShoppingCartList)
            {
                cart.Price = GetPriceBasedOnQuantity(cart);
                ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }

            return View(ShoppingCartVM);
        }



        [HttpPost]
        [ActionName("Summary")]
        [ValidateAntiForgeryToken]
        public IActionResult SummaryPOST()
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var claimsIdentity = (ClaimsIdentity)User.Identity;
                    var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

                    ShoppingCartVM.ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId,
                                                                                        includeProperties: "Product");

                    ShoppingCartVM.OrderHeader.OrderDate = DateTime.Now;
                    ShoppingCartVM.OrderHeader.ApplicationUserId = userId;

                    ApplicationUser applicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);



                    foreach (var cart in ShoppingCartVM.ShoppingCartList)
                    {
                        cart.Price = GetPriceBasedOnQuantity(cart);
                        ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
                    }

                    if (applicationUser.CompanyId.GetValueOrDefault() == 0)
                    {
                        // It's a Regular Customer Account And We Need To Capture
                        ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
                        ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
                    }
                    else
                    {
                        // It's a Company User
                        ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
                        ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusApproved;
                    }

                    _unitOfWork.OrderHeader.Add(ShoppingCartVM.OrderHeader);
                    _unitOfWork.Save();

                    foreach (var cart in ShoppingCartVM.ShoppingCartList)
                    {
                        OrderDetail orderDetail = new()
                        {
                            ProductId = cart.ProductId,
                            OrderHeaderId = ShoppingCartVM.OrderHeader.Id,
                            Price = cart.Price,
                            Count = cart.Count
                        };

                        _unitOfWork.OrderDetail.Add(orderDetail);
                        _unitOfWork.Save();
                    }


                    if (applicationUser.CompanyId.GetValueOrDefault() == 0)
                    {
                        // It's a Regular Customer Account And We Need To Capture Payment
                        // Stripe Logic


                        var domin = $"{Request.Scheme}://{Request.Host.Value}/";

                        var options = new Stripe.Checkout.SessionCreateOptions
                        {
                            SuccessUrl = domin + $"Customer/cart/OrderConfirmation?id={ShoppingCartVM.OrderHeader.Id}",
                            CancelUrl = domin + "customer/cart/index",
                            LineItems = new List<SessionLineItemOptions>(),
                            Mode = "payment"
                        };

                        foreach (var item in ShoppingCartVM.ShoppingCartList)
                        {
                            var sessionLineItem = new SessionLineItemOptions
                            {
                                PriceData = new SessionLineItemPriceDataOptions
                                {
                                    UnitAmount = (long)(item.Price * 100), // Convert From Dollar to Cents
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

                        _unitOfWork.OrderHeader.UpdateStripePaymentID(ShoppingCartVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
                        _unitOfWork.Save();

                        Response.Headers.Add("Location", session.Url);

                        return new StatusCodeResult(303);

                    }
                    return RedirectToAction(nameof(OrderConfirmation), new { id = ShoppingCartVM.OrderHeader.Id });
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Operation Faild {ex.Message}";
                return RedirectToAction(nameof(OrderConfirmation), new { id = ShoppingCartVM.OrderHeader.Id });

            }
            TempData["Error"] = $"Operation Faild";
            return RedirectToAction(nameof(OrderConfirmation), new { id = ShoppingCartVM.OrderHeader.Id });

        }


        public IActionResult OrderConfirmation(int id)
        {

            try
            {
                if (ModelState.IsValid)
                {
                    var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == id, includeProperties: "ApplicationUser");

                    if (orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment)
                    {
                        // This is an Order By Customer

                        var service = new SessionService();

                        var session = service.Get(orderHeader.SessionId);

                        if (session.PaymentStatus.ToLower() == "paid")
                        {
                            _unitOfWork.OrderHeader.UpdateStripePaymentID(id, session.Id, session.PaymentIntentId);
                            _unitOfWork.OrderHeader.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApprovrd);
                            _unitOfWork.Save();

                        }

                        HttpContext.Session.Clear();

                    }


                    _emailSender.SendEmailAsync(orderHeader.ApplicationUser.Email, "New Order - Bulky Book Web", $"<p>New Order Created - {orderHeader.Id}</p>");

                    var shoppingCarts = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == orderHeader.ApplicationUserId).ToList();

                    _unitOfWork.ShoppingCart.RemoveRange(shoppingCarts);
                    _unitOfWork.Save();

                    return View(id);
                }
                TempData["Error"] = $"Operation Faild";

                return View(id);
            }
            catch (Exception e)
            {
                TempData["Error"] = $"Operation Faild {e.Message}";

                return View(id);

            }
        }






















        public IActionResult Plus(int cartId)
        {
            var cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId);

            cartFromDb.Count += 1;

            _unitOfWork.ShoppingCart.Update(cartFromDb);
            _unitOfWork.Save();




            return RedirectToAction(nameof(Index));
        }

        public IActionResult Minus(int cartId)
        {
            var cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId,tracked:true);

            if (cartFromDb.Count <= 1)
            {

                HttpContext.Session.SetInt32(SD.SessionCart,
                  _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == cartFromDb.ApplicationUserId).Count() - 1);

                _unitOfWork.ShoppingCart.Remove(cartFromDb);
            }
            else
            {
                cartFromDb.Count -= 1;
                _unitOfWork.ShoppingCart.Update(cartFromDb);
            }
            _unitOfWork.Save();

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Remove(int cartId)
        {
            var cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId,tracked:true);

            if (cartFromDb is not null)
            {
                HttpContext.Session.SetInt32(SD.SessionCart,
                 _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == cartFromDb.ApplicationUserId).Count() - 1);

                _unitOfWork.ShoppingCart.Remove(cartFromDb);
                _unitOfWork.Save();

            }



            return RedirectToAction(nameof(Index));
        }










        private double GetPriceBasedOnQuantity(ShoppingCart shoppingCart)
        {
            if (shoppingCart.Count <= 50)
            {
                return shoppingCart.Product.Price;
            }
            else
            {
                if (shoppingCart.Count <= 100)
                {
                    return shoppingCart.Product.Price50;

                }
                else
                {
                    return shoppingCart.Product.Price100;

                }
            }
        }

    }
}
