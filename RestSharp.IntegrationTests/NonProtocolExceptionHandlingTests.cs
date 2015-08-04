﻿using System.Net;
using System.Threading;
using NUnit.Framework;
using RestSharp.IntegrationTests.Helpers;

namespace RestSharp.IntegrationTests
{
    [TestFixture]
    public class NonProtocolExceptionHandlingTests
    {
        /// <summary>
        /// Success of this test is based largely on the behavior of your current DNS.
        /// For example, if you're using OpenDNS this will test will fail; ResponseStatus will be Completed.
        /// </summary>
        [Test]
        public void Handles_Non_Existent_Domain()
        {
            var client = new RestClient("http://nonexistantdomainimguessing.org");
            var request = new RestRequest("foo");
            var response = client.Execute(request);

            Assert.AreEqual(ResponseStatus.Error, response.ResponseStatus);
        }

        public class StupidClass
        {
            public string Property { get; set; }
        }

        [Test]
        public void Task_Handles_Non_Existent_Domain()
        {
            var client = new RestClient("http://192.168.1.200:8001");
            var request = new RestRequest("/")
            {
                RequestFormat = DataFormat.Json,
                Method = Method.GET
            };

            var task = client.ExecuteTaskAsync<StupidClass>(request);
            task.Wait();

            var response = task.Result;

            Assert.IsInstanceOf<WebException>(response.ErrorException);
            Assert.AreEqual("Unable to connect to the remote server", response.ErrorException.Message);
            Assert.AreEqual(ResponseStatus.Error, response.ResponseStatus);
        }

        /// <summary>
        /// Tests that RestSharp properly handles a non-protocol error.
        /// Simulates a server timeout, then verifies that the ErrorException
        /// property is correctly populated.
        /// </summary>
        [Test]
        public void Handles_Server_Timeout_Error()
        {
            const string baseUrl = "http://localhost:8888/";

            using (SimpleServer.Create(baseUrl, TimeoutHandler))
            {
                var client = new RestClient(baseUrl);
                var request = new RestRequest("404") { Timeout = 500 };
                var response = client.Execute(request);

                Assert.NotNull(response.ErrorException);
                Assert.IsInstanceOf<WebException>(response.ErrorException);
                Assert.IsTrue(response.ErrorException.Message.Contains("The operation has timed out"));
            }
        }

        [Test]
        public void Handles_Server_Timeout_Error_Async()
        {
            const string baseUrl = "http://localhost:8888/";
            var resetEvent = new ManualResetEvent(false);

            using (SimpleServer.Create(baseUrl, TimeoutHandler))
            {
                var client = new RestClient(baseUrl);
                var request = new RestRequest("404") { Timeout = 500 };
                IRestResponse response = null;

                client.ExecuteAsync(request, responseCb =>
                                             {
                                                 response = responseCb;
                                                 resetEvent.Set();
                                             });

                resetEvent.WaitOne();

                Assert.NotNull(response);
                Assert.AreEqual(response.ResponseStatus, ResponseStatus.TimedOut);
                Assert.NotNull(response.ErrorException);
                Assert.IsInstanceOf<WebException>(response.ErrorException);
                Assert.AreEqual(response.ErrorException.Message, "The request timed-out.");
            }
        }

        [Test]
        public void Handles_Server_Timeout_Error_AsyncTask()
        {
            const string baseUrl = "http://localhost:8888/";

            using (SimpleServer.Create(baseUrl, TimeoutHandler))
            {
                var client = new RestClient(baseUrl);
                var request = new RestRequest("404") { Timeout = 500 };
                var task =  client.ExecuteTaskAsync(request);

                task.Wait();

                IRestResponse response = task.Result;

                Assert.NotNull(response);
                Assert.AreEqual(response.ResponseStatus, ResponseStatus.TimedOut);

                Assert.NotNull(response.ErrorException);
                Assert.IsInstanceOf<WebException>(response.ErrorException);
                Assert.AreEqual(response.ErrorException.Message, "The request timed-out.");
            }
        }

        /// <summary>
        /// Tests that RestSharp properly handles a non-protocol error.   
        /// Simulates a server timeout, then verifies that the ErrorException
        /// property is correctly populated.
        /// </summary>
        [Test]
        public void Handles_Server_Timeout_Error_With_Deserializer()
        {
            const string baseUrl = "http://localhost:8888/";

            using (SimpleServer.Create(baseUrl, TimeoutHandler))
            {
                var client = new RestClient(baseUrl);
                var request = new RestRequest("404") { Timeout = 500 };
                var response = client.Execute<Response>(request);

                Assert.Null(response.Data);
                Assert.NotNull(response.ErrorException);
                Assert.IsInstanceOf<WebException>(response.ErrorException);
                Assert.IsTrue(response.ErrorException.Message.Contains("The operation has timed out"));
            }
        }

        /// <summary>
        /// Simulates a long server process that should result in a client timeout
        /// </summary>
        /// <param name="context"></param>
        public static void TimeoutHandler(HttpListenerContext context)
        {
            Thread.Sleep(101000);
        }
    }
}
