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
        
        var parser = new Parser(args[0]);
        Console.WriteLine(parser.ToString());
    }
}
