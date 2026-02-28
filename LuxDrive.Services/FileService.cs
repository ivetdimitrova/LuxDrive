using LuxDrive.Data;
using LuxDrive.Data.Models;
using LuxDrive.Services.Interfaces;
using LuxDrive.ViewModels.File;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

using FileEntity = LuxDrive.Data.Models.File;

namespace LuxDrive.Services
{
    public class FileService : IFileService
    {
        private readonly LuxDriveDbContext _dbContext;

        public FileService(LuxDriveDbContext dbContext)
        {
            _dbContext = dbContext;
        }


        ///<summary>  Метод за смяна на името на даден файл. </summary>
        ///<param name="userId">Id-то на човека, който притежава файла.</param>
        ///<param name="fileId">Id-то на самия файл, който преименуваме.</param>
        ///<param name="newName">Как искаме да се казва файлът вече.</param>
        ///<exception cref="ArgumentException">Гърми, ако името е празно, потребителят е невалиден или файлът не съществува.</exception>

        public async Task ChangeFileNameAsync(string userId, Guid fileId, string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
                throw new ArgumentException("The file's name cannot be empty.");


            if (!Guid.TryParse(userId, out Guid userGuid))
                throw new ArgumentException("Invalid user id!");

            var file = await _dbContext.Files
                .FirstOrDefaultAsync(f => f.Id == fileId && f.UserId == userGuid);

            if (file == null)
                throw new ArgumentException("File not found!");

            string clean = newName.Trim();

            if (!string.IsNullOrEmpty(file.Extension) &&
                clean.EndsWith(file.Extension, StringComparison.OrdinalIgnoreCase))
            {
                clean = clean[..^file.Extension.Length];
            }
            else
            {
                var dotIndex = clean.LastIndexOf('.');
                if (dotIndex > 0) clean = clean.Substring(0, dotIndex);
            }

            if (file.Name == clean)
                return;

            file.Name = clean;
            await _dbContext.SaveChangesAsync();
        }


        ///<summary>Метод за създаване на нов запис на файл в базата.</summary>
        ///<param name="userId">Id-то на човека, който качва файла.</param>
        ///<param name="file">Самият файл, който идва от формата за качване.</param>
        ///<returns>Връща Айдито на току-що създадения файл. Ако няма такъв файл, връща null.</returns>
        ///<exception cref="ArgumentException">Гърми, ако Айдито на потребителя не е валидно.</exception>

        public async Task<Guid?> CreateFileAsync(string userId, IFormFile file)
        {
            if (!Guid.TryParse(userId, out Guid userGuid))
                throw new ArgumentException("Invalid user id!");

            if (file == null || string.IsNullOrWhiteSpace(file.FileName))
            {
                return null;
            }

            var newFile = new FileEntity
            {
                Id = Guid.NewGuid(),
                Name = Path.GetFileNameWithoutExtension(file.FileName),
                Extension = Path.GetExtension(file.FileName),
                Size = file.Length,
                StorageUrl = "",
                UploadAt = DateTime.UtcNow,
                UserId = userGuid
            };

            await _dbContext.Files.AddAsync(newFile);
            await _dbContext.SaveChangesAsync();

            return newFile.Id;
        }



        ///<summary>Метод за намиране и връщане на разширението на конкретен файл по неговото Id.
        ///Проверява дали подаденото Id е валидно и ако всичко е наред, дърпа само разширението от базата данни, без да следи обекта за промени.
        ///</summary>
        ///<param name="fileId">Уникалното Id на файла, чието разширение ни трябва.</param>
        ///<returns>Връща разширението като текст (например ".jpg") или null, ако не намери такъв файл.</returns>
        ///<exception cref="ArgumentException">Хвърля грешка, ако Id-то е празно или невалидно.</exception>


        public async Task<string?> GetFileExtensionAsync(Guid? fileId)
        {
            if (fileId == null || fileId == Guid.Empty)
                throw new ArgumentException("Invalid file id!");

            return await _dbContext.Files
                .AsNoTracking()
                .Where(f => f.Id == fileId)
                .Select(f => f.Extension)
                .FirstOrDefaultAsync();
        }



        ///<summary> Метод за взимане на всички файлове на даден потребител, които не са изтрити. 
        ///Попълва данните за всеки файл и му слага подходяща иконка (снимка, музика, документ и т.н.) според разширението му, за да се виждат красиво в списъка.
        ///</summary>
        ///<param name="userId">Id-то на потребителя, чиито файлове търсим.</param>
        ///<returns>Връща списък с файловете, готови за показване на екрана.</returns>
        ///<exception cref="ArgumentException">Хвърля грешка, ако Айдито на потребителя е грешно.</exception>

        public async Task<IEnumerable<IndexViewModel>> GetUserFilesAsync(string userId)
        {
            if (!Guid.TryParse(userId, out Guid userGuid))
                throw new ArgumentException("Invalid user id!");

            IEnumerable<IndexViewModel> files = await _dbContext.Files
                .Where(f => f.UserId == userGuid && f.IsDeleted == false)
                 .AsNoTracking()
                 .Select(f => new IndexViewModel
                 {
                     Id = f.Id,
                     Type = f.Extension,
                     StorageUrl = f.StorageUrl,
                     Name = f.Name,
                     Extension = f.Extension,
                     UploadedAt = f.UploadAt,
                     IsDeleted = f.IsDeleted,
                     Size = (int)f.Size

                 })
                 .ToListAsync();

            foreach (IndexViewModel file in files)
            {
                file.Icon = await this.GetFileIcon(file.Extension);
            }
            return files;
        }



        ///<summary>Метод за обновяване на интернет адреса (URL) на вече съществуващ файл. 
        ///Проверява дали подаденото Id и новият линк са валидни, намира файла в базата и му записва новия адрес.
        ///</summary>
        ///<param name="fileId">Уникалното Id на файла, който ще обновяваме.</param>
        ///<param name="url">Новият адрес, на който е качен файлът.</param>
        ///<exception cref="ArgumentException">Хвърля грешка, ако Id-то е невалидно, линкът е празен или файлът не е намерен.</exception>

        public async Task UpdateFileUrlAsync(Guid? fileId, string url)
        {
            if (fileId == null || fileId == Guid.Empty)
                throw new ArgumentException("Invalid file id!");

            if (string.IsNullOrEmpty(url))
                throw new ArgumentException("Invalid file's url!");

            var file = await _dbContext.Files.FirstOrDefaultAsync(f => f.Id == fileId);
            if (file == null)
                throw new ArgumentException("File not found!");

            file.StorageUrl = url;
            _dbContext.Update(file);
            await _dbContext.SaveChangesAsync();
        }


        ///<summary>Метод за споделяне на файл с друг потребител.
        ///Проверява дали двамата потребители са приятели и дали файлът вече не е бил споделен с този човек. 
        ///Ако всичко е точно, прави нов запис в базата данни за споделения файл.
        ///</summary>
        ///<param name="fileId">Id-то на файла, който се споделя.</param>
        ///<param name="senderId">Id-то на човека, който изпраща файла.</param>
        ///<param name="receiverId">Id-то на човека, който ще получи файла.</param>
        ///<exception cref="ArgumentException">Хвърля грешка при невалидно Id на изпращача.</exception>
        ///<exception cref="InvalidOperationException">Хвърля грешка, ако потребителите не са приятели.</exception>

        public async Task ShareFileAsync(Guid fileId, string senderId, Guid receiverId)
        {
            if (!Guid.TryParse(senderId, out Guid senderGuid))
            {
                throw new ArgumentException("Invalid IDs.");
            }

            bool areFriends = await _dbContext.UserFriends
                .AnyAsync(x => x.UserId == senderGuid && x.FriendId == receiverId || x.UserId == receiverId && x.FriendId == senderGuid);

            if (!areFriends) throw new InvalidOperationException("Users are not friends.");

            bool alreadyShared = await _dbContext.SharedFiles
                .AnyAsync(x => x.FileId == fileId && x.ReceiverId == receiverId);

            if (alreadyShared) return;

            var sharedFile = new SharedFile
            {
                FileId = fileId,
                SenderId = senderGuid,
                ReceiverId = receiverId,
                SharedOn = DateTime.UtcNow
            };

            _dbContext.SharedFiles.Add(sharedFile);
            await _dbContext.SaveChangesAsync();
        }


        ///<summary>
        ///Метод за взимане на всички файлове, които са били споделени с потребителя от други хора.
        ///Проверява  Id-то му, намира кои файлове са пратени към него и записва информацията за тях (име, линк и кой точно ги е пратил).
        ///</summary>
        ///<param name="userId">Id-то на потребителя, който си гледа споделените файлове.</param>
        ///<returns>Списък с файловете, които са ти изпратени, заедно с името на изпращача.</returns>
        ///<exception cref="ArgumentException">Хвърля грешка, ако Id-то на потребителя не е валидно.</exception>

        public async Task<IEnumerable<IndexViewModel>> GetSharedWithMeFilesAsync(string userId)
        {
            if (!Guid.TryParse(userId, out Guid userGuid))
                throw new ArgumentException("Invalid user id!");

            return await _dbContext.SharedFiles
                .Where(sf => sf.ReceiverId == userGuid)
                .Include(sf => sf.File)
                .Include(sf => sf.Sender)
                .AsNoTracking()
                .Select(sf => new IndexViewModel
                {
                    Id = sf.File.Id,
                    Name = sf.File.Name,
                    Extension = sf.File.Extension,
                    StorageUrl = sf.File.StorageUrl,
                    UploadedAt = sf.SharedOn,
                    SenderName = sf.Sender.UserName
                })
                .ToListAsync();
        }


        ///<summary>
        ///Метод за пресмятане на колко общо място заемат всички файлове на потребителя.
        ///Събира размерите на всеки негов файл от базата данни и връща общата сума. Ако потребителят няма качени файлове, връща 0.
        ///</summary>
        ///<param name="userId">Id-то на човека, за когото проверяваме колко памет е заел.</param>
        ///<returns>Общият размер на всички файлове като число.</returns>
        ///<exception cref="ArgumentException">Гърми, ако Id-то на потребителя е невалидно.</exception>

        public async Task<long> GetTotalUsedStorageAsync(string userId)
        {
            if (!Guid.TryParse(userId, out Guid userGuid))
                throw new ArgumentException("Invalid user id!");

            return await _dbContext.Files
                .Where(f => f.UserId == userGuid)
                .SumAsync(f => (long?)f.Size) ?? 0;
        }



        ///<summary>
        ///Метод за „меко“ изтриване на файл. 
        ///Вместо да го трие завинаги, той просто го маркира като изтрит в базата и записва кога точно се е случило това. 
        ///Файлът трябва да принадлежи на потребителя, който се опитва да го изтрие.
        ///</summary>
        ///<param name="id">Уникалното Id на файла, който ще махаме.</param>
        ///<param name="userIdStr">Id-то на потребителя под формата на текст.</param>
        ///<exception cref="ArgumentException">Хвърля грешка, ако потребителят е невалиден или ако файлът вече е изтрит или не съществува.</exception>

        public async Task DeleteUserFileAsync(Guid id, string userIdStr)
        {
            if (string.IsNullOrWhiteSpace(userIdStr))
                throw new ArgumentException("Invalid user id!");

            var file = await _dbContext.Files
                .FirstOrDefaultAsync(f => f.Id == id && f.UserId.ToString() == userIdStr);

            if (file == null || file.IsDeleted)
                throw new ArgumentException("File not found!");

            file.IsDeleted = true;
            file.DeletedOn = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
        }


        ///<summary>
        ///Метод за възстановяване на изтрит файл. 
        ///Намира файла в базата и премахва отметката за изтриване, като го прави отново видим за потребителя.
        ///</summary>
        ///<param name="id">Уникалното Id на файла, който да върнем.</param>
        ///<param name="userIdStr">Id-то на потребителя, на когото принадлежи файлът.</param>
        ///<exception cref="ArgumentException">Хвърля грешка, ако Id-тата са невалидни или файлът изобщо не съществува.</exception>


        public async Task RestoreUserFileAsync(Guid id, string userIdStr)
        {
            if (!Guid.TryParse(userIdStr, out Guid userId))
                throw new ArgumentException("Invalid user id!");

            if (id == Guid.Empty)
                throw new ArgumentException("Invalid file id!");

            var file = await _dbContext.Files
                .FirstOrDefaultAsync(f => f.Id == id && f.UserId.ToString() == userIdStr);

            if (file == null)
                throw new ArgumentException("File not found!");


            file.IsDeleted = false;
            file.DeletedOn = null;

            await _dbContext.SaveChangesAsync();

        }



        ///<summary>
        ///Метод за показване на всички файлове на потребителя, които са в кошчето (маркирани като изтрити).
        ///Подрежда ги така, че най-скоро изтритите да са най-отгоре, и взима само важната информация за тях като име, разширение и кога точно са били махнати.
        ///</summary>
        ///<param name="userIdStr">Id-то на потребителя, който проверява кошчето си.</param>
        ///<returns>Списък с изтритите файлове, готови за показване в изгледа на кошчето.</returns>
        ///<exception cref="ArgumentException">Хвърля грешка, ако Id-то на потребителя не е правилно.</exception>

        public async Task<IEnumerable<TrashViewModel>?> GetTrashedFilesAsync(string userIdStr)
        {
            if (!Guid.TryParse(userIdStr, out Guid userGuid))
                throw new ArgumentException("Invalid user id!");

            IEnumerable<TrashViewModel>? files = await _dbContext.Files
                .Where(f => f.UserId == userGuid && f.IsDeleted == true)
                .OrderByDescending(f => f.DeletedOn)
                .AsNoTracking()
                .Select(f => new TrashViewModel
                {
                    Id = f.Id.ToString(),
                    Name = f.Name,
                    Extension = f.Extension,
                    DeletedOn = f.DeletedOn,
                    StorageUrl = f.StorageUrl
                })

                .ToListAsync();

            foreach (TrashViewModel file in files)
            {
                file.Icon = await this.GetFileIcon(file.Extension);

            }


            return files;
        }


        /// <summary>
        /// Определя съответната FontAwesome икона въз основа на разширението на файла.
        /// </summary>
        /// <param name="extension">Разширението на файла (напр. ".jpg", ".pdf").</param>
        /// <returns>Стринг, съдържащ CSS класовете за FontAwesome икона.</returns>
        private async Task<string> GetFileIcon(string extension)
        {
            switch (extension)
            {
                case ".jpg":
                case ".jpeg":
                case ".png":
                case ".gif":
                case ".webp":
                    return "fas fa-image";

                case ".mp4":
                case ".avi":
                case ".mov":
                    return "fas fa-play";

                case ".mp3":
                case ".wav":
                    return "fas fa-music";

                case ".pdf":
                    return "fas fa-file-pdf";

                case ".doc":
                case ".docx":
                    return "fas fa-file-word";

                case ".xls":
                case ".xlsx":
                    return "fas fa-file-excel";

                case ".zip":
                case ".rar":
                case ".7z":
                    return "fas fa-file-archive";

                default:
                    return "fas fa-file";
            }
        }



        ///<summary>
        ///Метод за окончателно изтриване на файл от системата.
        ///Проверява дали файлът съществува и дали принадлежи на човека, който иска да го изтрие. 
        ///Ако всичко е наред, изтрива записа от базата данни завинаги и връща адреса на файла, за да може после да се изтрие и от облака (storage-а).
        ///</summary>
        ///<param name="id">Уникалното Id на файла, който ще се трие завинаги.</param>
        ///<param name="userIdStr">Id-то на потребителя под формата на текст.</param>
        ///<returns>Връща линка към файла в облака, за да бъде премахнат и оттам. Ако файлът не е намерен, връща null.</returns>

        public async Task<string?> PermanentDeleteFileAsync(Guid id, string userIdStr)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Invalid file id!");

            if (!Guid.TryParse(userIdStr, out Guid userGuid) || userGuid == Guid.Empty)
                throw new ArgumentException("Invalid user id!");

            FileEntity? file = await _dbContext.Files
                  .FirstOrDefaultAsync(f => f.Id == id && f.UserId.ToString() == userIdStr);

            if (file != null && !string.IsNullOrEmpty(file.StorageUrl))
            {
                _dbContext.Files.Remove(file);
                await _dbContext.SaveChangesAsync();

                return file.StorageUrl;

            }

            return null;
        }



        ///<summary>
        ///Метод за „меко“ изтриване на няколко файла едновременно (преместване в кошчето).
        ///Проверява кои от дадените файлове наистина принадлежат на потребителя и не са вече изтрити.
        ///Маркира ги всичките като "изтрити" и записва часа на изтриването.
        ///</summary>
        ///<param name="ids">Списък с Id-тата на всички файлове, които искаме да махнем.</param>
        ///<param name="userIdStr">Id-то на потребителя под формата на текст.</param>
        ///<returns>Връща true, ако е успял да изтрие поне един файл, иначе връща false.</returns>
        ///<exception cref="ArgumentException">Гърми, ако Id-то на потребителя е грешно.</exception>

        public async Task<bool> DeleteMultipleFilesAsync(List<Guid> ids, string userIdStr)
        {
            if (!Guid.TryParse(userIdStr, out Guid userGuid) || userGuid == Guid.Empty)
                throw new ArgumentException("Invalid user id!");

            List<FileEntity> filesToDelete = await _dbContext.Files
                .Where(f => ids.Contains(f.Id) && f.UserId == userGuid && !f.IsDeleted)
                .ToListAsync();

            if (!filesToDelete.Any())
            {
                return false;
            }

            foreach (var file in filesToDelete)
            {
                file.IsDeleted = true;
                file.DeletedOn = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync();
            return true;
        }


        ///<summary>
        ///Метод за споделяне на група от файлове наведнъж с друг потребител.
        ///Проверява дали получателят е валиден и след това завърта цикъл, за да извика основния метод за споделяне за всеки файл от списъка.
        ///</summary>
        ///<param name="ids">Списък с Id-тата на всички файлове, които искаме да пратим.</param>
        ///<param name="userIdStr">Id-то на човека, който споделя файловете.</param>
        ///<param name="receiverId">Id-то на човека, който ще ги получи.</param>
        ///<exception cref="ArgumentException">Гърми, ако Id-то на получателя е празно или невалидно.</exception>


        public async Task ShareMultipleFilesAsync(List<Guid> ids, string userIdStr, Guid receiverId)
        {
            if (receiverId == Guid.Empty)
                throw new ArgumentException("Invalid receiver id!");

            foreach (var fileId in ids)
            {

                await this.ShareFileAsync(fileId, userIdStr, receiverId);

            }

        }


        ///<summary>
        ///Метод за пълно изпразване на кошчето на потребителя.
        ///Намира всички файлове, които са отбелязани като изтрити, и ги премахва окончателно от базата данни. 
        ///Накрая връща списък с техните адреси (URL), за да могат да бъдат изтрити и от самото облачно хранилище.
        ///</summary>
        ///<param name="userIdStr">Id-то на потребителя, който си чисти кошчето.</param>
        ///<returns>Списък с адресите на всички изтрити файлове или null, ако кошчето е вече празно.</returns>
        ///<exception cref="ArgumentException">Гърми, ако Id-то на потребителя не е валидно.</exception>


        public async Task<List<string>?> EmptyTrashAsync(string userIdStr)
        {
            if (!Guid.TryParse(userIdStr, out Guid userGuid))
                throw new ArgumentException("Invalid user id!");

            List<FileEntity> trashedFiles = await _dbContext.Files
                .Where(f => f.UserId == userGuid && f.IsDeleted)
                .ToListAsync();

            if (!trashedFiles.Any())
                return null;

            List<string> filesStorageUrls = trashedFiles
                .Select(f => f.StorageUrl)
                .ToList();

            if (filesStorageUrls.Any(f => string.IsNullOrEmpty(f)))
            {
                return null;
            }

            foreach (var file in trashedFiles)
            {
                _dbContext.Files.Remove(file);
            }

            await _dbContext.SaveChangesAsync();
            return filesStorageUrls;
        }


        ///<summary>
        ///Метод за възстановяване на няколко файла едновременно от кошчето.
        ///Проверява кои от избраните файлове наистина са в кошчето и принадлежат на потребителя, след което ги прави отново активни (премахва датата на изтриване).
        ///</summary>
        ///<param name="ids">Списък с Id-тата на файловете, които искаме да върнем.</param>
        ///<param name="userIdStr">Id-то на потребителя под формата на текст.</param>
        ///<returns>Връща true, ако е успял да възстанови поне един файл, иначе връща false.</returns>
        ///<exception cref="ArgumentException">Гърми, ако списъкът с файлове е празен или Id-то на потребителя е грешно.</exception>

        public async Task<bool> RestoreMultipleFilesAsync(List<Guid> ids, string userIdStr)
        {
            if (ids == null || !ids.Any() || !Guid.TryParse(userIdStr, out Guid userGuid))
                throw new ArgumentException("Invalid ids!");

            var files = await _dbContext.Files
                .Where(f => ids.Contains(f.Id) && f.UserId == userGuid && f.IsDeleted)
                .ToListAsync();

            if (!files.Any())
                return false;

            foreach (var file in files)
            {
                file.IsDeleted = false;
                file.DeletedOn = null;
            }

            await _dbContext.SaveChangesAsync();
            return true;
        }



        ///<summary>
        ///Метод за подготвяне на файл за сваляне.
        ///Проверява дали Id-тата са точни и намира файла в базата (стига да не е изтрит). 
        ///Връща само най-важното: името, разширението и линка към мястото, където се съхранява файлът, за да може потребителят да си го изтегли.
        ///</summary>
        ///<param name="id">Уникалното Id на файла, който искаме да свалим.</param>
        ///<param name="userIdStr">Id-то на потребителя, който прави заявката.</param>
        ///<returns>Връща данните за файла (име и линк) или null, ако файлът не съществува или е изтрит.</returns>
        ///<exception cref="ArgumentException">Гърми, ако Айдито на потребителя или на файла е невалидно.</exception>

        public async Task<DownloadFileViewModel?> GetFileToDownloadAsync(Guid id, string userIdStr)
        {
            if (!Guid.TryParse(userIdStr, out Guid userGuid))
                throw new ArgumentException("Invalid user id!");

            if (id == Guid.Empty)
                throw new ArgumentException("Invalid file id!");

            return await _dbContext.Files
                .Where(f => f.Id == id && f.IsDeleted == false)
                .AsNoTracking()
                .Select(f => new DownloadFileViewModel
                {
                    Name = f.Name,
                    Extension = f.Extension,
                    StorageUrl = f.StorageUrl
                })
                .FirstOrDefaultAsync();
        }


        ///<summary>
        ///Метод за сваляне на  списък от файлове наведнъж.
        ///Проверява дали подадените Id-та са валидни и намира само тези файлове, които принадлежат на потребителя и не са изтрити.
        ///Връща списък с имената и адресите на файловете, за да могат да бъдат изтеглени заедно.
        ///</summary>
        ///<param name="ids">Списък с Id-тата на файловете, които потребителят иска да свали.</param>
        ///<param name="userIdStr">Id-то на потребителя, който прави заявката.</param>
        ///<returns>Списък с данни за сваляне на всеки файл или null, ако нищо не е намерено.</returns>
        ///<exception cref="ArgumentException">Гърми, ако списъкът с Id-та е празен или Айдито на потребителя е грешно.</exception>

        public async Task<List<DownloadFileViewModel>?> GetMultipleFilesToDownloadAsync(List<Guid> ids, string userIdStr)
        {
            if (ids == null || !ids.Any() || !Guid.TryParse(userIdStr, out Guid userGuid))
                throw new ArgumentException("Invalid ids!");

            return await _dbContext.Files
                .Where(f => ids.Contains(f.Id) && f.UserId == userGuid && !f.IsDeleted)
                .AsNoTracking()
                .Select(f => new DownloadFileViewModel
                {
                    Name = f.Name,
                    Extension = f.Extension,
                    StorageUrl = f.StorageUrl
                })
                .ToListAsync();
        }


        /// <summary>
        /// Изтрива окончателно множество файлове от базата данни за конкретен потребител.
        /// </summary>
        /// <param name="userId">Идентификаторът на потребителя като низ (String), който се парсва към Guid.</param>
        /// <param name="ids">Списък с уникални идентификатори (Guid) на файловете за изтриване.</param>
        /// <returns>
        /// Връща списък от URL адреси на изтритите файлове за последващо премахване от физическото хранилище.
        /// Връща <c>null</c>, ако някой от файловете няма валиден StorageUrl.
        /// </returns>
        /// <exception cref="ArgumentException">Хвърля се, ако подаденият Id-to на потребителя е не е валиден Guid.<paramref name="userId"</exception>
        public async Task<List<string>?> DeleteMultiplePermanentAsync(string userId, List<Guid> ids)
        {
            if (!Guid.TryParse(userId, out Guid userGuid))
                throw new ArgumentException("Invalid user id!");

            var filesToDelete = await _dbContext.Files
          .Where(f => ids.Contains(f.Id) && f.UserId == userGuid)
          .ToListAsync();

            List<string> filesStorageUrls = filesToDelete
                            .Select(f => f.StorageUrl)
                            .ToList();

            if (filesStorageUrls.Any(f => string.IsNullOrEmpty(f)))
            {
                return null;
            }

            foreach (var file in filesToDelete)
            {
                _dbContext.Files.Remove(file);
            }

            await _dbContext.SaveChangesAsync();
            return filesStorageUrls;
        }
    }
}
