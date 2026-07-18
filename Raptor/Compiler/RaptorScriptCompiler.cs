using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

// TODO: Implement bitwise and logical operators. For logical operators touch up on the if\else so that
//      the statement 'if(arr != null && arr[0] < 5)' evaluates the first statement first and if it's false
//      jump to the else block or to the next instruction.
//      Also implement true, false and null keywords.
//      Better control flow via break and continue statements will be nice as well.
//      Since the VM has array methods for singular bytes implementing string handling can be built with a little effort,
//      User defined functions in order to finally have functions that one can call.
//      High effort high value -> user defined structs

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
        For,

        // Identifiers and Literals
        Identifier,
        Number,

        // Operators
        Assign,
        Plus,
        Minus,
        Star,
        Slash,
        StarEquals,
        SlashEquals,
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
        Percent,
        PercentEquals,
        Ampersand,
        AmpersandEquals,
        AmpersandAmpersand,
        LessLess,
        GreaterGreater,
        Caret,
        CaretEquals,
        Pipe,
        PipeEquals,
        PipePipe,

        // Punctuation
        Semicolon,
        OpenParenthesis,
        CloseParenthesis,
        OpenBrace,
        CloseBrace,
        OpenBracket,
        CloseBracket,
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
                "for" => TokenType.For,
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
                '[' => new Token(TokenType.OpenBracket, "[", _line),
                ']' => new Token(TokenType.CloseBracket, "]", _line),
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
                '*' => Peek() switch
                {
                    '=' => ConsumeAndReturn(TokenType.StarEquals, "*="),
                    _ => new Token(TokenType.Star, "*", _line),
                },
                '/' => Peek() switch
                {
                    '=' => ConsumeAndReturn(TokenType.SlashEquals, "/="),
                    _ => new Token(TokenType.Slash, "/", _line),
                },
                '=' => Match('=')
                    ? new Token(TokenType.Equal, "==", _line)
                    : new Token(TokenType.Assign, "=", _line),
                '!' => Match('=')
                    ? new Token(TokenType.NotEqual, "!=", _line)
                    : throw new Exception($"Unexpected char '!' at line {_line}"),
                '<' => Peek() switch
                {
                    '=' => ConsumeAndReturn(TokenType.LessEqual, "<="),
                    '<' => ConsumeAndReturn(TokenType.LessLess, "<<"),
                    _ => new Token(TokenType.Less, "<", _line),
                },
                '>' => Peek() switch
                {
                    '=' => ConsumeAndReturn(TokenType.GreaterEqual, ">="),
                    '>' => ConsumeAndReturn(TokenType.GreaterGreater, ">>"),
                    _ => new Token(TokenType.Greater, ">", _line),
                },
                '%' => Peek() switch
                {
                    '=' => ConsumeAndReturn(TokenType.PercentEquals, "%="),
                    _ => new Token(TokenType.Percent, "%", _line),
                },
                '&' => Peek() switch
                {
                    '=' => ConsumeAndReturn(TokenType.AmpersandEquals, "&="),
                    '&' => ConsumeAndReturn(TokenType.AmpersandAmpersand, "&&"),
                    _ => new Token(TokenType.Ampersand, "&", _line),
                },
                '|' => Peek() switch
                {
                    '=' => ConsumeAndReturn(TokenType.PipeEquals, "|="),
                    '|' => ConsumeAndReturn(TokenType.PipePipe, "||"),
                    _ => new Token(TokenType.Pipe, "|", _line),
                },
                '^' => Peek() switch
                {
                    '=' => ConsumeAndReturn(TokenType.CaretEquals, "^="),
                    _ => new Token(TokenType.Caret, "^", _line),
                },
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
            if (Match(TokenType.For))
                return ParseFor();

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

        private ASTNode ParseFor()
        {
            Token forToken = Previous();
            Consume(TokenType.OpenParenthesis, "Expected '(' after 'for'.");
            ASTNode? initializer = null;
            if (Match(TokenType.Var))
            {
                initializer = ParseVarDecl();
            }
            else if (!Match(TokenType.Semicolon))
            {
                initializer = ParseExpressionStatement();
            }

            ASTNode? condition = null;
            if (!Check(TokenType.Semicolon))
            {
                condition = ParseExpression();
            }
            Consume(TokenType.Semicolon, "Expected ';' after loop condition.");
            ASTNode? increment = null;
            if (!Check(TokenType.Semicolon))
            {
                increment = ParseExpression();
            }
            Consume(TokenType.CloseParenthesis, "Expected ')' after loop step.");

            List<ASTNode> body = ParseBlock();
            return new ForNode(initializer, condition, increment, body) { Line = forToken.Line };
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
            if (expr is AssignmentNode || expr is CallNode || expr is IndexAssignmentNode)
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
            ASTNode expr = ParseLogicalOr();
            if (
                Match(
                    TokenType.Assign,
                    TokenType.PlusEquals,
                    TokenType.MinusEquals,
                    TokenType.SlashEquals,
                    TokenType.StarEquals,
                    TokenType.PipeEquals,
                    TokenType.CaretEquals,
                    TokenType.AmpersandEquals,
                    TokenType.PercentEquals
                )
            )
            {
                Token op = Previous();
                ASTNode value = ParseAssignmentExpression();
                if (expr is IdentifierNode id)
                {
                    // Desugar operators:
                    // x += y -> x = x + y
                    // x -= y -> x = x - y, etc
                    switch (op.Type)
                    {
                        case TokenType.PlusEquals:
                            value = new BinaryOpNode(id, "+", value) { Line = op.Line };
                            break;
                        case TokenType.MinusEquals:
                            value = new BinaryOpNode(id, "-", value) { Line = op.Line };
                            break;
                        case TokenType.SlashEquals:
                            value = new BinaryOpNode(id, "/", value) { Line = op.Line };
                            break;
                        case TokenType.StarEquals:
                            value = new BinaryOpNode(id, "*", value) { Line = op.Line };
                            break;
                        case TokenType.PipeEquals:
                            value = new BinaryOpNode(id, "|", value) { Line = op.Line };
                            break;
                        case TokenType.CaretEquals:
                            value = new BinaryOpNode(id, "^", value) { Line = op.Line };
                            break;
                        case TokenType.AmpersandEquals:
                            value = new BinaryOpNode(id, "&", value) { Line = op.Line };
                            break;
                        case TokenType.PercentEquals:
                            value = new BinaryOpNode(id, "%", value) { Line = op.Line };
                            break;
                    }

                    return new AssignmentNode(id.Name, value) { Line = op.Line };
                }
                else if (expr is IndexAccessNode indexAccess)
                {
                    switch (op.Type)
                    {
                        case TokenType.PlusEquals:
                            value = new BinaryOpNode(indexAccess, "+", value) { Line = op.Line };
                            break;
                        case TokenType.MinusEquals:
                            value = new BinaryOpNode(indexAccess, "-", value) { Line = op.Line };
                            break;
                        case TokenType.SlashEquals:
                            value = new BinaryOpNode(indexAccess, "/", value) { Line = op.Line };
                            break;
                        case TokenType.StarEquals:
                            value = new BinaryOpNode(indexAccess, "*", value) { Line = op.Line };
                            break;
                        case TokenType.PipeEquals:
                            value = new BinaryOpNode(indexAccess, "|", value) { Line = op.Line };
                            break;
                        case TokenType.CaretEquals:
                            value = new BinaryOpNode(indexAccess, "^", value) { Line = op.Line };
                            break;
                        case TokenType.AmpersandEquals:
                            value = new BinaryOpNode(indexAccess, "&", value) { Line = op.Line };
                            break;
                        case TokenType.PercentEquals:
                            value = new BinaryOpNode(indexAccess, "%", value) { Line = op.Line };
                            break;
                    }

                    return new IndexAssignmentNode(
                        indexAccess.ArrayExpr,
                        indexAccess.IndexExpr,
                        value
                    )
                    {
                        Line = op.Line,
                    };
                }

                throw new Exception($"Invalid assignment target at line {op.Line}.");
            }

            // Handle postfix increments: x++ or x--
            if (Match(TokenType.PlusPlus, TokenType.MinusMinus))
            {
                Token op = Previous();
                string mathOp = op.Type == TokenType.PlusPlus ? "+" : "-";
                var one = new NumberNode(1.0) { Line = op.Line };
                if (expr is IdentifierNode id)
                {
                    // Desugar postfix increment/decrement:
                    // x++ -> x = x + 1
                    // x-- -> x = x - 1
                    var value = new BinaryOpNode(id, mathOp, new NumberNode(1.0) { Line = op.Line })
                    {
                        Line = op.Line,
                    };
                    return new AssignmentNode(id.Name, value) { Line = op.Line };
                }
                else if (expr is IndexAccessNode indexAccess)
                {
                    var value = new BinaryOpNode(indexAccess, mathOp, one) { Line = op.Line };
                    return new IndexAssignmentNode(
                        indexAccess.ArrayExpr,
                        indexAccess.IndexExpr,
                        value
                    )
                    {
                        Line = op.Line,
                    };
                }
                throw new Exception($"Invalid increment/decrement target at line {op.Line}.");
            }

            return expr;
        }

        private ASTNode ParseComparison()
        {
            ASTNode expr = ParseShift();

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
                ASTNode right = ParseShift();
                expr = new BinaryOpNode(expr, op.Value, right) { Line = op.Line };
            }

            return expr;
        }

        private ASTNode ParseLogicalOr()
        {
            ASTNode expr = ParseLogicalAnd();
            while (Match(TokenType.PipePipe))
            {
                Token op = Previous();
                ASTNode right = ParseLogicalAnd();
                expr = new LogicalOpNode(expr, op.Value, right) { Line = op.Line };
            }
            return expr;
        }

        private ASTNode ParseLogicalAnd()
        {
            ASTNode expr = ParseBitwiseOr();
            while (Match(TokenType.AmpersandAmpersand))
            {
                Token op = Previous();
                ASTNode right = ParseBitwiseOr();
                expr = new LogicalOpNode(expr, op.Value, right) { Line = op.Line };
            }
            return expr;
        }

        private ASTNode ParseBitwiseOr()
        {
            ASTNode expr = ParseBitwiseXor();
            while (Match(TokenType.Pipe))
            {
                Token op = Previous();
                ASTNode right = ParseBitwiseXor();
                expr = new BinaryOpNode(expr, op.Value, right) { Line = op.Line };
            }
            return expr;
        }

        private ASTNode ParseBitwiseXor()
        {
            ASTNode expr = ParseBitwiseAnd();
            while (Match(TokenType.Caret))
            {
                Token op = Previous();
                ASTNode right = ParseBitwiseAnd();
                expr = new BinaryOpNode(expr, op.Value, right) { Line = op.Line };
            }
            return expr;
        }

        private ASTNode ParseBitwiseAnd()
        {
            ASTNode expr = ParseComparison();
            while (Match(TokenType.Ampersand))
            {
                Token op = Previous();
                ASTNode right = ParseComparison();
                expr = new BinaryOpNode(expr, op.Value, right) { Line = op.Line };
            }
            return expr;
        }

        private ASTNode ParseShift()
        {
            ASTNode expr = ParseTerm();
            while (Match(TokenType.LessLess, TokenType.GreaterGreater))
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

            while (Match(TokenType.Star, TokenType.Slash, TokenType.Percent))
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

            if (Match(TokenType.OpenBracket))
            {
                Token bracketToken = Previous();
                var elements = new List<ASTNode>();
                if (!Check(TokenType.CloseBracket))
                {
                    do
                    {
                        elements.Add(ParseExpression());
                    } while (Check(TokenType.Comma));
                }
                Consume(TokenType.CloseBracket, "Expected ']' after array elements.");
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
                ASTNode expr = new IdentifierNode(idToken.Value) { Line = idToken.Line };
                while (Match(TokenType.OpenBracket))
                {
                    ASTNode indexExpr = ParseExpression();
                    Consume(TokenType.CloseBracket, "Expected ']' after array index.");
                    expr = new IndexAccessNode(expr, indexExpr) { Line = idToken.Line };
                }
                return expr;
            }

            if (Match(TokenType.OpenParenthesis))
            {
                Token parenToken = Previous();
                ASTNode expr = ParseExpression();
                Consume(TokenType.CloseParenthesis, "Expected ')' after expression.");
                return expr;
            }

            throw new Exception(
                $"Expected expression at line {Peek().Line}, found '{Peek().Value}'."
            );
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
        public class Environment
        {
            // The parent scope (null if this is the global scope)
            public Environment? Enclosing { get; }

            // The variables defined *only* in this specific scope
            private readonly Dictionary<string, int> _values = new();

            public IReadOnlyDictionary<string, int> Variables => _values;

            public Environment(Environment? enclosing)
            {
                Enclosing = enclosing;
            }

            public void Define(string name, int registerIndex)
            {
                if (_values.ContainsKey(name))
                {
                    throw new Exception($"Variable '{name}' is already declared in this scope.");
                }
                _values[name] = registerIndex;
            }

            public bool TryGet(string name, out int registerIndex)
            {
                if (_values.TryGetValue(name, out registerIndex))
                {
                    return true;
                }

                if (Enclosing != null)
                {
                    return Enclosing.TryGet(name, out registerIndex);
                }

                registerIndex = -1;
                return false;
            }
        }

        private readonly ProgramNode _program;
        private readonly StringBuilder _sb = new();
        private readonly Environment _globalEnvironment;
        private Environment _environment;
        private readonly Dictionary<string, int> _propertyMappings = new();
        private int _regCounter = 1; // Start allocating registers from r1 (r0 acts as result accumulator)
        private int _labelCounter = 0;

        public Emitter(ProgramNode program, Dictionary<string, int>? propertyMappings = null)
        {
            _program = program;
            _environment = new Environment(null);
            _globalEnvironment = _environment;
            if (propertyMappings != null)
            {
                _propertyMappings = propertyMappings;
                int maxPropertyReg = 0;
                if (_propertyMappings.Count > 0)
                {
                    maxPropertyReg = _propertyMappings.Values.Max();
                }
                _regCounter = maxPropertyReg + 1;
            }
        }

        public IReadOnlyDictionary<string, int> Globals => _globalEnvironment.Variables;

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
                case ForNode forNode:
                    EmitFor(forNode);
                    break;
                case IndexAssignmentNode indexAssignmentNode:
                    EmitIndexAssignment(indexAssignmentNode);
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
            int regIndex = _regCounter++;
            _environment.Define(decl.Name, regIndex);

            int valueReg = EmitExpression(decl.Initializer);
            _sb.Append($"MOVE r{regIndex} r{valueReg} ");
            _sb.AppendLine($"; var {decl.Name}");
        }

        private void EmitAssignment(AssignmentNode assign)
        {
            int regIndex;
            if (_propertyMappings.TryGetValue(assign.TargetName, out int propReg))
            {
                regIndex = propReg;
            }
            else if (!_environment.TryGet(assign.TargetName, out int varReg))
            {
                throw new Exception($"Variable '{assign.TargetName}' is not declared.");
            }
            else
            {
                regIndex = varReg;
            }

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
            EmitBlock(ifNode.ThenBlock);
            _sb.AppendLine($"JUMP {endLabel}");

            _sb.AppendLine($"{elseLabel}:");
            if (ifNode.ElseBlock != null)
            {
                _sb.AppendLine("; else block");
                if (ifNode.ElseBlock != null)
                {
                    _sb.AppendLine("; else block");
                    EmitBlock(ifNode.ElseBlock);
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
            EmitBlock(whileNode.Body);
            _sb.AppendLine($"JUMP {loopLabel}");
            _sb.AppendLine($"{endLabel}:");
        }

        private void EmitFor(ForNode forNode)
        {
            int indexReg;
            if (forNode.Initializer != null)
            {
                if (forNode.Initializer is VarDeclNode varDeclNode)
                {
                    EmitVarDecl(varDeclNode);
                    _environment.TryGet(varDeclNode.Name, out int value);
                    indexReg = value;
                }
                else if (forNode.Initializer is AssignmentNode assignmentNode)
                {
                    EmitAssignment(assignmentNode);
                    _environment.TryGet(assignmentNode.TargetName, out int value);
                    indexReg = value;
                }
                else
                {
                    throw new Exception(
                        "For-loop initializer must be a variable declaration or assignment."
                    );
                }
            }
            else
            {
                indexReg = _regCounter++;
                _sb.AppendLine($"LOADC r{indexReg} 0.0 ; Dummy index for empty init");
            }

            string limitStr = "1.0";
            string compOp = "<";
            if (forNode.Condition != null)
            {
                if (forNode.Condition is BinaryOpNode binOp && IsComparisonOp(binOp.Op))
                {
                    compOp = binOp.Op;
                    limitStr = GetExpressionOperandString(binOp.Right);
                }
                else
                {
                    throw new Exception("For-loop condition must be a comparison (e.g., i < 10).");
                }
            }
            else
            {
                // Hack: No condition means infinite loop. We do 0 < 1
                _sb.AppendLine($"LOADC r{indexReg} 0.0");
                limitStr = "1.0";
                compOp = "<";
            }

            string stepStr = "0.0";
            if (forNode.Increment != null)
            {
                if (
                    forNode.Increment is AssignmentNode incAssign
                    && incAssign.Value is BinaryOpNode incMath
                )
                {
                    if (incMath.Op == "+")
                    {
                        stepStr = GetExpressionOperandString(incMath.Right);
                    }
                    else if (incMath.Op == "-")
                    {
                        // If the loop does i--, the step is negative.
                        if (incMath.Right is NumberNode numNode)
                        {
                            stepStr = (-numNode.Value).ToString("F1");
                        }
                        else
                        {
                            // If it's a register (e.g. i -= stepSize), we need to negate it.
                            stepStr = GetExpressionOperandString(incMath.Right);
                        }
                    }
                }
            }
            int labelId = _labelCounter++;
            string loopLabel = $"for_{labelId}";
            string endLabel = $"for_end_{labelId}";
            string exitOp = GetInverseOperator(compOp);
            _sb.AppendLine($"{loopLabel}:");
            _sb.AppendLine($"FOR r{indexReg} {limitStr} {stepStr} {exitOp} {endLabel}");
            EmitBlock(forNode.Body);
            _sb.AppendLine($"JUMP {loopLabel}");
            _sb.AppendLine($"{endLabel}:");
        }

        private void EmitIndexAssignment(IndexAssignmentNode node)
        {
            int destArrayReg = EmitExpression(node.ArrayExpr);
            int destIndexReg = EmitExpression(node.IndexExpr);
            int assignValueReg = EmitExpression(node.Value);

            _sb.AppendLine($"SETARR r{destArrayReg} r{destIndexReg} r{assignValueReg}");
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
                        _sb.AppendLine($"LT 1 r{leftReg} {rightStr}");
                        break;
                    case "<=":
                        _sb.AppendLine($"LE 1 r{leftReg} {rightStr}");
                        break;
                    case ">":
                        // a > b -> b < a
                        // Note: Left operand of LT must be a register, so evaluate Right if it's a constant
                        int rightReg = EmitExpression(bin.Right);
                        _sb.AppendLine($"LT 1 r{rightReg} r{leftReg}");
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

        private string GetInverseOperator(string op)
        {
            return op switch
            {
                "<" => ">=",
                "<=" => ">",
                ">" => "<=",
                ">=" => "<",
                "==" => "!=",
                "!=" => "==",
                _ => throw new Exception($"Cannot invert unknown operator: {op}"),
            };
        }

        private string GetExpressionOperandString(ASTNode node)
        {
            if (node is NumberNode num)
                return num.Value.ToString("F1");
            if (node is IdentifierNode id)
            {
                if (_environment.TryGet(id.Name, out int reg))
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
                    if (_propertyMappings.TryGetValue(id.Name, out int propReg))
                        return propReg;
                    if (!_environment.TryGet(id.Name, out int varReg))
                        throw new Exception($"Undefined identifier '{id.Name}'.");
                    return varReg;

                case BinaryOpNode binary:
                    return EmitBinaryOp(binary);

                case CallNode call:
                    if (call.MethodName == "alloc")
                    {
                        if (call.Arguments.Count != 1)
                            throw new Exception(
                                "alloc() expects exactly 1 argument (the array size)."
                            );
                        int sizeReg = EmitExpression(call.Arguments[0]);
                        int destReg = _regCounter++;
                        _sb.AppendLine($"NEWARR r{destReg} r{sizeReg}");
                        return destReg;
                    }
                    if (call.MethodName == "free")
                    {
                        if (call.Arguments.Count != 1)
                            throw new Exception(
                                "free() expects exactly 1 argument (the array to free)."
                            );

                        int freeArrReg = EmitExpression(call.Arguments[0]);

                        _sb.AppendLine($"FREEARR r{freeArrReg}");

                        return 0;
                    }
                    if (call.MethodName == "len")
                    {
                        if (call.Arguments.Count != 1)
                            throw new Exception(
                                "len() expects exactly 1 argument (the array to check)."
                            );
                        int lenArrReg = EmitExpression(call.Arguments[0]);
                        int destReg = _regCounter++;
                        _sb.AppendLine($"LENARR r{destReg} r{lenArrReg}");
                        return destReg;
                    }
                    int returnReg = _regCounter++;
                    EmitCall(call, returnReg);
                    return returnReg;
                case ArrayLiteralNode arrLiteral:
                    int arrReg = _regCounter++;
                    _sb.AppendLine($"NEWARR r{arrReg} {arrLiteral.Elements.Count}");
                    for (int i = 0; i < arrLiteral.Elements.Count; i++)
                    {
                        int elementReg = EmitExpression(arrLiteral.Elements[i]);
                        _sb.AppendLine($"SETARR r{arrReg} {i} r{elementReg}");
                    }
                    return arrReg;
                case IndexAccessNode indexAccess:
                    int targetArrayReg = EmitExpression(indexAccess.ArrayExpr);

                    int accessIndexReg = EmitExpression(indexAccess.IndexExpr);

                    int resultReg = _regCounter++;

                    _sb.AppendLine($"GETARR r{resultReg} r{targetArrayReg} r{accessIndexReg}");
                    return resultReg;
                case LogicalOpNode logicalNode:
                    int logicalResultReg = EmitExpression(logicalNode.Left);
                    string endLabel = $"logic_end{_labelCounter++}";
                    if (logicalNode.Op == "&&")
                    {
                        _sb.AppendLine($"EQ 0 r{logicalResultReg} 0");
                        _sb.AppendLine($"JUMP {endLabel}");
                    }
                    else if (logicalNode.Op == "||")
                    {
                        _sb.AppendLine($"EQ 1 r{logicalResultReg} 1");
                        _sb.AppendLine($"JUMP {endLabel}");
                    }
                    int rightSide = EmitExpression(logicalNode.Right);
                    _sb.AppendLine($"MOVE r{logicalResultReg} r{rightSide}");
                    _sb.AppendLine($"{endLabel}:");
                    return logicalResultReg;
                default:
                    throw new Exception(
                        $"Cannot emit expression node of type {node.GetType().Name}."
                    );
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
                case "%":
                    instruction = "MOD";
                    break;
                case "|":
                    instruction = "BINOR";
                    break;
                case "&":
                    instruction = "BINAND";
                    break;
                case "^":
                    instruction = "BINXOR";
                    break;
                case "<<":
                    instruction = "BINLSH";
                    break;
                case ">>":
                    instruction = "BINRSH";
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

            int[] argRegs = new int[call.Arguments.Count];
            for (int i = 0; i < call.Arguments.Count; i++)
            {
                argRegs[i] = EmitExpression(call.Arguments[i]);
            }

            for (int i = 0; i < call.Arguments.Count; i++)
            {
                _sb.AppendLine($"MOVE r{callBase + i} r{argRegs[i]}");
            }

            _sb.AppendLine($"CALL {call.MethodName}() r{callBase}");

            if (returnReg != 0 && returnReg != callBase)
            {
                _sb.AppendLine($"MOVE r{returnReg} r{callBase}");
            }
        }

        private void EmitBlock(List<ASTNode> statements)
        {
            Environment previous = _environment;

            _environment = new Environment(previous);

            try
            {
                foreach (var stmt in statements)
                {
                    EmitNode(stmt);
                }
            }
            finally
            {
                _environment = previous;
            }
        }
    }

    #endregion

    #region Compiler Entry Point

    public static class RaptorScriptCompiler
    {
        public static string Compile(
            string sourceText,
            Dictionary<string, int>? propertyMappings = null,
            bool printAst = false
        )
        {
            return Compile(sourceText, out _, propertyMappings, printAst);
        }

        public static string Compile(
            string sourceText,
            out IReadOnlyDictionary<string, int> variables,
            Dictionary<string, int>? propertyMappings = null,
            bool printAst = false
        )
        {
            var lexer = new Lexer(sourceText);
            var tokens = lexer.ScanTokens();
            var parser = new Parser(tokens);
            var program = parser.Parse();
            if (printAst)
            {
                Console.WriteLine("=== Abstract syntax tree ===");
                AstPrinter.Print(program);
                Console.WriteLine("============================");
            }
            var emitter = new Emitter(program, propertyMappings);
            string code = emitter.Emit();
            variables = emitter.Globals;
            return code;
        }
    }

    #endregion
}
