using System;
using System.Web.Mvc;

namespace OpenKendoBinder.ModelBinder.Mvc
{
    [ModelBinder(typeof(DataSourceModelBinder))]
    public class DataSourceMvcRequest : BaseDataSourceRequest
    {
    }

    [Obsolete("Use DataSourceMvcRequest")]
    [ModelBinder(typeof(DataSourceModelBinder))]
    public class DataSourceRequest : BaseDataSourceRequest
    {
    }
}