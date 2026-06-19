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
                case {Type: TokenType.Operator, Value: "{"}:
                    Consume(1);
                    Children.Add(new BlockNode());
                    break;
                
                case {Type: TokenType.Operator, Value: "}"}:
                    if (!isRoot) IsParsed = true;
                    else throw new ParserException($"Unexpected }} at {Token.Position}");
                    Consume(1);
                    break;
                
                case {Type: TokenType.Identifier}:
                case {Type: TokenType.Keyword}:
                    Children.Add(GetNode(Token));
                    break;
                
                case {Type: TokenType.Comment}:
                case {Type: TokenType.Semicolon}:
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

internal class ExpressionNode : Node
{
    public override string ToString() => "Expression";
    public ExpressionType Type { get; }
    public ExpressionNode? LeftHandSide { get; }
    public ExpressionNode? RightHandSide { get; }
    public string Value { get; }
    
    public enum ExpressionType { Root, Operand, Expression, Assignment }
    
    private (float left, float right) GetBindingPower(string? op) => op switch
    {
        null or "(" => (0f, 0f),
        "+" or "-" => (1.0f, 1.1f),
        "*" or "/" => (2.0f, 2.1f),
        _ => throw new ParserException($"Unknown operator at {Token.Position}")
    };
    
    public ExpressionNode(float rightBindingPower = 0, bool makeAtomNode = false)
    {
        if (makeAtomNode 
            || (Peek(1) is var nextToken && nextToken is { Type: TokenType.Semicolon } or { Type: TokenType.Operator, Value: ")" }) 
            || (nextToken is { Type: TokenType.Operator } && rightBindingPower > GetBindingPower(nextToken.Value).left))
        {
            Value = Token.Value!;
            Type = ExpressionType.Operand;
            LeftHandSide = null;
            RightHandSide = null;
            Consume(1);
            return;
        }
        
        switch (Token.Type)
        {
            case TokenType.Identifier or TokenType.StringLiteral or TokenType.RuneLiteral or TokenType.IntegerLiteral or TokenType.FloatingPointLiteral or TokenType.ImaginaryLiteral:
                LeftHandSide = new ExpressionNode(makeAtomNode: true);
                
                if (Token.Type is not TokenType.Operator and not TokenType.Keyword) throw new ParserException($"Expected an operator, got {Token.Type} at {Token.Position}");
                
                Type = ExpressionType.Expression;
                if (Token is { Type: TokenType.Operator, Value: "=" }) Type = ExpressionType.Assignment;
                Value = Token.Value!;
                rightBindingPower = GetBindingPower(Value).right;
                Consume(1);
                RightHandSide = new ExpressionNode(rightBindingPower);
                return;
            
            case TokenType.Operator when Token.Value is "(":
                throw new NotImplementedException();
            
            default:
                throw new ParserException($"Unexpected token at {Token.Position}");
        }
    }
}

internal class BreakNode : Node
{
    public override string ToString() => "Break";
    
    public BreakNode()
    {
        var position = Token.Position;
        Consume(1);
        if (!IsOutOfTokens && Token.Type == TokenType.Semicolon) IsParsed = true;
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
        if (!IsOutOfTokens && Token.Type == TokenType.Semicolon) IsParsed = true;
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
