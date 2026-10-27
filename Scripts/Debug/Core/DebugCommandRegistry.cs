#if UNITY_EDITOR || EIGHTAID_TEST_BUILD
using System;
using System.Collections.Generic;
using System.Linq;

public static class DebugCommandRegistry
{
    private static readonly Dictionary<string, DebugCommand> CommandsById = new Dictionary<string, DebugCommand>(StringComparer.Ordinal);
    private static readonly Dictionary<string, int> CategoryOrder = new Dictionary<string, int>(StringComparer.Ordinal);

    public static void SetCategoryOrder(params string[] categories)
    {
        CategoryOrder.Clear();
        for (int i = 0; i < categories.Length; i++)
        {
            CategoryOrder[categories[i]] = i;
        }
    }

    private static int GetCategoryOrder(string category)
    {
        int separator = category.IndexOf(" / ", StringComparison.Ordinal);
        string group = separator < 0 ? category : category.Substring(0, separator);
        return CategoryOrder.TryGetValue(group, out int index) ? index : int.MaxValue;
    }

    /// <summary>
    /// コマンドを登録します。同じ ID が登録された場合は後から登録した内容で上書きします。
    /// プロジェクト固有コマンドは IDebugCommandModule からこのメソッドを呼び出してください。
    /// </summary>
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
            commands = commands.Where(command => MatchesSearch(command, normalizedSearch));
        }

        return commands
            .OrderBy(command => GetCategoryOrder(command.Category))
            .ThenBy(command => command.Category, StringComparer.OrdinalIgnoreCase)
            .ThenBy(command => command.Label, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static bool TryGetCommand(string id, out DebugCommand command)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            command = null;
            return false;
        }

        return CommandsById.TryGetValue(id, out command);
    }

    public static bool MatchesSearch(DebugCommand command, string searchText)
    {
        searchText = searchText?.Trim();
        if (string.IsNullOrEmpty(searchText)) return true;
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
