using System.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Raptor.Compiler
{
#region Token Definitions

public enum TokenType
{
    // Keywords
    Var,
    If,
    Else,
    While,
    Return,

    // Identifiers and Literals
    Identifier,
    Number,

    // Operators
    Assign,
    Plus,
    Minus,
    Star,
    Slash,
    PlusEquals,
    MinusEquals,
    PlusPlus,
    MinusMinus,
    Equal,
    NotEqual,
    Less,
    LessEqual,
    Greater,
    GreaterEqual,

    // Punctuation
    Semicolon,
    OpenParenthesis,
    CloseParenthesis,
    OpenBrace,
    CloseBrace,
    Comma,

    EOF,
}

public record Token(TokenType Type, string Value, int Line);

#endregion

#region AST Nodes

public abstract class ASTNode
{
    public int Line { get; set; }
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

#endregion

#region Lexer

public class Lexer
{
    private readonly string _source;
    private int _index;
    private int _line = 1;

    public Lexer(string source)
    {
        _source = source;
    }

    public List<Token> ScanTokens()
    {
        var tokens = new List<Token>();
        while (!IsAtEnd())
        {
            char c = Peek();
            if (char.IsWhiteSpace(c))
            {
                if (c == '\n')
                    _line++;
                Advance();
                continue;
            }

            if (c == '/' && PeekNext() == '/')
            {
                // Single-line comment: consume until newline or EOF
                while (Peek() != '\n' && !IsAtEnd())
                    Advance();
                continue;
            }

            if (char.IsDigit(c))
            {
                tokens.Add(ScanNumber());
                continue;
            }

            if (char.IsLetter(c) || c == '_')
            {
                tokens.Add(ScanIdentifierOrKeyword());
                continue;
            }

            tokens.Add(ScanOperatorOrPunctuation());
        }

        tokens.Add(new Token(TokenType.EOF, "", _line));
        return tokens;
    }

    private char Advance() => _source[_index++];

    private char Peek() => IsAtEnd() ? '\0' : _source[_index];

    private char PeekNext() => _index + 1 >= _source.Length ? '\0' : _source[_index + 1];

    private bool IsAtEnd() => _index >= _source.Length;

    private Token ScanNumber()
    {
        int start = _index;
        while (char.IsDigit(Peek()))
            Advance();

        if (Peek() == '.' && char.IsDigit(PeekNext()))
        {
            Advance(); // Consume '.'
            while (char.IsDigit(Peek()))
                Advance();
        }

        string val = _source[start.._index];
        return new Token(TokenType.Number, val, _line);
    }

    private Token ScanIdentifierOrKeyword()
    {
        int start = _index;
        // Allow dots inside identifiers for namespaces, e.g. math.clamp
        while (char.IsLetterOrDigit(Peek()) || Peek() == '_' || Peek() == '.')
        {
            Advance();
        }

        string val = _source[start.._index];
        TokenType type = val switch
        {
            "var" => TokenType.Var,
            "if" => TokenType.If,
            "else" => TokenType.Else,
            "while" => TokenType.While,
            "return" => TokenType.Return,
            _ => TokenType.Identifier,
        };

        return new Token(type, val, _line);
    }

    private Token ScanOperatorOrPunctuation()
    {
        char c = Advance();
        return c switch
        {
            ';' => new Token(TokenType.Semicolon, ";", _line),
            '(' => new Token(TokenType.OpenParenthesis, "(", _line),
            ')' => new Token(TokenType.CloseParenthesis, ")", _line),
            '{' => new Token(TokenType.OpenBrace, "{", _line),
            '}' => new Token(TokenType.CloseBrace, "}", _line),
            ',' => new Token(TokenType.Comma, ",", _line),

            '+' => Peek() switch
            {
                '=' => ConsumeAndReturn(TokenType.PlusEquals, "+="),
                '+' => ConsumeAndReturn(TokenType.PlusPlus, "++"),
                _ => new Token(TokenType.Plus, "+", _line),
            },
            '-' => Peek() switch
            {
                '=' => ConsumeAndReturn(TokenType.MinusEquals, "-="),
                '-' => ConsumeAndReturn(TokenType.MinusMinus, "--"),
                _ => new Token(TokenType.Minus, "-", _line),
            },
            '*' => new Token(TokenType.Star, "*", _line),
            '/' => new Token(TokenType.Slash, "/", _line),

            '=' => Match('=')
                ? new Token(TokenType.Equal, "==", _line)
                : new Token(TokenType.Assign, "=", _line),
            '!' => Match('=')
                ? new Token(TokenType.NotEqual, "!=", _line)
                : throw new Exception($"Unexpected char '!' at line {_line}"),
            '<' => Match('=')
                ? new Token(TokenType.LessEqual, "<=", _line)
                : new Token(TokenType.Less, "<", _line),
            '>' => Match('=')
                ? new Token(TokenType.GreaterEqual, ">=", _line)
                : new Token(TokenType.Greater, ">", _line),

            _ => throw new Exception($"Unexpected character '{c}' at line {_line}"),
        };
    }

    private bool Match(char expected)
    {
        if (IsAtEnd() || _source[_index] != expected)
            return false;
        _index++;
        return true;
    }

    private Token ConsumeAndReturn(TokenType type, string value)
    {
        Advance(); // Consume the peeked character
        return new Token(type, value, _line);
    }
}

#endregion

#region Parser

public class Parser
{
    private readonly List<Token> _tokens;
    private int _current;

    public Parser(List<Token> tokens) => _tokens = tokens;

    public ProgramNode Parse()
    {
        var prog = new ProgramNode();
        while (!IsAtEnd())
        {
            prog.Statements.Add(ParseStatement());
        }
        return prog;
    }

    private ASTNode ParseStatement()
    {
        if (Match(TokenType.Var))
            return ParseVarDecl();
        if (Match(TokenType.If))
            return ParseIf();
        if (Match(TokenType.While))
            return ParseWhile();

        // Otherwise Expression statement (e.g. assignments, call expressions)
        return ParseExpressionStatement();
    }

    private ASTNode ParseVarDecl()
    {
        Token nameToken = Consume(TokenType.Identifier, "Expected variable name.");
        Consume(TokenType.Assign, "Expected '=' in variable declaration.");
        ASTNode initializer = ParseExpression();
        Consume(TokenType.Semicolon, "Expected ';' after declaration.");
        return new VarDeclNode(nameToken.Value, initializer) { Line = nameToken.Line };
    }

    private ASTNode ParseIf()
    {
        Token ifToken = Previous();
        Consume(TokenType.OpenParenthesis, "Expected '(' after 'if'.");
        ASTNode condition = ParseExpression();
        Consume(TokenType.CloseParenthesis, "Expected ')' after condition.");

        List<ASTNode> thenBlock = ParseBlock();
        List<ASTNode>? elseBlock = null;

        if (Match(TokenType.Else))
        {
            elseBlock = ParseBlock();
        }

        return new IfNode(condition, thenBlock, elseBlock) { Line = ifToken.Line };
    }

    private ASTNode ParseWhile()
    {
        Token whileToken = Previous();
        Consume(TokenType.OpenParenthesis, "Expected '(' after 'while'.");
        ASTNode condition = ParseExpression();
        Consume(TokenType.CloseParenthesis, "Expected ')' after condition.");

        List<ASTNode> body = ParseBlock();
        return new WhileNode(condition, body) { Line = whileToken.Line };
    }

    private List<ASTNode> ParseBlock()
    {
        Consume(TokenType.OpenBrace, "Expected '{' to start block.");
        var statements = new List<ASTNode>();
        while (!Check(TokenType.CloseBrace) && !IsAtEnd())
        {
            statements.Add(ParseStatement());
        }
        Consume(TokenType.CloseBrace, "Expected '}' to end block.");
        return statements;
    }

    private ASTNode ParseExpressionStatement()
    {
        ASTNode expr = ParseExpression();

        // Check for desugared increments/assignments
        if (expr is AssignmentNode || expr is CallNode)
        {
            Consume(TokenType.Semicolon, "Expected ';' after statement.");
            return expr;
        }

        throw new Exception(
            $"Statement at line {Peek().Line} is not a valid assignment or function call."
        );
    }

    private ASTNode ParseExpression()
    {
        return ParseAssignmentExpression();
    }

    private ASTNode ParseAssignmentExpression()
    {
        ASTNode expr = ParseComparison();

        if (Match(TokenType.Assign, TokenType.PlusEquals, TokenType.MinusEquals))
        {
            Token op = Previous();
            if (expr is IdentifierNode id)
            {
                ASTNode value = ParseAssignmentExpression();

                // Desugar operators:
                // x += y -> x = x + y
                // x -= y -> x = x - y
                if (op.Type == TokenType.PlusEquals)
                {
                    value = new BinaryOpNode(id, "+", value) { Line = op.Line };
                }
                else if (op.Type == TokenType.MinusEquals)
                {
                    value = new BinaryOpNode(id, "-", value) { Line = op.Line };
                }

                return new AssignmentNode(id.Name, value) { Line = op.Line };
            }

            throw new Exception($"Invalid assignment target at line {op.Line}.");
        }

        // Handle postfix increments: x++ or x--
        if (Match(TokenType.PlusPlus, TokenType.MinusMinus))
        {
            Token op = Previous();
            if (expr is IdentifierNode id)
            {
                // Desugar postfix increment/decrement:
                // x++ -> x = x + 1
                // x-- -> x = x - 1
                string mathOp = op.Type == TokenType.PlusPlus ? "+" : "-";
                var value = new BinaryOpNode(id, mathOp, new NumberNode(1.0) { Line = op.Line }) { Line = op.Line };
                return new AssignmentNode(id.Name, value) { Line = op.Line };
            }

            throw new Exception($"Invalid increment/decrement target at line {op.Line}.");
        }

        return expr;
    }

    private ASTNode ParseComparison()
    {
        ASTNode expr = ParseTerm();

        while (
            Match(
                TokenType.Less,
                TokenType.LessEqual,
                TokenType.Greater,
                TokenType.GreaterEqual,
                TokenType.Equal,
                TokenType.NotEqual
            )
        )
        {
            Token op = Previous();
            ASTNode right = ParseTerm();
            expr = new BinaryOpNode(expr, op.Value, right) { Line = op.Line };
        }

        return expr;
    }

    private ASTNode ParseTerm()
    {
        ASTNode expr = ParseFactor();

        while (Match(TokenType.Plus, TokenType.Minus))
        {
            Token op = Previous();
            ASTNode right = ParseFactor();
            expr = new BinaryOpNode(expr, op.Value, right) { Line = op.Line };
        }

        return expr;
    }

    private ASTNode ParseFactor()
    {
        ASTNode expr = ParsePrimary();

        while (Match(TokenType.Star, TokenType.Slash))
        {
            Token op = Previous();
            ASTNode right = ParsePrimary();
            expr = new BinaryOpNode(expr, op.Value, right) { Line = op.Line };
        }

        return expr;
    }

    private ASTNode ParsePrimary()
    {
        if (Match(TokenType.Number))
        {
            Token numToken = Previous();
            return new NumberNode(double.Parse(numToken.Value)) { Line = numToken.Line };
        }

        if (Match(TokenType.Identifier))
        {
            Token idToken = Previous();

            // Check if it's a function call: identifier(args...)
            if (Match(TokenType.OpenParenthesis))
            {
                var args = new List<ASTNode>();
                if (!Check(TokenType.CloseParenthesis))
                {
                    do
                    {
                        args.Add(ParseExpression());
                    } while (Match(TokenType.Comma));
                }
                Consume(TokenType.CloseParenthesis, "Expected ')' after arguments.");
                return new CallNode(idToken.Value, args) { Line = idToken.Line };
            }

            return new IdentifierNode(idToken.Value) { Line = idToken.Line };
        }

        if (Match(TokenType.OpenParenthesis))
        {
            Token parenToken = Previous();
            ASTNode expr = ParseExpression();
            Consume(TokenType.CloseParenthesis, "Expected ')' after expression.");
            return expr;
        }

        throw new Exception($"Expected expression at line {Peek().Line}, found '{Peek().Value}'.");
    }

    #region Helper Methods

    private bool Match(params TokenType[] types)
    {
        foreach (var type in types)
        {
            if (Check(type))
            {
                Advance();
                return true;
            }
        }
        return false;
    }

    private bool Check(TokenType type)
    {
        if (IsAtEnd())
            return false;
        return Peek().Type == type;
    }

    private Token Advance()
    {
        if (!IsAtEnd())
            _current++;
        return Previous();
    }

    private bool IsAtEnd() => Peek().Type == TokenType.EOF;

    private Token Peek() => _tokens[_current];

    private Token Previous() => _tokens[_current - 1];

    private Token Consume(TokenType type, string message)
    {
        if (Check(type))
            return Advance();
        throw new Exception($"{message} (Line {Peek().Line})");
    }

    #endregion
}

#endregion

#region Code Emitter

public class Emitter
{
    private readonly ProgramNode _program;
    private readonly StringBuilder _sb = new();
    private readonly Dictionary<string, int> _variables = new();
    private int _regCounter = 1; // Start allocating registers from r1 (r0 acts as result accumulator)
    private int _labelCounter = 0;

    public Emitter(ProgramNode program) => _program = program;

    public Dictionary<string, int> Variables => _variables;

    public string Emit()
    {
        _sb.AppendLine("; --------------------------------------------------------------");
        _sb.AppendLine(";  Generated Raptor Assembly (.rasm) from RaptorScript Source");
        _sb.AppendLine("; --------------------------------------------------------------");
        _sb.AppendLine();

        foreach (var statement in _program.Statements)
        {
            EmitNode(statement);
        }

        _sb.AppendLine("HALT");
        return _sb.ToString();
    }

    private void EmitNode(ASTNode node)
    {
        if (node.Line > 0)
        {
            _sb.AppendLine($"#LINE {node.Line}");
        }

        switch (node)
        {
            case VarDeclNode decl:
                EmitVarDecl(decl);
                break;
            case AssignmentNode assign:
                EmitAssignment(assign);
                break;
            case IfNode ifNode:
                EmitIf(ifNode);
                break;
            case WhileNode whileNode:
                EmitWhile(whileNode);
                break;
            case CallNode call:
                EmitCall(call, 0); // Accumulate in r0 by default
                break;
            default:
                throw new Exception(
                    $"Cannot emit node of type {node.GetType().Name} at root level."
                );
        }
    }

    private void EmitVarDecl(VarDeclNode decl)
    {
        if (_variables.ContainsKey(decl.Name))
            throw new Exception($"Variable '{decl.Name}' is already declared.");

        int regIndex = _regCounter++;
        _variables[decl.Name] = regIndex;

        _sb.AppendLine($"; var {decl.Name}");
        int valueReg = EmitExpression(decl.Initializer);
        _sb.AppendLine($"MOVE r{regIndex} r{valueReg}");
    }

    private void EmitAssignment(AssignmentNode assign)
    {
        if (!_variables.TryGetValue(assign.TargetName, out int regIndex))
            throw new Exception($"Variable '{assign.TargetName}' is not declared.");

        _sb.AppendLine($"; {assign.TargetName} = <expr>");
        int valueReg = EmitExpression(assign.Value);
        _sb.AppendLine($"MOVE r{regIndex} r{valueReg}");
    }

    private void EmitIf(IfNode ifNode)
    {
        int labelId = _labelCounter++;
        string elseLabel = $"else_{labelId}";
        string endLabel = $"end_{labelId}";

        _sb.AppendLine("; if condition");
        EmitBranchCondition(ifNode.Condition, elseLabel);

        _sb.AppendLine("; then block");
        foreach (var stmt in ifNode.ThenBlock)
        {
            EmitNode(stmt);
        }
        _sb.AppendLine($"JUMP {endLabel}");

        _sb.AppendLine($"{elseLabel}:");
        if (ifNode.ElseBlock != null)
        {
            _sb.AppendLine("; else block");
            foreach (var stmt in ifNode.ElseBlock)
            {
                EmitNode(stmt);
            }
        }

        _sb.AppendLine($"{endLabel}:");
    }

    private void EmitWhile(WhileNode whileNode)
    {
        int labelId = _labelCounter++;
        string loopLabel = $"while_{labelId}";
        string endLabel = $"while_end_{labelId}";

        _sb.AppendLine($"{loopLabel}:");
        _sb.AppendLine("; while condition");
        EmitBranchCondition(whileNode.Condition, endLabel);

        _sb.AppendLine("; while body");
        foreach (var stmt in whileNode.Body)
        {
            EmitNode(stmt);
        }
        _sb.AppendLine($"JUMP {loopLabel}");
        _sb.AppendLine($"{endLabel}:");
    }

    private void EmitBranchCondition(ASTNode cond, string jumpLabel)
    {
        if (cond is BinaryOpNode bin && IsComparisonOp(bin.Op))
        {
            int leftReg = EmitExpression(bin.Left);
            string rightStr = GetExpressionOperandString(bin.Right);

            switch (bin.Op)
            {
                case "<":
                    _sb.AppendLine($"LT 0 r{leftReg} {rightStr}");
                    break;
                case "<=":
                    _sb.AppendLine($"LE 1 r{leftReg} {rightStr}");
                    break;
                case ">":
                    // a > b -> b < a
                    // Note: Left operand of LT must be a register, so evaluate Right if it's a constant
                    int rightReg = EmitExpression(bin.Right);
                    _sb.AppendLine($"LT 0 r{rightReg} r{leftReg}");
                    break;
                case ">=":
                    // a >= b -> b <= a
                    int rightRegGe = EmitExpression(bin.Right);
                    _sb.AppendLine($"LE 1 r{rightRegGe} r{leftReg}");
                    break;
                case "==":
                    _sb.AppendLine($"EQ 1 r{leftReg} {rightStr}");
                    break;
                case "!=":
                    _sb.AppendLine($"EQ 0 r{leftReg} {rightStr}");
                    break;
            }
        }
        else
        {
            // Fallback: evaluate expression and jump if false (equal to 0.0)
            int condReg = EmitExpression(cond);
            _sb.AppendLine($"EQ 0 r{condReg} 0.0");
        }

        _sb.AppendLine($"JUMP {jumpLabel}");
    }

    private bool IsComparisonOp(string op)
    {
        return op is "<" or "<=" or ">" or ">=" or "==" or "!=";
    }

    private string GetExpressionOperandString(ASTNode node)
    {
        if (node is NumberNode num)
            return num.Value.ToString("F1");
        if (node is IdentifierNode id)
        {
            if (_variables.TryGetValue(id.Name, out int reg))
                return $"r{reg}";
            throw new Exception($"Undefined identifier '{id.Name}'.");
        }

        int regIndex = EmitExpression(node);
        return $"r{regIndex}";
    }

    private int EmitExpression(ASTNode node)
    {
        if (node.Line > 0)
        {
            _sb.AppendLine($"#LINE {node.Line}");
        }

        switch (node)
        {
            case NumberNode num:
                int numReg = _regCounter++;
                _sb.AppendLine($"LOADC r{numReg} {num.Value.ToString("F1")}");
                return numReg;

            case IdentifierNode id:
                if (!_variables.TryGetValue(id.Name, out int varReg))
                    throw new Exception($"Undefined identifier '{id.Name}'.");
                return varReg;

            case BinaryOpNode binary:
                return EmitBinaryOp(binary);

            case CallNode call:
                int returnReg = _regCounter++;
                EmitCall(call, returnReg);
                return returnReg;

            default:
                throw new Exception($"Cannot emit expression node of type {node.GetType().Name}.");
        }
    }

    private int EmitBinaryOp(BinaryOpNode binary)
    {
        int leftReg = EmitExpression(binary.Left);
        int rightReg = EmitExpression(binary.Right);
        int resReg = _regCounter++;

        string instruction;
        int firstReg = leftReg;
        int secondReg = rightReg;

        switch (binary.Op)
        {
            case "+":
                instruction = "ADD";
                break;
            case "-":
                instruction = "SUB";
                break;
            case "*":
                instruction = "MUL";
                break;
            case "/":
                instruction = "DIV";
                break;
            case "<":
                instruction = "LT";
                break;
            case "<=":
                instruction = "LE";
                break;
            case ">":
                // Desugar a > b -> b < a
                instruction = "LT";
                firstReg = rightReg;
                secondReg = leftReg;
                break;
            case ">=":
                // Desugar a >= b -> b <= a
                instruction = "LE";
                firstReg = rightReg;
                secondReg = leftReg;
                break;
            case "==":
                instruction = "EQ";
                break;
            case "!=":
                // Desugar a != b -> EQ r_res r_left r_right followed by EQ r_res r_res 0.0 (inverted)
                _sb.AppendLine($"EQ r{resReg} r{leftReg} r{rightReg}");
                int zeroReg = _regCounter++;
                _sb.AppendLine($"LOADC r{zeroReg} 0.0");
                _sb.AppendLine($"EQ r{resReg} r{resReg} r{zeroReg}");
                return resReg;
            default:
                throw new Exception($"Unsupported binary operator: {binary.Op}");
        }

        _sb.AppendLine($"{instruction} r{resReg} r{firstReg} r{secondReg}");
        return resReg;
    }

    private void EmitCall(CallNode call, int returnReg)
    {
        int callBase = _regCounter;

        // 1. Evaluate arguments first (this might increment _regCounter if args contain sub-calls/expressions)
        int[] argRegs = new int[call.Arguments.Count];
        for (int i = 0; i < call.Arguments.Count; i++)
        {
            argRegs[i] = EmitExpression(call.Arguments[i]);
        }

        // 2. Move arguments into registers sequentially starting from callBase + 1
        for (int i = 0; i < call.Arguments.Count; i++)
        {
            _sb.AppendLine($"MOVE r{callBase + i + 1} r{argRegs[i]}");
        }

        // 3. Call the method with callBase as return parameter window offset
        _sb.AppendLine($"CALL {call.MethodName}() r{callBase}");

        // 4. Move return value (placed in r{callBase} by FFI wrapper) to returnReg if needed
        if (returnReg != 0 && returnReg != callBase)
        {
            _sb.AppendLine($"MOVE r{returnReg} r{callBase}");
        }
    }
}

#endregion

#region Compiler Entry Point

public static class RaptorScriptCompiler
{
    public static string Compile(string sourceText)
    {
        return Compile(sourceText, out _);
    }

    public static string Compile(string sourceText, out Dictionary<string, int> variables)
    {
        var lexer = new Lexer(sourceText);
        var tokens = lexer.ScanTokens();

        var parser = new Parser(tokens);
        var program = parser.Parse();

        var emitter = new Emitter(program);
        string code = emitter.Emit();
        variables = emitter.Variables;
        return code;
    }
}

#endregion
}
