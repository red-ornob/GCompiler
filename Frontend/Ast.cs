namespace Frontend;

internal static class Ast
{
    internal abstract record Node(
        List<Node> Children
    );
    
    internal sealed record BlockNode(
        List<Node> Children
    ) : Node(Children);
    
    internal sealed record OperationNode(
        List<Node> Children,
        Node Left,
        Node Right,
        string Operator
    ) : Node(Children);
    
    internal sealed record AtomicNode(
        List<Node> Children,
        string Variable
    ) : Node(Children);
    
    internal sealed record IfNode(
        List<Node> Children,
        Node Condition,
        Node Then,
        Node Else
    ) : Node(Children);
    
    internal sealed record ForNode(
        List<Node> Children,
        Node Declaration,
        Node Condition,
        Node Increment,
        Node Body
    ) : Node(Children);
    
    internal sealed record BreakNode(
        List<Node> Children,
        Node Scope
    ) : Node(Children);
    
    internal sealed record ContinueNode(
        List<Node> Children,
        Node Scope
    ) : Node(Children);
    
    internal sealed record ReturnNode(
        List<Node> Children,
        Node Scope
    ) : Node(Children);
    
    internal sealed record FunctionNode(
        List<Node> Children,
        Node Identifier,
        List<Node> Parameters,
        Node Body
    ) : Node(Children);
}