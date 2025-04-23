#pragma warning disable

using System.Net.Http.Headers;
using System.Text;
using The_Untamed_Music_Player;
using The_Untamed_Music_Player.OnlineAPIs;
using The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI;
using The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI.Extensions;
using The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI.Extensions;

namespace The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI.Extensions;
internal static class HttpClientExtensions
{
    public static Task<HttpResponseMessage> SendAsync(this HttpClient client, HttpMethod method, string url) => client.SendAsync(method, url, null, null);

    public static Task<HttpResponseMessage> SendAsync(this HttpClient client, HttpMethod method, string url, IEnumerable<KeyValuePair<string, string>> queries, IEnumerable<KeyValuePair<string, string>> headers) => client.SendAsync(method, url, queries, headers, (byte[])null, "application/x-www-form-urlencoded");

    public static Task<HttpResponseMessage> SendAsync(this HttpClient client, HttpMethod method, string url, IEnumerable<KeyValuePair<string, string>> queries, IEnumerable<KeyValuePair<string, string>> headers, string content, string contentType) => client.SendAsync(method, url, queries, headers, content is null ? null : Encoding.UTF8.GetBytes(content), contentType);

    public static Task<HttpResponseMessage> SendAsync(this HttpClient client, HttpMethod method, string url, IEnumerable<KeyValuePair<string, string>> queries, IEnumerable<KeyValuePair<string, string>> headers, byte[] content, string contentType)
    {
        ArgumentNullException.ThrowIfNull(client);

        ArgumentNullException.ThrowIfNull(method);

        if (string.IsNullOrEmpty(url))
        {
            throw new ArgumentNullException(nameof(url));
        }

        if (string.IsNullOrEmpty(contentType))
        {
            throw new ArgumentNullException(nameof(contentType));
        }

        UriBuilder uriBuilder;
        HttpRequestMessage request;

        uriBuilder = new UriBuilder(url);
        if (queries is not null)
        {
            string query;

            query = queries.ToQueryString();
            if (!string.IsNullOrEmpty(query))
            {
                if (string.IsNullOrEmpty(uriBuilder.Query))
                {
                    uriBuilder.Query = query;
                }
                else
                {
                    uriBuilder.Query += "&" + query;
                }
            }
        }
        request = new HttpRequestMessage(method, uriBuilder.Uri);
        if (content is not null)
        {
            request.Content = new ByteArrayContent(content);
        }
        else if (queries is not null && method != HttpMethod.Get)
        {
            request.Content = new FormUrlEncodedContent(queries);
        }

        if (request.Content is not null)
        {
            request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        }

        if (headers is not null)
        {
            foreach (var header in headers)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return client.SendAsync(request);
    }
}
