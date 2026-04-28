namespace MediaMatch.Core.Expressions;

/// <summary>
/// Provides named variable bindings for media expression evaluation.
/// </summary>
public interface IMediaBindings
{
    /// <summary>
    /// Gets the value of the binding with the specified name.
    /// </summary>
    /// <param name="name">The binding name.</param>
    /// <returns>The binding value, or <see langword="null"/> if not found.</returns>
    object? GetValue(string name);

    /// <summary>
    /// Gets all available bindings as a dictionary.
    /// </summary>
    /// <returns>A read-only dictionary of all binding names and values.</returns>
    IReadOnlyDictionary<string, object?> GetAllBindings();

    /// <summary>
    /// Determines whether a binding with the specified name exists.
    /// </summary>
    /// <param name="name">The binding name to check.</param>
    /// <returns>A value indicating whether the binding exists.</returns>
    bool HasBinding(string name);
}
