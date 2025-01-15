using api_be.Services;

namespace api_be.Middleware
{
    public class JwtMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;

        public JwtMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context, IAuthService authService)
        {
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            if (token != null)
            {
                var user = await authService.ValidateTokenAsync(token);
                if (user != null)
                {
                    context.Items["User"] = user;  // Gán thông tin người dùng vào HttpContext
                }
            }

            await _next(context); // Tiếp tục xử lý request
        }
    }

}
