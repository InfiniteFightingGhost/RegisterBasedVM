namespace Raptor.Compiler
{
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
}
