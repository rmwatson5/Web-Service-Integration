using System.Net;
using AutoMapper;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;

namespace RW.WebServiceIntegration
{
    public class RestDataService : IRestDataService
    {
        protected readonly IMapper Mapper;

        public RestDataService(IMapper mapper)
        {
            this.Mapper = mapper;
        }

        public bool GetWebResponse<TResponse>(RequestWrapper requestParameters, out TResponse? response)
            where TResponse : class
        {
            response = null;
            var isSuccess = this.GetWebResponse(requestParameters, out var responseString);

            try
            {
                response = JsonConvert.DeserializeObject<TResponse>(responseString);
            }
            catch (Exception ex)
            {
                string requestSerialize;

                try
                {
                    requestSerialize = JsonConvert.SerializeObject(requestParameters.RequestPayload);
                }
                catch (Exception)
                {
                    requestSerialize = "";
                }

                throw new Exception($"Failed to deserialize object. Request: {requestSerialize} {Environment.NewLine} Response: {responseString}", ex);
            }

            return isSuccess;
        }

        public bool GetWebResponse(RequestWrapper requestParameters)
        {
            return this.GetWebResponse(requestParameters, out _);
        }

        public bool GetWebResponse(RequestWrapper requestParameters, out string responseString)
        {
            responseString = string.Empty;
            var serviceRequest = this.GetWebRequest(requestParameters);

            try
            {
                var serviceResponse = serviceRequest.GetResponse();
                using var responseStream = serviceResponse.GetResponseStream();
                if (responseStream != null)
                {
                    var responseReader = new StreamReader(responseStream);
                    responseString = responseReader.ReadToEnd();
                    responseStream.Close();
                }

                return true;
            }
            catch (WebException ex)
            {
                using (var stream = ex.Response?.GetResponseStream())
                {
                    if (stream != null)
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            responseString = reader.ReadToEnd();
                        }
                    }
                }
            }

            return false;
        }

        protected HttpWebRequest GetWebRequest(RequestWrapper requestParameters)
        {
            const string get = "GET";
            const string post = "POST";
            const string put = "PUT";
            const string delete = "DELETE";

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                                                   | SecurityProtocolType.Tls11
                                                   | SecurityProtocolType.Tls12;

            var serviceRequest = (HttpWebRequest)WebRequest.Create(requestParameters.Url);
            serviceRequest.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;

            switch (requestParameters.RequestType)
            {
                case RequestType.Delete:
                    serviceRequest.Method = delete;
                    break;
                case RequestType.Get:
                    serviceRequest = this.GetServiceRequestWithQuery(serviceRequest, requestParameters);
                    serviceRequest.Method = get;
                    break;
                case RequestType.Post:
                    serviceRequest.Method = post;
                    this.SetRequestData(requestParameters, serviceRequest);
                    break;
                case RequestType.Put:
                    serviceRequest.Method = put;
                    this.SetRequestData(requestParameters, serviceRequest);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (requestParameters.Headers?.Any() == true)
            {
                requestParameters.Headers.ToList().ForEach(kv => serviceRequest.Headers.Add(kv.Key, kv.Value));
            }

            this.SetAuthorization(requestParameters, serviceRequest);
            switch (requestParameters.RequestContentType)
            {
                case RequestContentType.FormUrlEncoded:
                    serviceRequest.ContentType = "application/x-www-form-urlencoded";
                    break;
                case RequestContentType.Json:
                default:
                    serviceRequest.ContentType = "application/json";
                    break;
            }

            serviceRequest.Accept = "application/json";
            return serviceRequest;
        }

        protected HttpWebRequest GetServiceRequestWithQuery(HttpWebRequest request, RequestWrapper requestParameters)
        {
            var builder = new UriBuilder(request.RequestUri)
            {
                Query = RequestHelpers.GetRequestQuery(requestParameters.RequestPayload)
            };

            return (HttpWebRequest)WebRequest.Create(builder.ToString());
        }

        protected void SetAuthorization(RequestWrapper requestParameters, HttpWebRequest serviceRequest)
        {
            if (requestParameters.RequestAuthorization == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(requestParameters.RequestAuthorization.Token))
            {
                var authorizationContext = new AuthenticationContext(requestParameters.RequestAuthorization.Authority);
                var clientCredentials = new ClientCredential(requestParameters.RequestAuthorization.ClientId,
                    requestParameters.RequestAuthorization.AppKey);
                var tokenTask = authorizationContext.AcquireTokenAsync(
                    requestParameters.RequestAuthorization.AppResourceId,
                    clientCredentials);
                tokenTask.Wait();

                requestParameters.RequestAuthorization.Token = $"Bearer {tokenTask.Result.AccessToken}";
            }


            serviceRequest.Headers["Authorization"] = $"{requestParameters.RequestAuthorization.Token}";
            serviceRequest.PreAuthenticate = true;
        }

        protected void SetRequestData(RequestWrapper requestParameters, HttpWebRequest serviceRequest)
        {
            // Serialize the request data and send it over
            if (requestParameters.RequestPayload == null)
            {
                return;
            }

            string serializedRequest;
            switch (requestParameters.RequestContentType)
            {
                case RequestContentType.FormUrlEncoded:
                    serializedRequest = this.SerializeFormUrlEncoded(requestParameters.RequestPayload);
                    break;
                case RequestContentType.Json:
                default:
                    serializedRequest = JsonConvert.SerializeObject(requestParameters.RequestPayload, NetwonsoftHelpers.WebServiceSerializerSettings);
                    break;
            }

            using var streamWriter = new StreamWriter(serviceRequest.GetRequestStream());
            streamWriter.Write(serializedRequest);
            streamWriter.Flush();
        }

        protected string SerializeFormUrlEncoded(object payload)
        {
            var keyValues = payload.ToKeyValue();
            var encodedContent = new FormUrlEncodedContent(keyValues);
            var readTask = encodedContent.ReadAsStringAsync();
            readTask.Wait();
            return readTask.Result;
        }
    }
}
