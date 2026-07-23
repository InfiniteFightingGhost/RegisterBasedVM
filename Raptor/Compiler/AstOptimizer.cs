namespace Raptor.Compiler
{
    public static class ASTOptimizer
    {
        public static ASTNode OptimizeNode(ASTNode node, ASTNode? parent = null)
        {
            return node switch
            {
                ProgramNode program => OptimizeProgram(program),
                BinaryOpNode binNode => OptimizeBinaryOpNode(binNode),
                LogicalOpNode logicalNode => OptimizeLogicalNode(logicalNode),
                VarDeclNode declNode => OptimizeVarDeclNode(declNode),
                IfNode ifNode => OptimizeIfNode(
                    ifNode,
                    (parent is not null)
                        ? parent
                        : throw new ArgumentException("If node expects parent as well.")
                ),
                _ => node,
            };
        }

        private static ASTNode OptimizeVarDeclNode(VarDeclNode declNode)
        {
            var log = (LogicalOpNode)declNode.Initializer;
            Console.WriteLine(log.Op);
            return new VarDeclNode(declNode.Name, OptimizeNode(declNode.Initializer))
            {
                Line = declNode.Line,
                Column = declNode.Column,
                Length = declNode.Length,
            };
        }

        private static ASTNode OptimizeBinaryOpNode(BinaryOpNode binNode)
        {
            ASTNode left = OptimizeNode(binNode.Left);
            ASTNode right = OptimizeNode(binNode.Right);
            if (left is NumberNode numLeft && right is NumberNode numRight)
            {
                double result = binNode.Op switch
                {
                    "+" => numLeft.Value + numRight.Value,
                    "-" => numLeft.Value - numRight.Value,
                    "*" => numLeft.Value * numRight.Value,
                    "/" => numLeft.Value / numRight.Value,
                    "^" => (ulong)numLeft.Value ^ (ulong)numRight.Value,
                    "&" => (ulong)numLeft.Value & (ulong)numRight.Value,
                    "|" => (ulong)numLeft.Value | (ulong)numRight.Value,
                    "<<" => (ulong)numLeft.Value << (int)numRight.Value,
                    ">>" => (ulong)numLeft.Value >> (int)numRight.Value,
                    "%" => numLeft.Value % numRight.Value,
                    "==" => (numLeft.Value == numRight.Value) ? 1 : 0,
                    ">" => (numLeft.Value > numRight.Value) ? 1 : 0,
                    "<" => (numLeft.Value < numRight.Value) ? 1 : 0,
                    ">=" => (numLeft.Value >= numRight.Value) ? 1 : 0,
                    "<=" => (numLeft.Value <= numRight.Value) ? 1 : 0,
                    "!=" => (numLeft.Value != numRight.Value) ? 1 : 0,
                    _ => double.NaN,
                };
                if (!double.IsNaN(result))
                {
                    return new NumberNode(result)
                    {
                        Line = numLeft.Line,
                        Column = numLeft.Column,
                        Length = result.ToString().Length,
                    };
                }
            }
            // Algebraic Identites (e.g., x * 1.0 -> x, x + 0.0 -> x)
            if (binNode.Op == "*" && right is NumberNode { Value: 1.0 })
                return left;

            if (binNode.Op == "*" && right is NumberNode { Value: 0.0 })
                return new NumberNode(0)
                {
                    Line = left.Line,
                    Column = left.Column,
                    Length = 1,
                };
            if (binNode.Op == "+" && right is NumberNode { Value: 0.0 })
                return left;
            //Base case: do nothing
            return binNode;
        }

        public static ASTNode OptimizeLogicalNode(LogicalOpNode logicalNode)
        {
            ASTNode left = OptimizeNode(logicalNode.Left);
            ASTNode right = OptimizeNode(logicalNode.Right);
            if (left is NumberNode numLeft && right is NumberNode numRight)
            {
                double result = logicalNode.Op switch
                {
                    "||" => (numLeft.Value == 1) ? 1 : (numRight.Value),
                    "&&" => (numLeft.Value == 0) ? 0 : (numRight.Value),
                    _ => double.NaN,
                };
                if (!double.IsNaN(result))
                {
                    return new NumberNode(result)
                    {
                        Line = numLeft.Line,
                        Column = numLeft.Column,
                        Length = result.ToString().Length,
                    };
                }
            }
            return logicalNode;
        }

        public static ASTNode OptimizeIfNode(IfNode ifNode, ASTNode parent)
        {
            var Condition = OptimizeNode(ifNode.Condition);
            if (Condition is NumberNode { Value: 0.0 })
            {
                if (ifNode.ElseBlock is null)
                    return null!; // TODO: Find out what will be the best thing to be returned here.
                switch (parent)
                {
                    case ProgramNode program:
                        {
                            int start = program.Statements.IndexOf(ifNode);
                            for (int i = 0; i < ifNode.ElseBlock.Count; i++)
                            {
                                program.Statements.Insert(
                                    start + i,
                                    OptimizeNode(ifNode.ElseBlock[i])
                                );
                            }
                            program.Statements.Remove(ifNode);
                        }
                        break;
                    case ForNode forNode:
                        {
                            int start = forNode.Body.IndexOf(ifNode);
                            for (int i = 0; i < ifNode.ElseBlock.Count; i++)
                            {
                                forNode.Body.Insert(start + i, OptimizeNode(ifNode.ElseBlock[i]));
                            }
                            forNode.Body.Remove(ifNode);
                        }
                        break;
                    case WhileNode whileNode:
                        {
                            int start = whileNode.Body.IndexOf(ifNode);
                            for (int i = 0; i < ifNode.ElseBlock.Count; i++)
                            {
                                whileNode.Body.Insert(start + i, OptimizeNode(ifNode.ElseBlock[i]));
                            }
                            whileNode.Body.Remove(ifNode);
                        }
                        break;
                    default:
                        break;
                }
            }
            else if (Condition is NumberNode { Value: 1.0 })
            {
                switch (parent)
                {
                    case ProgramNode program:
                        {
                            Console.WriteLine($"I need to optmize {ifNode.ThenBlock.Count}");
                            int start = program.Statements.IndexOf(ifNode);
                            for (int i = 0; i < ifNode.ThenBlock.Count; i++)
                            {
                                program.Statements.Insert(
                                    start + i,
                                    OptimizeNode(ifNode.ThenBlock[i])
                                );
                            }
                            program.Statements.Remove(ifNode);
                        }
                        break;
                    case ForNode forNode:
                        {
                            int start = forNode.Body.IndexOf(ifNode);
                            for (int i = 0; i < ifNode.ThenBlock.Count; i++)
                            {
                                forNode.Body.Insert(start + i, OptimizeNode(ifNode.ThenBlock[i]));
                            }
                            forNode.Body.Remove(ifNode);
                        }
                        break;
                    case WhileNode whileNode:
                        {
                            int start = whileNode.Body.IndexOf(ifNode);
                            for (int i = 0; i < ifNode.ThenBlock.Count; i++)
                            {
                                whileNode.Body.Insert(start + i, OptimizeNode(ifNode.ThenBlock[i]));
                            }
                            whileNode.Body.Remove(ifNode);
                        }
                        break;
                    default:
                        break;
                }
            }
            return ifNode;
        }

        public static ProgramNode OptimizeProgram(ProgramNode program)
        {
            for (int i = 0; i < program.Statements.Count; i++)
            {
                program.Statements[i] = OptimizeNode(program.Statements[i], program);
            }
            return program;
        }
    }
}
