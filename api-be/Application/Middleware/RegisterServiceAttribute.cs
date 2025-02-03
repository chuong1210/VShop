using Microsoft.Extensions.DependencyInjection;
using System;

namespace api_be.Middleware
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class RegisterServiceAttribute : Attribute
    {


        public ServiceLifetime Lifetime { get; }

        public RegisterServiceAttribute(ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            Lifetime = lifetime;
        }

    }
}
