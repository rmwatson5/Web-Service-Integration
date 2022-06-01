namespace RW.WebServiceIntegration
{
    public class RequestWrapper
    {
        public string Url { get; set; }
        public RequestType RequestType { get; set; }
        public RequestContentType RequestContentType { get; set; } = RequestContentType.Json;
        public object? RequestPayload { get; set; }
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
        public RequestAuthorization RequestAuthorization { get; set; }
    }
}
