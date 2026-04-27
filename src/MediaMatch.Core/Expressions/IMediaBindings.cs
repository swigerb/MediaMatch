namespace MediaMatch.Core.Expressions;

public interface IMediaBindings
{
    object? GetValue(string name);

    IReadOnlyDictionary<string, object?> GetAllBindings();

    bool HasBinding(string name);
}
