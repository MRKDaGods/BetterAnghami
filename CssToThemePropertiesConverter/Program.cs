using MRK;

Console.WriteLine("Input css:");

var input = Console.In.ReadToEnd();

var props = CssToThemePropertyConverter.Convert(input, out var unparsedLines);
Console.WriteLine("Properties: ");

foreach (var prop in props)
{
    Console.WriteLine($"Property({prop.Name}): {prop.Value}");
}

Console.WriteLine("Unparsed lines: ");
unparsedLines.ForEach(Console.WriteLine);

Console.ReadLine();