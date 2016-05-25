// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Web.Http.Dispatcher;
using System.Web.Http.OData.Properties;
using System.Web.Http.OData.Query.Expressions;
using System.Web.Http.OData.Query.Translator;
using System.Web.Http.OData.Query.Validators;
using Microsoft.Data.Edm;
using Microsoft.Data.OData.Query;
using Microsoft.Data.OData.Query.SemanticAst;

namespace System.Web.Http.OData.Query
{
    /// <summary>
    /// This defines a $filter OData query option for querying.
    /// </summary>
    public class FilterQueryOption
    {
        private static readonly IAssembliesResolver _defaultAssembliesResolver = new DefaultAssembliesResolver();
        private FilterClause _filterClause;
        private IQueryTranslator _queryTranslator;

        /// <summary>
        /// Initialize a new instance of <see cref="FilterQueryOption"/> based on the raw $filter value and 
        /// an EdmModel from <see cref="ODataQueryContext"/>.
        /// </summary>
        /// <param name="rawValue">The raw value for $filter query. It can be null or empty.</param>
        /// <param name="context">The <see cref="ODataQueryContext"/> which contains the <see cref="IEdmModel"/> and some type information</param>
        /// <param name="queryTranslator"></param>
        public FilterQueryOption(string rawValue, ODataQueryContext context, IQueryTranslator queryTranslator)
        {
            if (context == null)
            {
                throw Error.ArgumentNull("context");
            }

            if (String.IsNullOrEmpty(rawValue))
            {
                throw Error.ArgumentNullOrEmpty("rawValue");
            }

            _queryTranslator = queryTranslator;
            Context = context;
            RawValue = rawValue;
            Validator = new FilterQueryValidator();
        }

        /// <summary>
        ///  Gets the given <see cref="ODataQueryContext"/>.
        /// </summary>
        public ODataQueryContext Context { get; private set; }

        /// <summary>
        /// Gets or sets the Filter Query Validator
        /// </summary>
        public FilterQueryValidator Validator { get; set; }

        /// <summary>
        /// Gets the parsed <see cref="FilterClause"/> for this query option.
        /// </summary>
        public FilterClause FilterClause
        {
            get
            {
                if (_filterClause == null)
                {
                    _filterClause = ODataUriParser.ParseFilter(RawValue, Context.Model, Context.ElementType);
                }

                return _filterClause;
            }
        }

        /// <summary>
        ///  Gets the raw $filter value.
        /// </summary>
        public string RawValue { get; private set; }

        /// <summary>
        /// Apply the filter query to the given IQueryable.
        /// </summary>
        /// <remarks>
        /// The <see cref="ODataQuerySettings.HandleNullPropagation"/> property specifies
        /// how this method should handle null propagation.
        /// </remarks>
        /// <param name="query">The original <see cref="IQueryable"/>.</param>
        /// <param name="querySettings">The <see cref="ODataQuerySettings"/> that contains all the query application related settings.</param>
        /// <returns>The new <see cref="IQueryable"/> after the filter query has been applied to.</returns>
        public IQueryable ApplyTo(IQueryable query, ODataQuerySettings querySettings)
        {
            return ApplyTo(query, querySettings, _defaultAssembliesResolver);
        }

        /// <summary>
        /// Apply the filter query to the given IQueryable.
        /// </summary>
        /// <remarks>
        /// The <see cref="ODataQuerySettings.HandleNullPropagation"/> property specifies
        /// how this method should handle null propagation.
        /// </remarks>
        /// <param name="query">The original <see cref="IQueryable"/>.</param>
        /// <param name="querySettings">The <see cref="ODataQuerySettings"/> that contains all the query application related settings.</param>
        /// <param name="assembliesResolver">The <see cref="IAssembliesResolver"/> to use.</param>
        /// <returns>The new <see cref="IQueryable"/> after the filter query has been applied to.</returns>
        public IQueryable ApplyTo(IQueryable query, ODataQuerySettings querySettings, IAssembliesResolver assembliesResolver)
        {
            if (query == null)
            {
                throw Error.ArgumentNull("query");
            }
            if (querySettings == null)
            {
                throw Error.ArgumentNull("querySettings");
            }
            if (assembliesResolver == null)
            {
                throw Error.ArgumentNull("assembliesResolver");
            }
            if (Context.ElementClrType == null)
            {
                throw Error.NotSupported(SRResources.ApplyToOnUntypedQueryOption, "ApplyTo");
            }

            FilterClause filterClause = FilterClause;
            Contract.Assert(filterClause != null);

            // Ensure we have decided how to handle null propagation
            ODataQuerySettings updatedSettings = querySettings;
            if (querySettings.HandleNullPropagation == HandleNullPropagationOption.Default)
            {
                updatedSettings = new ODataQuerySettings(updatedSettings);
                updatedSettings.HandleNullPropagation = HandleNullPropagationOptionHelper.GetDefaultHandleNullPropagationOption(query);
            }

            Expression filter = FilterBinder.Bind(filterClause, Context.ElementClrType, Context.Model, assembliesResolver, updatedSettings);
            query = ExpressionHelpers.Where(query, filter, Context.ElementClrType);
            return query;
        }


        /// <summary>
        /// </summary>
        /// <param name="query"></param>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TDestination"></typeparam>
        /// <returns></returns>
        public IQueryable<TSource> Translate<TSource, TDestination>(IQueryable<TSource> query)
        {
            return Translate<TSource, TDestination>(query, new ODataQuerySettings(), _defaultAssembliesResolver);
        }

        /// <summary>
        /// </summary>
        /// <param name="query"></param>
        /// <param name="querySettings"></param>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TDestination"></typeparam>
        /// <returns></returns>
        public IQueryable<TSource> Translate<TSource, TDestination>(IQueryable<TSource> query, ODataQuerySettings querySettings)
        {
            return Translate<TSource, TDestination>(query, querySettings, _defaultAssembliesResolver);
        }

        /// <summary>
        /// </summary>
        /// <param name="query"></param>
        /// <param name="querySettings"></param>
        /// <param name="assembliesResolver"></param>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TDestination"></typeparam>
        /// <returns></returns>
        public IQueryable<TSource> Translate<TSource, TDestination>(IQueryable<TSource> query, ODataQuerySettings querySettings, IAssembliesResolver assembliesResolver)
        {
            var translator = new ExpressionTranslator<TSource, TDestination>(_queryTranslator);
            var translatedFilter = translator.TranslateFilter(FilterClause.Expression);

            ODataQuerySettings updatedSettings = querySettings;
            if (querySettings.HandleNullPropagation == HandleNullPropagationOption.Default)
            {
                updatedSettings = new ODataQuerySettings(updatedSettings);
                updatedSettings.HandleNullPropagation = HandleNullPropagationOptionHelper.GetDefaultHandleNullPropagationOption(query);
            }

            Expression filter = FilterBinder.Bind(translatedFilter, typeof(TSource), Context.Model, assembliesResolver, updatedSettings);
            query = ExpressionHelpers.Where(query, filter, typeof(TSource)) as IQueryable<TSource>;
            return query;
        }


        /// <summary>
        /// Validate the filter query based on the given <paramref name="validationSettings"/>. It throws an ODataException if validation failed.
        /// </summary>
        /// <param name="validationSettings">The <see cref="ODataValidationSettings"/> instance which contains all the validation settings.</param>
        public void Validate(ODataValidationSettings validationSettings)
        {
            if (validationSettings == null)
            {
                throw Error.ArgumentNull("validationSettings");
            }

            if (Validator != null)
            {
                Validator.Validate(this, validationSettings);
            }
        }
    }
}
