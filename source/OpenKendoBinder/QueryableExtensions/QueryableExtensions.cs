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

        public static DataSourceResponse<TEntity, TViewModel> ToDataSourceResponse<TEntity, TViewModel>(this IQueryable<TEntity> query, BaseDataSourceRequest request, IEnumerable<string> includes = null, Dictionary<string, string> mappings = null, Func<IQueryable<TEntity>, IEnumerable<TViewModel>> conversion = null, bool canUseAutoMapperProjection = true)
        {
            return new DataSourceResponse<TEntity, TViewModel>(request, query, includes, mappings, conversion, canUseAutoMapperProjection);
        }

        public static IEnumerable<TViewModel> FilterBy<TEntity, TViewModel>(this IQueryable<TEntity> query, BaseDataSourceRequest request, IEnumerable<string> includes = null, Dictionary<string, string> mappings = null, Func<IQueryable<TEntity>, IEnumerable<TViewModel>> conversion = null, bool canUseAutoMapperProjection = true)
        {
            return new DataSourceResponse<TEntity, TViewModel>(request, query, includes, mappings, conversion, canUseAutoMapperProjection).Data;
        }
    }
}