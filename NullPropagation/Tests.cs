using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NullPropagation
{
    public class Tests
    {
        [Fact]
        public void ToStringTest()
        {
            Expression<Func<string, string>> func = s => s == null ? null : s.ToString();

            Expression exp = NullPropagationRecoverer.RecoverNullPropagation(func);

            Assert.Equal("s => s?.ToString()", exp.ToString());
        }

        [Fact]
        public void NodeTest()
        {
            Expression<Func<Node, Node>> func = n =>
                n == null ? null :
                n.Next == null ? null :
                n.Next.Next == null ? null :
                n.Next.Next.Next.Next; //One more

            Expression exp = NullPropagationRecoverer.RecoverNullPropagation(func);

            Assert.Equal("n => n?.Next?.Next?.Next.Next", exp.ToString());
        }

        public class Node
        {
            public int Id; 
            public Node Next; 
        }

        [Fact]
        public void ValueType()
        {
            Expression<Func<Node, int?>> func = n =>
                n == null ? null :
                n.Next == null ? null :
                n.Next.Next == null ? null :
                (int?)n.Next.Next.Next.Next.Id; //One more

            Expression exp = NullPropagationRecoverer.RecoverNullPropagation(func);

            Assert.Equal("n => n?.Next?.Next?.Next.Next.Id", exp.ToString());
        }

        [Fact]
        public void ValueTypeReduce()
        {
            Expression<Func<Node, int?>> func = n =>
                n == null ? null :
                n.Next == null ? null :
                n.Next.Next == null ? null :
                (int?)n.Next.Next.Next.Next.Id; //One more

            Expression exp = NullPropagationRecoverer.RecoverNullPropagation(func);

            Expression reduced = ExpressionReducer.Reduce(exp);

            Assert.Equal(func.ToString(), reduced.ToString()); 
        }
    }

}
