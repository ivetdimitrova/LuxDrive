namespace LuxDrive.ViewModels.File
{
    public class TrashViewModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Extension { get; set; }

        public string Icon { get; set; } = string.Empty;

        public string StorageUrl { get; set; } = string.Empty;

        public DateTime? DeletedOn { get; set; }

        
        /* Изчислимо свойство (Computed Property), което определя оставащите дни до перманентно изтриване.
         Демонстрира прилагане на бизнес правилото за 30-дневен гратисен период.*/
        public int DaysLeft
        {
            get
            {
                var days = DeletedOn.HasValue
                    ? 30 - (DateTime.UtcNow - DeletedOn.Value).Days
                    : 30;
                return Math.Max(0, days);
            }
        }
        //Форматира разширението за по-добра четимост в потребителския интерфейс(UI Optimization).
        public string DisplayExtension => Extension?.ToUpper() ?? "";

        /* ИЗЧИСЛИМО СВОЙСТВО (Computed Property):
          Капсулира бизнес логиката за идентификация на изображения.  */
        public bool IsImage =>
            !string.IsNullOrEmpty(Extension) &&
            new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".svg" }
            .Contains(Extension.ToLower());
    }
}