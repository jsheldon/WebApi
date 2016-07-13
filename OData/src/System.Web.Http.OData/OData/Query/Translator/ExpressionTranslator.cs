using System.Linq;
using System.Text;
using System.Web.Http.OData.Query.Translator;
using Microsoft.Data.Edm;
using Microsoft.Data.OData.Query;
using Microsoft.Data.OData.Query.SemanticAst;

namespace System.Web.Http.OData.Query
{
    /// <summary>
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TDestination"></typeparam>
    internal sealed class ExpressionTranslator<TSource, TDestination>
    {
        private IQueryTranslator _queryTranslator;
        private string _currentNode;

        public ExpressionTranslator(IQueryTranslator queryTranslator)
        {
            _queryTranslator = queryTranslator;
        }

        public FilterClause TranslateFilter(QueryNode expression)
        {
            var translatedExpression = GetTranslatedExpression(expression);
            var model = _queryTranslator.GetEdmModel<TSource, TDestination>();
            var type = _queryTranslator.GetEdmType<TSource, TDestination>();
            return ODataUriParser.ParseFilter(translatedExpression, model, type);
        }

        public OrderByClause TranslateOrder(OrderByClause expression)
        {
            var translatedExpression = GetTranslatedExpression(expression);
            var model = GetEdmModel();
            var type = _queryTranslator.GetEdmType<TSource, TDestination>();
            return ODataUriParser.ParseOrderBy(translatedExpression, model, type);
        }

        public IEdmProperty TranslateProperty(IEdmProperty property)
        {
            var fieldName = _queryTranslator.TranslateField<TSource, TDestination>(property.Name);
            var type = _queryTranslator.GetEdmType<TSource, TDestination>() as IEdmEntityType;
            return type.DeclaredProperties.SingleOrDefault(p => p.Name == fieldName);
        }

        private string GetTranslatedExpression(OrderByClause expression)
        {

            var sb = new StringBuilder();
            Visit(ref sb, expression);
            return sb.ToString();
        }

        private void Visit(ref StringBuilder sb, OrderByDirection clause)
        {
            if (clause == OrderByDirection.Ascending)
                sb.Append(" asc");
            else
                sb.Append(" desc");
        }

        private void Visit(ref StringBuilder sb, OrderByClause clause)
        {
            Visit(ref sb, clause.Expression);
            Visit(ref sb, clause.Direction);
            if (clause.ThenBy != null)
            {
                sb.Append(",");
                Visit(ref sb, clause.ThenBy);
            }
        }

        private string GetTranslatedExpression(QueryNode expression)
        {
            var sb = new StringBuilder();
            Visit(ref sb, expression);
            return sb.ToString();
        }

        private void Visit(ref StringBuilder sb, BinaryOperatorNode expression)
        {
            // Ensure the fieldname is on the left side of the expression when evaluating against a value.
            if (expression.Right is SingleValuePropertyAccessNode)
            {
                Visit(ref sb, expression.Right);
                Visit(ref sb, expression.OperatorKind);
                Visit(ref sb, expression.Left);
            }
            else
            {
                Visit(ref sb, expression.Left);
                Visit(ref sb, expression.OperatorKind);
                Visit(ref sb, expression.Right);
            }
        }

        private void Visit(ref StringBuilder sb, BinaryOperatorKind operatorKind)
        {
            var oper = string.Empty;
            switch (operatorKind)
            {
                case BinaryOperatorKind.Add:
                    oper = "add";
                    break;
                case BinaryOperatorKind.Equal:
                    oper = "eq";
                    break;
                case BinaryOperatorKind.And:
                    oper = "and";
                    break;
                case BinaryOperatorKind.Divide:
                    oper = "div";
                    break;
                case BinaryOperatorKind.GreaterThan:
                    oper = "gt";
                    break;
                case BinaryOperatorKind.GreaterThanOrEqual:
                    oper = "ge";
                    break;
                case BinaryOperatorKind.LessThan:
                    oper = "lt";
                    break;
                case BinaryOperatorKind.LessThanOrEqual:
                    oper = "le";
                    break;
                case BinaryOperatorKind.Modulo:
                    oper = "mod";
                    break;
                case BinaryOperatorKind.Multiply:
                    oper = "mul";
                    break;
                case BinaryOperatorKind.NotEqual:
                    oper = "ne";
                    break;
                case BinaryOperatorKind.Or:
                    oper = "or";
                    break;
                case BinaryOperatorKind.Subtract:
                    oper = "sub";
                    break;
            }

            sb.Append($" {oper} ");
        }

        private void Visit(ref StringBuilder sb, QueryNode node)
        {
            if (node is SingleValuePropertyAccessNode)
                Visit(ref sb, (SingleValuePropertyAccessNode)node);
            else if (node is BinaryOperatorNode)
                Visit(ref sb, (BinaryOperatorNode)node);
            else if (node is ConstantNode)
                Visit(ref sb, (ConstantNode)node);
            else if (node is ConvertNode)
                Visit(ref sb, (ConvertNode)node);
            else if (node is SingleValueFunctionCallNode)
                Visit(ref sb, (SingleValueFunctionCallNode)node);
        }

        private void Visit(ref StringBuilder sb, SingleValuePropertyAccessNode node)
        {
            // Set the current node for use when the value is looked up
            _currentNode = node.Property.Name;
            var fieldName = _queryTranslator.TranslateField<TSource, TDestination>(_currentNode);
            sb.Append(fieldName);
        }

        private void Visit(ref StringBuilder sb, SingleValueFunctionCallNode node)
        {
            var argumentCount = 0;
            sb.Append($"{node.Name}(");
            foreach (var argument in node.Arguments)
            {
                if (argumentCount > 0)
                    sb.Append(", ");

                Visit(ref sb, argument);
                argumentCount++;
            }

            sb.Append(")");
        }

        private void Visit(ref StringBuilder sb, ConstantNode node)
        {
            var value = _queryTranslator.TranslateLiteralValue<TSource, TDestination>(_currentNode, node.LiteralText);
            if (value == null)
                sb.Append($"{node.LiteralText}");
            else
                sb.Append($"{value}");
        }

        private void Visit(ref StringBuilder sb, ConvertNode node)
        {
            Visit(ref sb, node.Source);
        }

        public IEdmModel GetEdmModel()
        {
            return _queryTranslator.GetEdmModel<TSource, TDestination>();
        }
    }
}