using System.Text;
using AnsiDemo.Shell;

AppOptions options;
try
{
    options = AppOptionsParser.Parse(args);
}
catch (ArgumentException)
{
    Console.Error.WriteLine(AppOptionsParser.Usage);
    return 2;
}

if (options.Frame is not null || options.Wav is not null)
{
    // Headless: leave the console mode and output encoding alone and write UTF-8 without a BOM.
    using var stdout = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    int code = HeadlessHost.Run(options, stdout, Console.Error);
    stdout.Flush();
    return code;
}

return ConsoleHost.Run(options);
