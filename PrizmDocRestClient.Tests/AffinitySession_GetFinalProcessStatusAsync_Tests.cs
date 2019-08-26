using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using WireMock;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Util;

namespace Accusoft.PrizmDoc.Net.Http.Tests
{
    [TestClass]
    public class AffinitySession_GetFinalProcessStatusAsync_Tests
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
        [DataRow("complete")]
        [DataRow("error")]
        [DataRow("wat")]
        public async Task GetFinalProcessStatusAsync_returns_when_the_state_becomes_anything_other_than_processing(string finalState)
        {
            int responsesSent = 0;

            mockServer
                .Given(Request.Create().WithPath("/wat/123").UsingGet())
                .RespondWith(Response.Create()
                    .WithCallback(req =>
                    {
                        var headers = new Dictionary<string, WireMockList<string>>();
                        headers.Add("Content-Type", new WireMockList<string>("application/json"));

                        var body = "{ \"processId\": \"123\", \"state\": \"" + (responsesSent < 1 ? "processing" : finalState) + "\" }";
                        responsesSent++;

                        return new ResponseMessage
                        {
                            StatusCode = 200,
                            Headers = headers,
                            BodyData = new BodyData
                            {
                                DetectedBodyType = BodyType.String,
                                BodyAsString = body
                            }
                        };
                    })
                );

            var session = client.CreateAffinitySession();


            using (var response = await session.GetFinalProcessStatusAsync("/wat/123"))
            {
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var obj = JObject.Parse(json);
                var stateParsedFromTheReturnValueOfTheFunctionUnderTest = (string)obj["state"];

                Assert.AreEqual(finalState, stateParsedFromTheReturnValueOfTheFunctionUnderTest);
            }
        }

        [TestMethod]
        [TestCategory("Slow")]
        public async Task GetFinalProcessStatusAsync_uses_an_initial_polling_delay_of_500ms_and_then_doubles_the_delay_between_each_poll_until_reaching_a_max_delay_of_8000ms()
        {
            int responsesSent = 0;

            mockServer
                .Given(Request.Create().WithPath("/wat/123").UsingGet())
                .RespondWith(Response.Create()
                    .WithCallback(req =>
                    {
                        var headers = new Dictionary<string, WireMockList<string>>();
                        headers.Add("Content-Type", new WireMockList<string>("application/json"));

                        var body = "{ \"processId\": \"123\", \"state\": \"" + (responsesSent < 7 ? "processing" : "complete") + "\" }";
                        responsesSent++;

                        return new ResponseMessage
                        {
                            StatusCode = 200,
                            Headers = headers,
                            BodyData = new BodyData
                            {
                                DetectedBodyType = BodyType.String,
                                BodyAsString = body
                            }
                        };
                    })
                );

            var session = client.CreateAffinitySession();

            using (var response = await session.GetFinalProcessStatusAsync("/wat/123"))
            {
                response.EnsureSuccessStatusCode();
            }

            var requests = mockServer.LogEntries.Select(x => x.RequestMessage).ToList();
            var delaysBetweenRequests = new List<TimeSpan>();
            for (var i = 1 ; i < requests.Count; i++)
            {
                delaysBetweenRequests.Add(requests[i].DateTime - requests[i - 1].DateTime);
            }

            const double ALLOWED_DELTA = 500.0;

            Assert.AreEqual(500.0, delaysBetweenRequests[0].TotalMilliseconds, ALLOWED_DELTA);
            Assert.AreEqual(1000.0, delaysBetweenRequests[1].TotalMilliseconds, ALLOWED_DELTA);
            Assert.AreEqual(2000.0, delaysBetweenRequests[2].TotalMilliseconds, ALLOWED_DELTA);
            Assert.AreEqual(4000.0, delaysBetweenRequests[3].TotalMilliseconds, ALLOWED_DELTA);
            Assert.AreEqual(8000.0, delaysBetweenRequests[4].TotalMilliseconds, ALLOWED_DELTA);
            Assert.AreEqual(8000.0, delaysBetweenRequests[5].TotalMilliseconds, ALLOWED_DELTA);
            Assert.AreEqual(8000.0, delaysBetweenRequests[6].TotalMilliseconds, ALLOWED_DELTA);
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
