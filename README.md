# PrizmDoc .NET REST Client

HTTP client designed to simplify making REST API calls to PrizmDoc Server.
Specifically:

1. Automatically handles affinity concerns
2. Provides a way to easily poll for process completion

_**NOTE:** This is a low-level library intended for people who want to make REST
API calls themselves. If you just want to use PrizmDoc Server functionality from
.NET, you probably want the higher-level
[Accusoft.PrizmDocServerSDK](https://www.nuget.org/packages/Accusoft.PrizmDocServerSDK/)
package._

## Installation

Add the [Accusoft.PrizmDocRestClient NuGet package](https://www.nuget.org/packages/Accusoft.PrizmDocRestClient/) to your project.

This will add a new `Accusoft.PrizmDoc.Net.Http` namespace containing the `PrizmDocRestClient` class.

## Example Usage

Here's an example demonstrating converting a DOCX to PDF:

```csharp
using Accusoft.PrizmDoc.Net.Http;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace MyApplication
{
    public static class Program
    {
        public static void Main()
        {
            MainAsync().Wait();
        }

        public static async Task MainAsync()
        {
            // Construct an instance of the PrizmDocRestClient.
            var client = new PrizmDocRestClient("https://api.accusoft.com")
            {
                DefaultRequestHeaders = {
                    { "Acs-Api-Key", "YOUR_API_KEY" }
                }
            };

            // Create an affinity session for our processing work.
            //
            // You should use an affinity session anytime you have a group
            // of HTTP requests that go together as part of a processing
            // chain. The session ensures that all HTTP requests will
            // automatically use the same affinity (be routed to the same
            // PrizmDoc Server machine in the cluster).
            AffinitySession session = client.CreateAffinitySession();

            string json;

            // Create a new work file for the input document
            using (FileStream inputFileStream = File.OpenRead("input.docx"))
            using (HttpResponseMessage response = await session.PostAsync("/PCCIS/V1/WorkFile", new StreamContent(inputFileStream)))
            {
                response.EnsureSuccessStatusCode();
                json = await response.Content.ReadAsStringAsync();
            }

            JObject inputWorkFile = JObject.Parse(json);
            string inputFileId = (string)inputWorkFile["fileId"];

            // Start a conversion process using the input work file
            string postContentConvertersJson =
@"{
    ""input"": {
        ""sources"": [
            {
                ""fileId"": """ + inputFileId + @"""
            }
        ],
        ""dest"": {
            ""format"": ""pdf""
        }
    }
}";

            using (HttpResponseMessage response = await session.PostAsync("/v2/contentConverters", new StringContent(postContentConvertersJson)))
            {
                response.EnsureSuccessStatusCode();
                json = await response.Content.ReadAsStringAsync();
            }

            JObject process = JObject.Parse(json);
            string processId = (string)process["processId"];

            // Wait for the process to finish
            using (HttpResponseMessage response = await session.GetFinalProcessStatusAsync($"/v2/contentConverters/{processId}"))
            {
                response.EnsureSuccessStatusCode();
                json = await response.Content.ReadAsStringAsync();
            }

            process = JObject.Parse(json);

            // Did the process error?
            if ((string)process["state"] != "complete")
            {
                throw new Exception("The process failed to complete:\n" + json);
            }

            // Download the output work file and save it to disk.
            string workFileId = (string)process["output"]["results"][0]["fileId"];
            using (HttpResponseMessage response = await session.GetAsync($"/PCCIS/V1/WorkFile/{workFileId}"))
            {
                response.EnsureSuccessStatusCode();

                using (Stream responseBodyStream = await response.Content.ReadAsStreamAsync())
                using (FileStream outputFileStream = File.OpenWrite("output.pdf"))
                {
                    await responseBodyStream.CopyToAsync(outputFileStream);
                }
            }
        }
    }
}
```
