﻿//
// IR.cs
//
// Author:
//       Tomona Nanase <nanase@users.noreply.github.com>
//
// The MIT License (MIT)
//
// Copyright (c) 2015 Tomona Nanase
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using Lury.Objects;

namespace Lury.Compiling
{
    class Routine
    {
        private readonly IEnumerable<Statement> statements;

        public Routine(IEnumerable<Statement> statements)
        {
            this.statements = statements;
        }

        public Routine()
        {
            this.statements = Enumerable.Empty<Statement>();
        }

        public LuryObject Evaluate(LuryObject context)
        {
            LuryObject ret = null;

            foreach (var statement in this.statements)
                ret = statement.Evaluate(context);

            return ret;
        }
    }

    abstract class Statement
    {
        public virtual LuryObject Evaluate(LuryObject context)
        {
            return null;
        }
    }

    class IfStatement : Statement
    {
        private readonly Node condition;
        private readonly Routine suite;
        private readonly IfStatement nextIf;

        public IfStatement(Routine elseSuite)
        {
            this.condition = null;
            this.suite = elseSuite;
            this.nextIf = null;
        }

        public IfStatement(Node condition, Routine suite, IfStatement nextIf)
        {
            this.condition = condition;
            this.suite = suite;
            this.nextIf = nextIf;
        }

        public IfStatement(Node condition, Routine suite)
        {
            this.condition = condition;
            this.suite = suite;
            this.nextIf = null;
        }

        public override LuryObject Evaluate(LuryObject context)
        {
            if (this.condition == null)
            {
                // else block
                this.suite.Evaluate(context);
            }
            else
            {
                // if block
                var cond = this.condition.Evaluate(context);

                if (!(cond is LuryBoolean))
                    throw new InvalidOperationException();

                if (cond == LuryBoolean.True)           // if suite
                    this.suite.Evaluate(context);
                else if (this.nextIf != null)           // elif block
                    this.nextIf.Evaluate(context);
            }

            return null;
        }
    }

    class WhileStatement : Statement
    {
        private readonly Node condition;
        private readonly Routine suite;
        private readonly Routine elseSuite;

        public WhileStatement(Node condition, Routine suite, Routine elseSuite)
        {
            this.condition = condition;
            this.suite = suite;
            this.elseSuite = elseSuite;
        }

        public WhileStatement(Node condition, Routine suite)
        {
            this.condition = condition;
            this.suite = suite;
            this.elseSuite = null;
        }

        public override LuryObject Evaluate(LuryObject context)
        {
            while (true)
            {
                var cond = this.condition.Evaluate(context);

                if (!(cond is LuryBoolean))
                    throw new InvalidOperationException();

                // TODO: else block?
                if (cond == LuryBoolean.True)
                    this.suite.Evaluate(context);
                else
                    break;
            }
            
            return null;
        }
    }

    class ExpressionStatement : Statement
    {
        private readonly Node expression;

        public ExpressionStatement(Node expression)
        {
            this.expression = expression;
        }

        public override LuryObject Evaluate(LuryObject context)
        {
            this.expression.Evaluate(context);
            return null;
        }
    }

    abstract class Node
    {
        public abstract LuryObject Evaluate(LuryObject context);
    }

    class LValueNode : Node
    {
        private readonly string reference;

        public LValueNode(object reference)
        {
            this.reference = ((Lexer.Token)reference).Text;
        }

        public LValueNode(string reference)
        {
            this.reference = reference;
        }

        public override LuryObject Evaluate(LuryObject context)
        {
            return context[this.reference];
        }

        public void Assign(LuryObject value, LuryObject context)
        {
            context[this.reference] = value;
        }
    }

    class ConstantNode : Node
    {
        private readonly LuryObject constant;

        public ConstantNode(LuryObject constant)
        {
            this.constant = constant;
        }
    
        public override LuryObject Evaluate(LuryObject context)
        {
            return this.constant;
        }
    }

    class UnaryNode : Node
    {
        private readonly Node target;
        private readonly UnaryOperator operation;

        public UnaryNode(Node target, UnaryOperator operation)
        {
            this.target = target;
            this.operation = operation;
        }

        public override LuryObject Evaluate(LuryObject context)
        {
            var value = this.target.Evaluate(context);

            switch (this.operation)
            {
                case UnaryOperator.SignNegative:
                    return value.Neg();

                case UnaryOperator.SignPositive:
                    return value.Pos();

                case UnaryOperator.BitwiseNot:
                    return value.BNot();

                case UnaryOperator.LogicalNot:
                    return value.LNot();

                default:
                    throw new InvalidOperationException();
            }
        }
    }

    class UnaryAssignNode : Node
    {
        private readonly Node target;
        private readonly UnaryAssignOperator operation;

        public UnaryAssignNode(Node target, UnaryAssignOperator operation)
        {
            this.target = target;
            this.operation = operation;
        }

        public override LuryObject Evaluate(LuryObject context)
        {
            if (!(this.target is LValueNode))
                throw new InvalidOperationException();

            var lvalue = (LValueNode)this.target;
            var dr_value = lvalue.Evaluate(context);

            switch (this.operation)
            {
                case UnaryAssignOperator.IncrementPostfix:
                    lvalue.Assign(dr_value.Inc(), context);
                    return dr_value;

                case UnaryAssignOperator.DecrementPostfix:
                    lvalue.Assign(dr_value.Dec(), context);
                    return dr_value;

                case UnaryAssignOperator.IncrementPrefix:
                    dr_value = dr_value.Inc();
                    lvalue.Assign(dr_value, context);
                    return dr_value;

                case UnaryAssignOperator.DecrementPrefix:
                    dr_value = dr_value.Dec();
                    lvalue.Assign(dr_value, context);
                    return dr_value;

                default:
                    throw new InvalidOperationException();
            }
        }
    }

    class BinaryNode : Node
    {
        private readonly Node x;
        private readonly Node y;
        private readonly BinaryOperator operation;

        public BinaryNode(Node x, Node y, BinaryOperator operation)
        {
            this.x = x;
            this.y = y;
            this.operation = operation;
        }

        public override LuryObject Evaluate(LuryObject context)
        {
            var x = this.x.Evaluate(context);
            var y = this.y.Evaluate(context);

            switch (this.operation)
            {
                case BinaryOperator.Power:
                    return x.Pow(y);

                case BinaryOperator.Multiplication:
                    return x.Mul(y);

                case BinaryOperator.Division:
                    return x.Div(y);

                case BinaryOperator.IntDivision:
                    return x.IDiv(y);

                case BinaryOperator.Modulo:
                    return x.Mod(y);

                case BinaryOperator.Addition:
                    return x.Add(y);

                case BinaryOperator.Subtraction:
                    return x.Sub(y);

                case BinaryOperator.Concatenation:
                    return x.Con(y);

                case BinaryOperator.LeftShift:
                    return x.Shl(y);

                case BinaryOperator.RightShift:
                    return x.Shl(y);

                case BinaryOperator.ArithmeticAnd:
                    return x.BAnd(y);

                case BinaryOperator.ArithmeticXor:
                    return x.BXor(y);

                case BinaryOperator.ArithmeticOr:
                    return x.BOr(y);

                case BinaryOperator.LogicalAnd:
                    return x.LAnd(y);

                case BinaryOperator.LogicalOr:
                    return x.LOr(y);

                case BinaryOperator.LessThan:
                    return x.CLt(y);

                case BinaryOperator.GreaterThan:
                    return x.CGt(y);

                case BinaryOperator.LessThanEqual:
                    return x.CELt(y);

                case BinaryOperator.GreaterThanEqual:
                    return x.CEGt(y);

                case BinaryOperator.Equal:
                    return x.CEq(y);

                case BinaryOperator.NotEqual:
                    return x.CNe(y);

                case BinaryOperator.Is:
                    return x.Is(y);

                case BinaryOperator.IsNot:
                    return x.IsNot(y);

                default:
                    throw new InvalidOperationException();
            }
        }
    }

    class BinaryAssignNode : Node
    {
        private readonly Node lvalue;
        private readonly Node rvalue;
        private readonly BinaryAssignOperator operation;

        public BinaryAssignNode(Node lvalue, Node rvalue, BinaryAssignOperator operation)
        {
            this.lvalue = lvalue;
            this.rvalue = rvalue;
            this.operation = operation;
        }

        public override LuryObject Evaluate(LuryObject context)
        {
            if (!(this.lvalue is LValueNode))
                throw new InvalidOperationException();

            var dst = (LValueNode)lvalue;
            var value = this.rvalue.Evaluate(context);

            if (this.operation == BinaryAssignOperator.Assign)
            {
                dst.Assign(value, context);
                return value;
            }

            var dstValue = dst.Evaluate(context);

            switch (this.operation)
            {
                case BinaryAssignOperator.Power:
                    value = dstValue.Pow(value);
                    break;

                case BinaryAssignOperator.Multiplication:
                    value = dstValue.Mul(value);
                    break;

                case BinaryAssignOperator.Division:
                    value = dstValue.Div(value);
                    break;

                case BinaryAssignOperator.IntDivision:
                    value = dstValue.IDiv(value);
                    break;

                case BinaryAssignOperator.Modulo:
                    value = dstValue.Mod(value);
                    break;

                case BinaryAssignOperator.Addition:
                    value = dstValue.Add(value);
                    break;

                case BinaryAssignOperator.Subtraction:
                    value = dstValue.Sub(value);
                    break;

                case BinaryAssignOperator.Concatenation:
                    value = dstValue.Con(value);
                    break;

                case BinaryAssignOperator.LeftShift:
                    value = dstValue.Shl(value);
                    break;

                case BinaryAssignOperator.RightShift:
                    value = dstValue.Shr(value);
                    break;

                case BinaryAssignOperator.ArithmeticAnd:
                    value = dstValue.BAnd(value);
                    break;

                case BinaryAssignOperator.ArithmeticXor:
                    value = dstValue.BXor(value);
                    break;

                case BinaryAssignOperator.ArithmeticOr:
                    value = dstValue.BOr(value);
                    break;

                default:
                    throw new InvalidOperationException();
            }

            dst.Assign(value, context);
            return value;
        }
    }

    class CallNode : Node
    {
        private readonly Node function;
        private readonly IEnumerable<Node> param;

        public CallNode(Node function, IEnumerable<Node> param)
        {
            this.function = function;
            this.param = param;
        }

        public CallNode(Node function)
        {
            this.function = function;
            this.param = Enumerable.Empty<Node>();
        }

        public override LuryObject Evaluate(LuryObject context)
        {
            var objects = this.param.Select(p => p == null ? null : p.Evaluate(context)).ToArray();

            return this.function.Evaluate(context).Call(objects);
        }
    }
}
