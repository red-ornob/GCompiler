using System.Text;

namespace Frontend;

internal class Lexer(string filePath): IDisposable
{
    private readonly StreamReader _fs = new StreamReader(filePath);
    private char _currChar;
    private int _lineNumber = 1;
    private int _columnNumber;
    
    
    private void Read() => _currChar = (char)_fs.Read();
    private char Peek() => (char)_fs.Peek();
    
    public Token? Advance()
    {
        Read();
        while (!EndOfStream)
        {
            _columnNumber++;
            if (char.IsLetter(_currChar) || _currChar == '_')
            {
                return LexIdentifier();
            }
            if (char.IsDigit(_currChar))
            {
                return LexIntegerLiteral();
            }
            if (_currChar == ';')
            {
                return new Token(TokenType.Semicolon, null); 
            }
            if (_currChar == '\n')
            {
                _lineNumber++;
                _columnNumber = 1;
                Read();
                continue;
            }
            if (char.IsWhiteSpace(_currChar))
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
        var buffer = new StringBuilder();
        buffer.Append(_currChar);
        char nextChar = Peek();
        while (!EndOfStream && (char.IsLetterOrDigit(nextChar) || nextChar == '_'))
        {
            Read();
            _columnNumber++;
            buffer.Append(_currChar);
            nextChar = Peek();
        }
        return new Token(TokenType.Identifier, buffer.ToString());
    }
    
    private Token LexIntegerLiteral()
    {
        var buffer = new StringBuilder();
        buffer.Append(_currChar);
        char nextChar = Peek();
        Span<char> baseFlags = ['b', 'B', 'o', 'O', 'x', 'X'];
        var currBase = '0';
        if (_currChar == '0' && baseFlags.Contains(nextChar))
        {
            Read();
            _columnNumber++;
            buffer.Append(_currChar);
            nextChar = Peek();
            currBase = _currChar;
        }
        
        while (!EndOfStream && (InBase(nextChar, currBase) || nextChar == '_'))
        {
            Read();
            _columnNumber++;
            buffer.Append(_currChar);
            nextChar = Peek();
            
            if (_currChar == '_' && !InBase(nextChar, currBase))
            {
                throw new LexerException($"Invalid integer literal: {filePath} at {_lineNumber}:{_columnNumber}");
            }
        }
        
        return new Token(TokenType.IntegerLiteral, buffer.ToString());
    }
    
    private bool InBase(char intChar, char baseChar)
    {
        Span<char> binaryDigits = ['0', '1'];
        Span<char> octalDigits = ['0', '1', '2', '3',  '4', '5', '6', '7'];
        Span<char> hexDigits = ['0', '1', '2', '3',  '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F'];
        
        baseChar = char.ToLower(baseChar);
        return baseChar switch
        {
            '0' => char.IsDigit(intChar),
            'b' => binaryDigits.Contains(intChar),
            'o' => octalDigits.Contains(intChar),
            'x' => hexDigits.Contains(char.ToUpper(intChar)),
            _ => throw new LexerException($"Invalid base character: {filePath} at {_lineNumber}:{_columnNumber}")
        };
    }
    
    private bool EndOfStream => _fs.EndOfStream;
    public void Dispose()
    {
        _fs.Dispose();
    }
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

public class LexerException : Exception
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