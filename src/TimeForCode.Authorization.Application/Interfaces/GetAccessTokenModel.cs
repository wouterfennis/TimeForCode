using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeForCode.Authorization.Application.Interfaces
{
    public class GetAccessTokenModel
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string Code { get; set; }
    }
}
