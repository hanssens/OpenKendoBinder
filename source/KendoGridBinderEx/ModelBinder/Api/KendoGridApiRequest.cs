using System.Web.Http.ModelBinding;

namespace OpenKendoBinder.ModelBinder.Api
{
    [ModelBinder(typeof(KendoGridApiModelBinder))]
    public class KendoGridApiRequest : KendoGridBaseRequest
    {
    }
}