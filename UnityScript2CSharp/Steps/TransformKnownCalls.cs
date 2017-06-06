using System;
using Boo.Lang.Compiler.Ast;
using Boo.Lang.Compiler.Steps;

namespace UnityScript2CSharp.Steps
{
    [Serializable]
    public class TransformKnownCalls : AbstractTransformerCompilerStep
    {
        protected bool isBinaryExp;
        public override void Run()
        {
            Visit(CompileUnit);
        }

        public override void LeaveBinaryExpression(BinaryExpression node)
        {
            Expression left;
            string value;
            if (node != null && node.Operator == BinaryOperatorType.Assign)
            {
                left = node.Left;
                if (node.Right is MethodInvocationExpression)
                {
                    var methodInvocationExpression = (MethodInvocationExpression)node.Right;
                    if (methodInvocationExpression.Target is MemberReferenceExpression)
                    {
                        var memberReferenceExpression = (MemberReferenceExpression)methodInvocationExpression.Target;
                        var target = memberReferenceExpression.Target;
                        if (memberReferenceExpression.Name == "AddComponent" && 1 == methodInvocationExpression.Arguments.Count && methodInvocationExpression.Arguments[0] is StringLiteralExpression)
                        {
                            var stringLiteralExpression = (StringLiteralExpression)methodInvocationExpression.Arguments[0];
                            value = stringLiteralExpression.Value;
                            var binaryExpression = new BinaryExpression(LexicalInfo.Empty);
                            binaryExpression.Operator = BinaryOperatorType.Assign;
                            binaryExpression.Left = Expression.Lift(left);
                            var arg_1D2_0 = binaryExpression;
                            var tryCastExpression = new TryCastExpression(LexicalInfo.Empty);
                            var arg_1B1_0 = tryCastExpression;
                            var methodInvocationExpression2 = new MethodInvocationExpression(LexicalInfo.Empty);
                            var arg_182_0 = methodInvocationExpression2;
                            var memberReferenceExpression2 = new MemberReferenceExpression(LexicalInfo.Empty)
                            {
                                Name = "AddComponent",
                                Target = Expression.Lift(target)
                            };
                            arg_182_0.Target = memberReferenceExpression2;
                            methodInvocationExpression2.Arguments = ExpressionCollection.FromArray(new Expression[]
                            {
                                Expression.Lift(value)
                            });
                            arg_1B1_0.Target = methodInvocationExpression2;
                            tryCastExpression.Type = TypeReference.Lift(value);
                            arg_1D2_0.Right = tryCastExpression;
                            ReplaceCurrentNode(binaryExpression);
                            isBinaryExp = true;
                            return;
                        }
                    }
                }
            }
            if (node.Operator != BinaryOperatorType.Assign) return;
            left = node.Left;
            if (!(node.Right is MethodInvocationExpression)) return;
            var methodInvocationExpression3 = (MethodInvocationExpression)node.Right;
            if (!(methodInvocationExpression3.Target is ReferenceExpression)) return;
            var referenceExpression = (ReferenceExpression)methodInvocationExpression3.Target;
            if (referenceExpression.Name != "AddComponent" ||
                1 != methodInvocationExpression3.Arguments.Count ||
                !(methodInvocationExpression3.Arguments[0] is StringLiteralExpression))
                return;
            var stringLiteralExpression2 = (StringLiteralExpression)methodInvocationExpression3.Arguments[0];
            value = stringLiteralExpression2.Value;
            var binaryExpression2 = new BinaryExpression(LexicalInfo.Empty)
            {
                Operator = BinaryOperatorType.Assign,
                Left = Expression.Lift(left)
            };
            var arg_3A3_0 = binaryExpression2;
            var tryCastExpression2 = new TryCastExpression(LexicalInfo.Empty);
            var arg_382_0 = tryCastExpression2;
            var methodInvocationExpression4 = new MethodInvocationExpression(LexicalInfo.Empty);
            var arg_353_0 = methodInvocationExpression4;
            var referenceExpression2 = new ReferenceExpression(LexicalInfo.Empty) {Name = "AddComponent"};
            arg_353_0.Target = referenceExpression2;
            methodInvocationExpression4.Arguments = ExpressionCollection.FromArray(new Expression[]
            {
                Expression.Lift(value)
            });
            arg_382_0.Target = methodInvocationExpression4;
            tryCastExpression2.Type = TypeReference.Lift(value);
            arg_3A3_0.Right = tryCastExpression2;
            ReplaceCurrentNode(binaryExpression2);
            isBinaryExp = true;
        }

        public override void LeaveMethodInvocationExpression(MethodInvocationExpression node)
        {
            if (node != null && node.Target is MemberReferenceExpression)
            {
                var memberReferenceExpression = (MemberReferenceExpression)node.Target;
                Expression target = memberReferenceExpression.Target;
                if (memberReferenceExpression.Name == "GetComponent" && 1 == node.Arguments.Count && node.Arguments[0] is StringLiteralExpression)
                {
                    var stringLiteralExpression = (StringLiteralExpression)node.Arguments[0];
                    string value = stringLiteralExpression.Value;
                    var tryCastExpression = new TryCastExpression(LexicalInfo.Empty);
                    var arg_13A_0 = tryCastExpression;
                    var methodInvocationExpression = new MethodInvocationExpression(LexicalInfo.Empty);
                    var arg_10B_0 = methodInvocationExpression;
                    var memberReferenceExpression2 = new MemberReferenceExpression(LexicalInfo.Empty)
                    {
                        Name = "GetComponent",
                        Target = Expression.Lift(target)
                    };
                    arg_10B_0.Target = memberReferenceExpression2;
                    methodInvocationExpression.Arguments = ExpressionCollection.FromArray(new Expression[]
                    {
                        Expression.Lift(value)
                    });
                    arg_13A_0.Target = methodInvocationExpression;
                    tryCastExpression.Type = TypeReference.Lift(value);
                    ReplaceCurrentNode(tryCastExpression);
                    return;
                }
            }
            if (node != null && node.Target is ReferenceExpression)
            {
                var referenceExpression = (ReferenceExpression)node.Target;
                if (referenceExpression.Name == "GetComponent" && 1 == node.Arguments.Count && node.Arguments[0] is StringLiteralExpression)
                {
                    var stringLiteralExpression2 = (StringLiteralExpression)node.Arguments[0];
                    string value = stringLiteralExpression2.Value;
                    var tryCastExpression2 = new TryCastExpression(LexicalInfo.Empty);
                    TryCastExpression arg_280_0 = tryCastExpression2;
                    var methodInvocationExpression2 = new MethodInvocationExpression(LexicalInfo.Empty);
                    MethodInvocationExpression arg_251_0 = methodInvocationExpression2;
                    var referenceExpression2 = new ReferenceExpression(LexicalInfo.Empty) {Name = "GetComponent"};
                    arg_251_0.Target = referenceExpression2;
                    methodInvocationExpression2.Arguments = ExpressionCollection.FromArray(new Expression[]
                    {
                        Expression.Lift(value)
                    });
                    arg_280_0.Target = methodInvocationExpression2;
                    tryCastExpression2.Type = TypeReference.Lift(value);
                    ReplaceCurrentNode(tryCastExpression2);
                    return;
                }
            }
            if (node != null && node.Target is MemberReferenceExpression)
            {
                var memberReferenceExpression3 = (MemberReferenceExpression)node.Target;
                Expression target = memberReferenceExpression3.Target;
                if (memberReferenceExpression3.Name == "GetComponent" && 1 == node.Arguments.Count && node.Arguments[0] is ReferenceExpression)
                {
                    var e = (ReferenceExpression)node.Arguments[0];
                    var methodInvocationExpression3 = new MethodInvocationExpression(LexicalInfo.Empty);
                    MethodInvocationExpression arg_3D8_0 = methodInvocationExpression3;
                    var genericReferenceExpression = new GenericReferenceExpression(LexicalInfo.Empty);
                    GenericReferenceExpression arg_3A9_0 = genericReferenceExpression;
                    var memberReferenceExpression4 = new MemberReferenceExpression(LexicalInfo.Empty)
                    {
                        Name = "GetComponent",
                        Target = Expression.Lift(target)
                    };
                    arg_3A9_0.Target = memberReferenceExpression4;
                    genericReferenceExpression.GenericArguments = TypeReferenceCollection.FromArray(new TypeReference[]
                    {
                        TypeReference.Lift(e)
                    });
                    arg_3D8_0.Target = genericReferenceExpression;
                    ReplaceCurrentNode(methodInvocationExpression3);
                    return;
                }
            }
            if (node != null && node.Target is ReferenceExpression)
            {
                var referenceExpression3 = (ReferenceExpression)node.Target;
                if (referenceExpression3.Name == "GetComponent" && 1 == node.Arguments.Count && node.Arguments[0] is ReferenceExpression)
                {
                    var e = (ReferenceExpression)node.Arguments[0];
                    var methodInvocationExpression4 = new MethodInvocationExpression(LexicalInfo.Empty);
                    var arg_4F9_0 = methodInvocationExpression4;
                    var genericReferenceExpression2 = new GenericReferenceExpression(LexicalInfo.Empty);
                    var arg_4CA_0 = genericReferenceExpression2;
                    var referenceExpression4 = new ReferenceExpression(LexicalInfo.Empty);
                    referenceExpression4.Name = "GetComponent";
                    arg_4CA_0.Target = referenceExpression4;
                    genericReferenceExpression2.GenericArguments = TypeReferenceCollection.FromArray(new TypeReference[]
                    {
                        TypeReference.Lift(e)
                    });
                    arg_4F9_0.Target = genericReferenceExpression2;
                    ReplaceCurrentNode(methodInvocationExpression4);
                    return;
                }
            }
            if (node != null && node.Target is MemberReferenceExpression)
            {
                var memberReferenceExpression5 = (MemberReferenceExpression)node.Target;
                Expression target = memberReferenceExpression5.Target;
                if (memberReferenceExpression5.Name == "GetComponents" && 1 == node.Arguments.Count && node.Arguments[0] is ReferenceExpression)
                {
                    var e = (ReferenceExpression)node.Arguments[0];
                    var methodInvocationExpression5 = new MethodInvocationExpression(LexicalInfo.Empty);
                    var arg_63D_0 = methodInvocationExpression5;
                    var genericReferenceExpression3 = new GenericReferenceExpression(LexicalInfo.Empty);
                    var arg_60E_0 = genericReferenceExpression3;
                    var memberReferenceExpression6 = new MemberReferenceExpression(LexicalInfo.Empty)
                    {
                        Name = "GetComponents",
                        Target = Expression.Lift(target)
                    };
                    arg_60E_0.Target = memberReferenceExpression6;
                    genericReferenceExpression3.GenericArguments = TypeReferenceCollection.FromArray(new TypeReference[]
                    {
                        TypeReference.Lift(e)
                    });
                    arg_63D_0.Target = genericReferenceExpression3;
                    ReplaceCurrentNode(methodInvocationExpression5);
                    return;
                }
            }
            if (node != null && node.Target is ReferenceExpression)
            {
                var referenceExpression5 = (ReferenceExpression)node.Target;
                if (referenceExpression5.Name == "GetComponents" && 1 == node.Arguments.Count && node.Arguments[0] is ReferenceExpression)
                {
                    var e = (ReferenceExpression)node.Arguments[0];
                    var methodInvocationExpression6 = new MethodInvocationExpression(LexicalInfo.Empty);
                    var arg_75E_0 = methodInvocationExpression6;
                    var genericReferenceExpression4 = new GenericReferenceExpression(LexicalInfo.Empty);
                    var arg_72F_0 = genericReferenceExpression4;
                    var referenceExpression6 = new ReferenceExpression(LexicalInfo.Empty) {Name = "GetComponents"};
                    arg_72F_0.Target = referenceExpression6;
                    genericReferenceExpression4.GenericArguments = TypeReferenceCollection.FromArray(new TypeReference[]
                    {
                        TypeReference.Lift(e)
                    });
                    arg_75E_0.Target = genericReferenceExpression4;
                    ReplaceCurrentNode(methodInvocationExpression6);
                    return;
                }
            }
            if (node != null && node.Target is MemberReferenceExpression)
            {
                var memberReferenceExpression7 = (MemberReferenceExpression)node.Target;
                Expression target = memberReferenceExpression7.Target;
                if (memberReferenceExpression7.Name == "GetComponentsInChildren" && 1 == node.Arguments.Count && node.Arguments[0] is ReferenceExpression)
                {
                    var e = (ReferenceExpression)node.Arguments[0];
                    var methodInvocationExpression7 = new MethodInvocationExpression(LexicalInfo.Empty);
                    var arg_8A2_0 = methodInvocationExpression7;
                    var genericReferenceExpression5 = new GenericReferenceExpression(LexicalInfo.Empty);
                    var arg_873_0 = genericReferenceExpression5;
                    var memberReferenceExpression8 = new MemberReferenceExpression(LexicalInfo.Empty)
                    {
                        Name = "GetComponentsInChildren",
                        Target = Expression.Lift(target)
                    };
                    arg_873_0.Target = memberReferenceExpression8;
                    genericReferenceExpression5.GenericArguments = TypeReferenceCollection.FromArray(new TypeReference[]
                    {
                        TypeReference.Lift(e)
                    });
                    arg_8A2_0.Target = genericReferenceExpression5;
                    ReplaceCurrentNode(methodInvocationExpression7);
                    return;
                }
            }
            if (node != null && node.Target is ReferenceExpression)
            {
                var referenceExpression7 = (ReferenceExpression)node.Target;
                if (referenceExpression7.Name == "GetComponentsInChildren" && 1 == node.Arguments.Count && node.Arguments[0] is ReferenceExpression)
                {
                    var e = (ReferenceExpression)node.Arguments[0];
                    var methodInvocationExpression8 = new MethodInvocationExpression(LexicalInfo.Empty);
                    MethodInvocationExpression arg_9C3_0 = methodInvocationExpression8;
                    var genericReferenceExpression6 = new GenericReferenceExpression(LexicalInfo.Empty);
                    GenericReferenceExpression arg_994_0 = genericReferenceExpression6;
                    var referenceExpression8 = new ReferenceExpression(LexicalInfo.Empty)
                    {
                        Name = "GetComponentsInChildren"
                    };
                    arg_994_0.Target = referenceExpression8;
                    genericReferenceExpression6.GenericArguments = TypeReferenceCollection.FromArray(new TypeReference[]
                    {
                        TypeReference.Lift(e)
                    });
                    arg_9C3_0.Target = genericReferenceExpression6;
                    ReplaceCurrentNode(methodInvocationExpression8);
                    return;
                }
            }
            if (node != null && node.Target is MemberReferenceExpression)
            {
                var memberReferenceExpression9 = (MemberReferenceExpression)node.Target;
                Expression target = memberReferenceExpression9.Target;
                if (memberReferenceExpression9.Name == "GetComponentInChildren" && 1 == node.Arguments.Count && node.Arguments[0] is ReferenceExpression)
                {
                    var e = (ReferenceExpression)node.Arguments[0];
                    var methodInvocationExpression9 = new MethodInvocationExpression(LexicalInfo.Empty);
                    var arg_B07_0 = methodInvocationExpression9;
                    var genericReferenceExpression7 = new GenericReferenceExpression(LexicalInfo.Empty);
                    var arg_AD8_0 = genericReferenceExpression7;
                    var memberReferenceExpression10 = new MemberReferenceExpression(LexicalInfo.Empty);
                    memberReferenceExpression10.Name = "GetComponentInChildren";
                    memberReferenceExpression10.Target = Expression.Lift(target);
                    arg_AD8_0.Target = memberReferenceExpression10;
                    genericReferenceExpression7.GenericArguments = TypeReferenceCollection.FromArray(new TypeReference[]
                    {
                        TypeReference.Lift(e)
                    });
                    arg_B07_0.Target = genericReferenceExpression7;
                    ReplaceCurrentNode(methodInvocationExpression9);
                    return;
                }
            }
            if (node != null && node.Target is ReferenceExpression)
            {
                var referenceExpression9 = (ReferenceExpression)node.Target;
                if (referenceExpression9.Name == "GetComponentInChildren" && 1 == node.Arguments.Count && node.Arguments[0] is ReferenceExpression)
                {
                    var e = (ReferenceExpression)node.Arguments[0];
                    var methodInvocationExpression10 = new MethodInvocationExpression(LexicalInfo.Empty);
                    var arg_C28_0 = methodInvocationExpression10;
                    var genericReferenceExpression8 = new GenericReferenceExpression(LexicalInfo.Empty);
                    var arg_BF9_0 = genericReferenceExpression8;
                    var referenceExpression10 = new ReferenceExpression(LexicalInfo.Empty)
                    {
                        Name = "GetComponentInChildren"
                    };
                    arg_BF9_0.Target = referenceExpression10;
                    genericReferenceExpression8.GenericArguments = TypeReferenceCollection.FromArray(new TypeReference[]
                    {
                        TypeReference.Lift(e)
                    });
                    arg_C28_0.Target = genericReferenceExpression8;
                    ReplaceCurrentNode(methodInvocationExpression10);
                    return;
                }
            }
            if (node != null && node.Target is MemberReferenceExpression)
            {
                var memberReferenceExpression11 = (MemberReferenceExpression)node.Target;
                var target = memberReferenceExpression11.Target;
                if (memberReferenceExpression11.Name == "AddComponent" && 1 == node.Arguments.Count && node.Arguments[0] is StringLiteralExpression)
                {
                    var stringLiteralExpression3 = (StringLiteralExpression)node.Arguments[0];
                    string value = stringLiteralExpression3.Value;
                    if (!isBinaryExp)
                    {
                        var methodInvocationExpression11 = new MethodInvocationExpression(LexicalInfo.Empty);
                        var arg_D49_0 = methodInvocationExpression11;
                        var memberReferenceExpression12 = new MemberReferenceExpression(LexicalInfo.Empty)
                        {
                            Name = "AddComponent",
                            Target = Expression.Lift(target)
                        };
                        arg_D49_0.Target = memberReferenceExpression12;
                        methodInvocationExpression11.Arguments = ExpressionCollection.FromArray(new Expression[]
                        {
                            Expression.Lift(value)
                        });
                        ReplaceCurrentNode(methodInvocationExpression11);
                        isBinaryExp = false;
                    }
                    return;
                }
            }
            if (node != null && node.Target is ReferenceExpression)
            {
                var referenceExpression11 = (ReferenceExpression)node.Target;
                if (referenceExpression11.Name == "AddComponent" && 1 == node.Arguments.Count && node.Arguments[0] is StringLiteralExpression)
                {
                    var stringLiteralExpression4 = (StringLiteralExpression)node.Arguments[0];
                    string value = stringLiteralExpression4.Value;
                    if (!isBinaryExp)
                    {
                        var methodInvocationExpression12 = new MethodInvocationExpression(LexicalInfo.Empty);
                        var arg_E70_0 = methodInvocationExpression12;
                        var referenceExpression12 = new ReferenceExpression(LexicalInfo.Empty) {Name = "AddComponent"};
                        arg_E70_0.Target = referenceExpression12;
                        methodInvocationExpression12.Arguments = ExpressionCollection.FromArray(new Expression[]
                        {
                            Expression.Lift(value)
                        });
                        ReplaceCurrentNode(methodInvocationExpression12);
                        isBinaryExp = false;
                    }
                    return;
                }
            }
            if (node != null && node.Target is MemberReferenceExpression)
            {
                var memberReferenceExpression13 = (MemberReferenceExpression)node.Target;
                var target = memberReferenceExpression13.Target;
                if (memberReferenceExpression13.Name == "AddComponent" && 1 == node.Arguments.Count && node.Arguments[0] is ReferenceExpression)
                {
                    var e = (ReferenceExpression)node.Arguments[0];
                    var methodInvocationExpression13 = new MethodInvocationExpression(LexicalInfo.Empty);
                    var arg_FDD_0 = methodInvocationExpression13;
                    var genericReferenceExpression9 = new GenericReferenceExpression(LexicalInfo.Empty);
                    var arg_FAE_0 = genericReferenceExpression9;
                    var memberReferenceExpression14 = new MemberReferenceExpression(LexicalInfo.Empty);
                    memberReferenceExpression14.Name = "AddComponent";
                    memberReferenceExpression14.Target = Expression.Lift(target);
                    arg_FAE_0.Target = memberReferenceExpression14;
                    genericReferenceExpression9.GenericArguments = TypeReferenceCollection.FromArray(new TypeReference[]
                    {
                        TypeReference.Lift(e)
                    });
                    arg_FDD_0.Target = genericReferenceExpression9;
                    ReplaceCurrentNode(methodInvocationExpression13);
                    return;
                }
            }
            if (node != null && node.Target is ReferenceExpression)
            {
                var referenceExpression13 = (ReferenceExpression)node.Target;
                if (referenceExpression13.Name == "AddComponent" && 1 == node.Arguments.Count && node.Arguments[0] is ReferenceExpression)
                {
                    var e = (ReferenceExpression)node.Arguments[0];
                    var methodInvocationExpression14 = new MethodInvocationExpression(LexicalInfo.Empty);
                    var arg_10FE_0 = methodInvocationExpression14;
                    var genericReferenceExpression10 = new GenericReferenceExpression(LexicalInfo.Empty);
                    var arg_10CF_0 = genericReferenceExpression10;
                    var referenceExpression14 = new ReferenceExpression(LexicalInfo.Empty) {Name = "AddComponent"};
                    arg_10CF_0.Target = referenceExpression14;
                    genericReferenceExpression10.GenericArguments = TypeReferenceCollection.FromArray(new TypeReference[]
                    {
                        TypeReference.Lift(e)
                    });
                    arg_10FE_0.Target = genericReferenceExpression10;
                    ReplaceCurrentNode(methodInvocationExpression14);
                    return;
                }
            }
            if (node != null && node.Target is ReferenceExpression)
            {
                var referenceExpression15 = (ReferenceExpression)node.Target;
                if (referenceExpression15.Name == "Instantiate" && 3 == node.Arguments.Count)
                {
                    var expression34 = node.Arguments[0];
                    var expressionType = expression34.ExpressionType;
                    if (expressionType != null)
                    {
                        var tryCastExpression3 = new TryCastExpression(LexicalInfo.Empty)
                        {
                            Target = Expression.Lift(node),
                            Type = TypeReference.Lift(CodeBuilder.CreateTypeReference(expressionType))
                        };
                        ReplaceCurrentNode(tryCastExpression3);
                    }
                }
            }
        }
    }
}
