using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models.Models;
using Bulky.Utility;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class OrderController : Controller
    {

        private readonly IUnitOfWork _unitOfWork;

        public OrderController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }



        public IActionResult Index()
        {
            return View();
        }





        #region API Calls

        [HttpGet]
        public IActionResult GetAll(string status)
        {
            IEnumerable<OrderHeader> model = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser").ToList();

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
