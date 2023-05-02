﻿// <copyright file="EmailGroupsTests.cs" company="WavePoint Co. Ltd.">
// Licensed under the MIT license. See LICENSE file in the samples root for full license information.
// </copyright>

using OnlineSales.DataAnnotations;
using OnlineSales.Interfaces;

namespace OnlineSales.Tests;
public class EmailGroupsTests : SimpleTableTests<EmailGroup, TestEmailGroup, EmailGroupUpdateDto, ISaveService<EmailGroup>>
{
    public EmailGroupsTests()
        : base("/api/email-groups")
    {
    }

    [Fact]
    public async Task GetWithWhereLikeTest()
    {
        // we are trying to test Where query in Postgres, so we have chosen a type without elastic indexing
        Attribute.GetCustomAttribute(typeof(EmailGroup), typeof(SupportsElasticAttribute)).Should().BeNull();

        var bulkEntitiesList = new List<EmailGroup>();

        var bulkList = TestData.GenerateAndPopulateAttributes<TestEmailGroup>("1", tc => tc.Name = "1 Test");
        bulkEntitiesList.Add(mapper.Map<EmailGroup>(bulkList));
        bulkList = TestData.GenerateAndPopulateAttributes<TestEmailGroup>("2", tc => tc.Name = "Test 2 z");
        bulkEntitiesList.Add(mapper.Map<EmailGroup>(bulkList));
        bulkList = TestData.GenerateAndPopulateAttributes<TestEmailGroup>("3", tc => tc.Name = "Test 3");
        bulkEntitiesList.Add(mapper.Map<EmailGroup>(bulkList));
        bulkList = TestData.GenerateAndPopulateAttributes<TestEmailGroup>("4", tc => tc.Name = "Te1st 3$");
        bulkEntitiesList.Add(mapper.Map<EmailGroup>(bulkList));

        App.PopulateBulkData<EmailGroup, ISaveService<EmailGroup>>(bulkEntitiesList);

        var result = await GetTest<List<EmailGroup>>(itemsUrl + "?filter[where][UpdatedAt][like]=.*est", HttpStatusCode.BadRequest);
        result.Should().BeNull();

        result = await GetTest<List<EmailGroup>>(itemsUrl + "?filter[where][Name][like]=.*est");
        result!.Count.Should().Be(3);
    }

    [Fact]
    public async Task GetWithWhereEqualTest()
    {
        // we are trying to test Where query in Postgres, so we have chosen a type without elastic indexing
        Attribute.GetCustomAttribute(typeof(EmailGroup), typeof(SupportsElasticAttribute)).Should().BeNull();

        var bulkEntitiesList = new List<EmailGroup>();

        var bulkList = TestData.GenerateAndPopulateAttributes<TestEmailGroup>("1", tc => tc.Name = "Test1");
        bulkEntitiesList.Add(mapper.Map<EmailGroup>(bulkList));
        bulkList = TestData.GenerateAndPopulateAttributes<TestEmailGroup>("2", tc => tc.Name = "Test2");
        bulkEntitiesList.Add(mapper.Map<EmailGroup>(bulkList));
        bulkList = TestData.GenerateAndPopulateAttributes<TestEmailGroup>("3", tc => tc.Name = "Test3");
        bulkEntitiesList.Add(mapper.Map<EmailGroup>(bulkList));
        bulkList = TestData.GenerateAndPopulateAttributes<TestEmailGroup>("4", tc => tc.Name = "Tes|t4");
        bulkEntitiesList.Add(mapper.Map<EmailGroup>(bulkList));
        bulkList = TestData.GenerateAndPopulateAttributes<TestEmailGroup>("5", tc => tc.Name = "Test1 Test2");
        bulkEntitiesList.Add(mapper.Map<EmailGroup>(bulkList));

        App.PopulateBulkData(bulkEntitiesList);

        var result = await GetTest<List<EmailGroup>>(itemsUrl + "?filter[where][UpdatedAt][eq]=null");
        result!.Count.Should().Be(5);
        result = await GetTest<List<EmailGroup>>(itemsUrl + "?filter[where][UpdatedAt][neq]=null");
        result!.Count.Should().Be(0);
        result = await GetTest<List<EmailGroup>>(itemsUrl + "?filter[where][Name][eq]=Test1|Test2");
        result!.Count.Should().Be(2);
        result = await GetTest<List<EmailGroup>>(itemsUrl + "?filter[where][Name][neq]=Test1|Test2");
        result!.Count.Should().Be(3);
        result = await GetTest<List<EmailGroup>>(itemsUrl + "?filter[where][Name][eq]=Test1");
        result!.Count.Should().Be(1);
        result = await GetTest<List<EmailGroup>>(itemsUrl + "?filter[where][Name][neq]=Test1");
        result!.Count.Should().Be(4);
        result = await GetTest<List<EmailGroup>>(itemsUrl + "?filter[where][Name][eq]=Test5|Test6");
        result!.Count.Should().Be(0);
        result = await GetTest<List<EmailGroup>>(itemsUrl + "?filter[where][Name][neq]=Test5|Test6");
        result!.Count.Should().Be(5);
        result = await GetTest<List<EmailGroup>>(itemsUrl + "?filter[where][Name][eq]=Test1|Tes\\|t4");
        result!.Count.Should().Be(2);
    }

    [Fact]
    public async Task GetWithWhereContainTest()
    {
        // we are trying to test Where query in Postgres, so we have chosen a type without elastic indexing
        Attribute.GetCustomAttribute(typeof(EmailGroup), typeof(SupportsElasticAttribute)).Should().BeNull();

        var bulkEntitiesList = new List<EmailGroup>();

        var bulkList = TestData.GenerateAndPopulateAttributes<TestEmailGroup>("1", tc => tc.Name = "1 Test");
        bulkEntitiesList.Add(mapper.Map<EmailGroup>(bulkList));
        bulkList = TestData.GenerateAndPopulateAttributes<TestEmailGroup>("2", tc => tc.Name = "Test 2 z");
        bulkEntitiesList.Add(mapper.Map<EmailGroup>(bulkList));
        bulkList = TestData.GenerateAndPopulateAttributes<TestEmailGroup>("3", tc => tc.Name = "Test 3");
        bulkEntitiesList.Add(mapper.Map<EmailGroup>(bulkList));
        bulkList = TestData.GenerateAndPopulateAttributes<TestEmailGroup>("4", tc => tc.Name = "Te*st 3");
        bulkEntitiesList.Add(mapper.Map<EmailGroup>(bulkList));

        App.PopulateBulkData(bulkEntitiesList);

        var result = await GetTest<List<EmailGroup>>(itemsUrl + "?filter[where][UpdatedAt][contains]=Test", HttpStatusCode.BadRequest);
        result.Should().BeNull();
        result = await GetTest<List<EmailGroup>>(itemsUrl + "?filter[where][Name][contains]=Test");
        result!.Count.Should().Be(0);
        result = await GetTest<List<EmailGroup>>(itemsUrl + "?filter[where][Name][contains]=*Test*");
        result!.Count.Should().Be(3);
        result = await GetTest<List<EmailGroup>>(itemsUrl + "?filter[where][Name][contains]=Test*");
        result!.Count.Should().Be(2);
        result = await GetTest<List<EmailGroup>>(itemsUrl + "?filter[where][Name][contains]=*Test");
        result!.Count.Should().Be(1);
        result = await GetTest<List<EmailGroup>>(itemsUrl + "?filter[where][Name][contains]=*Te\\*st*");
        result!.Count.Should().Be(1);
    }

    [Fact]
    public async Task GetWithWhereDateComparisonTest()
    {
        // we are trying to test Where query in Postgres, so we have chosen a type without elastic indexing
        Attribute.GetCustomAttribute(typeof(EmailGroup), typeof(SupportsElasticAttribute)).Should().BeNull();

        var bulkEntitiesList = new List<EmailGroup>();

        var bulkList = TestData.GenerateAndPopulateAttributes<TestEmailGroup>("1");
        bulkEntitiesList.Add(mapper.Map<EmailGroup>(bulkList));
        App.PopulateBulkData(bulkEntitiesList);

        var result = await GetTest<List<EmailGroup>>(itemsUrl + "?filter[where][CreatedAt]=", HttpStatusCode.BadRequest);
        result.Should().BeNull();

        var now = DateTime.UtcNow;
        var timeStr = now.ToString("yyyy-MM-dd'T'HH:mm:ss.fffK");

        result = await GetTest<List<EmailGroup>>(itemsUrl + "?filter[where][CreatedAt][gt]=" + timeStr);
        result!.Count.Should().Be(0);
        result = await GetTest<List<EmailGroup>>(itemsUrl + "?filter[where][CreatedAt][lt]=" + timeStr);
        result!.Count.Should().Be(1);
    }

    protected override EmailGroupUpdateDto UpdateItem(TestEmailGroup to)
    {
        var from = new EmailGroupUpdateDto();
        to.Name = from.Name = to.Name + "Updated";
        return from;
    }
}