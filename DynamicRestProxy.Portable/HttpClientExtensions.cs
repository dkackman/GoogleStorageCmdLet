﻿using System.IO;
using System.Net.Http;
using System.Dynamic;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DynamicRestProxy.PortableHttpClient
{
    static class HttpClientExtensions
    {
        public async static Task<T> Deserialize<T>(this HttpResponseMessage response, JsonSerializerSettings settings)
        {
            if (typeof(T) == typeof(HttpResponseMessage))
            {
                return (T)(object)response;
            }

            // if the client asked for a stream or byte array, return without serializing to a different type
            if (typeof(T) == typeof(Stream))
            {
                var stream = await response.Content.ReadAsStreamAsync();
                
                return (T)(object)stream;
            }

            if (typeof(T) == typeof(byte[]))
            {
                var bytes = await response.Content.ReadAsByteArrayAsync();
                return (T)(object)bytes;
            }

            var content = await response.Content.ReadAsStringAsync();

            if (!string.IsNullOrEmpty(content))
            {
                // return type is string, just return the content
                if (typeof(T) == typeof(string))
                {
                    return (T)(object)content;
                }

                // if the return type is object return a dynamic object
                if (typeof(T) == typeof(object))
                {
                    return DeserializeToDynamic(content.Trim(), settings);
                }

                // otherwise deserialize to the return type
                return JsonConvert.DeserializeObject<T>(content, settings);
            }

            // no content - return default
            return default(T);
        }

        static dynamic DeserializeToDynamic(string content, JsonSerializerSettings settings)
        {
            Debug.Assert(!string.IsNullOrEmpty(content));

            settings.Converters.Add(new ExpandoObjectConverter());
            if (content.StartsWith("[")) // when the result is a list we need to tell JSonConvert
            {
                return JsonConvert.DeserializeObject<List<dynamic>>(content, settings);
            }

            return JsonConvert.DeserializeObject<ExpandoObject>(content, settings);
        }
    }
}
