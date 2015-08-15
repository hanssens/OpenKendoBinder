using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenKendoBinder.QueryableExtensions
{
    public static class EnumerableExtensions
    {
        public static DataSourceResponse<TModel> ToDataSourceResponse<TModel>(this IEnumerable<TModel> query, BaseDataSourceRequest request)
        {
            return new DataSourceResponse<TModel>(request, query.AsQueryable());
        }
    }
}