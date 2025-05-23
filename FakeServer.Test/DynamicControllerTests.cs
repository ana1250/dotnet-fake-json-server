﻿using System.Dynamic;
using FakeServer.Common;
using FakeServer.Controllers;
using JsonFlatFileDataStore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FakeServer.Test;

public class DynamicControllerTests
{
    [Fact]
    public void GetCollections()
    {
        var filePath = UTHelpers.Up();
        var ds = new DataStore(filePath);
        var apiSettings = Options.Create(new ApiSettings());
        var dsSettings = Options.Create(new DataStoreSettings());

        var controller = new DynamicController(ds, apiSettings, dsSettings);

        var collections = controller.GetKeys();
        Assert.Equal(7, collections.Count());

        UTHelpers.Down(filePath);
    }

    [Fact]
    public async Task PutItem_NoUpsert()
    {
        var filePath = UTHelpers.Up();
        var ds = new DataStore(filePath);
        var apiSettings = Options.Create(new ApiSettings { UpsertOnPut = false });
        var dsSettings = Options.Create(new DataStoreSettings());

        var controller = new DynamicController(ds, apiSettings, dsSettings);

        var result = await controller.ReplaceItem("my_test", "2", JToken.Parse("{ 'id': 2, 'name': 'Raymond', 'age': 32 }"));
        Assert.IsType<NotFoundResult>(result);

        UTHelpers.Down(filePath);
    }

    [Fact]
    public async Task PutItem_Upsert()
    {
        var filePath = UTHelpers.Up();
        var ds = new DataStore(filePath);
        var apiSettings = Options.Create(new ApiSettings { UpsertOnPut = true });
        var dsSettings = Options.Create(new DataStoreSettings());

        var controller = new DynamicController(ds, apiSettings, dsSettings);

        var result = await controller.ReplaceItem("my_test", "2", JToken.Parse("{ 'id': 2, 'name': 'Raymond', 'age': 32 }"));
        Assert.IsType<NoContentResult>(result);

        var itemResult = controller.GetItem("my_test", "2");
        Assert.IsType<OkObjectResult>(itemResult);

        var okObjectResult = itemResult as OkObjectResult;
        dynamic item = okObjectResult.Value as ExpandoObject;
        Assert.Equal("Raymond", item.name);

        UTHelpers.Down(filePath);
    }

    [Fact]
    public async Task PutItem_Upsert_Id_String()
    {
        var filePath = UTHelpers.Up();
        var ds = new DataStore(filePath);
        var apiSettings = Options.Create(new ApiSettings { UpsertOnPut = true });
        var dsSettings = Options.Create(new DataStoreSettings());

        var controller = new DynamicController(ds, apiSettings, dsSettings);

        var result = await controller.ReplaceItem("my_test_string", "acdc", JToken.Parse("{ 'id': 2, 'text': 'Hello' }")) as NoContentResult;
        Assert.Equal(204, result.StatusCode);

        var itemResult = controller.GetItem("my_test_string", "acdc") as OkObjectResult;

        dynamic item = itemResult.Value as ExpandoObject;
        Assert.Equal("acdc", item.id);
        Assert.Equal("Hello", item.text);

        UTHelpers.Down(filePath);
    }

    [Fact]
    public void GetItems_FavouriteMovieWithQueryString()
    {
        var filePath = UTHelpers.Up();
        var ds = new DataStore(filePath);
        var apiSettings = Options.Create(new ApiSettings());
        var dsSettings = Options.Create(new DataStoreSettings());

        var controller = new DynamicController(ds, apiSettings, dsSettings);
        controller.ControllerContext = new ControllerContext();
        controller.ControllerContext.HttpContext = new DefaultHttpContext();
        controller.ControllerContext.HttpContext.Request.QueryString = new QueryString("?parents.favouriteMovie=Predator");

        // NOTE: Can't but skip and take to querystring with tests
        var result = controller.GetItems("families", 0, 100) as OkObjectResult;
        Assert.Equal(11, ((IEnumerable<dynamic>)result.Value).Count());

        UTHelpers.Down(filePath);
    }

    [Fact]
    public void GetItems_FriendsWithQueryString()
    {
        var filePath = UTHelpers.Up();
        var ds = new DataStore(filePath);
        var apiSettings = Options.Create(new ApiSettings());
        var dsSettings = Options.Create(new DataStoreSettings());

        var controller = new DynamicController(ds, apiSettings, dsSettings);
        controller.ControllerContext = new ControllerContext();
        controller.ControllerContext.HttpContext = new DefaultHttpContext();
        controller.ControllerContext.HttpContext.Request.QueryString = new QueryString("?children.friends.name=Castillo");

        var result = controller.GetItems("families") as OkObjectResult;
        Assert.Equal(2, ((IEnumerable<dynamic>)result.Value).Count());

        UTHelpers.Down(filePath);
    }

    [Fact]
    public void GetNested_ParentsSingleWork()
    {
        var filePath = UTHelpers.Up();
        var ds = new DataStore(filePath);
        var apiSettings = Options.Create(new ApiSettings());
        var dsSettings = Options.Create(new DataStoreSettings());

        var controller = new DynamicController(ds, apiSettings, dsSettings);

        var result = controller.GetNested("families", 1, "parents/1/work") as OkObjectResult;
        Assert.Equal("APEXTRI", ((dynamic)result.Value).companyName);

        UTHelpers.Down(filePath);
    }

    [Fact]
    public void GetNested_ParentsSingle()
    {
        var filePath = UTHelpers.Up();
        var ds = new DataStore(filePath);
        var apiSettings = Options.Create(new ApiSettings());
        var dsSettings = Options.Create(new DataStoreSettings());

        var controller = new DynamicController(ds, apiSettings, dsSettings);

        var result = controller.GetNested("families", 1, "parents/1") as OkObjectResult;
        Assert.Equal("Kim", ((dynamic)result.Value).name);

        UTHelpers.Down(filePath);
    }

    [Fact]
    public void GetNested_ParentsList()
    {
        var filePath = UTHelpers.Up();
        var ds = new DataStore(filePath);
        var apiSettings = Options.Create(new ApiSettings());
        var dsSettings = Options.Create(new DataStoreSettings());

        var controller = new DynamicController(ds, apiSettings, dsSettings);

        var result = controller.GetNested("families", 1, "parents") as OkObjectResult;
        Assert.Equal(2, ((IEnumerable<dynamic>)result.Value).Count());

        UTHelpers.Down(filePath);
    }

    [Fact]
    public void GetItems_UseResultObject()
    {
        var filePath = UTHelpers.Up();
        var ds = new DataStore(filePath);
        var apiSettings = Options.Create(new ApiSettings { UseResultObject = true });
        var dsSettings = Options.Create(new DataStoreSettings());

        var controller = new DynamicController(ds, apiSettings, dsSettings);
        controller.ControllerContext = new ControllerContext();
        controller.ControllerContext.HttpContext = new DefaultHttpContext();
        controller.ControllerContext.HttpContext.Request.QueryString = new QueryString("");

        var result = controller.GetItems("families", 4, 10) as OkObjectResult;

        dynamic resultObject = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(result.Value));

        Assert.Equal(10, resultObject.results.Count);
        Assert.Equal(4, resultObject.skip.Value);
        Assert.Equal(10, resultObject.take.Value);
        Assert.Equal(20, resultObject.count.Value);

        UTHelpers.Down(filePath);
    }

    [Fact]
    public void GetItems_UseResultObject_offsetlimit()
    {
        var filePath = UTHelpers.Up();
        var ds = new DataStore(filePath);
        var apiSettings = Options.Create(new ApiSettings { UseResultObject = true });
        var dsSettings = Options.Create(new DataStoreSettings());

        var controller = new DynamicController(ds, apiSettings, dsSettings);
        controller.ControllerContext = new ControllerContext();
        controller.ControllerContext.HttpContext = new DefaultHttpContext();
        controller.ControllerContext.HttpContext.Request.QueryString = new QueryString("?offset=5&limit=12");

        var result = controller.GetItems("families") as OkObjectResult;

        var resultObject = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(result.Value));

        Assert.Equal(12, resultObject.results.Count);
        Assert.Equal(5, resultObject.offset.Value);
        Assert.Equal(12, resultObject.limit.Value);
        Assert.Equal(20, resultObject.count.Value);

        UTHelpers.Down(filePath);
    }

    [Fact]
    public void GetItems_EmptyCollection()
    {
        var filePath = UTHelpers.Up();
        var ds = new DataStore(filePath);
        var apiSettings = Options.Create(new ApiSettings { UseResultObject = true });
        var dsSettings = Options.Create(new DataStoreSettings());

        var controller = new DynamicController(ds, apiSettings, dsSettings);
        controller.ControllerContext = new ControllerContext();
        controller.ControllerContext.HttpContext = new DefaultHttpContext();

        var result = controller.GetItems("empty_collection") as OkObjectResult;

        var resultObject = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(result.Value));

        Assert.Equal(0, resultObject.results.Count);
        Assert.Equal(0, resultObject.skip.Value);
        Assert.Equal(512, resultObject.take.Value);
        Assert.Equal(0, resultObject.count.Value);

        UTHelpers.Down(filePath);
    }

    [Fact]
    public void GetItems_UseResultObject_page_and_per_page()
    {
        var filePath = UTHelpers.Up();
        var ds = new DataStore(filePath);
        var apiSettings = Options.Create(new ApiSettings { UseResultObject = true });
        var dsSettings = Options.Create(new DataStoreSettings());

        var controller = new DynamicController(ds, apiSettings, dsSettings);
        controller.ControllerContext = new ControllerContext();
        controller.ControllerContext.HttpContext = new DefaultHttpContext();
        controller.ControllerContext.HttpContext.Request.QueryString = new QueryString("?page=1&per_page=12");

        var result = controller.GetItems("families", 1, 12) as OkObjectResult;

        var resultObject = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(result.Value));

        Assert.Equal(12, resultObject.results.Count);
        Assert.Equal(1, resultObject.page.Value);
        Assert.Equal(12, resultObject.per_page.Value);
        Assert.Equal(20, resultObject.count.Value);

        UTHelpers.Down(filePath);
    }

    [Fact]
    public void SingleItem_GetItems_Ok()
    {
        var filePath = UTHelpers.Up();
        var ds = new DataStore(filePath);
        var apiSettings = Options.Create(new ApiSettings { UpsertOnPut = true });
        var dsSettings = Options.Create(new DataStoreSettings());

        var controller = new DynamicController(ds, apiSettings, dsSettings);

        var itemResult = controller.GetItems("configuration");
        Assert.IsType<OkObjectResult>(itemResult);

        var okObjectResult = itemResult as OkObjectResult;
        dynamic item = okObjectResult.Value as ExpandoObject;
        Assert.Equal("abba", item.password);

        UTHelpers.Down(filePath);
    }

    [Fact]
    public void SingleItem_GetItem_BadRequest()
    {
        var filePath = UTHelpers.Up();
        var ds = new DataStore(filePath);
        var apiSettings = Options.Create(new ApiSettings { UpsertOnPut = true });
        var dsSettings = Options.Create(new DataStoreSettings());

        var controller = new DynamicController(ds, apiSettings, dsSettings);

        var itemResult = controller.GetItem("configuration", "0");
        Assert.IsType<BadRequestResult>(itemResult);

        UTHelpers.Down(filePath);
    }

    [Fact]
    public void SingleItem_GetNested_BadRequest()
    {
        var filePath = UTHelpers.Up();
        var ds = new DataStore(filePath);
        var apiSettings = Options.Create(new ApiSettings());
        var dsSettings = Options.Create(new DataStoreSettings());

        var controller = new DynamicController(ds, apiSettings, dsSettings);

        var result = controller.GetNested("configuration", 0, "ip");
        Assert.IsType<BadRequestResult>(result);

        UTHelpers.Down(filePath);
    }

    [Fact]
    public async Task SingleItem_AddNewItem_Conflict()
    {
        var filePath = UTHelpers.Up();
        var ds = new DataStore(filePath);
        var apiSettings = Options.Create(new ApiSettings());
        var dsSettings = Options.Create(new DataStoreSettings());

        var controller = new DynamicController(ds, apiSettings, dsSettings);

        var item = new { ip = "0.0.0.0", password = "hello" };

        var result = await controller.AddNewItem("configuration", JToken.FromObject(item));
        Assert.IsType<ConflictResult>(result);

        UTHelpers.Down(filePath);
    }

    [Fact]
    public async Task SingelItem_ReplaceSingleItem_NotFound()
    {
        var filePath = UTHelpers.Up();
        var ds = new DataStore(filePath);
        var apiSettings = Options.Create(new ApiSettings());
        var dsSettings = Options.Create(new DataStoreSettings());

        var controller = new DynamicController(ds, apiSettings, dsSettings);

        var item = new { value = "hello" };

        var result = await controller.ReplaceSingleItem("new_item", JToken.FromObject(item));
        Assert.IsType<NotFoundResult>(result);

        UTHelpers.Down(filePath);
    }

    [Fact]
    public async Task SingleItem_ReplaceSingleItem_Replaced()
    {
        var filePath = UTHelpers.Up();
        var ds = new DataStore(filePath);
        var apiSettings = Options.Create(new ApiSettings());
        var dsSettings = Options.Create(new DataStoreSettings());

        var controller = new DynamicController(ds, apiSettings, dsSettings);

        var updateItem = new { ip = "0.0.0.0", password = "hello" };

        var result = await controller.ReplaceSingleItem("configuration", JToken.FromObject(updateItem));
        Assert.IsType<NoContentResult>(result);

        var getResult = controller.GetItems("configuration");
        var okObjectResult = getResult as OkObjectResult;
        dynamic item = okObjectResult.Value as ExpandoObject;
        Assert.Equal("0.0.0.0", item.ip);
        Assert.Equal("hello", item.password);

        UTHelpers.Down(filePath);
    }

    [Fact]
    public async Task SingelItem_ReplaceSingleItem_Added()
    {
        var filePath = UTHelpers.Up();
        var ds = new DataStore(filePath);
        var apiSettings = Options.Create(new ApiSettings { UpsertOnPut = true });
        var dsSettings = Options.Create(new DataStoreSettings());

        var controller = new DynamicController(ds, apiSettings, dsSettings);

        var item = new { value = "hello" };

        var result = await controller.ReplaceSingleItem("new_item", JToken.FromObject(item));
        Assert.IsType<NoContentResult>(result);

        UTHelpers.Down(filePath);
    }
}