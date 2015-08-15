using System.Collections.Specialized;
using Newtonsoft.Json;
using OpenKendoBinder.Containers.Json;
using OpenKendoBinder.Extensions;
using OpenKendoBinder.ModelBinder.Api;

namespace OpenKendoBinder.ModelBinder
{
    public static class GridHelper
    {
        public static T Parse<T>(NameValueCollection queryString) where T : BaseDataSourceRequest, new()
        {
            return new T
            {
                Take = queryString.GetQueryValue("take", (int?)null),
                Page = queryString.GetQueryValue("page", (int?)null),
                Skip = queryString.GetQueryValue("skip", (int?)null),
                PageSize = queryString.GetQueryValue("pageSize", (int?)null),

                FilterObjectWrapper = FilterHelper.Parse(queryString),
                GroupObjects = GroupHelper.Parse(queryString),
                AggregateObjects = AggregateHelper.Parse(queryString),
                SortObjects = SortHelper.Parse(queryString)
            };
        }

        public static DataSourceApiRequest Parse(string jsonRequest)
        {
            var kendoJsonRequest = JsonConvert.DeserializeObject<DataSourceRequest>(jsonRequest);

            return new DataSourceApiRequest
            {
                Take = kendoJsonRequest.Take,
                Page = kendoJsonRequest.Page,
                PageSize = kendoJsonRequest.PageSize,
                Skip = kendoJsonRequest.Skip,
                Logic = kendoJsonRequest.Logic,
                GroupObjects = GroupHelper.Map(kendoJsonRequest.Groups),
                AggregateObjects = AggregateHelper.Map(kendoJsonRequest.AggregateObjects),
                FilterObjectWrapper = FilterHelper.MapRootFilter(kendoJsonRequest.Filter),
                SortObjects = SortHelper.Map(kendoJsonRequest.Sort)
            };
        }
    }
}