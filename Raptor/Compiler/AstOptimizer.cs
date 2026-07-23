namespace Raptor.Compiler
{
    public static class ASTOptimizer
    {
        public static ASTNode OptimizeNode(ASTNode node)
        {
            return node switch
            {
                ProgramNode program => OptimizeProgram(program),
                BinaryOpNode binNode => OptimizeBinaryOpNode(binNode),
                LogicalOpNode logicalNode => OptimizeLogicalNode(logicalNode),
                VarDeclNode declNode => OptimizeVarDeclNode(declNode),
                _ => node,
            };
        }

        private static ASTNode OptimizeVarDeclNode(VarDeclNode declNode)
        {
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
                    "||" => (numLeft.Value != 0) ? numLeft.Value : (numRight.Value),
                    "&&" => (numLeft.Value == 0) ? numLeft.Value : (numRight.Value),
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

        public static IEnumerable<ASTNode> OptimizeIfNode(IfNode ifNode)
        {
            var condition = OptimizeNode(ifNode.Condition);
            var thenBlock = OptimizeStatements(ifNode.ThenBlock);
            var elseBlock = ifNode.ElseBlock != null ? OptimizeStatements(ifNode.ElseBlock) : null;

            if (condition is NumberNode { Value: 0.0 })
            {
                return elseBlock ?? Enumerable.Empty<ASTNode>();
            }
            else if (condition is NumberNode { Value: 1.0 })
            {
                return thenBlock;
            }
            var optimizedIf = new IfNode(condition, thenBlock, elseBlock)
            {
                Line = ifNode.Line,
                Column = ifNode.Column,
                Length = ifNode.Length,
            };
            return new[] { optimizedIf };
        }

        public static IEnumerable<ASTNode> OptimizeForNode(ForNode forNode)
        {
            var init = forNode.Initializer != null ? OptimizeNode(forNode.Initializer) : null;
            var cond = forNode.Condition != null ? OptimizeNode(forNode.Condition) : null;
            var step = forNode.Increment != null ? OptimizeNode(forNode.Increment) : null;
            var body = OptimizeStatements(forNode.Body);
            if (cond is NumberNode { Value: 0.0 })
                return Enumerable.Empty<ASTNode>();
            var newForNode = new ForNode(init, cond, step, body)
            {
                Line = forNode.Line,
                Column = forNode.Column,
                Length = forNode.Length,
            };
            return new[] { newForNode };
        }

        public static IEnumerable<ASTNode> OptimizeWhileNode(WhileNode whileNode)
        {
            var cond = OptimizeNode(whileNode.Condition);
            var body = OptimizeStatements(whileNode.Body);
            if (cond is NumberNode { Value: 0.0 })
                return Enumerable.Empty<ASTNode>();
            var newWhileNode = new WhileNode(cond, body)
            {
                Line = whileNode.Line,
                Column = whileNode.Column,
                Length = whileNode.Length,
            };
            return new[] { newWhileNode };
        }

        private static List<ASTNode> OptimizeStatements(List<ASTNode> input)
        {
            var output = new List<ASTNode>(input.Count);
            foreach (var node in input)
            {
                if (node is IfNode ifNode)
                {
                    output.AddRange(OptimizeIfNode(ifNode));
                }
                else if (node is ForNode forNode)
                {
                    output.AddRange(OptimizeForNode(forNode));
                }
                else
                {
                    output.Add(OptimizeNode(node));
                }
            }
            return output;
        }

        public static ProgramNode OptimizeProgram(ProgramNode program)
        {
            var optimized = OptimizeStatements(program.Statements);
            program.Statements.Clear();
            program.Statements.AddRange(optimized);
            return program;
        }
    }
}
