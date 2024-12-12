using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models.Models;
using Bulky.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }


        public IActionResult Index()
        {
            var model = _unitOfWork.Product.GetAll(includeProperties:"Category").ToList();
            
            
            return View(model);
        }

        [HttpGet]
        public IActionResult Upsert(int? id) //Update & Insert
        {
            IEnumerable<SelectListItem> CategoryList = _unitOfWork.Category.GetAll()
                                                                  .Select(u => new SelectListItem
                                                                  {
                                                                      Text = u.Name,
                                                                      Value = u.Id.ToString()
                                                                  });
            //ViewBag.CategoryList = CategoryList;
            //ViewData["CategoryList"]=CategoryList;
            ProductVM productVM = new()
            {
                CategoryList = CategoryList,
                Product = new Product()
            };
            if(id is null || id == 0)
            {
                // For Create
                return View(productVM);
            }
            else
            {
                // For Update
                productVM.Product = _unitOfWork.Product.Get(u => u.Id == id);
                return View(productVM);
            }
        }


        [HttpPost]
        public IActionResult Upsert(ProductVM model,IFormFile? file)
        {
            if(ModelState.IsValid)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                if(file is not null)
                {
                    string fileName = Guid.NewGuid().ToString()+Path.GetExtension(file.FileName);
                    string productPath = Path.Combine(wwwRootPath, @"images\product");

                    if (!string.IsNullOrEmpty(model.Product.ImageUrl))
                    {
                        // Delete the old image
                        var oldPathImage=Path.Combine(wwwRootPath,model.Product.ImageUrl.TrimStart('\\'));
                        if(System.IO.File.Exists(oldPathImage))
                        {
                            System.IO.File.Delete(oldPathImage);
                        }
                    }
                    
                    using(var fileStream=new FileStream(Path.Combine(productPath,fileName), FileMode.Create))
                    {
                        file.CopyTo(fileStream);

                    }
                    model.Product.ImageUrl = @"\images\product\" + fileName;
                }
                if(model.Product.Id == 0)
                {
                    _unitOfWork.Product.Add(model.Product);

                }
                else
                {
                    _unitOfWork.Product.Update(model.Product);

                }
                _unitOfWork.Save();
                TempData["success"] = "Product Created Successfully";
                return RedirectToAction("Index");
            }
            else
            {
                model.CategoryList = _unitOfWork.Category.GetAll().Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                });
            }
            return View(model);

        }


        //[HttpGet]
        //public IActionResult Edit(int? id)
        //{
        //    if (id is null) return BadRequest();

        //    var model = _unitOfWork.Product.Get(u => u.Id == id);

        //    if(model is null) return NotFound();

        //    return View(model);
        //}


        //[HttpPost]
        //public IActionResult Edit(Product model)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        _unitOfWork.Product.Update(model);
        //        _unitOfWork.Save();
        //        TempData["success"] = "Product Updated Successfully";
        //        return RedirectToAction("Index");
        //    }

        //    return View(model);
        //}

        //[HttpGet]
        //public IActionResult Delete(int? id)
        //{
        //    if (id is null) return BadRequest();

        //    var model = _unitOfWork.Product.Get(u => u.Id == id);

        //    if (model is null) return NotFound();

        //    return View(model);
        //}


        //[HttpPost,ActionName("Delete")]
        //public IActionResult DeletePost(int? id)
        //{
        //    if (id is null) return BadRequest();

        //    var model = _unitOfWork.Product.Get(u => u.Id == id);

        //    if (model is null) return NotFound();

        //    _unitOfWork.Product.Remove(model);
        //    _unitOfWork.Save();
        //    TempData["success"] = "Product Deleted Successfully";

        //    return RedirectToAction("Index");

        //}


        #region API Calls

        [HttpGet]
        public IActionResult GetAll()
        {
            var model = _unitOfWork.Product.GetAll(includeProperties: "Category").ToList();

            return Json(new {data=model});

        }

        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var modelToBeDeleted = _unitOfWork.Product.Get(u => u.Id == id);


            if(modelToBeDeleted is null)
                return Json(new {success=false,message="Error While Deleting"});

            var oldPathImage = Path.Combine(_webHostEnvironment.WebRootPath, modelToBeDeleted.ImageUrl.TrimStart('\\'));
           
            if (System.IO.File.Exists(oldPathImage))
            {
                System.IO.File.Delete(oldPathImage);
            }

            _unitOfWork.Product.Remove(modelToBeDeleted);
            _unitOfWork.Save();

            return Json(new { success = true, message = "Deleted Successful" });


        }



        #endregion


    }
}
