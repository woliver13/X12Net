using woliver13.X12Net.CLI;

// x12tool <command> [options]
//   parse    <edi-text>                          — list segment IDs
//   validate <edi-text>                          — structural validation
//   edit     <edi-text> <seg> <elem-idx> <value> — edit one element

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: x12tool <parse|validate|edit> <edi-text> [segment element-index new-value]");
    return 1;
}

string command = args[0].ToLowerInvariant();
string input   = args[1];

switch (command)
{
    case "parse":
    {
        var result = X12Tool.Parse(input);
        if (!result.Success) { Console.Error.WriteLine(result.Error); return 1; }
        foreach (var id in result.SegmentIds)
            Console.WriteLine(id);
        return 0;
    }
    case "validate":
    {
        var result = X12Tool.Validate(input);
        if (result.IsValid) { Console.WriteLine("OK"); return 0; }
        foreach (var err in result.Errors)
            Console.Error.WriteLine(err);
        return 1;
    }
    case "edit":
    {
        if (args.Length < 5)
        {
            Console.Error.WriteLine("Usage: x12tool edit <edi-text> <segment-id> <element-index> <new-value>");
            return 1;
        }
        if (!int.TryParse(args[3], out int idx))
        {
            Console.Error.WriteLine("element-index must be an integer.");
            return 1;
        }
        var result = X12Tool.Edit(input, args[2], idx, args[4]);
        if (!result.Success) { Console.Error.WriteLine(result.Error); return 1; }
        Console.Write(result.Output);
        return 0;
    }
    default:
        Console.Error.WriteLine($"Unknown command '{command}'. Expected: parse, validate, edit.");
        return 1;
}
