using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimeForCode.Authorization.Application.Interfaces;

namespace TimeForCode.Authorization.Infrastructure.Services
{
    internal class RestService : IRestService
    {
        public RestService()
        {
            // initialize restsharp

        }

        public Task<string> GetAccessTokenAsync(GetAccessTokenModel model)
        {
            // Initialize the RestClient with the base URL
            var client = new RestClient("https://api.example.com");

            // Create a new RestRequest with the endpoint and method
            var request = new RestRequest("resource/endpoint", Method.Get);
        }

        public OutputModel Post<InputModel, OutputModel>(InputModel body)
            where InputModel : class
            where OutputModel : class
        {
            // Initialize the RestClient with the base URL
            var client = new RestClient("https://api.example.com");

            // Create a new RestRequest with the endpoint and method
            var request = new RestRequest("resource/endpoint", Method.Get);


            var uriBuilder = new UriBuilder
            {
                Scheme = "https",
                Path = "/login/oauth/access_token"
            };

            var query = HttpUtility.ParseQueryString(string.Empty);
            query["code"] = code;
            query["redirect_uri"] = _options.TokenExchangeUri;

            switch (identityProvider)
            {
                case IdentityProvider.Github:
                    uriBuilder.Host = "github.com";
                    query["client_id"] = _options.Github.ClientId;
                    query["client_secret"] = _options.Github.ClientSecret;
                    uriBuilder.Query = query.ToString();

                    return uriBuilder.Uri;
                default:
                    throw new NotImplementedException();
            }




            // Optionally, add parameters to the request
            request.AddParameter("param1", "value1");
            request.AddParameter("param2", "value2");

            // Execute the request and get the response
            var response = await client.ExecuteAsync(request);

            // Check the response status and handle the response
            if (response.IsSuccessful)
            {
                Console.WriteLine("Response Content: " + response.Content);
            }
            else
            {
                Console.WriteLine("Error: " + response.ErrorMessage);
            }
        }
    }
}
