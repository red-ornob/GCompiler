using System.Text;

namespace Frontend;

internal class Lexer(string filePath): IDisposable
{
    private readonly StreamReader _fs = new(filePath);
    private readonly List<Token> _tokenList = [];
    private string _line = "";
    private int _lineCount;
    private int _charNum;
    
    private char CurrChar => _charNum < _line.Length ? _line[_charNum] : '\0';
    private char NextChar => _charNum + 1 < _line.Length ? _line[_charNum + 1] : '\0';
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
            
            if (char.IsDigit(CurrChar) || CurrChar == '.')
            {
                _tokenList.Add(LexIntegerLiteral());
                continue;
            }
            
            if (CurrChar == '/' && NextChar is '/' or '*')
            {
                _tokenList.Add(LexComment());
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
        
        Span<String> keywords = [
            "break", "default", "func", "interface", "select",
            "case", "defer", "go", "map", "struct",
            "chan", "else", "goto", "package", "switch",
            "const", "fallthrough", "if", "range", "type",
            "continue", "for", "import", "return", "var"
        ];
        
        if (keywords.Contains(_line.Substring(startIndex, length))) return new(TokenType.Keyword, _line.Substring(startIndex, length));
        
        return new(TokenType.Identifier, _line.Substring(startIndex, length));
    }
    
    private Token LexIntegerLiteral()
    {
        var startIndex = _charNum;
        var length = 0;
        var isFloat = false;
        var currBase = '0';
        Span<char> digitBases = ['b', 'o', 'x'];
        if (CurrChar == '0' && digitBases.Contains(char.ToLower(NextChar)))
        {
            currBase = char.ToLower(NextChar);
            length += 2;
            _charNum += 2;
        }
        
        for (; _charNum < _line.Length 
               && (InBase(CurrChar, currBase) || CurrChar == '_' || CurrChar == '.'); _charNum++)
        {
            length++;
            if ((CurrChar == '_' && !InBase(NextChar, currBase)) || (NextChar == '_' && !InBase(CurrChar, currBase))) 
                throw new LexerException($"Invalid integer literal: {Position}");
            if (CurrChar == '.')
            {
                if (isFloat) throw new LexerException($"Invalid integer literal: {Position}");
                isFloat = true;
            }
        }
        
        if (_charNum < _line.Length && CurrChar == 'i') 
            return new(TokenType.ImaginaryLiteral, _line.Substring(startIndex, ++length));
        _charNum--;
        
        if (isFloat) return new(TokenType.FloatingPointLiteral, _line.Substring(startIndex, length));
        return new(TokenType.IntegerLiteral, _line.Substring(startIndex, length));
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
    
    private Token LexComment()
    {
        var startIndex = _charNum;
        if (NextChar == '/')
        {
            _charNum = _line.Length;
            return new(TokenType.Comment, _line.Substring(startIndex));
        }
        
        var stringBuilder = new StringBuilder();
        
        if (!_line.Contains("*/"))
        {
            stringBuilder.Append(_line.Substring(startIndex));
            _line = _fs.ReadLine() ?? string.Empty;
            _lineCount++;
        }
        while (!_line.Contains("*/") && !EndOfStream)
        {
            stringBuilder.Append(_line);
            _line = _fs.ReadLine() ?? string.Empty;
            _lineCount++;
        }
        
        _charNum = _line.IndexOf("*/", StringComparison.Ordinal) + 1;
        if (_charNum == 0) throw new LexerException($"Invalid comment {Position}");
        stringBuilder.Append(_line.Substring(0, _charNum + 1));
        return new(TokenType.Comment, stringBuilder.ToString());
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