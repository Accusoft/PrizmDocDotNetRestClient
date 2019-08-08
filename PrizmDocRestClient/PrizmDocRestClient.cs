using System;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Accusoft.PrizmDoc.Net.Http
{
    /// <summary>
    /// **(BETA)** HTTP client designed to simplify interactions with PrizmDoc Server.
    /// </summary>
    public class PrizmDocRestClient
    {
        // HttpClient should be used as a singleton.
        // See https://docs.microsoft.com/en-us/dotnet/api/system.net.http.httpclient?view=netcore-2.0#remarks, which says:
        // 
        //    HttpClient is intended to be instantiated once and re-used throughout the life of an application. 
        //    Instantiating an HttpClient class for every request will exhaust the number of sockets available 
        //    under heavy loads. This will result in SocketException errors.
        //
        // See also https://aspnetmonsters.com/2016/08/2016-08-27-httpclientwrong/
        internal static readonly HttpClient _httpClient = new HttpClient();

        // A note about BaseAddress and DefaultRequestHeaders:
        // We want callers to be able to construct multiple instances of PrizmDocRestClient,
        // each instance having its own BaseAddress and DefaultRequestHeaders. Because
        // all instances are using a shared, singleton instance of HttpClient, we can't
        // use the HttpClient's own BaseAddress or DefaultRequestHeaders. So, instead,
        // we define our own in this class and apply them ourselves before sending a
        // request (see AffinitySession.SendAsync).

        /// <summary>
        /// Base address (base URL) to be used for all REST API calls (such as <c>new Uri("https://api.accusoft.com")</c>).
        /// </summary>
        public Uri BaseAddress { get; set; }

        /// <summary>
        /// Request headers to be included in all REST API calls.
        /// </summary>
        /// <example>
        /// Setting an API key header to be sent in all requests:
        /// <code>
        /// var client = new PrizmDocRestClient("https://api.accusoft.com")
        /// {
        ///     DefaultRequestHeaders =
        ///        {
        ///            { "Acs-Api-Key", "YOUR_API_KEY" }
        ///        }
        /// };
        /// </code>
        /// </example>
        public HttpRequestHeaders DefaultRequestHeaders { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PrizmDocRestClient">PrizmDocRestClient</see> class using a specified base address (base URL).
        /// </summary>
        /// <param name="baseAddress">Base address (base URL) to be used for all HTTP requests (such as <c>"https://api.accusoft.com"</c>).</param>
        public PrizmDocRestClient(string baseAddress) : this(new Uri(baseAddress)) {}

        /// <summary>
        /// Initializes a new instance of the <see cref="PrizmDocRestClient">PrizmDocRestClient</see> class using a specified base address (base URL).
        /// </summary>
        /// <param name="baseAddress">Base address (base URL) to be used for all HTTP requests (such as <c>new Uri("https://api.accusoft.com")</c>).</param>
        public PrizmDocRestClient(Uri baseAddress)
        {
            BaseAddress = baseAddress ?? throw new ArgumentNullException("baseAddress");
            DefaultRequestHeaders = new HttpRequestMessage().Headers; // Hack to use an internal constructor to create an empty set of default headers.
        }

        /// <summary>
        /// Creates an <see cref="AffinitySession">AffinitySession</see> which you can use to make HTTP requests.
        /// </summary>
        /// <returns>A new <see cref="AffinitySession">AffinitySession</see>.</returns>
        public AffinitySession CreateAffinitySession() {
            return new AffinitySession(this);
        }
    }
}
