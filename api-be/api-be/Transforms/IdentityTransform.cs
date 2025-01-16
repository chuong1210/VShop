namespace api_be.Transforms
{
    public static class IdentityTransform
    {
        public static string UserNameAlreadyExists(string userName)
        {
            return $"Tên người dùng {userName} đã tồn tại!";
        }

        public static string EmailAlreadyExists(string userName)
        {
            return $"Email {userName} đã được đăng ký!";
        }

        public static string PhoneNumberAlreadyExists(string userName)
        {
            return $"Số điện thoại {userName} đã được đăng ký!";
        }

        public static string UserNotExists(string userName)
        {
            return $"Không tìm thấy người dùng {userName}!";
        }

        public static string InvalidCredentials(string userName)
        {
            return $"Thông tin xác thực của người dùng {userName} không hợp lệ!";
        }

        public static string TokenRequired()
        {
            return $"Vui lòng cung cấp trường accessToken";
        }

        public static string InvalidRefreshToken() => "Refresh token không hợp lệ.";
        public static string RefreshTokenUsed() => "Refresh token đã được sử dụng.";
        public static string RefreshTokenRevoked() => "Refresh token đã bị thu hồi.";
        public static string RefreshTokenExpired() => "Refresh token đã hết hạn.";
        public static string InvalidAccessToken() => "Access token không đúng định dạng JWT.";
        public static string AccessTokenNotExpired() => "Access token vẫn còn hiệu lực, không thể refresh.";


        public static string ForbiddenException()
        {
            return "Bạn không được phép truy cập tài nguyên này!";
        }

        public static string UnauthorizedException()
        {
            return "Unauthorized!";
        }
    }
}
