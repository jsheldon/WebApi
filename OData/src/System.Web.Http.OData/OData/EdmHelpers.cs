using System.Web.Http.OData.Formatter;
using Microsoft.Data.Edm;

namespace System.Web.Http.OData
{
    /// <summary>
    /// </summary>
    public static class EdmHelpers
    {
        /// <summary>
        /// </summary>
        /// <param name="model"></param>
        /// <param name="clrType"></param>
        /// <returns></returns>
        public static IEdmType GetEdmType(this IEdmModel model, Type clrType)
        {
            return EdmLibHelpers.GetEdmType(model, clrType);
        }    
    }
}