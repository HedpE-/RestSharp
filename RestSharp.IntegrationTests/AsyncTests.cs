﻿using System;
using System.Linq;
using System.Net;
using System.Threading;
using NUnit.Framework;
using RestSharp.IntegrationTests.Helpers;

namespace RestSharp.IntegrationTests
{
    [TestFixture]
    public class AsyncTests
    {
        [Test]
        public void Can_Perform_GET_Async()
        {
            Uri baseUrl = new Uri("http://localhost:8888/");
            const string val = "Basic async test";

            var resetEvent = new ManualResetEvent(false);

            using (SimpleServer.Create(baseUrl.AbsoluteUri, Handlers.EchoValue(val)))
            {
                var client = new RestClient(baseUrl);
                var request = new RestRequest("");

                client.ExecuteAsync(request, (response, asyncHandle) =>
                                             {
                                                 Assert.NotNull(response.Content);
                                                 Assert.AreEqual(val, response.Content);
                                                 resetEvent.Set();
                                             });

                resetEvent.WaitOne();
            }
        }

        [Test]
        public void Can_Perform_GET_Async_Without_Async_Handle()
        {
            Uri baseUrl = new Uri("http://localhost:8888/");
            const string val = "Basic async test";

            var resetEvent = new ManualResetEvent(false);

            using (SimpleServer.Create(baseUrl.AbsoluteUri, Handlers.EchoValue(val)))
            {
                var client = new RestClient(baseUrl);
                var request = new RestRequest("");

                client.ExecuteAsync(request, response =>
                                             {
                                                 Assert.NotNull(response.Content);
                                                 Assert.AreEqual(val, response.Content);
                                                 resetEvent.Set();
                                             });

                resetEvent.WaitOne();
            }
        }

        [Test]
        public void Can_Perform_GET_TaskAsync()
        {
            const string baseUrl = "http://localhost:8888/";
            const string val = "Basic async task test";

            using (SimpleServer.Create(baseUrl, Handlers.EchoValue(val)))
            {
                var client = new RestClient(baseUrl);
                var request = new RestRequest("");
                var task = client.ExecuteTaskAsync(request);

                task.Wait();

                Assert.NotNull(task.Result.Content);
                Assert.AreEqual(val, task.Result.Content);
            }
        }

        [Test]
        public void Can_Handle_Exception_Thrown_By_OnBeforeDeserialization_Handler()
        {
            const string baseUrl = "http://localhost:8888/";
            const string ExceptionMessage = "Thrown from OnBeforeDeserialization";

            using (SimpleServer.Create(baseUrl, Handlers.Generic<ResponseHandler>()))
            {
                var client = new RestClient(baseUrl);
                var request = new RestRequest("success");

                request.OnBeforeDeserialization += r => { throw new Exception(ExceptionMessage); };

                var task = client.ExecuteTaskAsync<Response>(request);

                task.Wait();

                var response = task.Result;

                Assert.AreEqual(ExceptionMessage, response.ErrorMessage);
                Assert.AreEqual(ResponseStatus.Error, response.ResponseStatus);
            }
        }

        [Test]
        public void Can_Perform_ExecuteGetTaskAsync_With_Response_Type()
        {
            const string baseUrl = "http://localhost:8888/";

            using (SimpleServer.Create(baseUrl, Handlers.Generic<ResponseHandler>()))
            {
                var client = new RestClient(baseUrl);
                var request = new RestRequest("success");
                var task = client.ExecuteTaskAsync<Response>(request);

                task.Wait();

                Assert.AreEqual("Works!", task.Result.Data.Message);
            }
        }

        [Test]
        public void Can_Perform_GetTaskAsync_With_Response_Type()
        {
            const string baseUrl = "http://localhost:8888/";

            using (SimpleServer.Create(baseUrl, Handlers.Generic<ResponseHandler>()))
            {
                var client = new RestClient(baseUrl);
                var request = new RestRequest("success");
                var task = client.GetTaskAsync<Response>(request);

                task.Wait();

                Assert.AreEqual("Works!", task.Result.Message);
            }
        }

        [Test]
        public void Can_Cancel_GET_TaskAsync()
        {
            const string baseUrl = "http://localhost:8888/";
            const string val = "Basic async task test";

            using (SimpleServer.Create(baseUrl, Handlers.EchoValue(val)))
            {
                var client = new RestClient(baseUrl);
                var request = new RestRequest("timeout");
                var cancellationTokenSource = new CancellationTokenSource();
                var task = client.ExecuteTaskAsync(request, cancellationTokenSource.Token);

                cancellationTokenSource.Cancel();

                Assert.True(task.IsCanceled);
            }
        }

        [Test]
        public void Can_Cancel_GET_TaskAsync_With_Response_Type()
        {
            const string baseUrl = "http://localhost:8888/";
            const string val = "Basic async task test";

            using (SimpleServer.Create(baseUrl, Handlers.EchoValue(val)))
            {
                var client = new RestClient(baseUrl);
                var request = new RestRequest("timeout");
                var cancellationTokenSource = new CancellationTokenSource();
                var task = client.ExecuteTaskAsync<Response>(request, cancellationTokenSource.Token);

                cancellationTokenSource.Cancel();

                Assert.True(task.IsCanceled);
            }
        }

        [Test]
        public void Handles_GET_Request_Errors_TaskAsync()
        {
            const string baseUrl = "http://localhost:8888/";

            using (SimpleServer.Create(baseUrl, UrlToStatusCodeHandler))
            {
                var client = new RestClient(baseUrl);
                var request = new RestRequest("404");
                var task = client.ExecuteTaskAsync(request);

                task.Wait();

                Assert.AreEqual(HttpStatusCode.NotFound, task.Result.StatusCode);
            }
        }

        [Test]
        public void Handles_GET_Request_Errors_TaskAsync_With_Response_Type()
        {
            const string baseUrl = "http://localhost:8888/";

            using (SimpleServer.Create(baseUrl, UrlToStatusCodeHandler))
            {
                var client = new RestClient(baseUrl);
                var request = new RestRequest("404");
                var task = client.ExecuteTaskAsync<Response>(request);

                task.Wait();

                Assert.Null(task.Result.Data);
            }
        }

        [Test]
        public void Can_Timeout_GET_TaskAsync()
        {
            const string baseUrl = "http://localhost:8888/";

            using (SimpleServer.Create(baseUrl, Handlers.Generic<ResponseHandler>()))
            {
                var client = new RestClient(baseUrl);
                var request = new RestRequest("timeout", Method.GET).AddBody("Body_Content");

                //Half the value of ResponseHandler.Timeout
                request.Timeout = 500;

                var task = client.ExecuteTaskAsync(request);

                task.Wait();

                var response = task.Result;

                Assert.AreEqual(ResponseStatus.TimedOut, response.ResponseStatus);
            }
        }

        [Test]
        public void Can_Timeout_PUT_TaskAsync()
        {
            const string baseUrl = "http://localhost:8888/";

            using (SimpleServer.Create(baseUrl, Handlers.Generic<ResponseHandler>()))
            {
                var client = new RestClient(baseUrl);
                var request = new RestRequest("timeout", Method.PUT).AddBody("Body_Content");

                //Half the value of ResponseHandler.Timeout
                request.Timeout = 500;

                var task = client.ExecuteTaskAsync(request);

                task.Wait();

                var response = task.Result;

                Assert.AreEqual(ResponseStatus.TimedOut, response.ResponseStatus);
            }
        }

        static void UrlToStatusCodeHandler(HttpListenerContext obj)
        {
            obj.Response.StatusCode = int.Parse(obj.Request.Url.Segments.Last());
        }

        public class ResponseHandler
        {
            void error(HttpListenerContext context)
            {
                context.Response.StatusCode = 400;
                context.Response.Headers.Add("Content-Type", "application/xml");
                context.Response.OutputStream.WriteStringUtf8(
@"<?xml version=""1.0"" encoding=""utf-8"" ?>
<Response>
    <Error>
        <Message>Not found!</Message>
    </Error>
</Response>");
            }

            void success(HttpListenerContext context)
            {
                context.Response.OutputStream.WriteStringUtf8(
@"<?xml version=""1.0"" encoding=""utf-8"" ?>
<Response>
    <Success>
        <Message>Works!</Message>
    </Success>
</Response>");
            }

            void timeout(HttpListenerContext context)
            {
                Thread.Sleep(1000);
            }
        }

        public class Response
        {
            public string Message { get; set; }
        }
    }
}
