using System.Reflection.PortableExecutable;
using System.Text.RegularExpressions;

namespace api_be.Validators
{
    public static class ValidatorCustom
    {
        public static bool IsAtLeastNYearsOld(DateTime? pDateOfBirth, int pYear)
        {
            DateTime currentDate = DateTime.Now;
            DateTime minimumBirthDate = currentDate.AddYears(-pYear);
            return pDateOfBirth <= minimumBirthDate;
        }

        public static bool BeValidEmail(string pEmail)
        {
            if (string.IsNullOrEmpty(pEmail))
                return false;

            string pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,4}$";
            return Regex.IsMatch(pEmail, pattern);
        }

        public static bool IsEqualOrAfterDay(DateTime? pTime, DateTime? pDay)
        {
            return pTime >= pDay;
        }

        public static bool IsAfterDay(DateTime? pTime, DateTime? pDay)
        {
            return pTime > pDay;
        }

        public static bool IsValidFile(string pFilePath)
        {
            if (string.IsNullOrEmpty(pFilePath))
                return false;

            string pattern = @"^[^.]+\.[a-zA-Z]+$";

            return Regex.IsMatch(pFilePath, pattern) && pFilePath.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
        }

        public static bool IsValidImage(string image)
        {
            // Kiểm tra URL hợp lệ
            if (Uri.TryCreate(image, UriKind.Absolute, out Uri? uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                return true;
            }

            // Kiểm tra Base64 hợp lệ
            return IsBase64String(image);
        }

        public static bool IsBase64String(string base64)
        {
            // Kiểm tra Base64 hợp lệ
            base64 = base64.Trim();
            return base64.Length % 4 == 0 && base64.All(c => "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=".Contains(c));
        }
        public static bool BeValidImage(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
                return false;

            // Các phần mở rộng hình ảnh hợp lệ
            string[] allowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".svg", ".webp" };
            string extension = Path.GetExtension(imagePath);

            // Kiểm tra phần mở rộng hợp lệ hoặc đường dẫn từ Cloudinary hoặc Firebase
            return allowedImageExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase) ||
                   IsValidCloudinaryUrl(imagePath) ||
                   IsValidFirebaseUrl(imagePath);
        }

        private static bool IsValidCloudinaryUrl(string url)
        {
            return !string.IsNullOrEmpty(url) && url.Contains("res.cloudinary.com");
        }

        private static bool IsValidFirebaseUrl(string url)
        {
            return !string.IsNullOrEmpty(url) &&
                   url.Contains("firebasestorage.googleapis.com") &&
                   url.Contains("?alt=media&token=");
        }

        public static bool IsValidGender(string pGender)
        {
            string[] genders = GetGender();

            return genders.Contains(pGender);
        }

        public static string[] GetGender()
        {
            return new string[]
            {
                "Nam",
                "Nữ",
                "Khác",
            };
        }
    }
}
