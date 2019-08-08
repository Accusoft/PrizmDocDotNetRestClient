using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Accusoft.PrizmDoc.Net.Http.Tests
{
    [TestClass]
    public class AffinitySession_HTTP_Verb_Convenience_Methods_tests
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

        [TestMethod]
        public async Task GetAsync()
        {
            mockServer
                .Given(Request.Create().WithPath("/wat/123").UsingGet())
                .RespondWith(Response.Create().WithStatusCode(200).WithBody("GET Response"));

            var session = client.CreateAffinitySession();

            HttpResponseMessage response;

            response = await session.GetAsync("/wat/123");
            response.EnsureSuccessStatusCode();
            Assert.AreEqual("GET Response", await response.Content.ReadAsStringAsync());
        }

        [TestMethod]
        public async Task PostAsync()
        {
            mockServer
                .Given(Request.Create().WithPath("/wat").UsingPost())
                .RespondWith(Response.Create().WithStatusCode(200).WithBody(req => $"You POSTed: {req.Body}"));

            var session = client.CreateAffinitySession();

            using (var response = await session.PostAsync("/wat", new StringContent("Hello world!")))
            {
                response.EnsureSuccessStatusCode();
                Assert.AreEqual("You POSTed: Hello world!", await response.Content.ReadAsStringAsync());
            }
        }

        [TestMethod]
        public async Task PutAsync()
        {
            mockServer
                .Given(Request.Create().WithPath("/wat/123").UsingPut())
                .RespondWith(Response.Create().WithStatusCode(200).WithBody(req => $"You PUT: {req.Body}"));

            var session = client.CreateAffinitySession();

            using (var response = await session.PutAsync("/wat/123", new StringContent("Hi there, friend.")))
            {
                response.EnsureSuccessStatusCode();
                Assert.AreEqual("You PUT: Hi there, friend.", await response.Content.ReadAsStringAsync());
            }
        }

        [TestMethod]
        public async Task DeleteAsync()
        {
            mockServer
                .Given(Request.Create().WithPath("/wat/123").UsingDelete())
                .RespondWith(Response.Create().WithStatusCode(200).WithBody(req => $"Resource deleted: {req.Path}"));

            var session = client.CreateAffinitySession();

            using (var response = await session.DeleteAsync("/wat/123"))
            {
                response.EnsureSuccessStatusCode();
                Assert.AreEqual("Resource deleted: /wat/123", await response.Content.ReadAsStringAsync());
            }
        }
    }
}
