namespace Raptor.Compiler
{
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
}
