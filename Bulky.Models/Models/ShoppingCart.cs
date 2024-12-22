using Bulky.Models.Models.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.Models.Models
{
    public class ShoppingCart
    {

        public int Id { get; set; }


        public int ProductId { get; set; }

        [ValidateNever]
        [ForeignKey(nameof(ProductId))]
        public Product Product { get; set; }



        [Range(1,10000,ErrorMessage ="Please enter a value between 1 and 10000")]
        public int Count { get; set; }



        public string ApplicationUserId { get; set; }

        [ValidateNever]
        [ForeignKey(nameof(ApplicationUserId))]
        public ApplicationUser MyProperty { get; set; }
    }
}
