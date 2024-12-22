using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models.Models;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles =SD.Role_Admin)]
    public class CategoryController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public CategoryController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            var objCategory = _unitOfWork.Category.GetAll();

            return View(objCategory);
        }


        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Category model) 
        {
            if (model.Name == model.DisplayOrder.ToString())
            {
                ModelState.AddModelError("name", "The Display Order Cannot Exactly Match The Name.");
            }


            if (ModelState.IsValid)
            {
                _unitOfWork.Category.Add(model);
                _unitOfWork.Save();
                TempData["success"] = "Category Created Successfully";
                return RedirectToAction("Index");
            }


            return View(model);
        }



        public IActionResult Edit(int? id)
        {
            if (id is null || id == 0) return NotFound();

            var model = _unitOfWork.Category.Get(u => u.Id == id);

            if (model is null) return NotFound();

            return View(model);
        }



        [HttpPost]
        public IActionResult Edit(Category model)
        {
            if (model.Name == model.DisplayOrder.ToString())
            {
                ModelState.AddModelError("name", "The Display Order Cannot Exactly Match The Name.");
            }


            if (ModelState.IsValid)
            {
                _unitOfWork.Category.Update(model);
                _unitOfWork.Save();
                TempData["success"] = "Category Updated Successfully";

                return RedirectToAction("Index");
            }


            return View(model);
        }



        public IActionResult Delete(int? id)
        {
            if (id is null || id == 0) return NotFound();

            var model = _unitOfWork.Category.Get(u => u.Id == id);


            if (model is null) return NotFound();

            return View(model);
        }


        [HttpPost, ActionName("Delete")]
        public IActionResult DeletePost(int? id)
        {
            var model = _unitOfWork.Category.Get(u => u.Id == id);


            if (model is null) return NotFound();

            _unitOfWork.Category.Remove(model);
            _unitOfWork.Save();
            TempData["success"] = "Category Deleted Successfully";
            return RedirectToAction("Index");

        }




    }
}
