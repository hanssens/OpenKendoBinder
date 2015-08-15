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

        public static DataSourceResponse<TEntity, TViewModel> ToDataSourceResponse<TEntity, TViewModel>(this IEnumerable<TEntity> query, BaseDataSourceRequest request, IEnumerable<string> includes = null, Dictionary<string, string> mappings = null, Func<IQueryable<TEntity>, IEnumerable<TViewModel>> conversion = null, bool canUseAutoMapperProjection = true)
        {
            return new DataSourceResponse<TEntity, TViewModel>(request, query.AsQueryable(), includes, mappings, conversion, canUseAutoMapperProjection);
        }
    }
}