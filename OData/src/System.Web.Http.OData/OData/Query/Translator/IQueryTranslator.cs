using Microsoft.Data.Edm;

namespace System.Web.Http.OData.Query.Translator
{
    /// <summary>
    /// </summary>
    public interface IQueryTranslator
    {
        /// <summary>
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TDestination"></typeparam>
        /// <returns></returns>
        IEdmType GetEdmType<TSource, TDestination>();

        /// <summary>
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TDestination"></typeparam>
        /// <returns></returns>
        IEdmModel GetEdmModel<TSource, TDestination>();

        /// <summary>
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        string TranslateField<TSource, TDestination>(string name);

        /// <summary>
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        string TranslateLiteralValue<TSource, TDestination>(string fieldName, string value);
    }
}