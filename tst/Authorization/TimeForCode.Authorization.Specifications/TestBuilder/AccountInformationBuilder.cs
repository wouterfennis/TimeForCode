using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimeForCode.Authorization.Domain.Entities;

namespace TimeForCode.Authorization.Specifications.TestBuilder
{
    internal static class AccountInformationBuilder
    {
        public static AccountInformation Build()
        {
            return new AccountInformation
            {
                IdentityProviderId = "IdentityProviderId",
                AvatarUrl = "AvatarUrl",
                Company = "Company",
                Id = new MongoDB.Bson.ObjectId(),
                Login = "Login",
                NodeId = "NodeId",
                Name = "Name",
                Email = "Email"
            };
        }
    }
}