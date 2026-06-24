using System.Text;

namespace Frontend;

internal class Lexer(string filePath)
{
    private readonly StreamReader _fs = new(filePath);
    private readonly List<char> _line = [];
    
    private int CharNum { get; set; }
    private int LineNum { get; set; }
    private int CharNumOffset { get; set; }
    private char GetChar(int index = 0) 
    {
        while (CharNum + index >= _line.Count)
        {
            if (_fs.ReadLine() is {} fsLine) _line.AddRange(fsLine);
            else return '\0';
            LineNum++;
            CharNumOffset = CharNum;
            _line.Add('\n');
        }
        return _line[CharNum + index];
    }
    
    private string Position() => $"{LineNum}:{CharNum - CharNumOffset} at {filePath}";
    
    private List<Token> TokenList { get; } = [];
    public int TokenNum { get; set; }
    
    public Token? Read(int index = 0)
    {
        while (TokenNum + index >= TokenList.Count)
        {
            TokenType tokenType;
            switch (GetChar())
            {
                case '\0':
                    return null;
                
                case var _ when char.IsLetter(GetChar()):
                case '_':
                    TokenList.Add(new Token(LexIdentifier(out tokenType), tokenType, Position()));
                    break;
                
                case var _ when char.IsDigit(GetChar()):
                case '.' when GetChar(1) is not '.':
                    TokenList.Add(new Token(LexIntegerLiteral(out tokenType), tokenType, Position()));
                    break;
                
                case '/' when GetChar(1) is '/' or '*':
                    LexComment();
                     break;
                
                case ';':
                    CharNum++;
                    TokenList.Add(new Token(null, TokenType.Semicolon, Position()));
                    break;
                
                case '\'' when GetChar(1) is '\\':
                    TokenList.Add(new Token(LexRuneLiteral(), TokenType.RuneLiteral, Position()));
                    break;
                
                case '\"' or '\'':
                    TokenList.Add(new Token(LexStringLiteral(), TokenType.StringLiteral, Position()));
                    break;
                
                case var _ when char.IsWhiteSpace(GetChar()):
                    LexWhitespace();
                    break;
                
                case var _ when LexOperator() is {} op:
                    TokenList.Add(new Token(op, TokenType.Operator, Position()));
                    break;
                
                default: 
                    throw new LexerException($"Unidentifiable token start: {Position()}");
            }
        }
        return TokenList[TokenNum + index];
    }
    
    private string LexIdentifier(out TokenType identifierType)
    {
        var sb = new StringBuilder();
        do
        {
            sb.Append(GetChar());
            CharNum++;
        } while (char.IsLetterOrDigit(GetChar()) || GetChar() is '_');
        var identifier = sb.ToString();
        
        Span<string> keywords = [
            "break", "func", "interface", "struct",
            "else", "goto", "package", "const", 
            "if", "range", "type", "continue", 
            "for", "import", "return", "var"
        ];
        identifierType = keywords.Contains(identifier) ? TokenType.Keyword : TokenType.Identifier;
        
        return identifier;
    }
    
    private string LexIntegerLiteral(out TokenType integerType)
    {
        var sb = new StringBuilder();
        integerType = TokenType.IntegerLiteral;
        
        var currBase = FindBase();
        
        do
        {
            if ((GetChar() is '_' && !InBase(GetChar(1), currBase)) || (GetChar(1) is '_' && GetChar() is '.')) throw new LexerException($"Invalid integer literal: {Position()}");
            
            if (GetChar() is '.')
            {
                if (integerType is TokenType.FloatingPointLiteral) throw new LexerException($"Invalid integer literal: {Position()}");
                integerType = TokenType.FloatingPointLiteral;
            }
            
            sb.Append(GetChar());
            CharNum++;
        } while (IsValidDigit());
        
        if (GetChar(0) is not 'i') return sb.ToString();
        
        integerType = TokenType.ImaginaryLiteral;
        sb.Append(GetChar());
        CharNum++;
        return sb.ToString();
        
        bool IsValidDigit() => InBase(GetChar(), currBase) || GetChar() is '_' || GetChar() is '.';
    }
    
    private char FindBase()
    {
        var baseChar = char.ToLower(GetChar(1));
        Span<char> digitBases = ['b', 'o', 'x'];
        if (GetChar() is '0' && digitBases.Contains(baseChar))
        {
            CharNum += 2;
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
    
    private void LexComment()
    {
        if (GetChar(1) is '/')
        {
            while (GetChar() is not '\n') CharNum++;
            CharNum++;
            return;
        }
        
        while (GetChar() is not '*' || GetChar(1) is not '/') CharNum++;
        CharNum += 2;
    }
    
    private void LexWhitespace()
    {
        try { do CharNum++; while (char.IsWhiteSpace(GetChar())); }
        catch (EndOfStreamException) { }
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
        
        var largestOperatorLength = operators.MaxBy(s => s.Length)!.Length;
        for (var i = largestOperatorLength ; i > 0; i--)
        {
            try { GetChar(i - 1); }
            catch (EndOfStreamException) { continue; }
            
            var currWord = string.Concat(_line[CharNum..(CharNum + i)]);
            if (operators.Where(n => n.Length == i).ToArray().Contains(currWord))
            {
                CharNum += i;
                return currWord;
            }
        }
        return null;
    }
    
    private string LexRuneLiteral()
    {
        CharNum++; // consumes '
        CharNum++; // consumes /
        
        var rune = GetChar() switch
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
        
        CharNum++; // consumes rune
        if (GetChar() != '\'') throw new LexerException($"Invalid rune literal: {Position()}");
        CharNum++; // consumes final '
        
        return rune;
    }
    
    private string LexStringLiteral()
    {
        var sb = new StringBuilder();
        var stringStart = GetChar();
        
        sb.Append(GetChar());
        CharNum++;
        
        while (GetChar() != stringStart)
        {
            if (GetChar() is '\\' && GetChar(1) == stringStart)
            {
                sb.Append(GetChar());
                CharNum++;
            }
            sb.Append(GetChar());
            CharNum++;
        }
        sb.Append(GetChar());
        CharNum++;
        
        return sb.ToString();
    }
    
    ~Lexer()
    {
        _fs.Dispose();
    }
}

internal enum TokenType
{
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

internal class Token(string? value, TokenType tokenType, string position)
{
    public TokenType Type { get; } = tokenType;
    public string? Value { get; } = value;
    public string Position { get; } = position;
    public override string ToString() => $"{Type}{((Value is not null) ? $": {Value}" : "")}";
}

public class LexerException : Exception
{    
    public LexerException()
    {
    }
    
    public LexerException(string message) : base(message)
    {
    }
    
    public LexerException(string message, Exception inner) : base(message, inner)
    {
    }
}
