using Bowling.Cli;

// No rolls given and nothing piped in: there is nothing to score, so show how to use the app
// (reading standard input would otherwise just wait).
string[] arguments = args.Length == 0 && !Console.IsInputRedirected ? ["--help"] : args;

return App.Run(arguments, Console.In, Console.Out, Console.Error);
