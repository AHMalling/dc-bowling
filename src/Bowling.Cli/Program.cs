using Bowling.Cli;

// Ask for rolls one at a time only when a person is typing: no arguments and nothing piped in.
var interactive = args.Length == 0 && !Console.IsInputRedirected;

return App.Run(args, Console.In, Console.Out, Console.Error, interactive);
