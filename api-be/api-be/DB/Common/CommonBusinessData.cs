
using api_be.Entities;

namespace api_be.DB.Common
{
    public static class CommonBusinessData
    {
       public static List<Type> ImmediateDeleteTypes = new List<Type> 
       { 
           typeof(Product),
       };
    }
}
