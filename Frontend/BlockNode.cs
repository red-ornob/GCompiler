namespace Frontend;

internal abstract class Node
{
    public abstract string NodeName { get; init; }
    
    public abstract override string ToString();
    
    protected static Lexer? Lexer { get; set; }
    protected static List<Token> Tokens { get; } = [];
    private static int TokenIndex { get; set; }
    protected static Token Token => Tokens[TokenIndex];
    
    protected static T? Parse<T>() where T : Node, new()
    {
        var result = new T();
        return result.IsParsed ? result : null;
    }
    
    protected bool IsParsed;
    protected bool ParsingCondition => !(Lexer!.EndOfStream && TokenIndex >= Tokens.Count) && !IsParsed;
    
    protected void ReplenishTokens()
    {
        if (TokenIndex < Tokens.Count || Lexer!.EndOfStream) return;
        List<Token> newTokens;
        do
        {
            newTokens = Lexer.Advance();
        } while (newTokens.Count == 0 && !Lexer.EndOfStream);
        Tokens.AddRange(newTokens);
    }
    
    protected bool Consume(int amount)
    {
        TokenIndex += amount;
        ReplenishTokens();
        return TokenIndex < Tokens.Count;
    }
    
    protected Token? Peek(int amount)
    {
        if (amount > 0) return Tokens[TokenIndex + amount];
        if (!Consume(amount)) return null;
        var token = Tokens[TokenIndex];
        TokenIndex -= amount;
        return token;
    }
}

internal class BlockNode : Node
{
    public override string NodeName { get; init; } = "block";
    
    private List<Node> Children { get; } = [];
    
    public override string ToString()
    {
        return $"{NodeName}: {string.Join(',', Children.Select(node => node.ToString()).ToList())}";
    }
    
    public BlockNode(string filename)
    {
        Lexer = new Lexer(filename);
        ProcessBlock();
    }
    
    public BlockNode()
    {
        ProcessBlock();
    }
    
    private void ProcessBlock()
    {
        if (Lexer is null) throw new ParserException("Lexer is null");
        
        bool isRoot = Tokens.Count == 0;
        
        ReplenishTokens();
        while (ParsingCondition)
        {
            switch (Token)
            {
                case {TokenType: TokenType.Operator, Value: "{"}:
                    Consume(1);
                    if (Parse<BlockNode>() is { } block) Children.Add(block);
                    else throw new ParserException($"Error parsing block at {Token.Position}");
                    break;
                
                case {TokenType: TokenType.Operator, Value: "}"}:
                    Consume(1);
                    if (!isRoot) IsParsed = true;
                    else throw new ParserException($"Unexpected }} at {Token.Position}");
                    break;
                
                case {TokenType: TokenType.Identifier}:
                case {TokenType: TokenType.Keyword}:
                    if (GetNode(Token)() is { } node) Children.Add(node);
                    else throw new ParserException($"Error parsing {Token.Value} token at {Token.Position}");
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
    }
    
    private static Func<Node?> GetNode(Token token) => token.Value switch
    {
        // "break" => Parse<BreakNode>,
        // "func" => Parse<FuncNode>,
        // "interface" => Parse<InterfaceNode>,
        // "struct" => Parse<StructNode>,
        // "else" => Parse<ElseNode>,
        // "goto" => Parse<GotoNode>,
        // "package" => Parse<PackageNode>,
        // "const" => Parse<ConstNode>,
        // "if" => Parse<IfNode>,
        // "range" => Parse<RangeNode>,
        // "type" => Parse<TypeNode>,
        // "continue" => Parse<ContinueNode>,
        // "for" => Parse<ForNode>,
        // "import" => Parse<ImportNode>,
        // "return" => Parse<ReturnNode>,
        // "var" => Parse<VarNode>,
        // _ => Parse<IdentifierNode>,
        _ => throw new NotImplementedException(),
    };
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
