using LuxDrive.Data;
using LuxDrive.Services;
using LuxDrive.Services.Interfaces;
using LuxDrive.ViewModels.File;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace LuxDrive.Controllers
{
    [Authorize]
    public class FileController : BaseController
    {
        private readonly SpacesService _spacesService;
        private readonly IFileService _fileService;

        public FileController(SpacesService spacesService, IFileService fileService)
        {
            _spacesService = spacesService;
            _fileService = fileService;
        }


        /// <summary>
        /// Метод за генериране на уникален ключ за съхранение на данни (напр. в бисквитки), специфичен за текущия потребител.
        /// Трансформира потребителското име (имейл) в безопасен низ чрез замяна на специални символи, 
        /// за да предотврати конфликти и да осигури правилно четене на данните за конкретния профил.
        /// </summary>
        /// <param name="baseKey">Основното име на ключа (напр. "CurrentPlan").</param>
        /// <returns>Връща комбиниран низ от основния ключ и преобразуваното потребителско име.</returns>
        private string GetUserKey(string baseKey)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated) return baseKey;
            string safeUserName = User.Identity.Name.Replace("@", "_").Replace(".", "_");
            return $"{baseKey}_{safeUserName}";
        }


        /// <summary>
        /// Метод за дефиниране на максималния капацитет за съхранение в байтове според абонаментния план.
        /// Използва switch израз за бързо съпоставяне на името на плана с неговия лимит.
        /// </summary>
        /// <param name="plan">Името на абонаментния план (напр. "Basic", "Pro", "Enterprise").</param>
        /// <returns>Връща лимита в байтове (long). Ако планът е неизвестен, се прилага стойността по подразбиране (10GB).</returns>
        private long GetMaxBytesForPlan(string plan)
        {
            return plan switch
            {
                "Basic" => 50L * 1024 * 1024 * 1024,
                "Pro" => 2048L * 1024 * 1024 * 1024,
                "Enterprise" => 100000L * 1024 * 1024 * 1024,
                _ => 10L * 1024 * 1024 * 1024
            };
        }

        /// <summary>
        /// Метод за форматиране на байтове в четими мерни единици (TB, GB, MB).
        /// Преобразува числови стойности от тип long в низ с подходящата наставка, 
        /// като закръгля резултата до втория или първия знак след десетичната запетая 
        /// за по-добра визуализация в потребителския интерфейс.
        /// </summary>
        /// <param name="bytes">Размерът в байтове, който трябва да бъде форматиран.</param>
        /// <returns>Връща форматиран низ (напр. "1.50 GB" или "500.2 MB").</returns>
        private string FormatBytes(long bytes)
        {
            if (bytes >= 1024L * 1024 * 1024 * 1024)
                return $"{(bytes / 1024.0 / 1024.0 / 1024.0 / 1024.0):F2} TB";

            if (bytes >= 1024 * 1024 * 1024)
                return $"{(bytes / 1024.0 / 1024.0 / 1024.0):F2} GB";

            double mb = (bytes / 1024.0) / 1024.0;
            return $"{mb:F1} MB";
        }

        /// <summary>
        /// Асинхронно изчислява заетото дисково пространство на потребителя.
        /// Методът самостоятелно извлича текущия план от бисквитките и списъка с файлове 
        /// чрез файловата услуга, след което подготвя данните за визуализация във ViewBag.
        /// </summary>
        /// <param name="userIdStr">Уникалният идентификатор на потребителя (Id).</param>
        private async Task CalculateStorageUsage()
        {
            string? userIdStr = GetUserId();

            string planKey = GetUserKey("CurrentPlan");
            string planName = Request.Cookies[planKey] ?? "Free";

            IEnumerable<IndexViewModel> files = await _fileService.GetUserFilesAsync(userIdStr);

            long totalUsedBytes = files.Sum(f => f.Size);
            long maxBytes = GetMaxBytesForPlan(planName);

            double percent = 0;
            if (planName == "Enterprise")
            {
                percent = totalUsedBytes > 0 ? 1 : 0;
            }
            else
            {
                percent = ((double)totalUsedBytes / maxBytes) * 100;
                if (percent > 100) percent = 100;
            }

            string totalLabel = FormatBytes(maxBytes);
            if (planName == "Enterprise") totalLabel = "Unlimited";

            string usedLabel = FormatBytes(totalUsedBytes);

            ViewBag.StoragePercent = (int)percent;
            ViewBag.StorageText = $"{usedLabel} / {totalLabel}";
        }


        /// <summary>
        /// Метод за визуализация на потребителските файлове.
        /// Извлича пълния списък с файлове на текущия потребител и асинхронно изчислява 
        /// заетото дисково пространство спрямо абонаментния план, преди да зареди изгледа.
        /// </summary>
        /// <returns>Връща изглед със списък от всички файлове на потребителя.</returns>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                string? userIdStr = GetUserId();
                if (userIdStr == null) return Unauthorized();

                IEnumerable<IndexViewModel> allUserFiles = await _fileService.GetUserFilesAsync(userIdStr);

                 await CalculateStorageUsage();

                return View(allUserFiles);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }


        /// <summary>
        /// Метод за зареждане на списък с файлове, които са споделени с текущия потребител от други хора.
        /// Методът изчислява текущото състояние на дисковото пространство на потребителя и 
        /// използва основния изглед "Index" за консистентно визуално представяне.
        /// </summary>
        /// <returns>Връща изглед със споделените файлове или Unauthorized при липса на сесия.</returns>
        [HttpGet]
        public async Task<IActionResult> SharedWithMe()
        {
            try
            {
                var userIdStr = GetUserId();
                if (userIdStr == null) return Unauthorized();

                IEnumerable<IndexViewModel> sharedFiles = await _fileService.GetSharedWithMeFilesAsync(userIdStr);

                await CalculateStorageUsage();

                return View("Index", sharedFiles);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }


        /// <summary>
        /// Метод за зареждане на списък с всички файлове, намиращи се в кошчето на потребителя.
        /// Изчислява текущото състояние на дисковото пространство и извлича маркираните 
        /// за изтриване файлове чрез файловата услуга.
        /// </summary>
        /// <returns>Връща изглед със списък от файлове в кошчето или статус Unauthorized при липса на сесия.</returns>
        [HttpGet]
        public async Task<IActionResult> Trash()
        {
            try
            {
                var userIdStr = GetUserId();
                if (userIdStr == null) return Unauthorized();

                await CalculateStorageUsage();

                IEnumerable<TrashViewModel>? trashedFiles = await _fileService.GetTrashedFilesAsync(userIdStr);

                return View(trashedFiles);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }


        /// <summary>
        /// Метод за качване на един или повече файлове в потребителския профил.
        /// Извършва проверка за налично дисково пространство спрямо текущия абонаментен план, 
        /// записва информацията за файловете в базата данни и прехвърля реалното им съдържание 
        /// към облачното хранилище (DigitalOcean Spaces).
        /// </summary>
        /// <param name="files">Списък от файлове, получени от формата за качване.</param>
        /// <returns>Пренасочва към основния панел при успех или показва грешка при липса на място.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(List<IFormFile> files)
        {
            try
            {
                var userIdStr = GetUserId();
                if (userIdStr == null) return Unauthorized();

                if (files == null || files.Count == 0)
                {
                    TempData["UploadError"] = "Select at least one file.";
                    return RedirectToAction(nameof(Index));
                }

                string planKey = GetUserKey("CurrentPlan");
                string currentPlan = Request.Cookies[planKey] ?? "Free";
                long maxStorageBytes = GetMaxBytesForPlan(currentPlan);

                long currentUsedBytes = await _fileService.GetTotalUsedStorageAsync(userIdStr);
                long newFilesBytes = files.Sum(f => f.Length);

                if (currentUsedBytes + newFilesBytes > maxStorageBytes)
                {
                    TempData["UploadError"] = $"Not enough space! You are trying to upload {FormatBytes(newFilesBytes)}, but you have {FormatBytes(maxStorageBytes - currentUsedBytes)} left on your {currentPlan} plan.";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var file in files)
                {
                    if (file == null || file.Length == 0) continue;

                    Guid? fileId = await _fileService.CreateFileAsync(userIdStr, file);
                    if (fileId == null) continue;

                    string? extension = await _fileService.GetFileExtensionAsync(fileId);
                    if (string.IsNullOrEmpty(extension))
                    {
                        extension = Path.GetExtension(file.FileName);
                    }

                    var key = $"{userIdStr}/{fileId}{extension}";


                    using var stream = file.OpenReadStream();
                    var url = await _spacesService.UploadAsync(stream, key, file.ContentType);

                    await _fileService.UpdateFileUrlAsync(fileId, url);


                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }


        /// <summary>
        /// Метод за преименуване на съществуващ файл.
        /// Извършва проверка за оторизация на потребителя и валидира новото име, след което 
        /// обновява информацията в базата данни чрез файловата услуга.
        /// </summary>
        /// <param name="id">Уникалният идентификатор на файла, който се преименува.</param>
        /// <param name="newName">Новото име, което потребителят е въвел.</param>
        /// <returns>Връща статус 200 (Ok) при успех или подходящ код за грешка при невалидни данни.</returns>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Rename(Guid id, string newName)
        {
            try
            {
                var userIdStr = GetUserId();
                if (userIdStr == null) return Unauthorized();

                if (string.IsNullOrWhiteSpace(newName))
                {
                    return BadRequest("Name cannot be empty.");
                }

                await _fileService.ChangeFileNameAsync(userIdStr, id, newName);

                return Ok();
            }
            catch (Exception)
            {
                return StatusCode(500, "An internal error occurred.");
            }

        }


        /// <summary>
        /// Метод за преместване на файл в кошчето.
        /// Идентифицира потребителя и подава заявка към файловата услуга за софтуерно изтриване (soft delete) на конкретния файл, 
        /// след което обновява изгледа чрез пренасочване към основния списък.
        /// </summary>
        /// <param name="id">Уникалният идентификатор на файла, който се изтрива.</param>
        /// <returns>Пренасочва към Index страницата или връща BadRequest при неуспешна операция.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var userIdStr = GetUserId();
                if (userIdStr == null) return Unauthorized();

                await _fileService.DeleteUserFileAsync(id, userIdStr);

                return RedirectToAction(nameof(Index));

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }


        }


        /// <summary>
        /// Метод за възстановяване на файл от кошчето.
        /// Валидира собствеността на файла и извиква услугата за премахване на флага за изтриване (soft delete), 
        /// след което връща потребителя към списъка с изтрити обекти.
        /// </summary>
        /// <param name="id">Уникалният идентификатор на файла, който се възстановява.</param>
        /// <returns>Пренасочва към изгледа Trash или връща BadRequest при грешка.</returns>
        [HttpPost]
        public async Task<IActionResult> Restore(Guid id)
        {
            try
            {
                var userIdStr = GetUserId();
                if (userIdStr == null) return Unauthorized();

                await _fileService.RestoreUserFileAsync(id, userIdStr);

                return RedirectToAction(nameof(Trash));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }


        /// <summary>
        /// Метод за окончателно изтриване на файл от системата.
        /// Премахва записа от базата данни и физически изтрива обекта от облачното хранилище (DigitalOcean Spaces).
        /// Тази операция е необратима и води до реално освобождаване на дисково пространство за потребителя.
        /// </summary>
        /// <param name="id">Уникалният идентификатор на файла за окончателно изтриване.</param>
        /// <returns>Пренасочва към изгледа на кошчето (Trash) след приключване на операцията.</returns>
        [HttpPost]
        public async Task<IActionResult> PermanentDelete(Guid id)
        {
            try
            {
                var userIdStr = GetUserId();
                if (userIdStr == null) return Unauthorized();

                string? fileStorageUrl = await _fileService.PermanentDeleteFileAsync(id, userIdStr);

                if (!string.IsNullOrEmpty(fileStorageUrl))
                {
                    var endpoint = "https://luxdrive.ams3.digitaloceanspaces.com/";
                    var key = fileStorageUrl.Replace(endpoint, string.Empty);
                    await _spacesService.DeleteAsync(key);

                }

                return RedirectToAction(nameof(Trash));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }

        /// <summary>
        /// Метод за групово изтриване на файлове (преместване в кошчето).
        /// Приема списък от идентификатори и изпълнява асинхронна операция за софтуерно изтриване на всички маркирани обекти, 
        /// принадлежащи на текущия потребител. Връща статус Ok при успех или NotFound, ако файловете не съществуват.
        /// </summary>
        /// <param name="ids">Списък с уникалните идентификатори (GUID) на файловете за изтриване.</param>
        /// <returns>Връща статус 200 (Ok), 404 (NotFound) или 400 (BadRequest) при възникнала грешка.</returns>
        [HttpPost]
        public async Task<IActionResult> DeleteMultiple([FromBody] List<Guid> ids)
        {
            try
            {
                var userIdStr = GetUserId();
                if (userIdStr == null) return Unauthorized();


                bool areDeleted = await _fileService.DeleteMultipleFilesAsync(ids, userIdStr);

                if (!areDeleted)
                {
                    return NotFound("No files were found or they are already deleted.");
                }
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }


        /// <summary>
        /// Метод за групово споделяне на файлове с друг потребител.
        /// Идентифицира подателя и използва услугата за файлове, за да създаде права за достъп 
        /// за избрания получател върху списък от конкретни файлови идентификатори.
        /// </summary>
        /// <param name="fileIds">Списък с уникалните идентификатори на файловете, които ще бъдат споделени.</param>
        /// <param name="receiverId">Уникалният идентификатор на потребителя, който ще получи достъп до файловете.</param>
        /// <returns>Връща статус 200 (Ok) при успешно споделяне или BadRequest при възникнала грешка.</returns>
        [HttpPost]
        public async Task<IActionResult> ShareMultiple(Guid receiverId, [FromBody] List<Guid> fileIds)
        {
            try
            {
                var userIdStr = GetUserId();
                if (userIdStr == null) return Unauthorized();

                await _fileService.ShareMultipleFilesAsync(fileIds, userIdStr, receiverId);

                return Ok("Files shared successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Метод за споделяне на конкретен файл с друг потребител.
        /// Проверява самоличността на изпращача и чрез файловата услуга предоставя права за достъп 
        /// на получателя, след което пренасочва потребителя с потвърждаващо съобщение.
        /// </summary>
        /// <param name="fileId">Уникалният идентификатор на файла, който се споделя.</param>
        /// <param name="receiverId">Уникалният идентификатор на потребителя получател.</param>
        /// <returns>Пренасочва към основния изглед (Index) със съобщение за успех или връща BadRequest при грешка.</returns>
        [HttpPost]
        public async Task<IActionResult> Share(Guid fileId, Guid receiverId)
        {
            try
            {
                var userIdStr = base.GetUserId();
                if (userIdStr == null) return BadRequest();

                await _fileService.ShareFileAsync(fileId, userIdStr, receiverId);

                TempData["AlertMessage"] = "File was shared.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Метод за цялостно изпразване на кошчето на потребителя.
        /// Извиква услуга за масово премахване на записи от базата данни и след това физически 
        /// изтрива съответните обекти от облачното хранилище (DigitalOcean Spaces) чрез итерация 
        /// по техните URL адреси.
        /// </summary>
        /// <returns>Пренасочва към изгледа на кошчето с потвърждение за успех или връща грешка при проблем.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmptyTrash()
        {
            try
            {
                var userIdStr = GetUserId();
                if (userIdStr == null) return Unauthorized();


                List<string>? filesUrls = await _fileService.EmptyTrashAsync(userIdStr);

                if (filesUrls == null)
                {
                    return BadRequest("No valid files were selected for sharing.");
                }


                foreach (var url in filesUrls)
                {


                    var endpoint = "https://luxdrive.ams3.digitaloceanspaces.com/";
                    var key = url.Replace(endpoint, string.Empty);
                    await _spacesService.DeleteAsync(key);

                }


                TempData["SuccessMessage"] = "Trash emptied successfully!";
                return RedirectToAction(nameof(Trash));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }

        /// <summary>
        /// Метод за групово възстановяване на файлове от кошчето.
        /// Приема списък от идентификатори и премахва флага за изтриване за всички съответни записи, 
        /// принадлежащи на потребителя, като ги връща в активния списък с файлове.
        /// </summary>
        /// <param name="ids">Списък с уникалните идентификатори на файловете за възстановяване.</param>
        /// <returns>Пренасочва към изгледа на кошчето (Trash) или връща BadRequest при грешка.</returns>
        [HttpPost]
        public async Task<IActionResult> RestoreMultiple(List<Guid> ids)
        {
            try
            {
                var userId = GetUserId();
                if (ids == null || !ids.Any()) return BadRequest("No files selected.");

                bool isRestored = await _fileService.RestoreMultipleFilesAsync(ids, userId);

                if (!isRestored)
                {
                    return BadRequest("Problem restoring files!");
                }
                return RedirectToAction(nameof(Trash));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }


        /// <summary>
        /// Метод за окончателно изтриване на избрани файлове от кошчето.
        /// Идентифицира файловете в базата данни, премахва ги и след това физически 
        /// изтрива съответните обекти от облачното хранилище (DigitalOcean Spaces), 
        /// като по този начин трайно освобождава заетото пространство.
        /// </summary>
        /// <param name="ids">Списък с уникални идентификатори на файловете, избрани за перманентно изтриване.</param>
        /// <returns>Пренасочва към изгледа на кошчето с потвърждение за успех или връща BadRequest при грешка.</returns>
        [HttpPost]
        public async Task<IActionResult> DeleteMultiplePermanent(List<Guid> ids)
        {
            try
            {
                var userId = GetUserId();
                if (ids == null || !ids.Any()) return BadRequest("No files selected.");


                List<string>? filesUrls = await _fileService.DeleteMultiplePermanentAsync(userId, ids);

                if (filesUrls == null)
                {
                    return BadRequest("No valid files were selected for sharing.");
                }


                foreach (var url in filesUrls)
                {

                    var endpoint = "https://luxdrive.ams3.digitaloceanspaces.com/";
                    var key = url.Replace(endpoint, string.Empty);
                    await _spacesService.DeleteAsync(key);



                }


                TempData["SuccessMessage"] = "Files was delete successfully!";
                return RedirectToAction(nameof(Trash));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }

        /// <summary>
        /// Метод за изтегляне на файл от потребителското хранилище.
        /// Проверява правата за достъп, извлича съдържанието от облака (DigitalOcean) чрез HTTP поток 
        /// и го предава на потребителя с оригиналното му име и разширение.
        /// </summary>
        /// <param name="id">Уникалният идентификатор на файла за изтегляне.</param>
        /// <returns>Връща файл като поток (stream) или статус NotFound/BadRequest при липса на достъп или проблем с облака.</returns>
        [HttpGet]
        public async Task<IActionResult> Download(Guid id)
        {
            try
            {
                var userIdStr = GetUserId();
                if (userIdStr == null) return Unauthorized();

                DownloadFileViewModel? file = await _fileService.GetFileToDownloadAsync(id, userIdStr);

                if (file == null) return NotFound();

                using var httpClient = new HttpClient();

                var response = await httpClient.GetAsync(file.StorageUrl);
                if (!response.IsSuccessStatusCode) return BadRequest("File not found in storage.");

                var stream = await response.Content.ReadAsStreamAsync();

                return File(stream, "application/octet-stream", file.Name + file.Extension);


            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }

        /// <summary>
        /// Метод за групово изтегляне на файлове под формата на компресиран ZIP архив.
        /// Извлича съдържанието на множество избрани файлове от облачното хранилище, 
        /// добавя ги динамично в архива в паметта и го предава на потребителя като един обект.
        /// </summary>
        /// <param name="ids">Списък с идентификатори на файловете, които трябва да бъдат архивирани и изтеглени.</param>
        /// <returns>Връща ZIP файл (application/zip) или статус NotFound/BadRequest при липса на достъп.</returns>
        [HttpPost]
        public async Task<IActionResult> DownloadMultiple([FromBody] List<Guid> ids)
        {
            try
            {
                var userIdStr = GetUserId();
                if (userIdStr == null) return Unauthorized();

                List<DownloadFileViewModel>? files = await _fileService.GetMultipleFilesToDownloadAsync(ids, userIdStr);

                if (files == null) return NotFound();

                using var ms = new MemoryStream();
                using (var archive = new System.IO.Compression.ZipArchive(ms, System.IO.Compression.ZipArchiveMode.Create, true))
                {
                    using var httpClient = new HttpClient();

                    foreach (var file in files)
                    {

                        var response = await httpClient.GetAsync(file.StorageUrl);
                        if (response.IsSuccessStatusCode)
                        {
                            var fileStream = await response.Content.ReadAsStreamAsync();

                            var entry = archive.CreateEntry(file.Name + file.Extension);
                            using var entryStream = entry.Open();
                            await fileStream.CopyToAsync(entryStream);
                        }

                    }
                }

                ms.Position = 0;
                return File(ms.ToArray(), "application/zip", "LuxDrive_Download.zip");
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }

        }


    }
}