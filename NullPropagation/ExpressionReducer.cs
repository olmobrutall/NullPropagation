using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace NullPropagation
{
    public class ExpressionReducer : ExpressionVisitor
    {
        public static Expression Reduce(Expression exp)
        {
            return new ExpressionReducer().Visit(exp); 
        }

        public override Expression Visit(Expression node)
        {
            if (node.CanReduce)
                return Visit(node.Reduce()); 

            return base.Visit(node);
        }
    }
}
