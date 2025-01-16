namespace api_be.Transforms
{
    public static class EventTransform
    {
        public static string DeleteObjectSuccess(string objectStr,string id)
        {
            return $"{objectStr} với id = {id} đã xóa thành công!";
        }
    }
}
