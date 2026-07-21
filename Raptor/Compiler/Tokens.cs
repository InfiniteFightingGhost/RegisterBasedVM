namespace Raptor.Compiler
{
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
}
