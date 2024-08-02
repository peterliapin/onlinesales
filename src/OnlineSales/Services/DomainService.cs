﻿// <copyright file="DomainService.cs" company="WavePoint Co. Ltd.">
// Licensed under the MIT license. See LICENSE file in the samples root for full license information.
// </copyright>

using System.Net.Mail;
using System.Reflection;
using DnsClient;
using DnsClient.Protocol;
using HtmlAgilityPack;
using OnlineSales.Data;
using OnlineSales.Entities;
using OnlineSales.Interfaces;

namespace OnlineSales.Services;

public class DomainService : IDomainService
{
    private static HashSet<string> freeDomains = InitDomainsList("free_domains.txt");

    private static HashSet<string> disposableDomains = InitDomainsList("disposable_domains.txt");

    private readonly LookupClient lookupClient;
    private readonly IMxVerifyService mxVerifyService;

    private PgDbContext pgDbContext;

    public DomainService(PgDbContext pgDbContext, IMxVerifyService mxVerifyService)
    {
        this.mxVerifyService = mxVerifyService;

        this.pgDbContext = pgDbContext;

        lookupClient = new LookupClient(new LookupClientOptions
        {
            UseCache = true,
            Timeout = new TimeSpan(0, 0, 60),
        });
    }

    public async Task Verify(Domain domain)
    {
        VerifyFreeAndDisposable(domain);

        if (domain.DnsCheck == null)
        {
            await VerifyDns(domain);
        }

        if (domain.DnsCheck is true)
        {
            if (domain.HttpCheck == null)
            {
                await VerifyHttp(domain);
            }

            if (domain.MxCheck == null)
            {
                await VerifyMX(domain);
            }
        }
        else
        {
            domain.HttpCheck = false;
            domain.MxCheck = false;
            domain.Url = null;
            domain.Title = null;
            domain.Description = null;
        }
    }

    public async Task SaveAsync(Domain domain)
    {
        VerifyFreeAndDisposable(domain);

        if (domain.Id > 0)
        {
            pgDbContext.Domains!.Update(domain);
        }
        else
        {
            await pgDbContext.Domains!.AddAsync(domain);
        }
    }

    public async Task SaveRangeAsync(List<Domain> domains)
    {
        domains.ForEach(d => VerifyFreeAndDisposable(d));

        var sortedDomains = domains.GroupBy(d => d.Id > 0);

        foreach (var group in sortedDomains)
        {
            if (group.Key)
            {
                pgDbContext.UpdateRange(group.ToList());
            }
            else
            {
                await pgDbContext.AddRangeAsync(group.ToList());
            }
        }
    }

    public string GetDomainNameByEmail(string email)
    {
        try
        {
            if (string.IsNullOrEmpty(email))
            {
                return string.Empty;
            }

            var address = new MailAddress(email);
            return address.Host;
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
            return string.Empty;
        }
    }

    public void SetDBContext(PgDbContext pgDbContext)
    {
        this.pgDbContext = pgDbContext;
    }

    private static HashSet<string> InitDomainsList(string filename)
    {
        var res = new HashSet<string>();

        var asm = Assembly.GetExecutingAssembly();
        var resourcePath = asm.GetManifestResourceNames().Single(str => str.EndsWith(filename));

        if (resourcePath == null)
        {
            throw new FileNotFoundException(filename);
        }

        using (var rsrcStream = asm.GetManifestResourceStream(resourcePath))
        {
            if (rsrcStream == null)
            {
                throw new FileNotFoundException(filename);
            }
            else
            {
                using (var sRdr = new StreamReader(rsrcStream))
                {
                    string? line = null;
                    while ((line = sRdr.ReadLine()) != null)
                    {
                        res.Add(line);
                    }
                }
            }
        }

        return res;
    }

    private void VerifyFreeAndDisposable(Domain domain)
    {
        if (domain.Free == null || domain.Disposable == null)
        {
            domain.Free = freeDomains.Contains(domain.Name);

            domain.Disposable = disposableDomains.Contains(domain.Name);
        }
    }

    private async Task VerifyHttp(Domain domain)
    {
        domain.HttpCheck = false;

        var urls = new string[]
        {
        "https://" + domain.Name,
        "https://www." + domain.Name,
        "http://" + domain.Name,
        "http://www." + domain.Name,
        };

        foreach (var url in urls)
        {
            var response = await GetRequest(url);

            if (response != null && response.RequestMessage != null && response.RequestMessage.RequestUri != null && response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                domain.HttpCheck = true;

                domain.Url = response.RequestMessage.RequestUri.ToString();
                var htmlDoc = new HtmlDocument();
                var contentStream = await response.Content.ReadAsStreamAsync();
                if (contentStream.Length == 0)
                {
                    continue;
                }

                htmlDoc.Load(contentStream);

                var title = GetTitle(htmlDoc);
                var description = GetDescription(htmlDoc);
                domain.Title = title != null ? HtmlEntity.DeEntitize(title) : null;
                domain.Description = description != null ? HtmlEntity.DeEntitize(description) : null;

                break;
            }
        }
    }

    private async Task VerifyMX(Domain domain)
    {
        domain.MxCheck = false;

        var mxRecords = await lookupClient.QueryAsync(domain.Name, QueryType.MX);

        var orderedMxRecordValues = from r in mxRecords.AllRecords
                                    where r is MxRecord
                                    orderby ((MxRecord)r).Preference ascending
                                    select ((MxRecord)r).Exchange.Value;

        foreach (var mxRecordValue in orderedMxRecordValues)
        {
            var mxVerify = await mxVerifyService.Verify(mxRecordValue);

            if (mxVerify)
            {
                domain.MxCheck = true;
                break;
            }
        }
    }

    private async Task VerifyDns(Domain domain)
    {
        domain.DnsRecords = null;
        domain.DnsCheck = false;

        var result = await lookupClient.QueryAsync(domain.Name, QueryType.ANY);

        var dnsRecords = GetDnsRecords(result, domain);

        if (dnsRecords.Count > 0)
        {
            domain.DnsCheck = true;
            domain.DnsRecords = dnsRecords;
        }
    }

    private List<DnsRecord> GetDnsRecords(IDnsQueryResponse dnsQueryResponse, Domain d)
    {
        var dnsRecords = new List<DnsRecord>();

        foreach (var dnsResponseRecord in dnsQueryResponse.AllRecords)
        {
            try
            {
                var dnsRecord = new DnsRecord
                {
                    DomainName = dnsResponseRecord.DomainName.Value,
                    RecordClass = dnsResponseRecord.RecordClass.ToString(),
                    RecordType = dnsResponseRecord.RecordType.ToString(),
                    TimeToLive = dnsResponseRecord.TimeToLive,
                };

                switch (dnsResponseRecord)
                {
                    case ARecord a:
                        if (dnsRecord.DomainName != d.Name + ".")
                        {
                            // we are only interested in an A record for the main domain
                            continue;
                        }

                        dnsRecord.Value = a.Address.ToString();
                        break;
                    case CNameRecord cname:
                        dnsRecord.Value = cname.CanonicalName.Value;
                        break;
                    case MxRecord mx:
                        dnsRecord.Value = mx.Exchange.Value;
                        break;
                    case TxtRecord txt:
                        dnsRecord.Value = string.Concat(txt.Text);
                        break;
                    case NsRecord ns:
                        dnsRecord.Value = ns.NSDName.Value;
                        break;
                    default:
                        continue;
                }

                dnsRecords.Add(dnsRecord);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error reading DNS record.");
            }
        }

        return dnsRecords;
    }

    private async Task<HttpResponseMessage?> GetRequest(string url)
    {
        var client = new HttpClient();

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            return await client.SendAsync(request);
        }
        catch
        {
            return null;
        }
    }

    private string? GetTitle(HtmlDocument htmlDoc)
    {
        var htmlNode = htmlDoc.DocumentNode.SelectSingleNode("//title");

        if (htmlNode != null && !string.IsNullOrEmpty(htmlNode.InnerText))
        {
            return htmlNode.InnerText;
        }

        var title = GetNodeContentByAttr(htmlDoc, "title");

        if (!string.IsNullOrEmpty(title))
        {
            return title;
        }

        htmlNode = htmlDoc.DocumentNode.SelectSingleNode("//h1");

        if (htmlNode != null && !string.IsNullOrEmpty(htmlNode.InnerText))
        {
            return htmlNode.InnerText;
        }

        return null;
    }

    private string? GetDescription(HtmlDocument htmlDoc)
    {
        return GetNodeContentByAttr(htmlDoc, "description");
    }

    private string? GetNodeContentByAttr(HtmlDocument htmlDoc, string value)
    {
        var result = GetNodeContentByAttr(htmlDoc, "name", value);
        if (result == null)
        {
            result = GetNodeContentByAttr(htmlDoc, "property", value);
        }

        return result;
    }

    private string? GetNodeContentByAttr(HtmlDocument htmlDoc, string attrName, string value)
    {
        string? GetNodeContent(HtmlDocument htmlDoc, string attrName, string value)
        {
            var htmlNode = htmlDoc.DocumentNode.SelectSingleNode($"//meta[@{attrName}='{value}']");
            if (htmlNode != null && htmlNode.Attributes.Contains("content"))
            {
                return htmlNode.GetAttributeValue("content", null);
            }

            return null;
        }

        var res = GetNodeContent(htmlDoc, attrName, value);
        if (res == null)
        {
            res = GetNodeContent(htmlDoc, attrName, "og:" + value);
        }

        return res;
    }
}