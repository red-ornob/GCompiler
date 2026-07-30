namespace Frontend;

internal class Parser(Lexer lexer)
{
    private Lexer Lexer { get; } = lexer;
    private Token CurrToken => Lexer.Read() ?? throw new EndOfStreamException($"Unexpected end of stream after {Lexer.Read(-1)!.Position}");
    private Token NextToken => Lexer.Read(1) ?? throw new EndOfStreamException($"Unexpected end of stream after {Lexer.Read()!.Position}");
    private void Advance(int amount = 1) => Lexer.TokenNum += amount;
    
    private bool IsAtomicType(TokenType type) => type is TokenType.Identifier or TokenType.IntegerLiteral or TokenType.FloatingPointLiteral or TokenType.ImaginaryLiteral or TokenType.RuneLiteral or TokenType.StringLiteral;
    
    public Ast.Node Expression(float rightBindingPower, bool rootExpression = true)
    {
        if (NextToken is { Type: TokenType.Semicolon }
            || (NextToken is { Type: TokenType.Operator, Value: ")" } && !rootExpression)
            || (NextToken is { Type: TokenType.Operator } && rightBindingPower > GetBindingPower(NextToken.Value).left))
        {
            return IsAtomicType(CurrToken.Type) 
                ? new Ast.AtomicNode([], CurrToken.Value!)
                : throw new ParserException($"Unexpected token in expression at {CurrToken.Position}");
        }
        
        if (IsAtomicType(CurrToken.Type))
        {
            var leftSide = new Ast.AtomicNode([], CurrToken.Value!);
            Advance();
            
            if (CurrToken.Type is not TokenType.Operator and not TokenType.Keyword) throw new ParserException($"Expected an operator, got {CurrToken.Type} at {CurrToken.Position}");
            
            var value = CurrToken.Value!;
            rightBindingPower = GetBindingPower(value).right;
            Advance();
            var rightSide = Expression(rightBindingPower, rootExpression);
            return new Ast.OperationNode([leftSide, rightSide], leftSide, rightSide, value);
        }
        
        if (CurrToken is { Type: TokenType.Operator, Value: "(" })
        {
            Advance();
            return Expression(0.1f, false);
        }
        
        throw new ParserException($"Unexpected token in expression at {CurrToken.Position}");
    }
    
    private (float left, float right) GetBindingPower(string? op) => op switch
    {
        null or "=" => (0f, 0.1f),
        "+" or "-" => (1.0f, 1.1f),
        "*" or "/" => (2.0f, 2.1f),
        _ => throw new ParserException($"Unknown operator at {CurrToken.Position}")
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

/*
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
*/
