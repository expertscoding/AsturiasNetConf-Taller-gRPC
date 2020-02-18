using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using IdentityModel;
using IdentityServer4.Test;
using Microsoft.AspNetCore.Identity;

namespace OidC.Services.InMemory
{
    public class TestUsers
    {
        public static List<TestUser> Users = new List<TestUser>
        {
            new TestUser
            {
                SubjectId = "123456", Username = "antonio.marin@expertscoding.es", Password = "Pass1234$",
                Claims =
                {
                    new Claim(JwtClaimTypes.Name, "Antonio Marín"),
                    new Claim(JwtClaimTypes.GivenName, "Antonio"),
                    new Claim(JwtClaimTypes.FamilyName, "Marín"),
                    new Claim(JwtClaimTypes.Email, "antonio.marin@expertscoding.es"),
                    new Claim(JwtClaimTypes.EmailVerified, "true", ClaimValueTypes.Boolean),
                    new Claim(JwtClaimTypes.WebSite, "http://expertscoding.es"),
                    new Claim(JwtClaimTypes.Address, @"{ 'street_address': 'One Hacker Way', 'locality': 'Heidelberg', 'postal_code': 69118, 'country': 'Germany' }", IdentityServer4.IdentityServerConstants.ClaimValueTypes.Json),
                    new Claim(JwtClaimTypes.Role, "Administrator"),
                }
            },
            new TestUser
            {
                SubjectId = "123457", Username = "manuel.vilachan@expertscoding.es", Password = "Pass456&",
                Claims =
                {
                    new Claim(JwtClaimTypes.Name, "Manu Vilachán"),
                    new Claim(JwtClaimTypes.GivenName, "Manu"),
                    new Claim(JwtClaimTypes.FamilyName, "Vilachán"),
                    new Claim(JwtClaimTypes.Email, "manuel.vilachan@expertscoding.es"),
                    new Claim(JwtClaimTypes.EmailVerified, "true", ClaimValueTypes.Boolean),
                    new Claim(JwtClaimTypes.WebSite, "http://expertscoding.es"),
                    new Claim(JwtClaimTypes.Address, @"{ 'street_address': 'One Hacker Way', 'locality': 'Heidelberg', 'postal_code': 69118, 'country': 'Germany' }", IdentityServer4.IdentityServerConstants.ClaimValueTypes.Json),
                    new Claim(JwtClaimTypes.Role, "Reader"),
                }
            }
        };
    }

    public static class TestUserExtensions
    {
        public static IdentityUser ToIdentityUser(this TestUser user)
        {
            var idUser = new IdentityUser
            {
                UserName = user.Username,
                NormalizedUserName = user.Username.ToUpper(),
                Email = user.Claims.FirstOrDefault(c => c.Type.Equals(JwtClaimTypes.Email))?.Value,
                NormalizedEmail = user.Claims.FirstOrDefault(c => c.Type.Equals(JwtClaimTypes.Email))?.Value.ToUpper(),
                Id = user.SubjectId,
                SecurityStamp = Guid.NewGuid().ToString(),
                PasswordHash = user.Password
            };

            return idUser;
        }
    }
}