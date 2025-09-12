﻿using System.ComponentModel.DataAnnotations;

namespace Ward_Management_System.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Name is required!.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Email is required!.")]
        [EmailAddress]
        public string EmailAddress { get; set; }

        [Required(ErrorMessage = "Password is required!.")]
        [StringLength(40, MinimumLength = 6, ErrorMessage = "The {0} must be atleast {2} and max {1} characters long.")]
        [DataType(DataType.Password)]
        [Compare("ConfirmPassword", ErrorMessage = "Password does not match!.")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Confirm Password is required!.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password.")]
        public string ConfirmPassword { get; set; }

        [Required]
        public int Age { get; set; }

        [Required]
        public string Address { get; set; }

        [Required(ErrorMessage = "Phone number is required!.")]
        [Phone]
        [Display(Name = "Alternate Phone Number")]
        public string PhoneNumber { get; set; }

        [Required]
        public string IdNumber { get; set; }

        [Required]
        public string Gender { get; set; }
    }
}
