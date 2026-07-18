using System;
using System.Collections.Generic;
using System.Linq;

namespace Raptor.Compiler
{
    public static class AstPrinter
    {
        public static void Print(ProgramNode program)
        {
            PrintList(program.Statements, "");
        }

        private static void PrintNode(string name, ASTNode node, string indent, bool isLast)
        {
            Console.Write(indent);
            Console.Write(isLast ? "└── " : "├── ");
            Console.WriteLine(name);

            string childIndent = indent + (isLast ? "    " : "│   ");

            switch (node)
            {
                case VarDeclNode decl:
                    PrintNode("Initializer", decl.Initializer, childIndent, true);
                    break;

                case AssignmentNode assign:
                    PrintNode("Value", assign.Value, childIndent, true);
                    break;

                case IndexAssignmentNode idxAssign:
                    PrintNode("Array", idxAssign.ArrayExpr, childIndent, false);
                    PrintNode("Index", idxAssign.IndexExpr, childIndent, false);
                    PrintNode("Value", idxAssign.Value, childIndent, true);
                    break;

                case IfNode ifNode:
                    PrintNode("Condition", ifNode.Condition, childIndent, false);
                    Console.WriteLine($"{childIndent}├── ThenBlock");
                    PrintList(ifNode.ThenBlock, childIndent + "│   ");
                    if (ifNode.ElseBlock != null && ifNode.ElseBlock.Count > 0)
                    {
                        Console.WriteLine($"{childIndent}└── ElseBlock");
                        PrintList(ifNode.ElseBlock, childIndent + "    ");
                    }
                    else
                    {
                        // Just an empty line to close the tree branch visually if no else
                        Console.WriteLine($"{childIndent}└── (No Else)");
                    }
                    break;

                case WhileNode whileNode:
                    PrintNode("Condition", whileNode.Condition, childIndent, false);
                    Console.WriteLine($"{childIndent}└── Body");
                    PrintList(whileNode.Body, childIndent + "    ");
                    break;

                case ForNode forNode:
                    if (forNode.Initializer != null)
                        PrintNode("Init", forNode.Initializer, childIndent, false);
                    if (forNode.Condition != null)
                        PrintNode("Cond", forNode.Condition, childIndent, false);
                    if (forNode.Increment != null)
                        PrintNode("Step", forNode.Increment, childIndent, false);
                    Console.WriteLine($"{childIndent}└── Body");
                    PrintList(forNode.Body, childIndent + "    ");
                    break;

                case CallNode call:
                    Console.WriteLine($"{childIndent}└── Arguments");
                    PrintList(call.Arguments, childIndent + "    ");
                    break;
                case LogicalOpNode logicalOp:
                    PrintNode("Left", logicalOp.Left, childIndent, false);
                    PrintNode("Right", logicalOp.Right, childIndent, true);
                    break;
                case BinaryOpNode binOp:
                    Console.WriteLine($"{childIndent}{binOp.Op}");
                    PrintNode("Left", binOp.Left, childIndent, false);
                    PrintNode("Right", binOp.Right, childIndent, true);
                    break;

                case IndexAccessNode idxAccess:
                    PrintNode("Array", idxAccess.ArrayExpr, childIndent, false);
                    PrintNode("Index", idxAccess.IndexExpr, childIndent, true);
                    break;

                case ArrayLiteralNode arrLit:
                    Console.WriteLine($"{childIndent}└── Elements");
                    PrintList(arrLit.Elements, childIndent + "    ");
                    break;

                case NumberNode number:
                    Console.WriteLine($"{childIndent}└──{number.Value}");
                    break;
                case IdentifierNode identifier:
                    Console.WriteLine($"{childIndent}└──{identifier.Name}");
                    break;

                default:
                    Console.WriteLine(
                        $"{childIndent}└── [Unknown Node Type: {node.GetType().Name}]"
                    );
                    break;
            }
        }

        private static void PrintList(List<ASTNode> nodes, string indent)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                bool isLast = (i == nodes.Count - 1);
                ASTNode child = nodes[i];

                // Determine the display name for the node
                string nodeName = child switch
                {
                    VarDeclNode v => $"VarDecl ({v.Name})",
                    AssignmentNode a => $"Assign ({a.TargetName} =)",
                    BinaryOpNode b => $"BinaryOp ({b.Op})",
                    NumberNode n => $"Number ({n.Value})",
                    IdentifierNode id => $"Identifier ({id.Name})",
                    CallNode c => $"Call ({c.MethodName})",
                    IfNode => "If",
                    WhileNode => "While",
                    ForNode => "For",
                    // ArrayLiteralNode => "ArrayLiteral",
                    // IndexAccessNode => "IndexAccess",
                    // IndexAssignmentNode => "IndexAssign",
                    _ => child.GetType().Name,
                };

                PrintNode(nodeName, child, indent, isLast);
            }
        }
    }
}
