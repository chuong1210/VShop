using api_be.Models.Responses;
using Microsoft.Extensions.Caching.Memory;

namespace api_be.Services.Imps
{
    public class OtpCacheModel
    {
        public string OtpCode { get; set; }
        public int Attempts { get; set; }
        public DateTime ExpiryTime { get; set; }
    }

    public class OtpService:IOtpService
    {

        private readonly IMemoryCache _cache;
        private readonly ISMSService _smsService;
        private const int OTP_LENGTH = 6;
        private const int MAX_ATTEMPTS = 3;
        private const int OTP_EXPIRY_MINUTES = 5;
        private const string OTP_PREFIX = "OTP_";

        public OtpService(IMemoryCache cache, ISMSService smsService)
        {
            _cache = cache;
            _smsService = smsService;
        }

        private string GenerateOtp()
        {
            Random random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        private string GetCacheKey(string phoneNumber)
        {
            return $"{OTP_PREFIX}{phoneNumber}";
        }

        public async Task<Result<bool>> SendOtp(string phoneNumber)
        {
            try
            {
                var cacheKey = GetCacheKey(phoneNumber);

                // Check if there's an existing OTP that's not expired
                if (_cache.TryGetValue(cacheKey, out OtpCacheModel existingOtp))
                {
                    if (DateTime.Now < existingOtp.ExpiryTime)
                    {
                        var timeToWait = (existingOtp.ExpiryTime - DateTime.Now).Seconds;
                        return Result<bool>.Failure(
                            $"Vui lòng đợi {timeToWait} giây trước khi yêu cầu OTP mới",
                            StatusCodes.Status400BadRequest
                        );
                    }
                }

                // Generate new OTP
                string otpCode = GenerateOtp();
                var otpModel = new OtpCacheModel
                {
                    OtpCode = otpCode,
                    Attempts = 0,
                    ExpiryTime = DateTime.Now.AddMinutes(OTP_EXPIRY_MINUTES)
                };

                // Store in cache
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(OTP_EXPIRY_MINUTES));

                _cache.Set(cacheKey, otpModel, cacheOptions);

                // Send SMS
                string message = $"Mã xác thực OTP của bạn là: {otpCode}. Mã có hiệu lực trong {OTP_EXPIRY_MINUTES} phút.";
                await _smsService.SendOTPAsync(phoneNumber, message);

                return Result<bool>.Success(true, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure(ex.Message, StatusCodes.Status500InternalServerError);
            }
        }

        public Result<bool> VerifyOtp(string phoneNumber, string otpCode)
        {
            try
            {
                var cacheKey = GetCacheKey(phoneNumber);

                if (!_cache.TryGetValue(cacheKey, out OtpCacheModel otpModel))
                {
                    return Result<bool>.Failure("Mã OTP đã hết hạn hoặc không tồn tại",
                        StatusCodes.Status400BadRequest);
                }

                // Check expiry
                if (DateTime.Now > otpModel.ExpiryTime)
                {
                    _cache.Remove(cacheKey);
                    return Result<bool>.Failure("Mã OTP đã hết hạn",
                        StatusCodes.Status400BadRequest);
                }

                // Check attempts
                if (otpModel.Attempts >= MAX_ATTEMPTS)
                {
                    _cache.Remove(cacheKey);
                    return Result<bool>.Failure("Bạn đã nhập sai quá nhiều lần. Vui lòng yêu cầu mã OTP mới",
                        StatusCodes.Status400BadRequest);
                }

                // Verify OTP
                if (otpModel.OtpCode != otpCode)
                {
                    otpModel.Attempts++;
                    _cache.Set(cacheKey, otpModel);
                    return Result<bool>.Failure(
                        $"Mã OTP không chính xác. Còn {MAX_ATTEMPTS - otpModel.Attempts} lần thử",
                        StatusCodes.Status400BadRequest
                    );
                }

                // OTP is valid - remove from cache
                _cache.Remove(cacheKey);
                return Result<bool>.Success(true, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure(ex.Message, StatusCodes.Status500InternalServerError);
            }
        }
    }
}
