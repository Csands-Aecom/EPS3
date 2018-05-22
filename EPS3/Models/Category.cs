using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;


namespace EPS3.Models
{
    public class Category
    {
        [Key]
        [Display(Name ="Category")]
        public int CategoryID { get; set; }
        public string CategoryCode { get; set; }
        public string CategoryName { get; set; }
        public string CategorySelector
        {
            get { return CategoryCode + " - " + CategoryName; }
        }
    }
}
