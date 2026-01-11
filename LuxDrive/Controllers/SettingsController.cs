using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LuxDrive.Data;
using LuxDrive.Data.Models;
using LuxDrive.Models;
using System.Text.RegularExpressions;

namespace LuxDrive.Controllers
{
    [Authorize]
    public class SettingsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly LuxDriveDbContext _context;

        public SettingsController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            LuxDriveDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        private async Task<UserSettingsViewModel> LoadViewModelAsync(ApplicationUser user)
        {
            var cards = await _context.PaymentCards
                                      .Where(c => c.UserId == user.Id.ToString())
                                      .ToListAsync();

            return new UserSettingsViewModel
            {
                Username = user.UserName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                SavedCards = cards
            };
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            var model = await LoadViewModelAsync(user);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(UserSettingsViewModel model)
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
            if (string.IsNullOrWhiteSpace(model.PhoneNumber))
            {
                ModelState.AddModelError("PhoneNumber", "Phone number is required.");
                isValid = false;
            }

            if (!isValid)
            {
                TempData["Error"] = "Please correct the errors in the profile.";
                return View("Index", await LoadViewModelAsync(user));
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

            await _userManager.UpdateAsync(user);
            await _signInManager.RefreshSignInAsync(user);
            TempData["Success"] = "Profile updated successfully!";
            return RedirectToAction("Index");
        }

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

        [HttpPost]
        public async Task<IActionResult> AddCard(UserSettingsViewModel model)
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

            string cleanNumber = model.NewCardNumber.Replace(" ", "");
            string type = cleanNumber.StartsWith("4") ? "Visa" : "MasterCard";
            string last4 = cleanNumber.Substring(cleanNumber.Length - 4);

            var newCard = new PaymentCard
            {
                UserId = user.Id.ToString(),
                CardLast4 = last4,
                CardType = type
            };

            _context.PaymentCards.Add(newCard);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Card added successfully!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> RemoveCard(int cardId)
        {
            TempData["ActiveTab"] = "billing";
            var card = await _context.PaymentCards.FindAsync(cardId);
            if (card != null)
            {
                _context.PaymentCards.Remove(card);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Card removed.";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var sharedFiles = _context.SharedFiles.Where(sf => sf.SenderId == user.Id || sf.ReceiverId == user.Id);
            _context.SharedFiles.RemoveRange(sharedFiles);

            var friendships = _context.UserFriends.Where(f => f.UserId == user.Id || f.FriendId == user.Id);
            _context.UserFriends.RemoveRange(friendships);

            var friendRequests = _context.FriendRequests.Where(fr => fr.SenderId == user.Id || fr.ReceiverId == user.Id);
            _context.FriendRequests.RemoveRange(friendRequests);

            var userFiles = _context.Files.Where(f => f.UserId == user.Id);
            _context.Files.RemoveRange(userFiles);

            var userCards = _context.PaymentCards.Where(c => c.UserId == user.Id.ToString());
            _context.PaymentCards.RemoveRange(userCards);

            await _context.SaveChangesAsync();

            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                await _signInManager.SignOutAsync();
                return RedirectToAction("Index", "Home");
            }

            TempData["ErrorMessage"] = "Грешка при изтриване на профила.";
            return RedirectToAction("Index");
        }
    }
}