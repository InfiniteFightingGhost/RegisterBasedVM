using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

/*
 * RaptorScript Compiler Engine
 * Translates high-level script syntax (.rapt) to optimized register bytecode assembly (.rasm / .rbc).
 * Feature roadmap: User-defined functions, string primitives, structs, and short-circuit evaluation.
 */

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
        True,
        False,

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

    public record Token(TokenType Type, string Lexeme, int Line, int Column = 0);

    #endregion

    #region AST Nodes

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
    #endregion

    #region Lexer

    public class Lexer
    {
        private readonly string _source;
        private int _index;
        private int _line = 1;
        private int _column = 0;
        private readonly DiagnosticReporter _reporter;

        public Lexer(string source, DiagnosticReporter reporter)
        {
            _source = source;
            _reporter = reporter;
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
                    {
                        _line++;
                        _column = -1;
                    }
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

                Token? result = ScanOperatorOrPunctuation();
                if (result != null)
                    tokens.Add(result);
                else
                {
                    LexerSynchronize();
                }
            }

            tokens.Add(new Token(TokenType.EOF, "", _line, _column));
            return tokens;
        }

        private char Advance()
        {
            _column++;
            return _source[_index++];
        }

        private char Peek() => IsAtEnd() ? '\0' : _source[_index];

        private char PeekNext() => _index + 1 >= _source.Length ? '\0' : _source[_index + 1];

        private bool IsAtEnd() => _index >= _source.Length;

        private void LexerSynchronize()
        {
            while (!IsAtEnd())
            {
                char c = Peek();

                // Stop skipping when we hit whitespace or statement punctuation
                if (char.IsWhiteSpace(c) || c == ';' || c == ')' || c == '}' || c == ']')
                {
                    return;
                }

                Advance(); // Discard the bad character
            }
        }

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
            return new Token(TokenType.Number, val, _line, _column);
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
                "true" => TokenType.True,
                "false" => TokenType.False,
                _ => TokenType.Identifier,
            };

            return new Token(type, val, _line, _column);
        }

        private Token? ScanOperatorOrPunctuation()
        {
            char c = Advance();
            try
            {
                return c switch
                {
                    ';' => new Token(TokenType.Semicolon, ";", _line, _column),
                    '(' => new Token(TokenType.OpenParenthesis, "(", _line, _column),
                    ')' => new Token(TokenType.CloseParenthesis, ")", _line, _column),
                    '[' => new Token(TokenType.OpenBracket, "[", _line, _column),
                    ']' => new Token(TokenType.CloseBracket, "]", _line, _column),
                    '{' => new Token(TokenType.OpenBrace, "{", _line, _column),
                    '}' => new Token(TokenType.CloseBrace, "}", _line, _column),
                    ',' => new Token(TokenType.Comma, ",", _line, _column),

                    '+' => Peek() switch
                    {
                        '=' => ConsumeAndReturn(TokenType.PlusEquals, "+="),
                        '+' => ConsumeAndReturn(TokenType.PlusPlus, "++"),
                        _ => new Token(TokenType.Plus, "+", _line, _column),
                    },
                    '-' => Peek() switch
                    {
                        '=' => ConsumeAndReturn(TokenType.MinusEquals, "-="),
                        '-' => ConsumeAndReturn(TokenType.MinusMinus, "--"),
                        _ => new Token(TokenType.Minus, "-", _line, _column),
                    },
                    '*' => Peek() switch
                    {
                        '=' => ConsumeAndReturn(TokenType.StarEquals, "*="),
                        _ => new Token(TokenType.Star, "*", _line, _column),
                    },
                    '/' => Peek() switch
                    {
                        '=' => ConsumeAndReturn(TokenType.SlashEquals, "/="),
                        _ => new Token(TokenType.Slash, "/", _line, _column),
                    },
                    '=' => Match('=')
                        ? new Token(TokenType.Equal, "==", _line, _column)
                        : new Token(TokenType.Assign, "=", _line, _column),
                    '!' => Peek() switch
                    {
                        '=' => new Token(TokenType.NotEqual, "!=", _line, _column),
                        _ => throw new LexerException($"Unexpected char '!'"),
                    },
                    '<' => Peek() switch
                    {
                        '=' => ConsumeAndReturn(TokenType.LessEqual, "<="),
                        '<' => ConsumeAndReturn(TokenType.LessLess, "<<"),
                        _ => new Token(TokenType.Less, "<", _line, _column),
                    },
                    '>' => Peek() switch
                    {
                        '=' => ConsumeAndReturn(TokenType.GreaterEqual, ">="),
                        '>' => ConsumeAndReturn(TokenType.GreaterGreater, ">>"),
                        _ => new Token(TokenType.Greater, ">", _line, _column),
                    },
                    '%' => Peek() switch
                    {
                        '=' => ConsumeAndReturn(TokenType.PercentEquals, "%="),
                        _ => new Token(TokenType.Percent, "%", _line, _column),
                    },
                    '&' => Peek() switch
                    {
                        '=' => ConsumeAndReturn(TokenType.AmpersandEquals, "&="),
                        '&' => ConsumeAndReturn(TokenType.AmpersandAmpersand, "&&"),
                        _ => new Token(TokenType.Ampersand, "&", _line, _column),
                    },
                    '|' => Peek() switch
                    {
                        '=' => ConsumeAndReturn(TokenType.PipeEquals, "|="),
                        '|' => ConsumeAndReturn(TokenType.PipePipe, "||"),
                        _ => new Token(TokenType.Pipe, "|", _line, _column),
                    },
                    '^' => Peek() switch
                    {
                        '=' => ConsumeAndReturn(TokenType.CaretEquals, "^="),
                        _ => new Token(TokenType.Caret, "^", _line, _column),
                    },
                    _ => throw new LexerException(
                        $"Unexpected character '{c}' at line {_line} at column {_column}"
                    ),
                };
            }
            catch (LexerException ex)
            {
                _reporter.Report(
                    new Diagnostic(
                        "E00017",
                        DiagnosticSeverity.Error,
                        ex.Message,
                        _line,
                        _column,
                        1
                    )
                );
                return null;
            }
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
            return new Token(type, value, _line, _column);
        }
    }

    [Serializable]
    internal class LexerException : Exception
    {
        public LexerException() { }

        public LexerException(string? message)
            : base(message) { }

        public LexerException(string? message, Exception? innerException)
            : base(message, innerException) { }
    }

    #endregion

    #region Parser

    public class Parser
    {
        private class ParseException : Exception { }

        private readonly List<Token> _tokens;
        private int _current;
        private readonly DiagnosticReporter _reporter;

        public Parser(List<Token> tokens, DiagnosticReporter reporter)
        {
            _tokens = tokens;
            _reporter = reporter;
        }

        private void Synchronize()
        {
            // Consume the token that caused/detected the error first
            Advance();

            while (!IsAtEnd())
            {
                // Boundary 1: If the previous token was a semicolon,
                // we are likely at the start of a new statement.
                if (Previous().Type == TokenType.Semicolon)
                    return;

                // Boundary 2: If the next token is a keyword that starts a new statement,
                // we can resume parsing safely here.
                switch (Peek().Type)
                {
                    case TokenType.Var:
                    case TokenType.If:
                    case TokenType.While:
                    case TokenType.For:
                    case TokenType.Return:
                        return;
                }

                // Otherwise, discard this token and keep looking
                Advance();
            }
        }

        public ProgramNode Parse()
        {
            var prog = new ProgramNode();
            while (!IsAtEnd())
            {
                try
                {
                    prog.Statements.Add(ParseStatement());
                }
                catch (ParseException)
                {
                    Synchronize();
                }
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
            return new VarDeclNode(nameToken.Lexeme, initializer)
            {
                Line = nameToken.Line,
                Column = nameToken.Column,
                Length = nameToken.Lexeme.Length,
            };
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

            return new IfNode(condition, thenBlock, elseBlock)
            {
                Line = ifToken.Line,
                Column = ifToken.Column,
                Length = ifToken.Lexeme.Length,
            };
        }

        private ASTNode ParseWhile()
        {
            Token whileToken = Previous();
            Consume(TokenType.OpenParenthesis, "Expected '(' after 'while'.");
            ASTNode condition = ParseExpression();
            Consume(TokenType.CloseParenthesis, "Expected ')' after condition.");

            List<ASTNode> body = ParseBlock();
            return new WhileNode(condition, body)
            {
                Line = whileToken.Line,
                Column = whileToken.Column,
                Length = whileToken.Lexeme.Length,
            };
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
            return new ForNode(initializer, condition, increment, body)
            {
                Line = forToken.Line,
                Column = forToken.Column,
                Length = forToken.Lexeme.Length,
            };
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

            _reporter.Report(
                new Diagnostic(
                    "E0023",
                    DiagnosticSeverity.Error,
                    $"Statement is not a valid assignment or function call.",
                    Peek().Line,
                    Peek().Column,
                    Peek().Lexeme.Length
                )
            );
            throw new ParseException();
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
                            value = new BinaryOpNode(id, "+", value)
                            {
                                Line = op.Line,
                                Column = op.Column,
                                Length = op.Lexeme.Length,
                            };
                            break;
                        case TokenType.MinusEquals:
                            value = new BinaryOpNode(id, "-", value)
                            {
                                Line = op.Line,
                                Column = op.Column,
                                Length = op.Lexeme.Length,
                            };
                            break;
                        case TokenType.SlashEquals:
                            value = new BinaryOpNode(id, "/", value)
                            {
                                Line = op.Line,
                                Column = op.Column,
                                Length = op.Lexeme.Length,
                            };
                            break;
                        case TokenType.StarEquals:
                            value = new BinaryOpNode(id, "*", value)
                            {
                                Line = op.Line,
                                Column = op.Column,
                                Length = op.Lexeme.Length,
                            };
                            break;
                        case TokenType.PipeEquals:
                            value = new BinaryOpNode(id, "|", value)
                            {
                                Line = op.Line,
                                Column = op.Column,
                                Length = op.Lexeme.Length,
                            };
                            break;
                        case TokenType.CaretEquals:
                            value = new BinaryOpNode(id, "^", value)
                            {
                                Line = op.Line,
                                Column = op.Column,
                                Length = op.Lexeme.Length,
                            };
                            break;
                        case TokenType.AmpersandEquals:
                            value = new BinaryOpNode(id, "&", value)
                            {
                                Line = op.Line,
                                Column = op.Column,
                                Length = op.Lexeme.Length,
                            };
                            break;
                        case TokenType.PercentEquals:
                            value = new BinaryOpNode(id, "%", value)
                            {
                                Line = op.Line,
                                Column = op.Column,
                                Length = op.Lexeme.Length,
                            };
                            break;
                    }

                    return new AssignmentNode(id.Name, value)
                    {
                        Line = op.Line,
                        Column = op.Column,
                        Length = op.Lexeme.Length,
                    };
                }
                else if (expr is IndexAccessNode indexAccess)
                {
                    switch (op.Type)
                    {
                        case TokenType.PlusEquals:
                            value = new BinaryOpNode(indexAccess, "+", value)
                            {
                                Line = op.Line,
                                Column = op.Column,
                                Length = op.Lexeme.Length,
                            };
                            break;
                        case TokenType.MinusEquals:
                            value = new BinaryOpNode(indexAccess, "-", value)
                            {
                                Line = op.Line,
                                Column = op.Column,
                                Length = op.Lexeme.Length,
                            };
                            break;
                        case TokenType.SlashEquals:
                            value = new BinaryOpNode(indexAccess, "/", value)
                            {
                                Line = op.Line,
                                Column = op.Column,
                                Length = op.Lexeme.Length,
                            };
                            break;
                        case TokenType.StarEquals:
                            value = new BinaryOpNode(indexAccess, "*", value)
                            {
                                Line = op.Line,
                                Column = op.Column,
                                Length = op.Lexeme.Length,
                            };
                            break;
                        case TokenType.PipeEquals:
                            value = new BinaryOpNode(indexAccess, "|", value)
                            {
                                Line = op.Line,
                                Column = op.Column,
                                Length = op.Lexeme.Length,
                            };
                            break;
                        case TokenType.CaretEquals:
                            value = new BinaryOpNode(indexAccess, "^", value)
                            {
                                Line = op.Line,
                                Column = op.Column,
                                Length = op.Lexeme.Length,
                            };
                            break;
                        case TokenType.AmpersandEquals:
                            value = new BinaryOpNode(indexAccess, "&", value)
                            {
                                Line = op.Line,
                                Column = op.Column,
                                Length = op.Lexeme.Length,
                            };
                            break;
                        case TokenType.PercentEquals:
                            value = new BinaryOpNode(indexAccess, "%", value)
                            {
                                Line = op.Line,
                                Column = op.Column,
                                Length = op.Lexeme.Length,
                            };
                            break;
                    }

                    return new IndexAssignmentNode(
                        indexAccess.ArrayExpr,
                        indexAccess.IndexExpr,
                        value
                    )
                    {
                        Line = op.Line,
                        Column = op.Column,
                        Length = op.Lexeme.Length,
                    };
                }

                _reporter.Report(
                    new Diagnostic(
                        "E0022",
                        DiagnosticSeverity.Error,
                        $"Invalid assignment target",
                        op.Line,
                        op.Column,
                        op.Lexeme.Length
                    )
                );
                throw new ParseException();
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
                        Column = op.Column,
                        Length = op.Lexeme.Length,
                    };
                    return new AssignmentNode(id.Name, value)
                    {
                        Line = op.Line,
                        Column = op.Column,
                        Length = op.Lexeme.Length,
                    };
                }
                else if (expr is IndexAccessNode indexAccess)
                {
                    var value = new BinaryOpNode(indexAccess, mathOp, one)
                    {
                        Line = op.Line,
                        Column = op.Column,
                        Length = op.Lexeme.Length,
                    };
                    return new IndexAssignmentNode(
                        indexAccess.ArrayExpr,
                        indexAccess.IndexExpr,
                        value
                    )
                    {
                        Line = op.Line,
                        Column = op.Column,
                        Length = op.Lexeme.Length,
                    };
                }
                _reporter.Report(
                    new Diagnostic(
                        "E0021",
                        DiagnosticSeverity.Error,
                        $"Invalid increment/decrement target",
                        op.Line,
                        op.Column,
                        op.Lexeme.Length
                    )
                );
                throw new ParseException();
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
                expr = new BinaryOpNode(expr, op.Lexeme, right)
                {
                    Line = op.Line,
                    Column = op.Column,
                    Length = op.Lexeme.Length,
                };
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
                expr = new LogicalOpNode(expr, op.Lexeme, right)
                {
                    Line = op.Line,
                    Column = op.Column,
                    Length = op.Lexeme.Length,
                };
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
                expr = new LogicalOpNode(expr, op.Lexeme, right)
                {
                    Line = op.Line,
                    Column = op.Column,
                    Length = op.Lexeme.Length,
                };
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
                expr = new BinaryOpNode(expr, op.Lexeme, right)
                {
                    Line = op.Line,
                    Column = op.Column,
                    Length = op.Lexeme.Length,
                };
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
                expr = new BinaryOpNode(expr, op.Lexeme, right)
                {
                    Line = op.Line,
                    Column = op.Column,
                    Length = op.Lexeme.Length,
                };
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
                expr = new BinaryOpNode(expr, op.Lexeme, right)
                {
                    Line = op.Line,
                    Column = op.Column,
                    Length = op.Lexeme.Length,
                };
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
                expr = new BinaryOpNode(expr, op.Lexeme, right)
                {
                    Line = op.Line,
                    Column = op.Column,
                    Length = op.Lexeme.Length,
                };
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
                expr = new BinaryOpNode(expr, op.Lexeme, right)
                {
                    Line = op.Line,
                    Column = op.Column,
                    Length = op.Lexeme.Length,
                };
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
                expr = new BinaryOpNode(expr, op.Lexeme, right)
                {
                    Line = op.Line,
                    Column = op.Column,
                    Length = op.Lexeme.Length,
                };
            }

            return expr;
        }

        private ASTNode ParsePrimary()
        {
            if (Match(TokenType.Number))
            {
                Token numToken = Previous();
                return new NumberNode(double.Parse(numToken.Lexeme))
                {
                    Line = numToken.Line,
                    Column = numToken.Column,
                    Length = numToken.Lexeme.Length,
                };
            }
            if (Match(TokenType.False))
            {
                return new NumberNode(0)
                {
                    Line = Previous().Line,
                    Column = Previous().Column,
                    Length = Previous().Lexeme.Length,
                };
            }
            if (Match(TokenType.True))
            {
                return new NumberNode(1)
                {
                    Line = Previous().Line,
                    Column = Previous().Column,
                    Length = Previous().Lexeme.Length,
                };
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
                    } while (Match(TokenType.Comma));
                }
                Consume(TokenType.CloseBracket, "Expected ']' after array elements.");
                return new ArrayLiteralNode(elements)
                {
                    Line = bracketToken.Line,
                    Column = bracketToken.Column,
                    Length = bracketToken.Lexeme.Length, // TODO: Make this actually have the whole array literal("[1,2,3,4]") be the length
                };
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
                    return new CallNode(idToken.Lexeme, args)
                    {
                        Line = idToken.Line,
                        Column = idToken.Column,
                        Length = idToken.Lexeme.Length,
                    };
                }
                ASTNode expr = new IdentifierNode(idToken.Lexeme)
                {
                    Line = idToken.Line,
                    Column = idToken.Column,
                    Length = idToken.Lexeme.Length,
                };
                while (Match(TokenType.OpenBracket))
                {
                    ASTNode indexExpr = ParseExpression();
                    Consume(TokenType.CloseBracket, "Expected ']' after array index.");
                    expr = new IndexAccessNode(expr, indexExpr)
                    {
                        Line = idToken.Line,
                        Column = idToken.Column,
                        Length = idToken.Lexeme.Length,
                    };
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
            _reporter.Report(
                new Diagnostic(
                    "E0020",
                    DiagnosticSeverity.Error,
                    $"Expected expression",
                    Peek().Line,
                    Peek().Column,
                    Peek().Lexeme.Length
                )
            );
            throw new ParseException();
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
            _reporter.Report(
                new Diagnostic(
                    "E0019",
                    DiagnosticSeverity.Error,
                    $"{message}",
                    Peek().Line,
                    Peek().Column,
                    Peek().Lexeme.Length
                )
            );
            throw new ParseException();
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
        private readonly DiagnosticReporter _reporter;

        public Emitter(
            ProgramNode program,
            DiagnosticReporter reporter,
            Dictionary<string, int>? propertyMappings = null
        )
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
            _reporter = reporter;
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
                    if (call.MethodName is "free" or "alloc" or "len")
                    {
                        EmitExpression(call);
                    }
                    else
                    {
                        EmitCall(call, 0); // Accumulate in r0 by default
                    }
                    break;
                default:
                    _reporter.Report(
                        new Diagnostic(
                            "E0023",
                            DiagnosticSeverity.Error,
                            $"Cannot emit node of type {node.GetType().Name} at root level.",
                            node.Line,
                            node.Column,
                            node.Length
                        )
                    );
                    throw new EmitException();
            }
        }

        private void EmitVarDecl(VarDeclNode decl)
        {
            if (_environment.Variables.ContainsKey(decl.Name))
            {
                _reporter.Report(
                    new Diagnostic(
                        "E0019",
                        DiagnosticSeverity.Error,
                        $"Variable '{decl.Name}' is already declared in this scope.",
                        decl.Line,
                        decl.Column,
                        decl.Length
                    )
                );
            }
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
                _reporter.Report(
                    new Diagnostic(
                        "E0018",
                        DiagnosticSeverity.Error,
                        $"Variable '{assign.TargetName}' is not declared.",
                        assign.Line,
                        assign.Column,
                        assign.Length
                    )
                );
                regIndex = 0; // Fallback register index to continue emitting and gather other errors
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
            var current = _environment;
            _environment = new Environment(current);
            try
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
                        _reporter.Report(
                            new Diagnostic(
                                "E0020",
                                DiagnosticSeverity.Error,
                                "For-loop initializer must be a variable declaration or assignment.",
                                forNode.Line,
                                forNode.Column,
                                forNode.Length
                            )
                        );
                        throw new EmitException();
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
                        _reporter.Report(
                            new Diagnostic(
                                "E0021",
                                DiagnosticSeverity.Error,
                                "For-loop condition must be a comparison (e.g., i < 10).",
                                forNode.Line,
                                forNode.Column,
                                forNode.Length
                            )
                        );
                        throw new EmitException();
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
            finally
            {
                if (_environment.Enclosing == null)
                    throw new Exception("How is this possible?");
                _environment = _environment.Enclosing;
            }
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
                _ => throw new EmitException($"Cannot invert unknown operator: {op}"),
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
                _reporter.Report(
                    new Diagnostic(
                        "E0018",
                        DiagnosticSeverity.Error,
                        $"Undefined identifier '{id.Name}'",
                        id.Line,
                        id.Column,
                        id.Length
                    )
                );
                return "r0";
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
                    {
                        _reporter.Report(
                            new Diagnostic(
                                "E0018",
                                DiagnosticSeverity.Error,
                                $"Undefined identifier '{id.Name}'",
                                id.Line,
                                id.Column,
                                id.Length
                            )
                        );
                        return 0;
                    }
                    return varReg;

                case BinaryOpNode binary:
                    return EmitBinaryOp(binary);

                case CallNode call:
                    if (call.MethodName == "alloc")
                    {
                        if (call.Arguments.Count != 1)
                        {
                            _reporter.Report(
                                new Diagnostic(
                                    "E0024",
                                    DiagnosticSeverity.Error,
                                    "alloc() expects exactly 1 argument (the array size).",
                                    call.Line,
                                    call.Column,
                                    call.Length
                                )
                            );
                            throw new EmitException();
                        }
                        int sizeReg = EmitExpression(call.Arguments[0]);
                        int destReg = _regCounter++;
                        _sb.AppendLine($"NEWARR r{destReg} r{sizeReg}");
                        return destReg;
                    }
                    if (call.MethodName == "free")
                    {
                        if (call.Arguments.Count != 1)
                        {
                            _reporter.Report(
                                new Diagnostic(
                                    "E0025",
                                    DiagnosticSeverity.Error,
                                    "free() expects exactly 1 argument (the array to free).",
                                    call.Line,
                                    call.Column,
                                    call.Length
                                )
                            );
                            throw new EmitException();
                        }

                        int freeArrReg = EmitExpression(call.Arguments[0]);

                        _sb.AppendLine($"FREEARR r{freeArrReg}");

                        return 0;
                    }
                    if (call.MethodName == "len")
                    {
                        if (call.Arguments.Count != 1)
                        {
                            _reporter.Report(
                                new Diagnostic(
                                    "E0026",
                                    DiagnosticSeverity.Error,
                                    "len() expects exactly 1 argument (the array to check).",
                                    call.Line,
                                    call.Column,
                                    call.Length
                                )
                            );
                            throw new EmitException();
                        }
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
                    int zeroRegLogical = _regCounter++;
                    _sb.AppendLine($"LOADC r{zeroRegLogical} 0.0");
                    if (logicalNode.Op == "&&")
                    {
                        // Jump to endLabel if Left is falsey (r{logicalResultReg} == 0.0)
                        _sb.AppendLine($"EQ 0 r{logicalResultReg} r{zeroRegLogical}");
                        _sb.AppendLine($"JUMP {endLabel}");
                    }
                    else if (logicalNode.Op == "||")
                    {
                        // Jump to endLabel if Left is truthy (r{logicalResultReg} != 0.0)
                        _sb.AppendLine($"EQ 1 r{logicalResultReg} r{zeroRegLogical}");
                        _sb.AppendLine($"JUMP {endLabel}");
                    }
                    int rightSide = EmitExpression(logicalNode.Right);
                    _sb.AppendLine($"MOVE r{logicalResultReg} r{rightSide}");
                    _sb.AppendLine($"{endLabel}:");
                    return logicalResultReg;
            }

            throw new EmitException();
        }

        private int EmitBinaryOp(BinaryOpNode binary)
        {
            int leftReg = EmitExpression(binary.Left);
            int rightReg = EmitExpression(binary.Right);
            int resReg = _regCounter++;

            switch (binary.Op)
            {
                case "+":
                    _sb.AppendLine($"ADD r{resReg} r{leftReg} r{rightReg}");
                    return resReg;
                case "-":
                    _sb.AppendLine($"SUB r{resReg} r{leftReg} r{rightReg}");
                    return resReg;
                case "*":
                    _sb.AppendLine($"MUL r{resReg} r{leftReg} r{rightReg}");
                    return resReg;
                case "/":
                    _sb.AppendLine($"DIV r{resReg} r{leftReg} r{rightReg}");
                    return resReg;
                case "%":
                    _sb.AppendLine($"MOD r{resReg} r{leftReg} r{rightReg}");
                    return resReg;
                case "|":
                    _sb.AppendLine($"BINOR r{resReg} r{leftReg} r{rightReg}");
                    return resReg;
                case "&":
                    _sb.AppendLine($"BINAND r{resReg} r{leftReg} r{rightReg}");
                    return resReg;
                case "^":
                    _sb.AppendLine($"BINXOR r{resReg} r{leftReg} r{rightReg}");
                    return resReg;
                case "<<":
                    _sb.AppendLine($"BINLSH r{resReg} r{leftReg} r{rightReg}");
                    return resReg;
                case ">>":
                    _sb.AppendLine($"BINRSH r{resReg} r{leftReg} r{rightReg}");
                    return resReg;
                case "<":
                    {
                        string skipLabel = $"cmp_skip{_labelCounter++}";
                        _sb.AppendLine($"LOADC r{resReg} 1.0");
                        _sb.AppendLine($"LT 1 r{leftReg} r{rightReg}");
                        _sb.AppendLine($"JUMP {skipLabel}");
                        _sb.AppendLine($"LOADC r{resReg} 0.0");
                        _sb.AppendLine($"{skipLabel}:");
                        return resReg;
                    }
                case "<=":
                    {
                        string skipLabel = $"cmp_skip{_labelCounter++}";
                        _sb.AppendLine($"LOADC r{resReg} 1.0");
                        _sb.AppendLine($"LE 1 r{leftReg} r{rightReg}");
                        _sb.AppendLine($"JUMP {skipLabel}");
                        _sb.AppendLine($"LOADC r{resReg} 0.0");
                        _sb.AppendLine($"{skipLabel}:");
                        return resReg;
                    }
                case ">":
                    {
                        // a > b -> b < a
                        string skipLabel = $"cmp_skip{_labelCounter++}";
                        _sb.AppendLine($"LOADC r{resReg} 1.0");
                        _sb.AppendLine($"LT 1 r{rightReg} r{leftReg}");
                        _sb.AppendLine($"JUMP {skipLabel}");
                        _sb.AppendLine($"LOADC r{resReg} 0.0");
                        _sb.AppendLine($"{skipLabel}:");
                        return resReg;
                    }
                case ">=":
                    {
                        // a >= b -> b <= a
                        string skipLabel = $"cmp_skip{_labelCounter++}";
                        _sb.AppendLine($"LOADC r{resReg} 1.0");
                        _sb.AppendLine($"LE 1 r{rightReg} r{leftReg}");
                        _sb.AppendLine($"JUMP {skipLabel}");
                        _sb.AppendLine($"LOADC r{resReg} 0.0");
                        _sb.AppendLine($"{skipLabel}:");
                        return resReg;
                    }
                case "==":
                    {
                        string skipLabel = $"cmp_skip{_labelCounter++}";
                        _sb.AppendLine($"LOADC r{resReg} 1.0");
                        _sb.AppendLine($"EQ 1 r{leftReg} r{rightReg}");
                        _sb.AppendLine($"JUMP {skipLabel}");
                        _sb.AppendLine($"LOADC r{resReg} 0.0");
                        _sb.AppendLine($"{skipLabel}:");
                        return resReg;
                    }
                case "!=":
                    {
                        string skipLabel = $"cmp_skip{_labelCounter++}";
                        _sb.AppendLine($"LOADC r{resReg} 1.0");
                        _sb.AppendLine($"EQ 0 r{leftReg} r{rightReg}");
                        _sb.AppendLine($"JUMP {skipLabel}");
                        _sb.AppendLine($"LOADC r{resReg} 0.0");
                        _sb.AppendLine($"{skipLabel}:");
                        return resReg;
                    }
                default:
                    _reporter.Report(
                        new Diagnostic(
                            "E0028",
                            DiagnosticSeverity.Error,
                            $"Unsupported binary operator: {binary.Op}",
                            binary.Line,
                            binary.Column,
                            binary.Length
                        )
                    );
                    throw new EmitException();
            }
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

    [Serializable]
    internal class EmitException : Exception
    {
        public EmitException() { }

        public EmitException(string? message)
            : base(message) { }

        public EmitException(string? message, Exception? innerException)
            : base(message, innerException) { }
    }

    #endregion

    #region Compiler Entry Point

    public static class RaptorScriptCompiler
    {
        public static string Compile(
            string sourceText,
            DiagnosticReporter reporter,
            Dictionary<string, int>? propertyMappings = null,
            bool printAst = false
        )
        {
            return Compile(sourceText, out _, reporter, propertyMappings, printAst);
        }

        public static string Compile(
            string sourceText,
            out IReadOnlyDictionary<string, int> variables,
            DiagnosticReporter reporter,
            Dictionary<string, int>? propertyMappings = null,
            bool printAst = false
        )
        {
            var lexer = new Lexer(sourceText, reporter);
            var tokens = lexer.ScanTokens();
            if (reporter.HasErrors)
            {
                throw new CompileException("Syntax errors detected.");
            }
            var parser = new Parser(tokens, reporter);
            var program = parser.Parse();
            if (reporter.HasErrors)
            {
                throw new CompileException("Syntax errors detected.");
            }
            if (printAst)
            {
                Console.WriteLine("=== Abstract syntax tree ===");
                AstPrinter.Print(program);
                Console.WriteLine("============================");
            }
            var emitter = new Emitter(program, reporter, propertyMappings);
            try
            {
                string code = emitter.Emit();
                if (reporter.HasErrors)
                {
                    throw new CompileException("Compilation errors detected.");
                }
                variables = emitter.Globals;
                return code;
            }
            catch (EmitException)
            {
                throw new CompileException("Compilation errors detected.");
            }
        }
    }

    [Serializable]
    public class CompileException : Exception
    {
        public CompileException() { }

        public CompileException(string? message)
            : base(message) { }

        public CompileException(string? message, Exception? innerException)
            : base(message, innerException) { }
    }

    #endregion
}
