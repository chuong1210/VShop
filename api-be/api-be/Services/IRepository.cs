using api_be.Domain.Common;
using api_be.Models.Common;
using api_be.Models.Responses;
using Twilio.TwiML.Voice;

namespace api_be.Services
{
    public interface IRepository<T> where T  : BaseDto 
    {
        Task<Result<T>> AddAsync(T request) ;
        Task<Result<T>> UpdateAsync(T request);
        Task<bool> DeleteAsync(int id);
        Task<Result<T>> GetByIdAsync(int id);
        Task<PaginatedResult<List<T>>> GetAllAsync();
    }
}
