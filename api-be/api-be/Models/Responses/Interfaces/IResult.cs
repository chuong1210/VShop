﻿namespace api_be.Models.Responses.Interfaces
{
    public interface IResult<T>
    {
        T Data { get; set; }

        List<string> Messages { get; set; }

        bool Succeeded { get; set; }

        int Code { get; set; }
    }
}
