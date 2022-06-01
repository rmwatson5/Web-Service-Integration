using Newtonsoft.Json;

namespace RW.WebServiceIntegration
{
    public static class NetwonsoftHelpers
    {
        public static JsonSerializerSettings WebServiceSerializerSettings => new()
        {
            TypeNameHandling = TypeNameHandling.None,
            NullValueHandling = NullValueHandling.Ignore,
            DateFormatString = "MM/dd/yyyy"
        };

        public static JsonSerializerSettings StandardSerializerSettings => new()
        {
            TypeNameHandling = TypeNameHandling.Objects,
            ReferenceLoopHandling = ReferenceLoopHandling.Error,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            DateFormatString = "MM/dd/yyyy",
            Error = (sender, args) => args.ErrorContext.Handled = true
        };

        public static JsonSerializer WebServiceSerializer => JsonSerializer.Create(WebServiceSerializerSettings);

        public static JsonSerializer StandardSerializer => JsonSerializer.Create(StandardSerializerSettings);

        public static bool TryDeserialize<T>(this string value, out T? item)
            where T : class
        {
            item = null;

            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            item = JsonConvert.DeserializeObject<T>(value);
            return item != null;
        }
    }
}
