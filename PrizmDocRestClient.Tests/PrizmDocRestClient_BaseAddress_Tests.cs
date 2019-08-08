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
    public class PrizmDocRestClient_BaseAddress_Tests
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
        public async Task BaseAddress_without_trailing_slash_is_applied_correctly_to_each_request()
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

            var baseAddress = "http://localhost:" + mockServer.Ports.First();

            client = new PrizmDocRestClient(baseAddress);

            var session = client.CreateAffinitySession();

            using (var response = await session.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/wat/123")))
            {
                Assert.AreEqual(baseAddress + "/wat/123", response.RequestMessage.RequestUri.ToString());
            }

            using (var response = await session.GetAsync("/wat/123"))
            {
                Assert.AreEqual(baseAddress + "/wat/123", response.RequestMessage.RequestUri.ToString());
            }

            using (var response = await session.PostAsync("/wat", new StringContent("body")))
            {
                Assert.AreEqual(baseAddress + "/wat", response.RequestMessage.RequestUri.ToString());
            }

            using (var response = await session.PutAsync("/wat/123", new StringContent("body")))
            {
                Assert.AreEqual(baseAddress + "/wat/123", response.RequestMessage.RequestUri.ToString());
            }

            using (var response = await session.DeleteAsync("/wat/123"))
            {
                Assert.AreEqual(baseAddress + "/wat/123", response.RequestMessage.RequestUri.ToString());
            }
        }

        [TestMethod]
        public async Task BaseAddress_with_trailing_slash_is_applied_correctly_to_each_request()
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

            var baseAddressWithoutTrailingSlash = "http://localhost:" + mockServer.Ports.First();
            var baseAddressWithTrailingSlash = baseAddressWithoutTrailingSlash + "/";

            client = new PrizmDocRestClient(baseAddressWithTrailingSlash);

            var session = client.CreateAffinitySession();

            using (var response = await session.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/wat/123")))
            {
                Assert.AreEqual(baseAddressWithoutTrailingSlash + "/wat/123", response.RequestMessage.RequestUri.ToString());
            }

            using (var response = await session.GetAsync("/wat/123"))
            {
                Assert.AreEqual(baseAddressWithoutTrailingSlash + "/wat/123", response.RequestMessage.RequestUri.ToString());
            }

            using (var response = await session.PostAsync("/wat", new StringContent("body")))
            {
                Assert.AreEqual(baseAddressWithoutTrailingSlash + "/wat", response.RequestMessage.RequestUri.ToString());
            }

            using (var response = await session.PutAsync("/wat/123", new StringContent("body")))
            {
                Assert.AreEqual(baseAddressWithoutTrailingSlash + "/wat/123", response.RequestMessage.RequestUri.ToString());
            }

            using (var response = await session.DeleteAsync("/wat/123"))
            {
                Assert.AreEqual(baseAddressWithoutTrailingSlash + "/wat/123", response.RequestMessage.RequestUri.ToString());
            }
        }
    }
}
