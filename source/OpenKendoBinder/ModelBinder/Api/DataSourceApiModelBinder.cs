using System;
using System.Collections.Specialized;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.ModelBinding;

namespace OpenKendoBinder.ModelBinder.Api
{
    public class DataSourceApiModelBinder : IModelBinder
    {
        private NameValueCollection _queryString;

        public bool BindModel(HttpActionContext actionContext, ModelBindingContext bindingContext)
        {
            if (actionContext == null)
            {
                throw new ArgumentNullException("actionContext");
            }
            if (bindingContext == null)
            {
                throw new ArgumentNullException("bindingContext");
            }

            string content = actionContext.Request.Content.ReadAsStringAsync().Result;

            try
            {
                // Try to parse as Json
                bindingContext.Model = GridHelper.Parse(content);
            }
            catch (Exception)
            {
                // Parse the QueryString
                _queryString = GetQueryString(content);
                bindingContext.Model = GridHelper.Parse<DataSourceApiRequest>(_queryString);
            }

            return true;
        }

        private NameValueCollection GetQueryString(string content)
        {
            return HttpUtility.ParseQueryString(content);
        }
    }
}