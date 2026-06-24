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
        
        try
        {
            // var parser = new Parser(args[0]);
            // Console.WriteLine(parser.ToString());
            var lexer = new Lexer(args[0]);
            while (true)
            {
                var token = lexer.Read();
                if  (token == null) return;
                Console.WriteLine($"{token.Type}: {token.Value ?? "null"}  at {token.Position}");
                lexer.TokenNum++;
            }
        }
        catch (LexerException e)
        {
            Console.Error.WriteLine("Lexer Exception");
            Console.Error.WriteLine(e.Message);
        }
        // catch (EndOfStreamException e)
        // {
            // Console.Error.WriteLine("Lexer Exception");
            // Console.Error.WriteLine(e.Message);
        // }
        // catch (ParserException e)
        // {
        //     Console.Error.WriteLine("Parser Exception");
        //     Console.Error.WriteLine(e.Message);
        // }
    }
}
