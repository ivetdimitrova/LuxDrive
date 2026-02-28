using LuxDrive.Data.Models;
using LuxDrive.Services;
using LuxDrive.Services.Interfaces;
using LuxDrive.ViewModels.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LuxDrive.Controllers
{
    [Authorize]
    public class SettingsController : BaseController
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly SpacesService _spacesService;
        private readonly IPaymentCardService _paymentCardService;
        private readonly IApplicationUserService _applicationUserService;

        public SettingsController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
             SpacesService spacesService,
             IPaymentCardService paymentCardService,
             IApplicationUserService applicationUserService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _spacesService = spacesService;
            _paymentCardService = paymentCardService;
            _applicationUserService = applicationUserService;
        }

        /// <summary>
        /// Помощен метод за подготвяне на модела с данни за настройките.
        /// Извлича потребителското име, личните данни, профилната снимка и списъка със запазени карти.
        /// </summary>
        private async Task<UserSettingsViewModel> LoadViewModelAsync(ApplicationUser user)
        {

            return new UserSettingsViewModel
            {
                Username = user.UserName.Contains("@") ? user.UserName.Split('@')[0] : user.UserName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                SavedCards = await _paymentCardService.GetUserCardsAsync(user.Id),
                ProfileImageUrl = string.IsNullOrEmpty(user.ProfileImagePath)
                                  ? "/images/default-avatar.png"
                                  : user.ProfileImagePath
            };

        }


        /// <summary>
        /// Зарежда основната страница с настройки, като попълва всички актуални данни за потребителя.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return RedirectToAction("Login", "Account");
                var model = await LoadViewModelAsync(user);
                return View(model);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }


        /// <summary>
        /// Обработва промените в профила – имена, имейл и телефон. 
        /// Тук е логиката за качване на нова профилна снимка в DigitalOcean Spaces 
        /// или изтриване на старата, както и опресняване на сесията на потребителя.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UpdateProfile(UserSettingsViewModel model, string RemovePhoto)
        {
            TempData["ActiveTab"] = "profile";
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            ModelState.Clear();

            bool isValid = true;
            if (string.IsNullOrWhiteSpace(model.FirstName))
            {
                ModelState.AddModelError("FirstName", "First name is required.");
                isValid = false;
            }
            if (string.IsNullOrWhiteSpace(model.LastName))
            {
                ModelState.AddModelError("LastName", "Last name is required.");
                isValid = false;
            }
            if (string.IsNullOrWhiteSpace(model.Email))
            {
                ModelState.AddModelError("Email", "Email is required.");
                isValid = false;
            }


            if (!isValid)
            {
                TempData["Error"] = "Please correct the errors in the profile.";
                return View("Index", await LoadViewModelAsync(user));
            }


            if (RemovePhoto == "true")
            {
                if (!string.IsNullOrEmpty(user.ProfileImagePath))
                {
                    var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.ProfileImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);

                    user.ProfileImagePath = null;
                }
            }
            else if (model.ProfileImage != null && model.ProfileImage.Length > 0)
            {
                if (model.ProfileImage.Length > 2 * 1024 * 1024)
                {
                    TempData["Error"] = "Image size must be less than 2MB.";
                    return View("Index", await LoadViewModelAsync(user));
                }

                var key = string.Empty;

                if (!string.IsNullOrEmpty(user.ProfileImagePath))
                {
                    var endpoint = "https://luxdrive.ams3.digitaloceanspaces.com/";
                    key = user.ProfileImagePath.Replace(endpoint, string.Empty);
                    await _spacesService.DeleteAsync(key);
                }

                string extension = Path.GetExtension(model.ProfileImage.FileName);

                key = $"profilePhotos/{user.Id.ToString()}{extension}";

                using (var stream = model.ProfileImage.OpenReadStream())
                {
                    var url = await _spacesService.UploadAsync(stream, key, model.ProfileImage.ContentType);

                    user.ProfileImagePath = url;
                }


            }





            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.PhoneNumber = model.PhoneNumber;

            if (model.Email != user.Email)
            {
                var emailResult = await _userManager.SetEmailAsync(user, model.Email);
                if (!emailResult.Succeeded)
                {
                    TempData["Error"] = "Email update failed: " + emailResult.Errors.First().Description;
                    return View("Index", await LoadViewModelAsync(user));
                }
                user.UserName = model.Email;
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                TempData["Error"] = "Update failed: " + result.Errors.First().Description;
                return View("Index", await LoadViewModelAsync(user));
            }

            await _signInManager.RefreshSignInAsync(user);
            TempData["Success"] = "Profile updated successfully!";

            await _userManager.UpdateAsync(user);

            return RedirectToAction("Index");
        }


        /// <summary>
        /// Метод за премахване на профилната снимка на текущия потребител.
        /// Проверява дали потребителят има зададена снимка, изтрива физическия файл от сървъра (wwwroot), 
        /// нулира пътя към нея в базата данни и връща потвърждение за успех.
        /// </summary>
        /// <returns>Пренасочва потребителя обратно към страницата с настройки.</returns>
        [HttpPost]
        public async Task<IActionResult> RemoveProfilePicture()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null && !string.IsNullOrEmpty(user.ProfileImagePath))
            {
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.ProfileImagePath.TrimStart('/'));
                if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);

                user.ProfileImagePath = string.Empty;

                await _userManager.UpdateAsync(user);
                TempData["Success"] = "Profile picture removed.";
            }
            return RedirectToAction("Index");
        }


        /// <summary>
        /// Метод за сигурна промяна на потребителската парола.
        /// Извършва валидация на задължителните полета, проверява дали новата парола е потвърдена правилно 
        /// и дали текущата парола е вярна. При успех обновява паролата в базата данни и опреснява 
        /// автентикационната сесия на потребителя, за да остане вписан.
        /// </summary>
        /// <param name="model">Моделът, съдържащ текущата парола, новата парола и нейното потвърждение.</param>
        /// <returns>Пренасочва към началната страница с настройки при успех или презарежда изгледа с грешки при неуспех.</returns>
        [HttpPost]
        public async Task<IActionResult> ChangePassword(UserSettingsViewModel model)
        {
            TempData["ActiveTab"] = "security";
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            ModelState.Clear();

            if (string.IsNullOrEmpty(model.CurrentPassword) || string.IsNullOrEmpty(model.NewPassword) || string.IsNullOrEmpty(model.ConfirmNewPassword))
            {
                TempData["Error"] = "All password fields are required.";
                return View("Index", await LoadViewModelAsync(user));
            }

            if (model.NewPassword != model.ConfirmNewPassword)
            {
                TempData["Error"] = "New passwords do not match.";
                ModelState.AddModelError("ConfirmNewPassword", "Passwords do not match.");
                return View("Index", await LoadViewModelAsync(user));
            }

            if (!await _userManager.CheckPasswordAsync(user, model.CurrentPassword))
            {
                TempData["Error"] = "Incorrect current password.";
                ModelState.AddModelError("CurrentPassword", "Incorrect password.");
                return View("Index", await LoadViewModelAsync(user));
            }

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                TempData["Success"] = "Password changed successfully!";
                return RedirectToAction("Index");
            }
            else
            {
                string errorMsg = result.Errors.First().Description;
                TempData["Error"] = "Failed: " + errorMsg;
                ModelState.AddModelError("NewPassword", errorMsg);
                return View("Index", await LoadViewModelAsync(user));
            }
        }


        /// <summary>
        /// Метод за добавяне на нова дебитна или кредитна карта към потребителския профил.
        /// Извършва детайлна валидация на номера на картата, CVC кода и срока на годност.
        /// При валидни данни идентифицира типа на картата (Visa, Mastercard, Amex), 
        /// извлича последните четири цифри за целите на сигурността и запазва информацията в базата данни.
        /// </summary>
        /// <param name="model">Моделът, съдържащ данните за новата карта.</param>
        /// <returns>Пренасочва към настройките при успех или презарежда страницата с описание на грешките.</returns>
        [HttpPost]
        public async Task<IActionResult> AddCard(UserSettingsViewModel model)
        {
            try
            {
                TempData["ActiveTab"] = "billing";
                var user = await _userManager.GetUserAsync(User);

                ModelState.Clear();

                bool isCardValid = true;

                if (string.IsNullOrEmpty(model.NewCardNumber) || model.NewCardNumber.Length < 12)
                {
                    ModelState.AddModelError("NewCardNumber", "Invalid card number.");
                    isCardValid = false;
                }

                if (string.IsNullOrEmpty(model.NewCardCvc) || model.NewCardCvc.Length != 3)
                {
                    ModelState.AddModelError("NewCardCvc", "Invalid CVC.");
                    isCardValid = false;
                }

                if (string.IsNullOrEmpty(model.NewCardExpiry))
                {
                    ModelState.AddModelError("NewCardExpiry", "Required.");
                    isCardValid = false;
                }
                else
                {
                    var parts = model.NewCardExpiry.Split('/');
                    if (parts.Length != 2 || !int.TryParse(parts[0], out int month) || month < 1 || month > 12)
                    {
                        ModelState.AddModelError("NewCardExpiry", "Invalid month.");
                        isCardValid = false;
                    }
                }

                if (!isCardValid)
                {
                    TempData["Error"] = "Please correct the card details.";
                    return View("Index", await LoadViewModelAsync(user));
                }

                string cleanNumber = model.NewCardNumber.Replace(" ", "").Trim();
                string last4 = cleanNumber.Substring(cleanNumber.Length - 4);

                string cardType = cleanNumber.StartsWith("4") ? "visa" :
                                 cleanNumber.StartsWith("5") ? "mastercard" :
                                 cleanNumber.StartsWith("3") ? "amex" : "unknown";

                await _paymentCardService.CreateCardAsync(user.Id, last4, cardType);
                TempData["Success"] = "Card added successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }


        /// <summary>
        /// Метод за сигурно премахване на запазена разплащателна карта.
        /// Първо извлича идентификатора на текущия потребител и проверява неговата автентичност. 
        /// След това извиква услугата за изтриване на картата по нейното уникално Id, като гарантира, 
        /// че картата принадлежи на същия потребител, за да се предотврати неоторизиран достъп.
        /// </summary>
        /// <param name="cardId">Уникалният идентификатор на картата, която трябва да бъде изтрита.</param>
        /// <returns>Пренасочва към страницата с настройки (таб „Billing“) със съобщение за успех или връща грешка.</returns>
        [HttpPost]
        public async Task<IActionResult> RemoveCard(Guid cardId)
        {
            try
            {
                string? userIdStr = base.GetUserId();
                if (userIdStr == null) return Unauthorized();

                TempData["ActiveTab"] = "billing";

                await _paymentCardService.DeleteCardAsync(cardId, userIdStr);

                TempData["Success"] = "Card removed.";

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }


        /// <summary>
        /// Метод за окончателно изтриване на потребителския профил от системата.
        /// Първо премахва всички свързани данни чрез външната услуга , 
        /// след което изтрива самия потребител от Identity базата данни. 
        /// При успех изписва потребителя (SignOut) и го пренасочва към началната страница.
        /// </summary>
        /// <returns>Пренасочва към началната страница при успех или показва съобщение за грешка при проблем.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return NotFound();

                await _applicationUserService.DeleteAccountAsync(user.Id.ToString());

                var result = await _userManager.DeleteAsync(user);

                if (result.Succeeded)
                {
                    await _signInManager.SignOutAsync();
                    return RedirectToAction("Index", "Home");
                }

                TempData["ErrorMessage"] = "Error deleting account.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }


        /// <summary>
        /// Метод за симулирано нулиране на потребителска парола чрез административен токен.
        /// Намира потребителя по имейл, генерира автоматично токен за нулиране и прилага новата парола.
        /// Използва се основно за автоматизирано тестване или бързо възстановяване на достъп.
        /// </summary>
        /// <param name="email">Имейл адресът на потребителя, чиято парола ще бъде нулирана.</param>
        /// <param name="newPassword">Новата парола, която да бъде зададена.</param>
        /// <returns>Връща JSON обект за успех или списък с грешки при невалидна парола.</returns>
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> SimulateResetPassword(string email, string newPassword)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return NotFound();

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (result.Succeeded)
            {
                return Ok(new { success = true });
            }

            return BadRequest(result.Errors);
        }
    }
}