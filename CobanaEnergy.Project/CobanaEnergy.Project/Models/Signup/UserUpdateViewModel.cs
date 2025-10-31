using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.Signup
{
    public class UserUpdateViewModel
    {
        [Required]
        public string UserId { get; set; }

        [Required]
        [Display(Name = "Username")]
        public string UserName { get; set; }

        [MaxLength(50, ErrorMessage = "Job Title cannot exceed 50 characters")]
        [Display(Name = "Job Title")]
        public string JobTitle { get; set; }

        [MaxLength(10, ErrorMessage = "Extension Number cannot exceed 10 characters")]
        [RegularExpression(@"^[0-9]*$", ErrorMessage = "Extension Number must contain only digits")]
        [Display(Name = "Extension Number")]
        public string ExtensionNumber { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email format")]
        [Display(Name = "Email")]
        public string Email { get; set; }
    }
}
