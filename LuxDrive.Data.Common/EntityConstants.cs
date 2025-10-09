namespace LuxDrive.Data.Common
{
    public static class EntityConstants
    {
        public static class ApplicationUser
        {
            public const int FirstNameLength = 100;
            public const int LastNameLength = 150;


            //Table column comments
            public const string FirstNameComment = "User's first name";
            public const string LastNameComment = "User's last name";
        }

        public static class File
        {
            public const int NameLength = 255;
            public const int ExtensionLength = 10;

            //Table column comments
            public const string IdComment = "Unique identifier for the file record";
            public const string NameComment = "Original name of the uploaded file";
            public const string ExtensionComment = "File extension including the dot";
            public const string SizeComment = "File size in bytes";
            public const string StorageUrlComment = "URL or path where the file is stored";
            public const string UploadAtComment = "UTC date and time when the file was uploaded";
            public const string UserIdComment = "Identifier of the user who uploaded the file";

        }
    }
}
