using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace LambdaTest
{
    public class Program
    {
        public static void Main()
        {
            Expression<Func<City, int?>> countryNameLength = s => s?.State?.Country?.Name?.Length;
            Console.WriteLine(countryNameLength.ToString());

            var f1 = countryNameLength.Compile();

            Console.WriteLine("null:" + f1(null));
            Console.WriteLine("null:" + f1(new City { }));
            Console.WriteLine("null:" + f1(new City { State = new State {  } }));
            Console.WriteLine("null:" + f1(new City { State = new State { Country = new Country() { Name = null } } }));
            Console.WriteLine("4:" + f1(new City { State = new State { Country = new Country() { Name = "hola" } } }));

            Console.ReadLine();
        }
    }

    class City
    {
        public State State ; 
    }

    class State
    {
        public Country Country;
    }

    public class Country
    {
        public string Name;
    }
}

namespace System.Linq.Expressions
{
    public class ExpressionCSharp60
    {
        public static NullPropagationExpression NullPropagation(Expression receiver, ParameterExpression accessParameter, Expression accessExpression)
        {
            return new NullPropagationExpression(receiver, accessParameter, accessExpression);
        }
    }

    static class NullableExtensions
    {
        public static Type Nullify(this Type type)
        {
            if (type.IsValueType && Nullable.GetUnderlyingType(type) == null)
                return typeof(Nullable<>).MakeGenericType(type);

            return type;
        }

        public static Type Unnullify(this Type type)
        {
            return Nullable.GetUnderlyingType(type) ?? type;
        }
    }

    public class NullPropagationExpression : Expression
    {
        public Expression Receiver { get; private set; }
        public ParameterExpression AccessParameter { get; private set; }
        public Expression AccessExpression { get; private set; }
        public Type type;

        public NullPropagationExpression(Expression receiver, ParameterExpression accessParameter, Expression accessExpression)
        {
            this.Receiver = receiver;
            this.AccessParameter = accessParameter;
            this.AccessExpression = accessExpression;
            this.type = AccessExpression.Type.Nullify();
        }

        public override ExpressionType NodeType
        {
            get { return ExpressionType.Extension; }
        }

        public override Type Type
        {
            get { return type; }
        }

        public override bool CanReduce
        {
            get { return true; }
        }

        protected override Expression VisitChildren(ExpressionVisitor visitor)
        {
            var receiver = visitor.Visit(Receiver);
            var accessParameter = visitor.Visit(AccessParameter);
            var accessExpression = visitor.Visit(AccessExpression);

            return this.Update(receiver, accessParameter, accessExpression);
        }

        private Expression Update(Expression receiver, Expression accessParameter, Expression accessExpression)
        {
            if (receiver != this.Receiver || accessParameter != this.AccessParameter || accessExpression != this.AccessExpression)
                return new NullPropagationExpression(receiver, AccessParameter, accessExpression);

            return this;
        }

        public override Expression Reduce()
        {
            var fullAccessExpression = ExpressionReplacer.Replace(AccessExpression, AccessParameter,
                    Nullable.GetUnderlyingType(Receiver.Type) == null ? Receiver : Expression.Property(Receiver, "Value"));

            if (fullAccessExpression.Type != this.Type)
                fullAccessExpression = Expression.Convert(fullAccessExpression, this.Type);

            return Expression.Condition(Expression.Equal(Receiver, Expression.Constant(null, Receiver.Type)),
                Expression.Constant(null, Type), fullAccessExpression);
        }

        class ExpressionReplacer : ExpressionVisitor
        {
            Expression find;
            Expression replaceBy;

            internal static Expression Replace(Expression expression, Expression find, Expression replaceBy)
            {
                ExpressionReplacer replacer = new ExpressionReplacer { find = find, replaceBy = replaceBy };

                return replacer.Visit(expression);
            }

            public override Expression Visit(Expression node)
            {
                if (node == find)
                    return replaceBy;

                return base.Visit(node);
            }
        }

        public override string ToString()
        {
            string receiver = Receiver.ToString();

            if (!(Receiver.NodeType == ExpressionType.MemberAccess ||
                Receiver.NodeType == ExpressionType.Call ||
                Receiver.NodeType == ExpressionType.Parameter))
                receiver = "(" + receiver + ")";

            var access = AccessExpression.ToString();
            var start = this.AccessParameter.ToString();
            if (access.StartsWith(start + ".") || access.StartsWith(start + "["))
                return receiver + "?" + access.Substring(start.Length);

            return receiver + "?(" + this.AccessParameter.ToString() + " => " + access + ")";
        }
    }

}
