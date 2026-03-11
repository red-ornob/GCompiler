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
    private string Position(int lineOffset = 0, int charOffset = 0) => $"{_lineCount + lineOffset}:{_charNum + 1 + charOffset} at {filePath}";
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
                _tokenList.Add(new Token(LexIdentifier(out var identifierType), identifierType));
                continue;
            }
            
            if (char.IsDigit(CurrChar) || (CurrChar == '.' && NextChar != '.'))
            {
                _tokenList.Add(new Token(LexIntegerLiteral(out var integerType), integerType));
                continue;
            }
            
            if (CurrChar == '/' && NextChar is '/' or '*')
            {
                _tokenList.Add(new Token(LexComment(), TokenType.Comment));
                continue;
            }
            
            if (CurrChar == ';')
            {
                _tokenList.Add(new Token(null, TokenType.Semicolon));
                continue;
            }
            
            if (CurrChar == '\'')
            {
                _tokenList.Add(new Token(LexRuneLiteral(), TokenType.RuneLiteral));
                continue;
            }
            
            if (CurrChar is '\"' or '\'')
            {
                _tokenList.Add(new Token(LexStringLiteral(), TokenType.StringLiteral));
                continue;
            }
            
            if (char.IsWhiteSpace(CurrChar))
            {
                continue;
            }
            
            if (LexOperatorAndPunctuation() is { } op)
            {
                _tokenList.Add(new Token(op, TokenType.OperatorAndPunctuation));
                continue;
            }
            
            throw new LexerException($"Unidentifiable token start: {Position()}");
        }
        
        return _tokenList;
    }
    
    private string LexIdentifier(out TokenType identifierType)
    {
        var startIndex = _charNum;
        var length = 0;
        for (; _charNum < _line.Length && (char.IsLetterOrDigit(CurrChar) || CurrChar == '_'); _charNum++) length++;
        _charNum--;
        
        Span<string> keywords = [
            "break", "default", "func", "interface", "select",
            "case", "defer", "go", "map", "struct",
            "chan", "else", "goto", "package", "switch",
            "const", "fallthrough", "if", "range", "type",
            "continue", "for", "import", "return", "var"
        ];
        
        identifierType = TokenType.Identifier;
        if (keywords.Contains(_line.Substring(startIndex, length))) identifierType = TokenType.Keyword;
        return _line.Substring(startIndex, length);
    }
    
    private string LexIntegerLiteral(out TokenType integerType)
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
                throw new LexerException($"Invalid integer literal: {Position()}");
            if (CurrChar == '.')
            {
                if (isFloat) throw new LexerException($"Invalid integer literal: {Position()}");
                isFloat = true;
            }
        }
        
        integerType = TokenType.ImaginaryLiteral;
        if (_charNum < _line.Length && CurrChar == 'i')
            return _line.Substring(startIndex, ++length);
        _charNum--;
        
        integerType = TokenType.FloatingPointLiteral;
        if (isFloat) return _line.Substring(startIndex, length);
        
        integerType = TokenType.IntegerLiteral;
        return _line.Substring(startIndex, length);
    }
    
    private bool InBase(char intChar, char baseChar)
    {
        Span<char> binaryDigits = ['0', '1'];
        Span<char> octalDigits = ['0', '1', '2', '3',  '4', '5', '6', '7'];
        Span<char> hexDigits = ['0', '1', '2', '3',  '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F'];
        
        return char.ToLower(baseChar) switch
        {
            '0' => char.IsDigit(intChar),
            'b' => binaryDigits.Contains(intChar),
            'o' => octalDigits.Contains(intChar),
            'x' => hexDigits.Contains(char.ToUpper(intChar)),
            _ => throw new LexerException($"Invalid base character: {Position()}")
        };
    }
    
    private string LexComment()
    {
        var startIndex = _charNum;
        if (NextChar == '/')
        {
            _charNum = _line.Length;
            return _line.Substring(startIndex);
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
        if (_charNum == 0) throw new LexerException($"Invalid comment {Position()}");
        stringBuilder.Append(_line.Substring(0, _charNum + 1));
        return stringBuilder.ToString();
    }
    
    private string? LexOperatorAndPunctuation()
    {
        List<string> punctuations =
        [
            "+", "&", "+=", "&=", "&&", "==", "!=", "(", ")", "-", "|", "-=", "|=", "||", 
            "<", "<=", "[", "]", "*", "^", "*=", "^=", "<-", ">", ">=", "{", "}", "/",
            "<<", "/=", "<<=", "++", "=", ":=", ",", "%", ">>", "%=", ">>=", "--", 
            "!", "...", ":", "&^", "&^=", "~"
        ];
        
        for (var i = 3; i > 0; i--)
        {
            if (_charNum + i > _line.Length) continue;
            var currWord = _line[_charNum..(_charNum + i)];
            if (punctuations.Where(n => n.Length == i).ToList().Contains(currWord))
            {
                _charNum += i;
                return currWord;
            }
        }
        return null;
    }
    
    private string LexRuneLiteral()
    {
        _charNum++;
        if (CurrChar != '\\') throw new LexerException($"Invalid rune literal: {Position()}");
        
        _charNum++;
        var rune = CurrChar;
        
        _charNum++;
        if (CurrChar != '\'') throw new LexerException($"Invalid rune literal: {Position()}");
        
        return rune switch
        {
            'a' => @"\a",
            'b' => @"\b",
            'f' => @"\f",
            'n' => @"\n",
            'r' => @"\r",
            't' => @"\t",
            'v' => @"\v",
            '\\' => @"\\",
            '\'' => @"\'",
            '\"' => @"\""",
            _ => throw new LexerException($"Invalid rune literal: {Position(0, -1)}")
        };
    }
    
    private string LexStringLiteral()
    {
        var startIndex = _charNum;
        var stringStart = _line[_charNum];
        _charNum++;
        
        while (_charNum < _line.Length && CurrChar != stringStart)
        {
            if (CurrChar == '\\' && NextChar == stringStart) _charNum++;
            _charNum++;
        }
        
        if (CurrChar != stringStart) throw new LexerException($"Invalid string literal: {Position()}");
        
        return _line[startIndex..(_charNum + 1)];
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

internal class Token(string? value, TokenType tokenType)
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