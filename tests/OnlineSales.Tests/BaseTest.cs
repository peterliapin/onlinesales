﻿// <copyright file="BaseTest.cs" company="WavePoint Co. Ltd.">
// Licensed under the MIT license. See LICENSE file in the samples root for full license information.
// </copyright>

using System.Globalization;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using AutoMapper;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Mvc.Testing;
using OnlineSales.Controllers;
using OnlineSales.Helpers;

namespace OnlineSales.Tests;

public class BaseTest : IDisposable
{
    protected static readonly TestApplication App = new TestApplication();

    protected readonly HttpClient client;
    protected readonly IMapper mapper;

    static BaseTest()
    {
        AssertionOptions.AssertEquivalencyUsing(e => e.Using(new RelaxedEnumEquivalencyStep()));
    }

    protected BaseTest()
    {
        client = App.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });

        mapper = App.GetMapper();
        App.CleanDatabase();
    }

    public virtual void Dispose()
    {
        client.Dispose();
    }

    protected static StringContent PayloadToStringContent(object payload)
    {
        var payloadString = JsonHelper.Serialize(payload);

        return new StringContent(payloadString, Encoding.UTF8, "application/json");
    }

    protected virtual AuthenticationHeaderValue? GetAuthenticationHeaderValue()
    {
        return null;
    }

    protected async Task SyncElasticSearch()
    {
        var taskExecuteResponce = await GetRequest("/api/tasks/execute/SyncEsTask");
        taskExecuteResponce.Should().NotBeNull();
        taskExecuteResponce.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await taskExecuteResponce.Content.ReadAsStringAsync();
        var task = JsonHelper.Deserialize<TaskExecutionDto>(content);
        task!.Completed.Should().BeTrue();
    }

    protected Task<HttpResponseMessage> GetRequest(string url)
    {
        return Request(HttpMethod.Get, url, null);
    }

    protected virtual Task<HttpResponseMessage> Request(HttpMethod method, string url, object? payload)
    {
        var request = new HttpRequestMessage(method, url);

        if (payload != null)
        {
            request.Content = PayloadToStringContent(payload);
        }

        request.Headers.Authorization = GetAuthenticationHeaderValue();

        return client.SendAsync(request);
    }

    protected async Task<HttpResponseMessage> GetTest(string url, HttpStatusCode expectedCode = HttpStatusCode.OK)
    {
        var response = await GetRequest(url);

        response.StatusCode.Should().Be(expectedCode);

        return response;
    }

    protected async Task<T?> GetTest<T>(string url, HttpStatusCode expectedCode = HttpStatusCode.OK)
        where T : class
    {
        var response = await GetTest(url, expectedCode);

        var content = await response.Content.ReadAsStringAsync();

        if (expectedCode == HttpStatusCode.OK)
        {
            CheckForRedundantProperties<T>(content);

            return JsonHelper.Deserialize<T>(content);
        }
        else
        {
            return null;
        }
    }

    protected async Task<List<TI>?> GetTestCSV<TI>(string url, HttpStatusCode expectedCode = HttpStatusCode.OK)
    where TI : class
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = GetAuthenticationHeaderValue();

        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/csv"));

        var response = await client.SendAsync(request);

        var content = await response.Content.ReadAsStringAsync();

        if (expectedCode == HttpStatusCode.OK)
        {
            TextReader tr = new StringReader(content);
            var reader = new CsvReader(tr, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = (args) => char.ToLower(args.Header[0]) + args.Header.Substring(1),
                MissingFieldFound = null,
                HeaderValidated = null,
            });

            var res = reader.GetRecords<TI>();
            return res.ToList();
        }
        else
        {
            return null;
        }
    }

    protected async Task<T?> PostTest<T>(string url, object payload, HttpStatusCode expectedCode = HttpStatusCode.Created)
        where T : class
    {
        var response = await Request(HttpMethod.Post, url, payload);

        response.StatusCode.Should().Be(expectedCode);

        var content = await response.Content.ReadAsStringAsync();

        if (expectedCode == HttpStatusCode.OK)
        {
            CheckForRedundantProperties<T>(content);

            return JsonHelper.Deserialize<T>(content);
        }
        else
        {
            return null;
        }
    }

    protected async Task<string> PostTest(string url, object payload, HttpStatusCode expectedCode = HttpStatusCode.Created)
    {
        var response = await Request(HttpMethod.Post, url, payload);

        response.StatusCode.Should().Be(expectedCode);

        var location = string.Empty;

        if (expectedCode == HttpStatusCode.Created)
        {
            location = response.Headers?.Location?.LocalPath ?? string.Empty;
            location.Should().StartWith(url);

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonHelper.Deserialize<BaseEntityWithId>(content);
            result.Should().NotBeNull();
            result!.Id.Should().BePositive();
        }

        return location;
    }

    protected async virtual Task<ImportResult> PostImportTest(string url, string importFileName, HttpStatusCode expectedCode = HttpStatusCode.OK)
    {
        var response = await ImportRequest(HttpMethod.Post, $"{url}/import", importFileName);

        response.StatusCode.Should().Be(expectedCode);

        var content = await response.Content.ReadAsStringAsync();

        return JsonHelper.Deserialize<ImportResult>(content)!;
    }

    protected async Task<HttpResponseMessage> Patch(string url, object payload)
    {
        var response = await Request(HttpMethod.Patch, url, payload);
        return response;
    }

    protected async Task<HttpResponseMessage> PatchTest(string url, object payload, HttpStatusCode expectedCode = HttpStatusCode.OK)
    {
        var response = await Patch(url, payload);

        response.StatusCode.Should().Be(expectedCode);

        return response;
    }

    protected async Task<HttpResponseMessage> DeleteTest(string url, HttpStatusCode expectedCode = HttpStatusCode.NoContent)
    {
        var response = await Request(HttpMethod.Delete, url, null);

        response.StatusCode.Should().Be(expectedCode);

        return response;
    }

    protected string GetResouceFileTextContent(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();

        var resourcePath = assembly.GetManifestResourceNames()
                .Single(str => str.EndsWith(fileName));

        if (resourcePath is null)
        {
            return string.Empty;
        }

        var stream = assembly!.GetManifestResourceStream(resourcePath);

        if (stream != null)
        {
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        return string.Empty;
    }

    private void CheckForRedundantProperties<T>(string content)
    {
        if (typeof(BaseEntity).IsAssignableFrom(typeof(T)))
        {
            var isCollection = content.StartsWith('[');

            if (isCollection)
            {
                var resultCollection = JsonHelper.Deserialize<List<BaseEntity>>(content)!;
                resultCollection.Should().NotBeNull();
                if (resultCollection.Count > 0)
                {
                    resultCollection[0].CreatedByIp.Should().BeNull();
                    resultCollection[0].UpdatedByIp.Should().BeNull();
                    resultCollection[0].CreatedByUserAgent.Should().BeNull();
                    resultCollection[0].UpdatedByUserAgent.Should().BeNull();
                }
            }
            else
            {
                var result = JsonHelper.Deserialize<BaseEntity>(content)!;
                result.Should().NotBeNull();
                result.CreatedByIp.Should().BeNull();
                result.UpdatedByIp.Should().BeNull();
                result.CreatedByUserAgent.Should().BeNull();
                result.UpdatedByUserAgent.Should().BeNull();
            }
        }
    }

    private Task<HttpResponseMessage> ImportRequest(HttpMethod method, string url, string importFileName)
    {
        StringContent content;

        var request = new HttpRequestMessage(method, url);

        var textContent = GetResouceFileTextContent(importFileName);

        if (Path.GetExtension(importFileName)!.ToLower() == ".csv")
        {
            content = new StringContent(textContent, Encoding.UTF8, "text/csv");
        }
        else
        {
            content = new StringContent(textContent, Encoding.UTF8, "application/json");
        }

        request.Content = content;
        request.Headers.Authorization = GetAuthenticationHeaderValue();

        return client.SendAsync(request);
    }
}