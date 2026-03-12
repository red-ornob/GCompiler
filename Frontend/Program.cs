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
        
        using var lexer = new Lexer(args[0]);
        while (!lexer.EndOfStream)
        {
            try
            {
                foreach (var token in lexer.Advance()) Console.WriteLine(token);
            }
            catch (LexerException e)
            {
                Console.Error.WriteLine("Lexer Exception");
                Console.Error.WriteLine(e.Message);
                break;
            }
        }
    }
}