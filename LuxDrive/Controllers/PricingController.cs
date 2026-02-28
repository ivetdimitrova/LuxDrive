using LuxDrive.Data;
using LuxDrive.Data.Models;
using LuxDrive.Services.Interfaces;
using LuxDrive.ViewModels.Pricing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LuxDrive.Controllers
{
    public class PricingController : BaseController
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPaymentCardService _paymentCardService;

        public PricingController(LuxDriveDbContext context, UserManager<ApplicationUser> userManager, IPaymentCardService paymentCardService)
        {
            _userManager = userManager;
            _paymentCardService = paymentCardService;
        }

        /// <summary>
        /// Метод за генериране на уникален ключ за файл, базиран на името на текущия потребител.
        /// Ако потребителят е влязъл, методът „изчиства“ потребителското му име от специални символи (@ и .) и го добавя към базовото име на файла, 
        /// за да се гарантира уникалност и по-лесно проследяване в облачното хранилище.
        /// </summary>
        /// <param name="baseKey">Оригиналното име или път на файла.</param>
        /// <returns>Връща модифицирания ключ с добавено потребителско име или оригиналния ключ, ако потребителят не е логнат.</returns>
        private string GetUserKey(string baseKey)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated) return baseKey;
            string safeUserName = User.Identity.Name.Replace("@", "_").Replace(".", "_");
            return $"{baseKey}_{safeUserName}";
        }


        /// <summary>
        /// Метод за зареждане на началната страница за абонаменти и управление на текущия план.
        /// Проверява дали потребителят е вписан, дали има свързана платежна карта и извлича информация за активния план и датата му на изтичане от бисквитките.
        /// Ако планът е изтекъл, автоматично го подновява (ако има карта) или го прекратява, след което предава данните към изгледа чрез ViewBag.
        /// </summary>
        /// <returns>Връща изгледа на страницата с информация за абонаментния план на потребителя.</returns>
        public async Task<IActionResult> Index()
        {
            try
            {
                string currentPlan = "None";
                string expiryDateStr = "";
                bool hasCard = false;

                if (User.Identity != null && User.Identity.IsAuthenticated)
                {
                    var user = await _userManager.GetUserAsync(User);
                    if (user != null)
                    {
                        hasCard = await this._paymentCardService.HasUserLinkedCardAsync(user.Id);

                        string planKey = GetUserKey("CurrentPlan");
                        string expiryKey = GetUserKey("PlanExpiry");

                        currentPlan = Request.Cookies[planKey] ?? "None";
                        string storedDate = Request.Cookies[expiryKey];

                        if (currentPlan != "None" && !string.IsNullOrEmpty(storedDate))
                        {
                            if (DateTime.TryParse(storedDate, out DateTime expiryDate))
                            {
                                if (DateTime.Now > expiryDate)
                                {
                                    if (hasCard)
                                    {
                                        expiryDate = DateTime.Now.AddMonths(1);
                                        CookieOptions option = new CookieOptions { Expires = DateTime.Now.AddDays(400) };
                                        Response.Cookies.Append(expiryKey, expiryDate.ToString(), option);
                                        expiryDateStr = expiryDate.ToString("dd.MM.yyyy");
                                    }
                                    else
                                    {
                                        currentPlan = "None";
                                        Response.Cookies.Delete(planKey);
                                        Response.Cookies.Delete(expiryKey);
                                        expiryDateStr = "";
                                    }
                                }
                                else
                                {
                                    expiryDateStr = expiryDate.ToString("dd.MM.yyyy");
                                }
                            }
                        }
                    }
                }

                ViewBag.CurrentPlan = currentPlan;
                ViewBag.HasCard = hasCard;
                ViewBag.ExpiryDate = expiryDateStr;

                return View();
            }
            catch (Exception ex)
            {
                return BadRequest("Error removing: " + ex.Message);
            }

        }


        /// <summary>
        /// Метод за зареждане на страницата за завършване на плащането (Checkout).
        /// Приема избрания абонаментен план и подготвя модела за плащане, като прехвърля името на плана към изгледа.
        /// Ако не е избран валиден план, пренасочва потребителя обратно към началната страница с плановете.
        /// </summary>
        /// <param name="plan">Името на избрания от потребителя абонаментен план.</param>
        /// <returns>Връща изгледа за плащане с попълнени данни за плана.</returns>
        [Authorize]
        [HttpGet]
        public IActionResult Checkout(string plan)
        {
            if (string.IsNullOrEmpty(plan)) return RedirectToAction(nameof(Index));

            var model = new CheckoutViewModel
            {
                PlanName = plan
            };

            return View(model);
        }


        /// <summary>
        /// Метод за обработка на плащането и активиране на избрания абонаментен план.
        /// Валидира данните от формата, идентифицира типа на банковата карта, записва последните ѝ четири цифри в базата данни и 
        /// създава бисквитки за текущия план и неговата валидност (обикновено за един месец).
        /// </summary>
        /// <param name="model">Моделът с данните за плащането (план, номер на карта, титуляр и др.).</param>
        /// <returns>Пренасочва към началната страница с потвърждение за успешен абонамент или показва грешка при неуспех.</returns>
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Process(CheckoutViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Checkout", model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            await Task.Delay(2000);

            try
            {
                string cleanNumber = model.CardNumber.Replace(" ", "").Trim();
                string last4 = cleanNumber.Substring(cleanNumber.Length - 4);

                string cardType = cleanNumber.StartsWith("4") ? "visa" :
                                 cleanNumber.StartsWith("5") ? "mastercard" :
                                 cleanNumber.StartsWith("3") ? "amex" : "unknown";

                await _paymentCardService.CreateCardAsync(user.Id, last4, cardType);
                string planKey = GetUserKey("CurrentPlan");
                string expiryKey = GetUserKey("PlanExpiry");
                DateTime validUntil = DateTime.Now.AddMonths(1);
                CookieOptions option = new CookieOptions { Expires = DateTime.Now.AddDays(400) };

                Response.Cookies.Append(planKey, model.PlanName, option);
                Response.Cookies.Append(expiryKey, validUntil.ToString(), option);

                TempData["SuccessMessage"] = $"Successfully activated {model.PlanName} plan!";
                return RedirectToAction("Index");

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }


        }



        /// <summary>
        /// Метод за бърза покупка на абонаментен план чрез вече запазена разплащателна карта.
        /// Проверява дали потребителят има активна карта в базата данни. Ако има такава, автоматично активира избрания план, 
        /// обновява бисквитките за абонамент и пренасочва потребителя към началната страница. 
        /// Ако няма запазена карта, го препраща към пълната страница за плащане (Checkout).
        /// </summary>
        /// <param name="plan">Името на плана, който потребителят иска да закупи бързо.</param>
        /// <returns>Пренасочва към Index при успех или към Checkout при липса на карта.</returns>

        [Authorize]
        public async Task<IActionResult> QuickPurchase(string plan)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                bool hasCardInDb = await _paymentCardService.HasUserLinkedCardAsync(user.Id);

                if (!hasCardInDb)
                {
                    TempData["ErrorMessage"] = "No saved card found. Please add a card.";
                    return RedirectToAction("Checkout", new { plan = plan });
                }

                string planKey = GetUserKey("CurrentPlan");
                string expiryKey = GetUserKey("PlanExpiry");

                await Task.Delay(1500);

                DateTime validUntil = DateTime.Now.AddMonths(1);
                CookieOptions option = new CookieOptions { Expires = DateTime.Now.AddDays(400) };

                Response.Cookies.Append(planKey, plan, option);
                Response.Cookies.Append(expiryKey, validUntil.ToString(), option);

                TempData["SuccessMessage"] = $"Successfully upgraded to {plan} plan!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }



        /// <summary>
        /// Метод за прекратяване на текущия платен абонамент и връщане към безплатен план.
        /// Изтрива бисквитките, съхраняващи информация за активния план и датата му на изтичане, 
        /// след което пренасочва потребителя към началната страница с потвърждение за промяната.
        /// </summary>
        /// <returns>Пренасочва към Index страницата със съобщение за успешно преминаване към безплатен план.</returns>
        [Authorize]
        public IActionResult Downgrade()
        {
            string planKey = GetUserKey("CurrentPlan");
            string expiryKey = GetUserKey("PlanExpiry");

            Response.Cookies.Delete(planKey);
            Response.Cookies.Delete(expiryKey);

            TempData["SuccessMessage"] = "Successfully switched back to the Free plan.";
            return RedirectToAction("Index");
        }


        /// <summary>
        /// Метод за зареждане на страницата за контакт , ако потребителя иска собствена план .
        /// Инициализира нов празен модел за контактната форма и го предава към изгледа, 
        /// подготвяйки интерфейса за изпращане на бизнес запитвания.
        /// </summary>
        /// <returns>Връща изгледа с празната форма за контакт.</returns>
        [HttpGet]
        public IActionResult ContactSales()
        {
            ContactSalesViewModel model = new ContactSalesViewModel();
            return View(model);
        }


        /// <summary>
        /// Метод за обработка на изпратеното запитване от потребителя, който иска собствена план
        /// Валидира данните от модела и проверява типа на заявката. Ако заявката е асинхронна (AJAX), 
        /// връща JSON отговор за успех, в противен случай пренасочва потребителя обратно към изгледа с попълнените данни.
        /// </summary>
        /// <param name="model">Моделът с попълнената информация от контактната форма.</param>
        /// <returns>Връща статус код 200 (Ok) при AJAX заявка, BadRequest при невалидни данни или оригиналния изглед.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ContactSales(ContactSalesViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Ok(new { success = true });
            }

            return View(model);
        }
    }
}