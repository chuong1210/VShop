﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace api_be.Middleware
{
    public static class AuthorizationExtensions
    {
        public static void AddPermissionPoliciesFromAttributes(this AuthorizationOptions options, Assembly assembly)
        {
            var controllerTypes = assembly.GetTypes().Where(type => typeof(ControllerBase).IsAssignableFrom(type));

            foreach (var controllerType in controllerTypes)
            {
                var methods = controllerType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

                foreach (var methodInfo in methods)
                {
                    var permissionAttributes = methodInfo.GetCustomAttributes<PermissionAttribute>(true);

                    foreach (var attribute in permissionAttributes)
                    {
                        options.AddPolicy(attribute.Policy, policy =>
                        {
                            policy.Requirements.Add(new PermissionRequirement(attribute.Policy));
                        });
                    }
                }
            }
        }

        public static List<string> GetPermissionPoliciesFromAttributes(Assembly assembly)
        {
            List<string> permissions = new List<string>();

            var controllerTypes = assembly.GetTypes().Where(type => typeof(ControllerBase).IsAssignableFrom(type));

            foreach (var controllerType in controllerTypes)
            {
                var methods = controllerType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

                foreach (var methodInfo in methods)
                {
                    var permissionAttributes = methodInfo.GetCustomAttributes<PermissionAttribute>(true);

                    foreach (var attribute in permissionAttributes)
                    {
                        permissions.Add(attribute.Policy);
                    }
                }
            }
            return permissions;
        }
    }
}
