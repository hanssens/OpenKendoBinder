using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenKendoBinder.QueryableExtensions
{
    public static class QueryableExtensions
    {
        public static DataSourceResponse<TModel> ToDataSourceResponse<TModel>(this IQueryable<TModel> query, BaseDataSourceRequest request)
        {
            return new DataSourceResponse<TModel>(request, query);
        }
    }
}