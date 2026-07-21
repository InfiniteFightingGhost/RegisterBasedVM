namespace Raptor.Compiler
{
    public abstract class ASTNode
    {
        public int Line { get; set; }
        public int Column { get; set; }
        public int Length { get; set; }
    }

    public class ProgramNode : ASTNode
    {
        public List<ASTNode> Statements { get; } = new();
    }

    public class VarDeclNode : ASTNode
    {
        public string Name { get; }
        public ASTNode Initializer { get; }

        public VarDeclNode(string name, ASTNode initializer)
        {
            Name = name;
            Initializer = initializer;
        }
    }

    public class AssignmentNode : ASTNode
    {
        public string TargetName { get; }
        public ASTNode Value { get; }

        public AssignmentNode(string targetName, ASTNode value)
        {
            TargetName = targetName;
            Value = value;
        }
    }

    public class IfNode : ASTNode
    {
        public ASTNode Condition { get; }
        public List<ASTNode> ThenBlock { get; }
        public List<ASTNode>? ElseBlock { get; }

        public IfNode(ASTNode condition, List<ASTNode> thenBlock, List<ASTNode>? elseBlock)
        {
            Condition = condition;
            ThenBlock = thenBlock;
            ElseBlock = elseBlock;
        }
    }

    public class WhileNode : ASTNode
    {
        public ASTNode Condition { get; }
        public List<ASTNode> Body { get; }

        public WhileNode(ASTNode condition, List<ASTNode> body)
        {
            Condition = condition;
            Body = body;
        }
    }

    public class ForNode : ASTNode
    {
        public ASTNode? Initializer { get; }
        public ASTNode? Condition { get; }
        public ASTNode? Increment { get; }
        public List<ASTNode> Body { get; }

        public ForNode(
            ASTNode? initializer,
            ASTNode? condition,
            ASTNode? increment,
            List<ASTNode> body
        )
        {
            Initializer = initializer;
            Condition = condition;
            Increment = increment;
            Body = body;
        }
    }

    public class CallNode : ASTNode
    {
        public string MethodName { get; }
        public List<ASTNode> Arguments { get; } = new();

        public CallNode(string methodName, List<ASTNode> arguments)
        {
            MethodName = methodName;
            Arguments = arguments;
        }
    }

    public class LogicalOpNode : ASTNode
    {
        public ASTNode Left { get; }
        public string Op { get; }
        public ASTNode Right { get; }

        public LogicalOpNode(ASTNode left, string op, ASTNode right)
        {
            Left = left;
            Op = op;
            Right = right;
        }
    }

    public class BinaryOpNode : ASTNode
    {
        public ASTNode Left { get; }
        public string Op { get; }
        public ASTNode Right { get; }

        public BinaryOpNode(ASTNode left, string op, ASTNode right)
        {
            Left = left;
            Op = op;
            Right = right;
        }
    }

    public class NumberNode : ASTNode
    {
        public double Value { get; }

        public NumberNode(double value) => Value = value;
    }

    public class IdentifierNode : ASTNode
    {
        public string Name { get; }

        public IdentifierNode(string name) => Name = name;
    }

    public class ArrayLiteralNode : ASTNode
    {
        public ArrayLiteralNode(List<ASTNode> elements)
        {
            Elements = elements;
        }

        public List<ASTNode> Elements { get; } = new();
    }

    public class IndexAccessNode : ASTNode
    {
        public IndexAccessNode(ASTNode arrayExpr, ASTNode indexExpr)
        {
            ArrayExpr = arrayExpr;
            IndexExpr = indexExpr;
        }

        public ASTNode ArrayExpr { get; }
        public ASTNode IndexExpr { get; }
    }

    public class IndexAssignmentNode : ASTNode
    {
        public IndexAssignmentNode(ASTNode arrayExpr, ASTNode indexExpr, ASTNode value)
        {
            ArrayExpr = arrayExpr;
            IndexExpr = indexExpr;
            Value = value;
        }

        public ASTNode ArrayExpr { get; }
        public ASTNode IndexExpr { get; }
        public ASTNode Value { get; }
    }
}
