using api_be.Core.Domain;
using api_be.Application.Responses;
using Twilio.TwiML.Voice;
using api_be.Domain.ResultResponses;
using api_be.Application.Models.Common;

namespace api_be.Application.Services
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
