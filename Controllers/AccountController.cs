using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Ward_Management_System.Models;
using Ward_Management_System.ViewModels;

namespace Ward_Management_System.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<Users> signInManager;
        private readonly UserManager<Users> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly IEmailSender emailSender;

        public AccountController(SignInManager<Users> signInManager, UserManager<Users> userManager, RoleManager<IdentityRole> roleManager, IEmailSender emailSender)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.emailSender = emailSender;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var result = await signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
            //Checking if login is successful
            if (result.Succeeded)                            
            {
                var user = await userManager.FindByEmailAsync(model.Email);
                var roles = await userManager.GetRolesAsync(user);

                //Redirects user to authorized dashboard
                if (roles.Contains("Admin"))                  
                {
                    return RedirectToAction("Admin", "Admin");
                }
                else if (roles.Contains("User"))
                {
                    return RedirectToAction("Index", "User");
                }
                else if (roles.Contains("WardAdmin"))
                {
                    return RedirectToAction("Index", "WardAdmin");
                }
                else if (roles.Contains("Nurse"))
                {
                    return RedirectToAction("Index", "Nurse");
                }
                else if (roles.Contains("NurseSister"))
                {
                    return RedirectToAction("Index", "NurseSister");
                }
                else if (roles.Contains("Doctor"))
                {
                    return RedirectToAction("Index", "Doctor");
                }


                // Default fallback
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, "Invalid Login Attempt");
            return View(model);
        }

        //Registers new user
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = new Users
            {
                FullName = model.Name,
                UserName = model.EmailAddress,
                NormalizedUserName = model.EmailAddress.ToUpper(),
                Email = model.EmailAddress,
                NormalizedEmail = model.EmailAddress.ToUpper(),
                Age = model.Age,
                Address = model.Address,
                PhoneNumber = model.PhoneNumber,
                IdNumber = model.IdNumber,
                Gender = model.Gender

            };

            var result = await userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                var roleExist = await roleManager.RoleExistsAsync("User");

                if (!roleExist)
                {
                    var role = new IdentityRole("User");
                    await roleManager.CreateAsync(role);
                }
                await userManager.AddToRoleAsync(user, "User");

                await signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Login", "Account");
            }
            foreach(var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        //Veryifing email when changing password
        [HttpGet]
        public IActionResult VerifyEmail()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyEmail(VerifyEmailViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await userManager.FindByEmailAsync(model.Email);

            if(user == null)
            {
                ModelState.AddModelError("", "User not found!");
                return View(model);
            }
            var otp = new Random().Next(100000, 999999).ToString();

            TempData["OTP"] = otp;
            TempData["Email"] = model.Email;

            await emailSender.SendEmailAsync(model.Email, "Password Reset Verification", $"Your OTP code is {otp}");

            return RedirectToAction("VerifyCode");
        }

        // Checks the code matches
        [HttpGet]
        public IActionResult VerifyCode()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyCode(string code)
        {
            var storedOtp = TempData["OTP"]?.ToString();
            var email = TempData["Email"]?.ToString();

            if (string.IsNullOrEmpty(storedOtp) || string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError("", "Session expired. Please try again.");
                return View();
            }

            if (storedOtp != code)
            {
                ModelState.AddModelError("", "Invalid verification code!");
                return View();
            }

            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                ModelState.AddModelError("", "User not found!");
                return View();
            }

            // OTP verified, now go to Change Password
            return RedirectToAction("ChangePassword", "Account", new { username = user.UserName });
        }

        [HttpGet]
        public async Task<IActionResult> TestEmail()
        {
            await emailSender.SendEmailAsync("recipient@example.com", "Test Email", "<h2>This is a test email from Ward Management System 🚀</h2>");
            return Content("Email sent (check your inbox / spam folder).");
        }


        //Gets the new password after changing it
        [HttpGet]
        public IActionResult ChangePassword(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("VerifyEmail", "Account");
            }
            return View(new ChangePasswordViewModel { Email = username });
        }
        //Posts it to the database as new password
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Something went wrong!");
                return View(model);
            }

            var user = await userManager.FindByNameAsync(model.Email);
            if(user == null)
            {
                ModelState.AddModelError("", "User not found.");
                return View(model);
            }
            var result = await userManager.RemovePasswordAsync(user);
            if (result.Succeeded)
            {
                result = await userManager.AddPasswordAsync(user, model.NewPassword);
                return RedirectToAction("Login", "Account");
            }
            else
            {
                foreach(var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return View(model);
            }
        }

        //Logs you out 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}
