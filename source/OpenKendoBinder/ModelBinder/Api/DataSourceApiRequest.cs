using System.Web.Http.ModelBinding;

namespace OpenKendoBinder.ModelBinder.Api
{
    [ModelBinder(typeof(DataSourceApiModelBinder))]
    public class DataSourceApiRequest : BaseDataSourceRequest
    {
    }
}