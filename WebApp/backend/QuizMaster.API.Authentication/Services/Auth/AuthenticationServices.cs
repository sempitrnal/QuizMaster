﻿using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using QuizMaster.API.Authentication.Configuration;
using QuizMaster.API.Authentication.Models;
using QuizMaster.API.Authentication.Services.Temp;
using QuizMaster.Library.Common.Entities.Accounts;
using QuizMaster.Library.Common.Utilities;
using System.Text.Json;

namespace QuizMaster.API.Authentication.Services.Auth
{
    public class AuthenticationServices : IAuthenticationServices
    {
        private readonly IRepository repository;
        private readonly AppSettings appSettings;
        public AuthenticationServices(IRepository repository, IOptions<AppSettings> options)
        {
            this.repository = repository;
            appSettings = options.Value;
        }

        public AuthResponse Authenticate(AuthRequest authRequest)
        {
            UserAccount userAccount = repository.GetUserByUsername(authRequest.Username);

            if (userAccount.Id == -1) { userAccount = repository.GetUserByEmail(authRequest.Email); }
            if (userAccount.Id == -1) { return new() { Token = null }; };

            // intialize password hasher
            PasswordHasher<UserAccount> hasher = new();

            // check if password is correct
            var passwordVerification = hasher.VerifyHashedPassword(userAccount, userAccount.PasswordHash, authRequest.Password);
            if (PasswordVerificationResult.Success != passwordVerification) { return new() { Token = null }; };

            // attributes to store in the JWT token
            Dictionary<string, string> keyValuePairs = new Dictionary<string, string>();
            // I will be storing the whole object in the token
            string userAccountJson = JsonConvert.SerializeObject(userAccount);
            keyValuePairs.Add("user", userAccountJson);
            keyValuePairs.Add("timestamp", DateTime.UtcNow.ToString());

            // include user roles in the token
            string userRolesJson = JsonConvert.SerializeObject(repository.GetRoles(userAccount.Id));
            keyValuePairs.Add("roles", userRolesJson);

            // generate the token
            string jwtToken = JWTHelper.GenerateJsonWebToken(appSettings.JWTSecret, keyValuePairs);

            return new() { Token = jwtToken };
        }
    }
}
