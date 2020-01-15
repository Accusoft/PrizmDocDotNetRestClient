using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Accusoft.PrizmDoc.Net.Http.Tests
{
    [TestClass]
    public class AffinitySession_SendAsync_AffinityToken_Tests
    {
        static PrizmDocRestClient client;
        static FluentMockServer mockServer;

        [ClassInitialize]
        public static void BeforeAll(TestContext context)
        {
            mockServer = FluentMockServer.Start();
            client = new PrizmDocRestClient("http://localhost:" + mockServer.Ports.First());
            client.DefaultRequestHeaders.Add("Acs-Api-Key", System.Environment.GetEnvironmentVariable("API_KEY"));
        }

        [ClassCleanup]
        public static void AfterAll()
        {
            mockServer.Stop();
            mockServer.Dispose();
        }

        [TestInitialize]
        public void BeforeEach()
        {
            mockServer.Reset();
        }

        [DataTestMethod]
        [DataRow("application/json")]
        [DataRow("application/json; charset=utf-8")]
        public async Task SendAsync_automatically_finds_affinity_token_in_a_JSON_response_and_uses_it_in_subsequent_requests(string responseContentType)
        {
            const string AFFINITY_TOKEN = "example-affinity-token";
            IResponseBuilder responseWithAffinityToken = Response.Create()
                        .WithStatusCode(200)
                        .WithHeader("Content-Type", responseContentType)
                        .WithBody("{ \"id\": 123, \"affinityToken\": \"" + AFFINITY_TOKEN + "\" }");

            mockServer
                .Given(Request.Create().WithPath("/wat").UsingPost())
                .RespondWith(responseWithAffinityToken);

            mockServer
                .Given(Request.Create().WithPath("/wat/123").UsingGet())
                .RespondWith(responseWithAffinityToken);

            AffinitySession session = client.CreateAffinitySession();

            string originalAffinityToken = null;

            Assert.IsNull(session.AffinityToken);

            // First request
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/wat"))
            using (HttpResponseMessage response = await session.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                Assert.IsFalse(response.RequestMessage.Headers.Contains("Accusoft-Affinity-Token"), "An Accusoft-Affinity-Token header was incorrectly sent in the first request.");

                string json = await response.Content.ReadAsStringAsync();
                JObject obj = JObject.Parse(json);
                originalAffinityToken = (string)obj["affinityToken"];
                Assert.AreEqual(AFFINITY_TOKEN, originalAffinityToken, "The mock server did not respond with the affinityToken value we expected. Something is wrong with this test.");
                Assert.AreEqual(AFFINITY_TOKEN, session.AffinityToken, "The AffinitySession.AffinityToken property was not set after making the first request!");
            }

            // Second request
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "/wat/123"))
            using (HttpResponseMessage response = await session.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                Assert.IsTrue(response.RequestMessage.Headers.Contains("Accusoft-Affinity-Token"), "Having already received an affinityToken in the response to the first request, the second request failed to include an Accusoft-Affinity-Token header.");
                Assert.AreEqual(originalAffinityToken, response.RequestMessage.Headers.GetValues("Accusoft-Affinity-Token").FirstOrDefault(), "Having already received an affinityToken in the response to the first request, the second request included an Accusoft-Affinity-Token header but did not set it to the correct value.");
                Assert.AreEqual(AFFINITY_TOKEN, session.AffinityToken, "The AffinitySession.AffinityToken value changed unexpectedly after making the second request!");
            }

            // Third request
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "/wat/123"))
            using (HttpResponseMessage response = await session.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                Assert.IsTrue(response.RequestMessage.Headers.Contains("Accusoft-Affinity-Token"), "Having already received an affinityToken in the response to the first request, the third request failed to include an Accusoft-Affinity-Token header.");
                Assert.AreEqual(originalAffinityToken, response.RequestMessage.Headers.GetValues("Accusoft-Affinity-Token").FirstOrDefault(), "Having already received an affinityToken in the response to the first request, the third request included an Accusoft-Affinity-Token header but did not set it to the correct value.");
                Assert.AreEqual(AFFINITY_TOKEN, session.AffinityToken, "The AffinitySession.AffinityToken value changed unexpectedly after making the third request!");
            }
        }

        [TestMethod]
        public async Task SendAsync_does_not_attempt_to_find_an_affinity_token_when_the_response_media_type_is_not_JSON()
        {
            const string NON_JSON_RESPONSE_CONTENT_TYPE = "text/plain";
            const string AFFINITY_TOKEN = "example-affinity-token";
            IResponseBuilder responseWithNonJsonContentType = Response.Create()
                        .WithStatusCode(200)
                        .WithHeader("Content-Type", NON_JSON_RESPONSE_CONTENT_TYPE)
                        .WithBody("{ \"id\": 123, \"affinityToken\": \"" + AFFINITY_TOKEN + "\" }");

            mockServer
                .Given(Request.Create().WithPath("/wat").UsingPost())
                .RespondWith(responseWithNonJsonContentType);

            mockServer
                .Given(Request.Create().WithPath("/wat/123").UsingGet())
                .RespondWith(responseWithNonJsonContentType);

            AffinitySession session = client.CreateAffinitySession();

            // First request
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/wat"))
            using (HttpResponseMessage response = await session.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                Assert.IsFalse(response.RequestMessage.Headers.Contains("Accusoft-Affinity-Token"), "An Accusoft-Affinity-Token header was incorrectly sent in the first request.");
            }

            // Second request
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "/wat/123"))
            using (HttpResponseMessage response = await session.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                Assert.IsFalse(response.RequestMessage.Headers.Contains("Accusoft-Affinity-Token"), "An Accusoft-Affinity-Token header was incorrectly sent in the second request.");
            }

            // Third request
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "/wat/123"))
            using (HttpResponseMessage response = await session.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                Assert.IsFalse(response.RequestMessage.Headers.Contains("Accusoft-Affinity-Token"), "An Accusoft-Affinity-Token header was incorrectly sent in the third request.");
            }
        }

        [DataTestMethod]
        [DataRow("application/json")]
        [DataRow("application/json; charset=utf-8")]
        public async Task SendAsync_gracefully_stops_looking_for_an_affinity_token_when_the_response_content_type_indicates_JSON_but_the_body_is_not_valid_JSON(string responseContentType)
        {
            IResponseBuilder responseWhoseBodyIsNotActuallyValidJson = Response.Create()
                        .WithStatusCode(200)
                        .WithHeader("Content-Type", responseContentType)
                        .WithBody("This is not JSON");

            mockServer
                .Given(Request.Create().WithPath("/wat").UsingPost())
                .RespondWith(responseWhoseBodyIsNotActuallyValidJson);

            mockServer
                .Given(Request.Create().WithPath("/wat/123").UsingGet())
                .RespondWith(responseWhoseBodyIsNotActuallyValidJson);

            AffinitySession session = client.CreateAffinitySession();

            // First request
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/wat"))
            using (HttpResponseMessage response = await session.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                Assert.IsFalse(response.RequestMessage.Headers.Contains("Accusoft-Affinity-Token"), "An Accusoft-Affinity-Token header was incorrectly sent in the first request.");
            }

            // Second request
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "/wat/123"))
            using (HttpResponseMessage response = await session.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                Assert.IsFalse(response.RequestMessage.Headers.Contains("Accusoft-Affinity-Token"), "An Accusoft-Affinity-Token header was incorrectly sent in the second request.");
            }

            // Third request
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "/wat/123"))
            using (HttpResponseMessage response = await session.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                Assert.IsFalse(response.RequestMessage.Headers.Contains("Accusoft-Affinity-Token"), "An Accusoft-Affinity-Token header was incorrectly sent in the third request.");
            }
        }

        [TestMethod]
        public async Task The_programmer_can_override_the_affinity_token_on_a_specific_request_if_they_need_to()
        {
            const string SESSION_AFFINITY_TOKEN = "session-affinity-token";
            const string CUSTOM_AFFINITY_TOKEN = "custom-affinity-token";

            IResponseBuilder responseWithAffinityToken = Response.Create()
                        .WithStatusCode(200)
                        .WithHeader("Content-Type", "application/json")
                        .WithBody("{ \"id\": 123, \"affinityToken\": \"" + SESSION_AFFINITY_TOKEN + "\" }");

            mockServer
                .Given(Request.Create().WithPath("/wat").UsingPost())
                .RespondWith(responseWithAffinityToken);

            mockServer
                .Given(Request.Create().WithPath("/wat/123").UsingGet())
                .RespondWith(responseWithAffinityToken);

            AffinitySession session = client.CreateAffinitySession();

            string originalAffinityToken = null;

            // First request
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/wat"))
            using (HttpResponseMessage response = await session.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                Assert.IsFalse(response.RequestMessage.Headers.Contains("Accusoft-Affinity-Token"), "An Accusoft-Affinity-Token header was incorrectly sent in the first request.");

                string json = await response.Content.ReadAsStringAsync();
                JObject obj = JObject.Parse(json);
                originalAffinityToken = (string)obj["affinityToken"];
                Assert.AreEqual(SESSION_AFFINITY_TOKEN, originalAffinityToken, "The mock server did not respond with the affinityToken value we expected. Something is wrong with this test.");
            }

            // Second request
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "/wat/123"))
            using (HttpResponseMessage response = await session.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                Assert.IsTrue(response.RequestMessage.Headers.Contains("Accusoft-Affinity-Token"), "Having already received an affinityToken in the response to the first request, the second request failed to include an Accusoft-Affinity-Token header.");
                Assert.AreEqual(SESSION_AFFINITY_TOKEN, response.RequestMessage.Headers.GetValues("Accusoft-Affinity-Token").FirstOrDefault(), "Having already received an affinityToken in the response to the first request, the second request included an Accusoft-Affinity-Token header but did not set it to the correct value.");
            }

            // Third request: Use a custom affinity token for this request
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "/wat/123"))
            {
                request.Headers.Add("Accusoft-Affinity-Token", CUSTOM_AFFINITY_TOKEN);

                using (HttpResponseMessage response = await session.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    Assert.IsTrue(response.RequestMessage.Headers.Contains("Accusoft-Affinity-Token"), "Having already received an affinityToken in the response to the first request, the third request failed to include an Accusoft-Affinity-Token header.");
                    Assert.AreEqual(CUSTOM_AFFINITY_TOKEN, response.RequestMessage.Headers.GetValues("Accusoft-Affinity-Token").FirstOrDefault(), "Having already received an affinityToken in the response to the first request, the third request included an Accusoft-Affinity-Token header but did not set it to the correct value.");
                }
            }
        }

        [TestMethod]
        public async Task The_programmer_can_construct_a_new_affinity_session_pre_locked_to_a_specified_affinity_token()
        {
            const string EXISTING_AFFINITY_TOKEN = "existing-affinity-token";
            const string NEW_AFFINITY_TOKEN = "new-affinity-token";
            IResponseBuilder responseWithAffinityToken = Response.Create()
                        .WithStatusCode(200)
                        .WithHeader("Content-Type", "application/json")
                        .WithBody("{ \"id\": 123, \"affinityToken\": \"" + NEW_AFFINITY_TOKEN + "\" }");

            mockServer
                .Given(Request.Create().WithPath("/wat").UsingPost())
                .RespondWith(responseWithAffinityToken);

            mockServer
                .Given(Request.Create().WithPath("/wat/123").UsingGet())
                .RespondWith(responseWithAffinityToken);

            AffinitySession session = client.CreateAffinitySession(EXISTING_AFFINITY_TOKEN);

            Assert.AreEqual(EXISTING_AFFINITY_TOKEN, session.AffinityToken);

            // First request
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/wat"))
            using (HttpResponseMessage response = await session.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                Assert.IsTrue(response.RequestMessage.Headers.Contains("Accusoft-Affinity-Token"), "The first request failed to include an Accusoft-Affinity-Token header with the provided existing affinity token.");
                Assert.AreEqual(EXISTING_AFFINITY_TOKEN, session.AffinityToken, "The AffinitySession.AffinityToken value changed unexpectedly after making the first request!");
            }

            // Second request
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "/wat/123"))
            using (HttpResponseMessage response = await session.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                Assert.IsTrue(response.RequestMessage.Headers.Contains("Accusoft-Affinity-Token"), "The second request failed to include an Accusoft-Affinity-Token header with the provided existing affinity token.");
                Assert.AreEqual(EXISTING_AFFINITY_TOKEN, session.AffinityToken, "The AffinitySession.AffinityToken value changed unexpectedly after making the second request!");
            }

            // Third request
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "/wat/123"))
            using (HttpResponseMessage response = await session.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                Assert.IsTrue(response.RequestMessage.Headers.Contains("Accusoft-Affinity-Token"), "The third request failed to include an Accusoft-Affinity-Token header with the provided existing affinity token.");
                Assert.AreEqual(EXISTING_AFFINITY_TOKEN, session.AffinityToken, "The AffinitySession.AffinityToken value changed unexpectedly after making the third request!");
            }
        }
    }
}
