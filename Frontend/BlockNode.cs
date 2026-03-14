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

internal class BlockNode : Node
{
    public override string NodeName { get; init; } = "Block";
    
    private List<Node> Children { get; init; } = [];
    
    public override string ToString()
    {
        throw new NotImplementedException();
    }
    
    public BlockNode(string filename) : this()
    {
        Lexer = new Lexer(filename);
    }
    
    public BlockNode()
    {
        if (Lexer is null) throw new ParserException("Lexer is null");
        while (!Lexer!.EndOfStream)
        {
            try
            {
                if (TokenIndex == Tokens.Count)
                {
                    var newTokens = Lexer.Advance();
                    if (newTokens.Count > 0) Tokens.AddRange(newTokens);
                    else break;
                }
                
                var token = Tokens[TokenIndex];
                if (token.TokenType is TokenType.Comment) continue;
                if (token.TokenType is not TokenType.Keyword and not TokenType.Identifier) throw new ParserException($"Parser error at {token.Position}");
                
                if (GetNode(token)() is { } node) Children.Add(node);
                else throw new ParserException($"Parser error at {token.Position}");
            }
            catch (LexerException e)
            {
                Console.Error.WriteLine("Lexer Exception");
                Console.Error.WriteLine(e.Message);
                return;
            }
        }
        IsParsed = true;
    }
    
    private static Func<Node?> GetNode(Token token) => token.Value switch
    {
        "break" => Parse<BreakNode>,
        "func" => Parse<FuncNode>,
        "interface" => Parse<InterfaceNode>,
        "struct" => Parse<StructNode>,
        "else" => Parse<ElseNode>,
        "goto" => Parse<GotoNode>,
        "package" => Parse<PackageNode>,
        "const" => Parse<ConstNode>,
        "if" => Parse<IfNode>,
        "range" => Parse<RangeNode>,
        "type" => Parse<TypeNode>,
        "continue" => Parse<ContinueNode>,
        "for" => Parse<ForNode>,
        "import" => Parse<ImportNode>,
        "return" => Parse<ReturnNode>,
        "var" => Parse<VarNode>,
        _ => Parse<IdentifierNode>,
    };
}

internal class BreakNode : Node
{
}

internal class FuncNode : Node
{
}

internal class InterfaceNode : Node
{
}

internal class StructNode : Node
{
}

internal class ElseNode : Node
{
}

internal class GotoNode : Node
{
}

internal class PackageNode : Node
{
}

internal class ConstNode : Node
{
}

internal class IfNode : Node
{
}

internal class RangeNode : Node
{
}

internal class TypeNode : Node
{
}

internal class ContinueNode : Node
{
}

internal class ForNode : Node
{
}

internal class ImportNode : Node
{
}

internal class ReturnNode : Node
{
}

internal class VarNode : Node
{
}

internal class IdentifierNode : Node
{
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
