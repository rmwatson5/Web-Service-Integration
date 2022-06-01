using System.Globalization;
using System.Reflection;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RW.WebServiceIntegration
{
    public static class RequestHelpers
    {
        public static string GetRequestQuery(object? requestParameters)
        {
            if (requestParameters == null)
            {
                return string.Empty;
            }

            var query = string.Empty;
            var properties = requestParameters.GetType().GetProperties();
            foreach (var property in properties)
            {
                var jsonAttribute = property.GetCustomAttribute<JsonPropertyAttribute>();
                var propertyName = jsonAttribute?.PropertyName ?? property.Name;

                query = string.IsNullOrWhiteSpace(query) ? "?" : query + "&";
                query += $"{propertyName}={property.GetValue(requestParameters)}";
            }

            return query;
        }

        public static IDictionary<string, string>? ToKeyValue(this object? metaToken)
        {
            if (metaToken == null)
            {
                return null;
            }

            if (metaToken is not JToken token)
            {
                return JObject.FromObject(metaToken).ToKeyValue();
            }

            if (token.HasValues)
            {
                var contentData = new Dictionary<string, string>();

                return token.Children()
                    .ToList()
                    .Select(child => child.ToKeyValue())
                    .Where(childContent => childContent != null)
                    .Aggregate(contentData, (current, childContent) => current.Concat(childContent)
                        .ToDictionary(k => k.Key, v => v.Value));
            }

            var jValue = token as JValue;
            if (jValue?.Value == null)
            {
                return null;
            }

            var value = jValue?.Type == JTokenType.Date ?
                jValue?.ToString("o", CultureInfo.InvariantCulture) :
                jValue?.ToString(CultureInfo.InvariantCulture);

            return new Dictionary<string, string> { { token.Path, HttpUtility.UrlEncode(value) } };
        }
    }
}
