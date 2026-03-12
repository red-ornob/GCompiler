using System.Text;

namespace Frontend;

internal class Lexer(string filePath)
{
    private readonly StreamReader _fs = new(filePath);
    private readonly List<Token> _tokenList = [];
    private string _line = "";
    private int _lineCount;
    private int _charNum;
    
    private char CurrChar => _charNum < _line.Length ? _line[_charNum] : '\0';
    private char NextChar => _charNum + 1 < _line.Length ? _line[_charNum + 1] : '\0';
    private string Position() => $"{_lineCount}:{_charNum + 1} at {filePath}";
    public bool EndOfStream => _fs.EndOfStream;
    
    public List<Token> Advance()
    {
        _tokenList.Clear();
        _line = _fs.ReadLine() ?? string.Empty;
        _lineCount++;
        _charNum = 0;
        
        for (; _charNum < _line.Length; _charNum++)
        {
            TokenType tokenType;
            switch (CurrChar)
            {
                case var _ when char.IsLetter(CurrChar):
                case '_':
                    _tokenList.Add(new Token(LexIdentifier(out tokenType), tokenType));
                    break;
                
                case var _ when char.IsDigit(CurrChar):
                case '.' when NextChar is not '.':
                    _tokenList.Add(new Token(LexIntegerLiteral(out tokenType), tokenType));
                    break;
                
                case '/' when NextChar is '/' or '*':
                    _tokenList.Add(new Token(LexComment(), TokenType.Comment));
                    break;
                
                case ';':
                    _tokenList.Add(new Token(null, TokenType.Semicolon));
                    break;
                
                case '\'' when NextChar is '\\':
                    _tokenList.Add(new Token(LexRuneLiteral(), TokenType.RuneLiteral));
                    break;
                
                case '\"' or '\'':
                    _tokenList.Add(new Token(LexStringLiteral(), TokenType.StringLiteral));
                    break;
                
                case var _ when char.IsWhiteSpace(CurrChar):
                    break;
                
                case var _ when LexOperator() is {} op:
                    _tokenList.Add(new Token(op, TokenType.Operator));
                    break;
                
                default: 
                    throw new LexerException($"Unidentifiable token start: {Position()}");
            }
        }
        
        return _tokenList;
    }
    
    private string LexIdentifier(out TokenType identifierType)
    {
        var startIndex = _charNum;
        while ((char.IsLetterOrDigit(CurrChar) || CurrChar is '_') && _charNum < _line.Length)
        {
            _charNum++;
        }
        _charNum--;
        var identifier = _line[startIndex..(_charNum + 1)];
        
        Span<string> keywords = [
            "break", "default", "func", "interface", "select",
            "case", "defer", "go", "map", "struct",
            "chan", "else", "goto", "package", "switch",
            "const", "fallthrough", "if", "range", "type",
            "continue", "for", "import", "return", "var"
        ];
        identifierType = keywords.Contains(identifier) ? TokenType.Keyword : TokenType.Identifier;
        
        return identifier;
    }
    
    private string LexIntegerLiteral(out TokenType integerType)
    {
        var startIndex = _charNum;
        integerType = TokenType.IntegerLiteral;
        
        var currBase = FindBase();
        
        while(IsValidDigit() && _charNum < _line.Length)
        {
            if ((CurrChar is '_' && !InBase(NextChar, currBase)) || (NextChar is '_' && CurrChar is '.')) throw new LexerException($"Invalid integer literal: {Position()}");
            
            if (CurrChar is '.')
            {
                if (integerType is TokenType.FloatingPointLiteral) throw new LexerException($"Invalid integer literal: {Position()}");
                integerType = TokenType.FloatingPointLiteral;
            }
            
            _charNum++;
        }
        
        if (_charNum < _line.Length && NextChar is 'i') integerType = TokenType.ImaginaryLiteral;
        else _charNum--;
        
        return _line[startIndex..(_charNum + 1)];
        
        bool IsValidDigit() => InBase(CurrChar, currBase) || CurrChar is '_' || CurrChar is '.';
    }
    
    private char FindBase()
    {
        var baseChar = char.ToLower(NextChar);
        Span<char> digitBases = ['b', 'o', 'x'];
        if (CurrChar is '0' && digitBases.Contains(baseChar))
        {
            _charNum += 2;
            return baseChar;
        }
        return '0';
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
        if (NextChar is '/')
        {
            _charNum = _line.Length;
            return _line[startIndex..];
        }
        
        var stringBuilder = new StringBuilder();
        
        if (!_line.Contains("*/"))
        {
            stringBuilder.Append(_line[startIndex..]);
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
        if (_charNum is 0) throw new LexerException($"Invalid comment {Position()}");
        stringBuilder.Append(_line[0..(_charNum + 1)]);
        return stringBuilder.ToString();
    }
    
    private string? LexOperator()
    {
        List<string> operators =
        [
            "+", "&", "+=", "&=", "&&", "==", "!=", "(", ")", "-", "|", "-=", "|=", "||", 
            "<", "<=", "[", "]", "*", "^", "*=", "^=", "<-", ">", ">=", "{", "}", "/",
            "<<", "/=", "<<=", "++", "=", ":=", ",", "%", ">>", "%=", ">>=", "--", 
            "!", "...", ":", "&^", "&^=", "~"
        ];
        
        var largestOperator = operators.MaxBy(s => s.Length)!;
        for (var i = largestOperator.Length ; i > 0; i--)
        {
            if (_charNum + i > _line.Length) continue;
            var currWord = _line[_charNum..(_charNum + i)];
            if (operators.Where(n => n.Length == i).ToList().Contains(currWord))
            {
                _charNum += i - 1;
                return currWord;
            }
        }
        return null;
    }
    
    private string LexRuneLiteral()
    {
        _charNum++; // consumes '
        _charNum++; // consumes /
        
        var rune = CurrChar switch
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
            _ => throw new LexerException($"Invalid rune literal: {Position()}")
        };
        
        _charNum++;
        if (CurrChar != '\'') throw new LexerException($"Invalid rune literal: {Position()}");
        
        return rune;
    }
    
    private string LexStringLiteral()
    {
        var startIndex = _charNum;
        var stringStart = _line[_charNum];
        _charNum++;
        
        while (CurrChar != stringStart && _charNum < _line.Length)
        {
            if (CurrChar is '\\' && NextChar == stringStart) _charNum++;
            _charNum++;
        }
        
        if (CurrChar != stringStart) throw new LexerException($"Invalid string literal: {Position()}");
        
        return _line[startIndex..(_charNum + 1)];
    }
    
    ~Lexer()
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
    Operator,
    IntegerLiteral,
    FloatingPointLiteral,
    ImaginaryLiteral,
    RuneLiteral,
    StringLiteral,
}

internal class Token(string? value, TokenType tokenType)
{
    public override string ToString() => $"{tokenType}{((value is not null) ? $": {value}" : "")}";
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