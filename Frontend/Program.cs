namespace Frontend;

static class Program
{
    static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("usage: gc file.gc");
            return;
        }
        if (!Path.Exists(args[0]))
        {
            Console.WriteLine("Could not find file: " + args[0]);
            return;
        }
        
        
        using var lexer = new Lexer(args[0]);
        while (true)
        {
            try
            {
                if (lexer.Advance() is { } token) Console.WriteLine(token.ToString());
                else break;
            }
            catch (LexerException e)
            {
                Console.Error.Write(e.Message);
                break;
            }
        }
    }
}