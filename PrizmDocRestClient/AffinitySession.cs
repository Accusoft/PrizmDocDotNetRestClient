using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Accusoft.PrizmDoc.Net.Http
{
    /// <summary>
    /// Class representing an "affinity session", a group of HTTP requests all related to a single document processing workflow and which must all be routed to the same machine.
    /// </summary>
    public class AffinitySession
    {
        private readonly PrizmDocRestClient client;

        /// <summary>
        /// Affinity token currently in use for this session.
        /// It is unlikely you will ever need to use this value directly, but we expose it for transparency.
        /// The <see cref="AffinitySession">AffinitySession</see> class automatically ensures that HTTP requests have
        /// the <c>Accusoft-Affinity-Token</c> request header set correctly.
        /// This property just tells you the value of the affinity token
        /// that this class is using when it makes HTTP requests. Initially,
        /// this property will be <c>null</c>. However, the first time a
        /// JSON response is returned with an <c>"affinityToken"</c> value,
        /// this property will be set and "locked" to that value, and the <see cref="AffinitySession">AffinitySession</see>
        /// will ensure that all subsequent HTTP requests include an
        /// <c>Accuosft-Affinity-Token</c> request header set to this value.
        /// </summary>
        public string AffinityToken { get; private set; }

        internal AffinitySession(PrizmDocRestClient client)
        {
            this.client = client;
        }

        /// <summary>
        /// Repeatedly GETs a resource which represents a process until the response JSON <c>"state"</c> is a value other than <c>"processing"</c>,
        /// then returns the response. This is a convenience method which you can call to simply await the final status of a process.
        /// </summary>
        /// <param name="processResource">Path to a RESTful resource which represents a process, such as a content converter process (like <c>"/v2/contentConverters/ElkNzWtrUJp4rXI5YnLUgw"</c>) or markup burner process (like <c>"/PCCIS/V1/MarkupBurner/ElkNzWtrUJp4rXI5YnLUgw"</c>).</param>
        /// <returns>Task which returns the final HTTP response with information about the final status of the process.</returns>
        public async Task<HttpResponseMessage> GetFinalProcessStatusAsync(string processResource)
        {
            Tuple<string, HttpResponseMessage> stateAndResponse;
            string state;
            const int START_DELAY = 500; // ms = 1/2 second
            const int MAX_DELAY = 8000; // ms = 5 seconds

            var firstRequest = true;
            var delay = START_DELAY;

            do
            {
                if (!firstRequest)
                {
                    await Task.Delay(delay);

                    if (delay < MAX_DELAY)
                    {
                        delay *= 2;
                    }
                }

                stateAndResponse = await GetCurrentProcessStateAndHttpResponseMessage(processResource);
                state = stateAndResponse.Item1;

                firstRequest = false;
            } while (state == "processing");

            var response = stateAndResponse.Item2;
            return response;
        }

        private async Task<Tuple<string, HttpResponseMessage>> GetCurrentProcessStateAndHttpResponseMessage(string processResource)
        {
            var response = await GetAsync(processResource);
            response.EnsureSuccessStatusCode();

            if (response.Content.Headers.ContentType.MediaType != "application/json")
            {
                throw new Exception("After sending an HTTP request to GET process status, the response Content-Type was not application/json. Are you sure you are using the correct URL to get information about a process?");
            }

            string state = null;

            try
            {
                var json = await response.Content.ReadAsStringAsync();
                var obj = JObject.Parse(json);
                state = (string)obj["state"];
            }
            catch
            {
                throw new Exception("After sending an HTTP request to GET process status, the response could not be parsed as JSON. Are you sure you are using the correct URL to get information about a process?");
            }

            if (state == null)
            {
                throw new Exception("After sending an HTTP request to GET process status, the HTTP response did not appear to be providing information about a process. For any process, the response body should be JSON with a \"state\" property, but this response JSON did not have a \"state\" property. Are you sure you are using the correct URL to get information about a process?");
            }

            return Tuple.Create(state, response);
        }

        /// <summary>
        /// Send an HTTP GET to the specified path.
        /// </summary>
        /// <param name="path">Path you want to GET.</param>
        /// <returns>Task which returns the HTTP response.</returns>
        public Task<HttpResponseMessage> GetAsync(string path)
        {
            return SendAsync(new HttpRequestMessage(HttpMethod.Get, path));
        }

        /// <summary>
        /// Send an HTTP POST to the specified path.
        /// </summary>
        /// <param name="path">Path you want to POST to.</param>
        /// <param name="content">Request body.</param>
        /// <returns>Task which returns the HTTP response.</returns>
        public Task<HttpResponseMessage> PostAsync(string path, HttpContent content)
        {
            return SendAsync(new HttpRequestMessage(HttpMethod.Post, path)
            {
                Content = content
            });
        }

        /// <summary>
        /// Send an HTTP PUT to the specified path.
        /// </summary>
        /// <param name="path">Path you want to PUT to.</param>
        /// <param name="content">Request body.</param>
        /// <returns>Task which returns the HTTP response.</returns>
        public Task<HttpResponseMessage> PutAsync(string path, HttpContent content)
        {
            return SendAsync(new HttpRequestMessage(HttpMethod.Put, path)
            {
                Content = content
            });
        }

        /// <summary>
        /// Send an HTTP DELETE to the specified path.
        /// </summary>
        /// <param name="path">Path you want to DELETE.</param>
        /// <returns>Task which returns the HTTP response.</returns>
        public Task<HttpResponseMessage> DeleteAsync(string resource)
        {
            return SendAsync(new HttpRequestMessage(HttpMethod.Delete, resource));
        }

        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
        {
            // If we have an affinity token for this session
            // and the request does not have a custom affinity token set,
            // set the affinity token for the request.
            if (AffinityToken != null && !request.Headers.Contains("Accusoft-Affinity-Token"))
            {
                request.Headers.Add("Accusoft-Affinity-Token", AffinityToken);
            }

            // A note about applying the BaseAddress and DefaultRequestHeaders:
            // We want callers to be able to construct multiple instances of PrizmDocRestClient,
            // each instance having its own BaseAddress and DefaultRequestHeaders. Because
            // all instances are using a shared, singleton instance of HttpClient, we can't
            // use the HttpClient's own BaseAddress or DefaultRequestHeaders. So, instead,
            // we need to apply the PrizmDocRestClient instance's own BaseAddress
            // and DefaultRequestHeaders before sending each request.

            // Apply the BaseAddress.
            request.RequestUri = new Uri(client.BaseAddress, request.RequestUri);

            // Apply the DefaultRequestHeaders.
            if (client.DefaultRequestHeaders != null)
            {
                foreach (var header in client.DefaultRequestHeaders)
                {
                    if (!request.Headers.Contains(header.Key))
                    {
                        request.Headers.Add(header.Key, header.Value);
                    }
                }
            }

            // Send the reuqest.
            var response = await PrizmDocRestClient._httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            // If we don't have an affinity token for this session, look for an affinity token in the response JSON.
            if (AffinityToken == null &&
                response.Content.Headers.ContentType != null &&
                response.Content.Headers.ContentType.MediaType == "application/json") {
                var json = await response.Content.ReadAsStringAsync();

                JObject obj;

                try {
                    obj = JObject.Parse(json);
                } catch {
                    // Couldn't parse the JSON as an object, so just return early.
                    return response;
                }

                var affinityToken = (string)obj["affinityToken"];
                if (affinityToken != null) {
                    AffinityToken = affinityToken;
                }
            }

            return response;
        }
    }
}
