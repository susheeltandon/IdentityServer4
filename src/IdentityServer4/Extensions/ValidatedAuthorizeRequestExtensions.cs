﻿// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityModel;
using IdentityServer4.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace IdentityServer4.Validation
{
    public static class ValidatedAuthorizeRequestExtensions
    {
        public static string GetPrefixedAcrValue(this ValidatedAuthorizeRequest request, string prefix)
        {
            var value = request.AuthenticationContextReferenceClasses
                .FirstOrDefault(x => x.StartsWith(prefix));

            if (value != null)
            {
                value = value.Substring(prefix.Length);
            }

            return value;
        }

        public static string GetIdP(this ValidatedAuthorizeRequest request)
        {
            return request.GetPrefixedAcrValue(Constants.KnownAcrValues.HomeRealm);
        }

        public static string GetTenant(this ValidatedAuthorizeRequest request)
        {
            return request.GetPrefixedAcrValue(Constants.KnownAcrValues.Tenant);
        }

        public static IEnumerable<string> GetAcrValues(this ValidatedAuthorizeRequest request)
        {
            return request
                .AuthenticationContextReferenceClasses
                .Where(acr => !Constants.KnownAcrValues.All.Any(well_known => acr.StartsWith(well_known)))
                .Distinct();
        }

        public static string GenerateSessionStateValue(this ValidatedAuthorizeRequest request)
        {
            if (!request.IsOpenIdRequest) return null;
            if (request.SessionId.IsMissing()) return null;
            if (request.ClientId.IsMissing()) return null;
            if (request.RedirectUri.IsMissing()) return null;

            var clientId = request.ClientId;
            var sessionId = request.SessionId;
            var salt = CryptoRandom.CreateUniqueId();

            var uri = new Uri(request.RedirectUri);
            var origin = uri.Scheme + "://" + uri.Host;
            if (!uri.IsDefaultPort)
            {
                origin += ":" + uri.Port;
            }

            var bytes = Encoding.UTF8.GetBytes(clientId + origin + sessionId + salt);
            byte[] hash;

            using (var sha = SHA256.Create())
            {
                hash = sha.ComputeHash(bytes);
            }

            return Base64Url.Encode(hash) + "." + salt;
        }
    }
}