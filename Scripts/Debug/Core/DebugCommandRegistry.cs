#if UNITY_EDITOR || DEVELOPMENT_BUILD || DAISHOU_DEBUG
using System;
using System.Collections.Generic;
using System.Linq;

public static class DebugCommandRegistry
{
    private static readonly Dictionary<string, DebugCommand> CommandsById = new Dictionary<string, DebugCommand>(StringComparer.Ordinal);

    public static void Register(DebugCommand command)
    {
        if (command == null || string.IsNullOrWhiteSpace(command.Id))
        {
            return;
        }

        CommandsById[command.Id] = command;
    }

    public static IReadOnlyList<DebugCommand> GetVisibleCommands(DebugCommandContext context, string searchText)
    {
        string normalizedSearch = searchText?.Trim();
        IEnumerable<DebugCommand> commands = CommandsById.Values.Where(command => command.IsVisible(context));

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            commands = commands.Where(command => Matches(command, normalizedSearch));
        }

        return commands
            .OrderBy(command => command.Category, StringComparer.OrdinalIgnoreCase)
            .ThenBy(command => command.Label, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool Matches(DebugCommand command, string searchText)
    {
        return Contains(command.Id, searchText) ||
               Contains(command.Label, searchText) ||
               Contains(command.Category, searchText) ||
               Contains(command.Description, searchText);
    }

    private static bool Contains(string source, string value)
    {
        return !string.IsNullOrEmpty(source) &&
               source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
#endif
