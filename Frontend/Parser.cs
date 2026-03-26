namespace Frontend;

internal class Parser
{
    private Node RootNode { get; }
    
    public Parser(string filename)
    {
        Node.Lexer = new Lexer(filename);
        RootNode = new BlockNode();
    }
    
    public override string ToString() => RootNode.ToString();
}

internal abstract class Node
{
    public abstract override string ToString();
    
    public static Lexer? Lexer
    {
        get;
        set => field ??= value;
    }
    
    protected static List<Token> Tokens { get; } = [];
    private static int TokenIndex { get; set; }
    protected static Token Token => Tokens[TokenIndex];
    
    protected static T? Parse<T>() where T : Node, new()
    {
        try
        {
            return new T();
        }
        catch (ParserException)
        {
            return null;
        }
    }
    
    protected bool IsParsed;
    protected static bool IsOutOfTokens => Lexer!.EndOfStream && TokenIndex >= Tokens.Count;
    
    protected static void ReplenishTokens(int minTokens = 1)
    {
        if (TokenIndex < Tokens.Count || Lexer!.EndOfStream) return;
        while (minTokens > 0 && !Lexer.EndOfStream)
        {
            var newTokens = Lexer.Advance();
            Tokens.AddRange(newTokens);
            minTokens -= newTokens.Count;
        }
    }
    
    protected bool Consume(int amount)
    {
        TokenIndex += amount;
        ReplenishTokens(amount);
        return TokenIndex < Tokens.Count;
    }
    
    protected Token? Peek(int amount)
    {
        if (amount < 0) return Tokens[TokenIndex + amount];
        if (!Consume(amount)) return null;
        var token = Tokens[TokenIndex];
        TokenIndex -= amount;
        return token;
    }
}

internal class BlockNode : Node
{
    private List<Node> Children { get; } = [];
    
    public override string ToString() => $"Block({string.Join(",", Children.Select(node => node.ToString()))})";
    
    public BlockNode()
    {
        if (Lexer is null) throw new ParserException("Lexer is null");
        
        bool isRoot = Tokens.Count == 0;
        var initialPos = isRoot ? null : Token.Position;
        
        ReplenishTokens();
        while (!(IsOutOfTokens) && !IsParsed)
        {
            switch (Token)
            {
                case {TokenType: TokenType.Operator, Value: "{"}:
                    Consume(1);
                    Children.Add(new BlockNode());
                    break;
                
                case {TokenType: TokenType.Operator, Value: "}"}:
                    if (!isRoot) IsParsed = true;
                    else throw new ParserException($"Unexpected }} at {Token.Position}");
                    Consume(1);
                    break;
                
                case {TokenType: TokenType.Identifier}:
                case {TokenType: TokenType.Keyword}:
                    Children.Add(GetNode(Token));
                    break;
                
                case {TokenType: TokenType.Comment}:
                case {TokenType: TokenType.Semicolon}:
                    Consume(1);
                    break;
                
                default:
                    throw new ParserException($"{Token.Value} not allowed at {Token.Position}");
            }
            ReplenishTokens();
        }
        
        if (isRoot) IsParsed = true;
        else if (!IsParsed) throw new ParserException($"Incomplete block at {initialPos}");
    }
    
    private static Node GetNode(Token token) => token.Value switch
    {
        "break" => new BreakNode(),
        // "func" => new FuncNode(),
        // "interface" => new InterfaceNode(),
        // "struct" => new StructNode(),
        // "else" => new ElseNode(),
        // "goto" => new GotoNode(),
        // "package" => new PackageNode(),
        // "const" => new ConstNode(),
        // "if" => new IfNode(),
        // "range" => new RangeNode(),
        // "type" => new TypeNode(),
        "continue" => new ContinueNode(),
        // "for" => new ForNode(),
        // "import" => new ImportNode(),
        // "return" => new ReturnNode(),
        // "var" => new VarNode(),
        // _ => new IdentifierNode(),
        _ => throw new NotImplementedException(),
    };
}

internal class BreakNode : Node
{
    public override string ToString() => "Break";
    
    public BreakNode()
    {
        var position = Token.Position;
        Consume(1);
        if (!IsOutOfTokens && Token.TokenType == TokenType.Semicolon) IsParsed = true;
        else throw new ParserException($"Missing semicolon at {position}");
        Consume(1);
    }
}

internal class ContinueNode : Node
{
    public override string ToString() => "Continue";
    
    public ContinueNode()
    {
        var position = Token.Position;
        Consume(1);
        if (!IsOutOfTokens && Token.TokenType == TokenType.Semicolon) IsParsed = true;
        else throw new ParserException($"Missing semicolon at {position}");
        Consume(1);
    }
}

public class ParserException : Exception
{
    public ParserException()
    {
    }
    
    public ParserException(string message) : base(message)
    {
    }
    
    public ParserException(string message, Exception inner) : base(message, inner)
    {
    }
}
