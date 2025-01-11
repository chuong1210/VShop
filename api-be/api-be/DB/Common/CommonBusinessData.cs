
using api_be.Domain.Entities;

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
