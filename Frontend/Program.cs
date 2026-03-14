namespace Frontend;

static class Program
{
    static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("usage: gc file.g");
            return;
        }
        if (!Path.Exists(args[0]))
        {
            Console.WriteLine($"Could not find file: {args[0]}");
            return;
        }
        
        BlockNode parser;
        try
        {
            parser = new BlockNode(args[0]);
            Console.WriteLine(parser.ToString());
        }
        catch (ParserException e)
        {
            Console.Error.WriteLine("Parser Exception");
            Console.Error.WriteLine(e.Message);
        }
    }
}
