namespace Frontend;

internal class Lexer(string filePath): IDisposable
{
    private readonly StreamReader _fs = new StreamReader(filePath);
    private char _currentChar;
    private int _lineNumber = 1;
    private int _columnNumber;
    
    private void Read() => _currentChar = (char)_fs.Read();
    private char Peek() => (char)_fs.Peek();
    
    public Token? Advance()
    {
        Read();
        while (!EndOfStream)
        {
            _columnNumber++;
            if (char.IsLetter(_currentChar) || _currentChar == '_')
            {
                return LexIdentifier();
            }
            if (_currentChar == ';')
            {
                return new Token(TokenType.Semicolon, null); 
            }
            if (_currentChar == '\n')
            {
                _lineNumber++;
                _columnNumber = 1;
                Read();
                continue;
            }
            if (char.IsWhiteSpace(_currentChar))
            {
                Read();
                continue;
            }
            throw new LexerException($"Unidentifiable token start: {filePath} at {_lineNumber}:{_columnNumber}");
        }
        return null;
    }
    
    private Token LexIdentifier()
    {
        string buffer = _currentChar.ToString();
        char nextChar = Peek();
        while (!EndOfStream && (char.IsLetterOrDigit(nextChar) || nextChar == '_'))
        {
            Read();
            _columnNumber++;
            buffer = buffer + _currentChar;
            nextChar = Peek();
        }
        return new Token(TokenType.Identifier, buffer);
    }
    
    private bool EndOfStream => _fs.EndOfStream;
    public void Dispose() => _fs.Dispose();
}

internal enum TokenType
{
    Comment,
    Semicolon,
    Identifier,
    Keyword,
    OperatorAndPunctuation,
    IntegerLiteral,
    FloatingPointLiteral,
    ImaginaryLiteral,
    RuneLiteral,
    StringLiteral,
}

internal class Token(TokenType tokenType, string? value)
{
    TokenType Type { get; } = tokenType;
    string? Value { get; } = value;
    
    public override string ToString() => $"{Type}{((Value is not null) ? $": {Value}" : "")}";
}

internal class LexerException : Exception
{    
    public LexerException()
    {
    }
    
    public LexerException(string message)
        : base(message)
    {
    }
    
    public LexerException(string message, Exception inner)
        : base(message, inner)
    {
    }
}