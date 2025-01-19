namespace api_be.DB.Services
{
    public static class CommonService
    {
        public static string InternalCodeGeneration(string pPrefix, DateTime pDate)
        {
            string suffix = pDate.ToString("yyyyMMddHHmmss");
            return pPrefix + suffix;
        }
    }
}
