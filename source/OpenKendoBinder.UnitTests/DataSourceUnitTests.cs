using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using OpenKendoBinder.QueryableExtensions;
using OpenKendoBinder.UnitTests.Entities;
using OpenKendoBinder.UnitTests.Helpers;

namespace OpenKendoBinder.UnitTests
{
    [TestFixture]
    public class DataSourceUnitTests : TestHelper
    {
        [Test]
        public void Test_KendoDataSourceModelBinder_Grid_Page()
        {
            var form = new NameValueCollection
            {
                {"take", "5"},
                {"skip", "0"},
                {"page", "1"},
                {"pagesize", "5"}
            };

            var gridRequest = SetupBinder(form, null);

            var employees = InitEmployeesWithData().AsQueryable();
            var response = employees.ToDataSourceResponse(gridRequest);

            Assert.IsNotNull(response);
            Assert.AreEqual(employees.Count(), response.Total);
            Assert.IsNotNull(response.Data);
            Assert.AreEqual(5, response.Data.Count());
        }

        [Test]
        public void Test_KendoDataSourceModelBinder_Grid_Sort_Filter_EntitiesWithNullValues()
        {
            var form = new NameValueCollection
            {
                {"sort[0][field]", "Id"},
                {"sort[0][dir]", "asc"},

                {"filter[filters][0][field]", "LastName"},
                {"filter[filters][0][operator]", "contains"},
                {"filter[filters][0][value]", "s"},
                {"filter[filters][1][field]", "Email"},
                {"filter[filters][1][operator]", "contains"},
                {"filter[filters][1][value]", "r"},
                {"filter[logic]", "or"}
            };

            var gridRequest = SetupBinder(form, null);

            var employeeList = InitEmployeesWithData().ToList();
            foreach (var employee in employeeList.Where(e => e.LastName.Contains("e")))
            {
                employee.LastName = null;
                employee.FirstName = null;
            }

            var employees = employeeList.AsQueryable();

            var response = employees.ToDataSourceResponse(gridRequest);
            Assert.IsNotNull(response);

            Assert.AreEqual(3, response.Total);
            Assert.IsNotNull(response.Data);
        }

        [Test]
        public void Test_KendoDataSourceModelBinder_Grid_Page_Filter_Sort()
        {
            var form = new NameValueCollection
            {
                {"take", "5"},
                {"skip", "0"},
                {"page", "1"},
                {"pagesize", "5"},
                
                {"sort[0][field]", "FirstName"},
                {"sort[0][dir]", "asc"},
                {"sort[1][field]", "Email"},
                {"sort[1][dir]", "desc"},

                {"filter[filters][0][logic]", "or"},
                {"filter[filters][0][filters][0][field]", "Company.Name"},
                {"filter[filters][0][filters][0][operator]", "eq"},
                {"filter[filters][0][filters][0][value]", "A"},
                {"filter[filters][0][filters][1][field]", "Company.Name"},
                {"filter[filters][0][filters][1][operator]", "contains"},
                {"filter[filters][0][filters][1][value]", "B"},
                
                {"filter[filters][1][field]", "LastName"},
                {"filter[filters][1][operator]", "contains"},
                {"filter[filters][1][value]", "s"},
                {"filter[logic]", "and"}
            };

            var gridRequest = SetupBinder(form, null);

            var employees = InitEmployeesWithData().AsQueryable();
            var response = employees.ToDataSourceResponse(gridRequest);
            Assert.IsNotNull(response);

            Assert.AreEqual(4, response.Total);
            Assert.IsNotNull(response.Data);
            Assert.AreEqual(4, response.Data.Count());

            Assert.AreEqual("Bill Smith", response.Data.First().FullName);
            Assert.AreEqual("Jack Smith", response.Data.Last().FullName);

            var query = response.AsQueryable();
            Assert.AreEqual("Bill Smith", query.First().FullName);
            Assert.AreEqual("Jack Smith", query.Last().FullName);
        }

        [Test]
        public void Test_KendoDataSourceModelBinder_One_GroupBy_WithIncludes()
        {
            var form = new NameValueCollection
            {
                {"take", "5"},
                {"skip", "0"},
                {"page", "1"},
                {"pagesize", "5"},

                {"group[0][field]", "Country.Name"},
                {"group[0][dir]", "asc"}
            };

            var gridRequest = SetupBinder(form, null);
            Assert.AreEqual(1, gridRequest.GroupObjects.Count());
            Assert.AreEqual(0, gridRequest.GroupObjects.First().AggregateObjects.Count());

            //var includes = new[] {"Company", "Company.MainCompany", "Country"};
            var employees = InitEmployeesWithData().AsQueryable();
            var response = new DataSourceResponse<Employee>(gridRequest, employees);

            Assert.IsNull(response.Data);
            Assert.IsNotNull(response.Groups);
            var json = JsonConvert.SerializeObject(response.Groups, Formatting.Indented);
            Assert.IsNotNull(json);

            var groups = response.Groups as List<DataSourceGroup>;
            Assert.IsNotNull(groups);

            Assert.AreEqual(2, groups.Count());
            Assert.AreEqual(employees.Count(), response.Total);

            var employeesFromFirstGroup = groups.First().items as IEnumerable<Employee>;
            Assert.IsNotNull(employeesFromFirstGroup);

            var employeesFromFirstGroupList = employeesFromFirstGroup.ToList();
            Assert.AreEqual(4, employeesFromFirstGroupList.Count);

            var testEmployee = employeesFromFirstGroupList.First();
            Assert.AreEqual("Belgium", testEmployee.Country.Name);
            Assert.AreEqual("B", testEmployee.Company.Name);
        }

        [Test]
        public void Test_KendoDataSourceModelBinder_Aggregates_WithIncludes()
        {
            var form = new NameValueCollection
            {
                {"take", "5"},
                {"skip", "0"},
                {"page", "1"},
                {"pagesize", "5"},

                {"aggregate[0][field]", "Id"},
                {"aggregate[0][aggregate]", "sum"},
                {"aggregate[1][field]", "Id"},
                {"aggregate[1][aggregate]", "min"},
                {"aggregate[2][field]", "Id"},
                {"aggregate[2][aggregate]", "max"},
                {"aggregate[3][field]", "Id"},
                {"aggregate[3][aggregate]", "count"},
                {"aggregate[4][field]", "Id"},
                {"aggregate[4][aggregate]", "average"}
            };

            var gridRequest = SetupBinder(form, null);
            Assert.IsNull(gridRequest.GroupObjects);
            Assert.AreEqual(5, gridRequest.AggregateObjects.Count());
            
            var employees = InitEmployeesWithData().AsQueryable();
            var response = new DataSourceResponse<Employee>(gridRequest, employees);

            Assert.IsNull(response.Groups);
            Assert.IsNotNull(response.Data);
            Assert.AreEqual(5, response.Data.Count());

            Assert.IsNotNull(response.Aggregates);
            var json = JsonConvert.SerializeObject(response.Aggregates, Formatting.Indented);
            Assert.IsNotNull(json);

            var aggregatesAsDictionary = response.Aggregates as Dictionary<string, Dictionary<string, object>>;
            Assert.IsNotNull(aggregatesAsDictionary);
            Assert.AreEqual(1, aggregatesAsDictionary.Keys.Count);
            Assert.AreEqual("Id", aggregatesAsDictionary.Keys.First());

            var aggregatesForId = aggregatesAsDictionary["Id"];
            Assert.AreEqual(5, aggregatesForId.Keys.Count);
            Assert.AreEqual(78, aggregatesForId["sum"]);
            Assert.AreEqual(1, aggregatesForId["min"]);
            Assert.AreEqual(12, aggregatesForId["max"]);
            Assert.AreEqual(12, aggregatesForId["count"]);
            Assert.AreEqual(6.5d, aggregatesForId["average"]);
        }

        [Test]
        public void Test_KendoDataSourceModelBinder_Aggregates_WithIncludes_NoResults()
        {
            var form = new NameValueCollection
            {
                {"take", "5"},
                {"skip", "0"},
                {"page", "1"},
                {"pagesize", "5"},

                {"filter[filters][0][field]", "LastName"},
                {"filter[filters][0][operator]", "equals"},
                {"filter[filters][0][value]", "xxx"},
                {"filter[filters][1][field]", "Email"},
                {"filter[filters][1][operator]", "contains"},
                {"filter[filters][1][value]", "r"},
                {"filter[logic]", "or"},

                {"aggregate[0][field]", "Id"},
                {"aggregate[0][aggregate]", "sum"},
                {"aggregate[1][field]", "Id"},
                {"aggregate[1][aggregate]", "min"},
                {"aggregate[2][field]", "Id"},
                {"aggregate[2][aggregate]", "max"},
                {"aggregate[3][field]", "Id"},
                {"aggregate[3][aggregate]", "count"},
                {"aggregate[4][field]", "Id"},
                {"aggregate[4][aggregate]", "average"}
            };

            var gridRequest = SetupBinder(form, null);
            Assert.IsNull(gridRequest.GroupObjects);
            Assert.AreEqual(5, gridRequest.AggregateObjects.Count());

            var employees = InitEmployeesWithData().AsQueryable();
            var response = new DataSourceResponse<Employee>(gridRequest, employees);

            Assert.IsNull(response.Groups);
            Assert.IsNotNull(response.Data);
            Assert.AreEqual(0, response.Data.Count());

            Assert.IsNotNull(response.Aggregates);
            var json = JsonConvert.SerializeObject(response.Aggregates, Formatting.Indented);
            Assert.IsNotNull(json);

            var aggregatesAsDictionary = response.Aggregates as Dictionary<string, Dictionary<string, object>>;
            Assert.IsNotNull(aggregatesAsDictionary);
            Assert.AreEqual(0, aggregatesAsDictionary.Keys.Count);
        }

        [Test]
        public void Test_KendoDataSourceModelBinder_One_GroupBy_WithoutIncludes()
        {
            var form = new NameValueCollection
            {
                {"take", "5"},
                {"skip", "0"},
                {"page", "1"},
                {"pagesize", "5"},

                {"group[0][field]", "LastName"},
                {"group[0][dir]", "asc"}
            };

            var gridRequest = SetupBinder(form, null);
            Assert.AreEqual(1, gridRequest.GroupObjects.Count());
            Assert.AreEqual(0, gridRequest.GroupObjects.First().AggregateObjects.Count());

            var employees = InitEmployees().AsQueryable();
            var response = employees.ToDataSourceResponse(gridRequest);

            Assert.IsNull(response.Data);
            Assert.IsNotNull(response.Groups);
            var json = JsonConvert.SerializeObject(response.Groups, Formatting.Indented);
            Assert.IsNotNull(json);

            var groups = response.Groups as List<DataSourceGroup>;
            Assert.IsNotNull(groups);

            Assert.AreEqual(5, groups.Count());
            Assert.AreEqual(employees.Count(), response.Total);

            var employeesFromFirstGroup = groups.First().items as IEnumerable<Employee>;
            Assert.IsNotNull(employeesFromFirstGroup);

            var employeesFromFirstGroupList = employeesFromFirstGroup.ToList();
            Assert.AreEqual(1, employeesFromFirstGroupList.Count);

            var testEmployee = employeesFromFirstGroupList.First();
            Assert.IsNull(testEmployee.Country);
        }

        [Test]
        public void Test_KendoDataSourceModelBinder_One_GroupBy_One_Aggregate_Count()
        {
            var form = new NameValueCollection
            {
                {"take", "5"},
                {"skip", "0"},
                {"page", "1"},
                {"pagesize", "5"},

                {"sort[0][field]", "FullName"},
                {"sort[0][dir]", "asc"},

                {"group[0][field]", "FirstName"},
                {"group[0][dir]", "asc"},
                {"group[0][aggregates][0][field]", "FirstName"},
                {"group[0][aggregates][0][aggregate]", "count"}
            };

            var gridRequest = SetupBinder(form, null);
            Assert.AreEqual(1, gridRequest.GroupObjects.Count());
            Assert.AreEqual(1, gridRequest.GroupObjects.First().AggregateObjects.Count());

            var employees = InitEmployeesWithData().AsQueryable();
            var response = employees.ToDataSourceResponse(gridRequest);

            Assert.IsNull(response.Data);
            Assert.IsNotNull(response.Groups);
            var json = JsonConvert.SerializeObject(response.Groups, Formatting.Indented);
            Assert.IsNotNull(json);

            var groups = response.Groups as List<DataSourceGroup>;
            Assert.IsNotNull(groups);

            Assert.AreEqual(5, groups.Count());
            Assert.AreEqual(employees.Count(), response.Total);
        }

        [Test]
        public void Test_KendoDataSourceModelBinder_One_GroupBy_One_Aggregate_Sum()
        {
            var form = new NameValueCollection
            {
                {"take", "10"},
                {"skip", "0"},
                {"page", "1"},
                {"pagesize", "10"},

                {"group[0][field]", "LastName"},
                {"group[0][dir]", "asc"},
                {"group[0][aggregates][0][field]", "EmployeeNumber"},
                {"group[0][aggregates][0][aggregate]", "sum"},
            };

            var gridRequest = SetupBinder(form, null);
            Assert.AreEqual(1, gridRequest.GroupObjects.Count());
            Assert.AreEqual(1, gridRequest.GroupObjects.First().AggregateObjects.Count());

            var employees = InitEmployeesWithData().AsQueryable();
            var response = employees.ToDataSourceResponse(gridRequest);

            Assert.IsNull(response.Data);
            Assert.IsNotNull(response.Groups);
            var json = JsonConvert.SerializeObject(response.Groups, Formatting.Indented);
            Assert.IsNotNull(json);

            var groups = response.Groups as List<DataSourceGroup>;
            Assert.IsNotNull(groups);

            Assert.AreEqual(9, groups.Count());
            Assert.AreEqual(employees.Count(), response.Total);

            var groupBySmith = groups.FirstOrDefault(g => g.value.ToString() == "Smith");
            Assert.IsNotNull(groupBySmith);

            var items = groupBySmith.items as List<Employee>;
            Assert.IsNotNull(items);
            Assert.AreEqual(2, items.Count);
            Assert.AreEqual(2, items.Count(e => e.LastName == "Smith"));

            var aggregates = groupBySmith.aggregates as Dictionary<string, Dictionary<string, object>>;
            Assert.IsNotNull(aggregates);

            Assert.IsTrue(aggregates.ContainsKey("EmployeeNumber"));
            var aggregatesNumber = aggregates["EmployeeNumber"];
            Assert.IsNotNull(aggregatesNumber);
            Assert.AreEqual(1, aggregatesNumber.Count);

            var aggregateSum = aggregatesNumber.First();
            Assert.IsNotNull(aggregateSum);
            Assert.AreEqual("sum", aggregateSum.Key);
            Assert.AreEqual(2003, aggregateSum.Value);
        }

        /*
        take=10&skip=0&page=1&pageSize=10&
        group[0][field]=CompanyName&
        group[0][dir]=asc&
        group[0][aggregates][0][field]=Number&
        group[0][aggregates][0][aggregate]=min&
        group[0][aggregates][1][field]=Number&
        group[0][aggregates][1][aggregate]=max&
        group[0][aggregates][2][field]=Number&
        group[0][aggregates][2][aggregate]=average&
        group[0][aggregates][3][field]=Number&
        group[0][aggregates][3][aggregate]=count&

        group[1][field]=LastName&
        group[1][dir]=asc&
        group[1][aggregates][0][field]=Number&
        group[1][aggregates][0][aggregate]=min&
        group[1][aggregates][1][field]=Number&
        group[1][aggregates][1][aggregate]=max&
        group[1][aggregates][2][field]=Number&
        group[1][aggregates][2][aggregate]=average&
        group[1][aggregates][3][field]=Number&
        group[1][aggregates][3][aggregate]=count
         * */
        [Test]
        public void Test_KendoDataSourceModelBinder_Two_GroupBy_One_Aggregate_Min()
        {
            var form = new NameValueCollection
            {
                {"take", "10"},
                {"skip", "0"},
                {"page", "1"},
                {"pagesize", "10"},

                {"group[0][field]", "Company.Name"},
                {"group[0][dir]", "asc"},
                {"group[0][aggregates][0][field]", "EmployeeNumber"},
                {"group[0][aggregates][0][aggregate]", "min"},
          
                {"group[1][field]", "LastName"},
                {"group[1][dir]", "asc"},
                {"group[1][aggregates][0][field]", "EmployeeNumber"},
                {"group[1][aggregates][0][aggregate]", "min"},
            };

            var gridRequest = SetupBinder(form, null);
            Assert.AreEqual(2, gridRequest.GroupObjects.Count());
            Assert.AreEqual(1, gridRequest.GroupObjects.First().AggregateObjects.Count());
            Assert.AreEqual(1, gridRequest.GroupObjects.Last().AggregateObjects.Count());

            var employees = InitEmployeesWithData().AsQueryable();
            var response = employees.ToDataSourceResponse(gridRequest);

            Assert.IsNull(response.Data);
            Assert.IsNotNull(response.Groups);
            var json = JsonConvert.SerializeObject(response.Groups, Formatting.Indented);
            Assert.IsNotNull(json);

            var groups = response.Groups as List<DataSourceGroup>;
            Assert.IsNotNull(groups);

            Assert.AreEqual(10, groups.Count());
            Assert.AreEqual(employees.Count(), response.Total);

            /*
            var groupBySmith = groups.FirstOrDefault(g => g.value.ToString() == "Smith");
            Assert.IsNotNull(groupBySmith);

            var items = groupBySmith.items as List<EmployeeVM>;
            Assert.IsNotNull(items);
            Assert.AreEqual(2, items.Count);
            Assert.AreEqual(2, items.Count(e => e.Last == "Smith"));

            var aggregates = groupBySmith.aggregates as Dictionary<string, Dictionary<string, object>>;
            Assert.IsNotNull(aggregates);

            Assert.IsTrue(aggregates.ContainsKey("Number"));
            var aggregatesNumber = aggregates["Number"];
            Assert.IsNotNull(aggregatesNumber);
            Assert.AreEqual(1, aggregatesNumber.Count);

            var aggregateSum = aggregatesNumber.First();
            Assert.IsNotNull(aggregateSum);
            Assert.AreEqual("sum", aggregateSum.Key);
            Assert.AreEqual(2003, aggregateSum.Value);
            */
        }

        //take=10&
        //skip=0&
        //page=1&
        //pageSize=10&
        //group[0][field]=Id&
        //group[0][dir]=asc&
        //group[0][aggregates][0][field]=Id&
        //group[0][aggregates][0][aggregate]=count
        [Test]
        public void Test_KendoDataSourceModelBinder_A()
        {
            var form = new NameValueCollection
            {
                {"take", "10"},
                {"skip", "0"},
                {"page", "1"},
                {"pagesize", "10"},

                {"group[0][field]", "Id"},
                {"group[0][dir]", "asc"},
                {"group[0][aggregates][0][field]", "Id"},
                {"group[0][aggregates][0][aggregate]", "count"}
            };

            var gridRequest = SetupBinder(form, null);
            Assert.AreEqual(1, gridRequest.GroupObjects.Count());
            Assert.AreEqual(1, gridRequest.GroupObjects.First().AggregateObjects.Count());
            Assert.AreEqual(1, gridRequest.GroupObjects.Last().AggregateObjects.Count());

            var employees = InitEmployeesWithData().AsQueryable();
            var response = employees.ToDataSourceResponse(gridRequest);

            Assert.IsNull(response.Data);
            Assert.IsNotNull(response.Groups);
            var json = JsonConvert.SerializeObject(response.Groups, Formatting.Indented);
            Assert.IsNotNull(json);

            var groups = response.Groups as List<DataSourceGroup>;
            Assert.IsNotNull(groups);

            Assert.AreEqual(10, groups.Count());
            Assert.AreEqual(employees.Count(), response.Total);
        }

        [Test]
        //{"take":5,"skip":0,"page":1,"pageSize":5,"group":[]}
        public void Test_KendoDataSourceModelBinder_Json_WithoutIncludes()
        {
            var form = new NameValueCollection
            {
                {"take", "5"},
                {"skip", "0"},
                {"page", "1"},
                {"pagesize", "5"},

                {"group", "[]"}
            };

            var gridRequest = SetupBinder(form, null);
            Assert.IsNull(gridRequest.GroupObjects);

            var employees = InitEmployees().AsQueryable();
            var response = employees.ToDataSourceResponse(gridRequest);

            Assert.IsNotNull(response);
            Assert.IsNull(response.Groups);
            Assert.NotNull(response.Data);

            Assert.AreEqual(employees.Count(), response.Total);
            Assert.IsNotNull(response.Data);
            Assert.AreEqual(5, response.Data.Count());
        }

        [Test]
        public void Test_KendoDataSourceModelBinder_Json_Filter()
        {
            var form = new NameValueCollection
            {
                {"take", "5"},
                {"skip", "0"},
                {"page", "1"},
                {"pagesize", "5"},

                {"group", "[]"},
                //{"filter", "{\"logic\":\"and\",\"filters\":[{\"field\":\"CompanyName\",\"operator\":\"eq\",\"value\":\"A\"}]}"},
                {"filter", "{\"logic\":\"and\",\"filters\":[{\"logic\":\"or\",\"filters\":[{\"field\":\"LastName\",\"operator\":\"contains\",\"value\":\"s\"},{\"field\":\"LastName\",\"operator\":\"endswith\",\"value\":\"ll\"}]},{\"field\":\"FirstName\",\"operator\":\"startswith\",\"value\":\"n\"}]}"},
                {"sort", "[{\"field\":\"FirstName\",\"dir\":\"asc\",\"compare\":null},{\"field\":\"LastName\",\"dir\":\"desc\",\"compare\":null}]"}
            };

            var gridRequest = SetupBinder(form, null);
            Assert.IsNull(gridRequest.GroupObjects);

            var employees = InitEmployees().AsQueryable();
            var response = employees.ToDataSourceResponse(gridRequest);

            Assert.IsNotNull(response);
            Assert.IsNull(response.Groups);
            Assert.NotNull(response.Data);

            Assert.AreEqual(1, response.Total);
            Assert.IsNotNull(response.Data);
            Assert.AreEqual(1, response.Data.Count());
        }

        [Test]
        //{"take":5,"skip":0,"page":1,"pageSize":5,"group":[{"field":"LastName","dir":"asc","aggregates":[]}]}
        public void Test_KendoDataSourceModelBinder_Json_One_GroupBy_WithoutIncludes()
        {
            var form = new NameValueCollection
            {
                {"take", "5"},
                {"skip", "0"},
                {"page", "1"},
                {"pagesize", "5"},

                {"group", "[{\"field\":\"LastName\",\"dir\":\"asc\",\"aggregates\":[]}]"}
            };

            var gridRequest = SetupBinder(form, null);
            Assert.AreEqual(1, gridRequest.GroupObjects.Count());
            Assert.AreEqual(0, gridRequest.GroupObjects.First().AggregateObjects.Count());

            var employees = InitEmployees().AsQueryable();
            var response = employees.ToDataSourceResponse(gridRequest);

            Assert.IsNull(response.Data);
            Assert.IsNotNull(response.Groups);
            var json = JsonConvert.SerializeObject(response.Groups, Formatting.Indented);
            Assert.IsNotNull(json);

            var groups = response.Groups as List<DataSourceGroup>;
            Assert.IsNotNull(groups);

            Assert.AreEqual(5, groups.Count());
            Assert.AreEqual(employees.Count(), response.Total);

            var employeesFromFirstGroup = groups.First().items as IEnumerable<Employee>;
            Assert.IsNotNull(employeesFromFirstGroup);

            var employeesFromFirstGroupList = employeesFromFirstGroup.ToList();
            Assert.AreEqual(1, employeesFromFirstGroupList.Count);

            var testEmployee = employeesFromFirstGroupList.First();
            Assert.IsNull(testEmployee.Country);
        }

        [Test]
        //{"take":5,"skip":0,"page":1,"pageSize":5,"group":[{"field":"LastName","dir":"asc","aggregates":["field":"Number","aggregate":"Sum"]}]}
        public void Test_KendoDataSourceModelBinder_Json_One_GroupBy_One_Aggregate_Sum()
        {
            var form = new NameValueCollection
            {
                {"take", "10"},
                {"skip", "0"},
                {"page", "1"},
                {"pagesize", "10"},

                {"group", "[{\"field\":\"LastName\",\"dir\":\"asc\",\"aggregates\":[{\"field\":\"EmployeeNumber\",\"aggregate\":\"sum\"}]}]"}
            };

            var gridRequest = SetupBinder(form, null);
            Assert.AreEqual(1, gridRequest.GroupObjects.Count());
            Assert.AreEqual(1, gridRequest.GroupObjects.First().AggregateObjects.Count());

            var employees = InitEmployeesWithData().AsQueryable();
            var response = employees.ToDataSourceResponse(gridRequest);

            Assert.IsNull(response.Data);
            Assert.IsNotNull(response.Groups);
            var json = JsonConvert.SerializeObject(response.Groups, Formatting.Indented);
            Assert.IsNotNull(json);

            var groups = response.Groups as List<DataSourceGroup>;
            Assert.IsNotNull(groups);

            Assert.AreEqual(9, groups.Count());
            Assert.AreEqual(employees.Count(), response.Total);

            var groupBySmith = groups.FirstOrDefault(g => g.value.ToString() == "Smith");
            Assert.IsNotNull(groupBySmith);

            var items = groupBySmith.items as List<Employee>;
            Assert.IsNotNull(items);
            Assert.AreEqual(2, items.Count);
            Assert.AreEqual(2, items.Count(e => e.LastName == "Smith"));

            var aggregates = groupBySmith.aggregates as Dictionary<string, Dictionary<string, object>>;
            Assert.IsNotNull(aggregates);

            Assert.IsTrue(aggregates.ContainsKey("EmployeeNumber"));
            var aggregatesNumber = aggregates["EmployeeNumber"];
            Assert.IsNotNull(aggregatesNumber);
            Assert.AreEqual(1, aggregatesNumber.Count);

            var aggregateSum = aggregatesNumber.First();
            Assert.IsNotNull(aggregateSum);
            Assert.AreEqual("sum", aggregateSum.Key);
            Assert.AreEqual(2003, aggregateSum.Value);
        }

        public interface IKendoResolver
        {
            string GetM();
        }
        
    }
}