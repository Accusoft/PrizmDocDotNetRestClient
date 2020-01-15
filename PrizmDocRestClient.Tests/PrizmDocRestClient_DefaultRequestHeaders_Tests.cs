using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Accusoft.PrizmDoc.Net.Http.Tests
{
    [TestClass]
    public class PrizmDocRestClient_DefaultRequestHeaders_Tests
    {
        static PrizmDocRestClient client;
        static FluentMockServer mockServer;

        [ClassInitialize]
        public static void BeforeAll(TestContext context)
        {
            mockServer = FluentMockServer.Start();
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

        [TestMethod]
        public async Task DefaultRequestHeaders_are_correctly_applied_to_each_request()
        {
            mockServer
                .Given(Request.Create().WithPath("/wat/123").UsingGet())
                .RespondWith(Response.Create().WithStatusCode(200).WithBody("GET Response"));

            mockServer
                .Given(Request.Create().WithPath("/wat").UsingPost())
                .RespondWith(Response.Create().WithStatusCode(200).WithBody("POST Response"));

            mockServer
                .Given(Request.Create().WithPath("/wat/123").UsingPut())
                .RespondWith(Response.Create().WithStatusCode(200).WithBody("PUT Response"));

            mockServer
                .Given(Request.Create().WithPath("/wat/123").UsingDelete())
                .RespondWith(Response.Create().WithStatusCode(200).WithBody("DELETE Response"));

            string baseAddress = "http://localhost:" + mockServer.Ports.First();

            client = new PrizmDocRestClient(baseAddress)
            {
                DefaultRequestHeaders =
                {
                    { "Some-Header", "An example value" },
                    { "Some-Other-Header", "Another example value" },
                }
            };

            AffinitySession session = client.CreateAffinitySession();

            using (HttpResponseMessage response = await session.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/wat/123")))
            {
                Assert.IsTrue(response.RequestMessage.Headers.Contains("Some-Header"));
                Assert.AreEqual("An example value", response.RequestMessage.Headers.GetValues("Some-Header").SingleOrDefault());
                Assert.IsTrue(response.RequestMessage.Headers.Contains("Some-Other-Header"));
                Assert.AreEqual("Another example value", response.RequestMessage.Headers.GetValues("Some-Other-Header").SingleOrDefault());
            }

            using (HttpResponseMessage response = await session.GetAsync("/wat/123"))
            {
                Assert.IsTrue(response.RequestMessage.Headers.Contains("Some-Header"));
                Assert.AreEqual("An example value", response.RequestMessage.Headers.GetValues("Some-Header").SingleOrDefault());
                Assert.IsTrue(response.RequestMessage.Headers.Contains("Some-Other-Header"));
                Assert.AreEqual("Another example value", response.RequestMessage.Headers.GetValues("Some-Other-Header").SingleOrDefault());
            }

            using (HttpResponseMessage response = await session.PostAsync("/wat", new StringContent("body")))
            {
                Assert.IsTrue(response.RequestMessage.Headers.Contains("Some-Header"));
                Assert.AreEqual("An example value", response.RequestMessage.Headers.GetValues("Some-Header").SingleOrDefault());
                Assert.IsTrue(response.RequestMessage.Headers.Contains("Some-Other-Header"));
                Assert.AreEqual("Another example value", response.RequestMessage.Headers.GetValues("Some-Other-Header").SingleOrDefault());
            }

            using (HttpResponseMessage response = await session.PutAsync("/wat/123", new StringContent("body")))
            {
                Assert.IsTrue(response.RequestMessage.Headers.Contains("Some-Header"));
                Assert.AreEqual("An example value", response.RequestMessage.Headers.GetValues("Some-Header").SingleOrDefault());
                Assert.IsTrue(response.RequestMessage.Headers.Contains("Some-Other-Header"));
                Assert.AreEqual("Another example value", response.RequestMessage.Headers.GetValues("Some-Other-Header").SingleOrDefault());
            }

            using (HttpResponseMessage response = await session.DeleteAsync("/wat/123"))
            {
                Assert.IsTrue(response.RequestMessage.Headers.Contains("Some-Header"));
                Assert.AreEqual("An example value", response.RequestMessage.Headers.GetValues("Some-Header").SingleOrDefault());
                Assert.IsTrue(response.RequestMessage.Headers.Contains("Some-Other-Header"));
                Assert.AreEqual("Another example value", response.RequestMessage.Headers.GetValues("Some-Other-Header").SingleOrDefault());
            }
        }
    }
}
