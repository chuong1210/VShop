
using api_be.Core.Entities;

namespace api_be.Infrastructure.DB.Common
{
    public static class CommonBusinessData
    {
       public static List<Type> ImmediateDeleteTypes = new List<Type> 
       { 
           typeof(Product),
       };
    }
}
