using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace NullPropagation
{
    class NullPropagationRecoverer : ExpressionVisitor
    {
        public static Expression RecoverNullPropagation(Expression exp)
        {
            return new NullPropagationRecoverer().Visit(exp);
        }

        protected override Expression VisitConditional(ConditionalExpression node)
        {
            var baseNode = (ConditionalExpression)base.VisitConditional(node);

            if (baseNode.Test.NodeType != ExpressionType.Equal && baseNode.Test.NodeType != ExpressionType.NotEqual)
                return baseNode;

            var test = (BinaryExpression)baseNode.Test;
            var receiver =
                IsNull(test.Right) ? test.Left :
                IsNull(test.Left) ? test.Right : null;

            if (receiver == null || IsNull(receiver))
                return baseNode;

            var shouldBeNull = baseNode.Test.NodeType == ExpressionType.Equal ? baseNode.IfTrue : baseNode.IfFalse;
            if (!IsNull(shouldBeNull))
                return baseNode;

            var oldAccessExpression = baseNode.Test.NodeType == ExpressionType.Equal ? baseNode.IfFalse : baseNode.IfTrue;

            var param = Expression.Parameter(Nullable.GetUnderlyingType(receiver.Type) ?? receiver.Type);

            var newAccessExpression = ExpressionReplacerComparing.Replace(oldAccessExpression, receiver, param);

            if (oldAccessExpression == newAccessExpression)
                return baseNode;

            if (newAccessExpression.NodeType == ExpressionType.Convert)
            {
                var conv = (UnaryExpression)newAccessExpression;
                if (conv.Operand.Type != conv.Type && conv.Operand.Type == conv.Type.Unnullify())
                    newAccessExpression = conv.Operand;
            }

            return new NullPropagationExpression(receiver, param, newAccessExpression);
        }

        public bool IsNull(Expression exp)
        {
            return exp.NodeType == ExpressionType.Constant && ((ConstantExpression)exp).Value == null ||
                exp.NodeType == ExpressionType.Convert && IsNull(((UnaryExpression)exp).Operand);
        }
    }

    class ExpressionReplacerComparing : ExpressionVisitor
    {
        Expression find;
        Expression replaceBy;

        internal static Expression Replace(Expression expression, Expression find, Expression replaceBy)
        {
            ExpressionReplacerComparing replacer = new ExpressionReplacerComparing { find = find, replaceBy = replaceBy };

            return replacer.Visit(expression);
        }

        public override Expression Visit(Expression node)
        {
            if (ExpressionComparer.AreEqual(node, find))
                return replaceBy;

            return base.Visit(node);
        }
    }
}
