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
}