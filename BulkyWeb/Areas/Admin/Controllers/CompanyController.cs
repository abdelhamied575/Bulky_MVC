using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CompanyController : Controller 
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly IWebHostEnvironment _webHostEnvironment;

        public CompanyController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }


        public IActionResult Index()
        {
            var model = _unitOfWork.Company.GetAll().ToList();
            return View(model);
        }

        [HttpGet]
        public IActionResult Upsert(int? id) //Update & Insert
        {
            if(id is null || id == 0)
            {
                // For Create
                return View(new Company());
            }
            else
            {
                // For Update
                Company model = _unitOfWork.Company.Get(u => u.Id == id);
                return View(model);
            }
        }


        [HttpPost]
        public IActionResult Upsert(Company model)
        {
            if(ModelState.IsValid)
            {
               
                if(model.Id == 0)
                {
                    _unitOfWork.Company.Add(model);

                }
                else
                {
                    _unitOfWork.Company.Update(model);

                }
                _unitOfWork.Save();
                TempData["success"] = "Company Created Successfully";
                return RedirectToAction("Index");
            }
            
            return View(model);

        }


        //[HttpGet]
        //public IActionResult Edit(int? id)
        //{
        //    if (id is null) return BadRequest();

        //    var model = _unitOfWork.Company.Get(u => u.Id == id);

        //    if(model is null) return NotFound();

        //    return View(model);
        //}


        //[HttpPost]
        //public IActionResult Edit(Company model)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        _unitOfWork.Company.Update(model);
        //        _unitOfWork.Save();
        //        TempData["success"] = "Company Updated Successfully";
        //        return RedirectToAction("Index");
        //    }

        //    return View(model);
        //}

        //[HttpGet]
        //public IActionResult Delete(int? id)
        //{
        //    if (id is null) return BadRequest();

        //    var model = _unitOfWork.Company.Get(u => u.Id == id);

        //    if (model is null) return NotFound();

        //    return View(model);
        //}


        //[HttpPost,ActionName("Delete")]
        //public IActionResult DeletePost(int? id)
        //{
        //    if (id is null) return BadRequest();

        //    var model = _unitOfWork.Company.Get(u => u.Id == id);

        //    if (model is null) return NotFound();

        //    _unitOfWork.Company.Remove(model);
        //    _unitOfWork.Save();
        //    TempData["success"] = "Company Deleted Successfully";

        //    return RedirectToAction("Index");

        //}


        #region API Calls

        [HttpGet]
        public IActionResult GetAll()
        {
            var model = _unitOfWork.Company.GetAll().ToList();

            return Json(new {data=model});

        }

        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var modelToBeDeleted = _unitOfWork.Company.Get(u => u.Id == id);


            if(modelToBeDeleted is null)
                return Json(new {success=false,message="Error While Deleting"});

            

            _unitOfWork.Company.Remove(modelToBeDeleted);
            _unitOfWork.Save();

            return Json(new { success = true, message = "Deleted Successful" });


        }



        #endregion


    }
}
