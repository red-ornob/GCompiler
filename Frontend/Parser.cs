namespace Frontend;

internal abstract class Node
{
    public abstract string NodeName { get; init; }
    
    public abstract override string ToString();
    
    protected static Lexer? Lexer { get; set; }
    protected static List<Token> Tokens { get; } = [];
    protected static int TokenIndex { get; set; } = 0;
    
    public static T? Parse<T>() where T : Node, new()
    {
        var result = new T();
        return result.IsParsed ? result : null;
    }
    
    protected bool IsParsed;
}

internal class Parser : Node
{ 
    public override string NodeName { get; init; } = "Root";
    
    private List<Node> Children { get; init; } = [];
    
    public override string ToString()
    {
        throw new NotImplementedException();
    }
    
    public Parser(string filename)
    {
        Lexer = new Lexer(filename);
        IsParsed = true;
        while (!Lexer.EndOfStream)
        {
            try
            {
                if (TokenIndex == Tokens.Count) Tokens.AddRange(Lexer.Advance());
                var token = Tokens[TokenIndex];
                if (token.TokenType is not TokenType.Keyword and not TokenType.Identifier) throw new ParserException($"Parser error at {token.Position}");
                Children.Add(GetNode(token)());
            }
            catch (LexerException e)
            {
                Console.Error.WriteLine("Lexer Exception");
                Console.Error.WriteLine(e.Message);
                break;
            }
        }
    }
    
    private static Func<Node> GetNode(Token token) => token.Value switch
    {
        "break" => BreakNode,
        "func" => FuncNode,
        "else" => ElseNode,
        "goto" => GotoNode,
        "package" => PackageNode,
        "const" => ConstNode,
        "if" => IfNode,
        "range" => RangeNode,
        "type" => TypeNode,
        "continue" => ContinueNode,
        "for" => ForNode,
        "import" => ImportNode,
        "return" => ReturnNode,
        "var" => VarNode,
        _ => IdentifierNode
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
