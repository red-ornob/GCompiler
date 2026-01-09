namespace Frontend;

internal class Lexer(string filePath): IDisposable
{
    private readonly StreamReader _fs = new(filePath);
    private readonly List<Token> _tokenList = [];
    private string _line = "";
    private int _lineCount;
    private int _charNum;
    
    private char CurrChar => _line[_charNum];
    private char NextChar => _line[_charNum + 1];
    private string Position => $"{_lineCount}:{_charNum + 1} at {filePath}";
    public bool EndOfStream => _fs.EndOfStream;
    
    public List<Token> Advance()
    {
        _tokenList.Clear();
        _line = _fs.ReadLine() ?? string.Empty;
        _lineCount++;
        _charNum = 0;
        
        for (; _charNum < _line.Length; _charNum++)
        {
            if (char.IsLetter(CurrChar) || CurrChar == '_')
            {
                _tokenList.Add(LexIdentifier());
                continue;
            }
            
            if (char.IsDigit(CurrChar))
            {
                _tokenList.Add(LexIntegerLiteral());
                continue;
            }
            
            if (CurrChar == ';')
            {
                _tokenList.Add(new Token(TokenType.Semicolon, null));
                continue;
            }
            
            if (char.IsWhiteSpace(CurrChar))
            {
                continue;
            }
            
            throw new LexerException($"Unidentifiable token start: {Position}");
        }
        
        return _tokenList;
    }
    
    private Token LexIdentifier()
    {
        var startIndex = _charNum;
        var length = 0;
        for (; _charNum < _line.Length && (char.IsLetterOrDigit(CurrChar) || CurrChar == '_'); _charNum++) length++;
        _charNum--;
        return new Token(TokenType.Identifier, _line.Substring(startIndex, length));
    }
    
    private Token LexIntegerLiteral()
    {
        var startIndex = _charNum;
        var length = 0;
        var currBase = '0';
        if (CurrChar == '0' && char.IsLetter(CurrChar))
        {
            length++;
            _charNum++;
            currBase = char.ToLower(CurrChar);
        }
        
        for (; _charNum < _line.Length && (InBase(CurrChar, currBase) || CurrChar == '_'); _charNum++)
        {
            length++;
            if (CurrChar == '_' && !InBase(NextChar, currBase))
            {
                throw new LexerException($"Invalid integer literal: {Position}");
            }
        }
        _charNum--;
        return new Token(TokenType.IntegerLiteral, _line.Substring(startIndex, length));
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
            _ => throw new LexerException($"Invalid base character: {Position}")
        };
    }
    
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