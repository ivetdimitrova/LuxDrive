namespace LuxDrive.Data.Common
{
    public static class EntityConstants
    {
        public static class ApplicationUser
        {
            public const int FirstNameLength = 100;
            public const int LastNameLength = 150;


            public const string FirstNameComment = "User's first name";
            public const string LastNameComment = "User's last name";
        }

        public static class File
        {
            public const int NameLength = 255;
            public const int ExtensionLength = 10;

            public const string IdComment = "Unique identifier for the file record";
            public const string NameComment = "Original name of the uploaded file";
            public const string ExtensionComment = "File extension including the dot";
            public const string SizeComment = "File size in bytes";
            public const string StorageUrlComment = "URL or path where the file is stored";
            public const string UploadAtComment = "UTC date and time when the file was uploaded";
            public const string UserIdComment = "Identifier of the user who uploaded the file";

        }

        public static class FriendRequest
            {
            public const string IdComment = "Unique identifier for the friend request";
            public const string SenderIdComment = "Identifier of the user who sent the friend request";
            public const string ReceiverIdComment = "Identifier of the user who received the friend request";
            public const string StatusComment = "Current status of the friend request (Pending, Accepted, Rejected)";
            public const string CreatedOnComment = "UTC date and time when the friend request was created";
        }

        public static class PaymentCard
        {
            public const string IdComment = "Unique identifier for the payment card record";
            public const string UserIdComment = "Identifier of the user who owns the payment card";
            public const string CardLast4Comment = "Last four digits of the payment card number";
            public const string CardTypeComment = "Type of the payment card (e.g., Visa, MasterCard)";
        }

        public static class SharedFile
        {
            public const string IdComment = "Unique identifier for the file share record";
            public const string FileIdComment = "Identifier of the file being shared";
            public const string SenderIdComment = "Identifier of the user who shared the file";
            public const string ReceiverIdComment = "Identifier of the user who received the shared file";
            public const string SharedOnComment = "UTC date and time when the file was shared";
        }

        public static class UserFriend
        {
            public const string UserIdComment = "Identifier of the user in the friendship relationship";
            public const string FriendIdComment = "Identifier of the related friend user";
        }

    }
}
