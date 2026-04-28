namespace MediaMatch.Core.Expressions;

/// <summary>
/// Evaluates and validates rename-pattern expressions against media bindings.
/// </summary>
public interface IExpressionEngine
{
    /// <summary>
    /// Evaluates the specified expression using the provided media bindings.
    /// </summary>
    /// <param name="expression">The expression to evaluate.</param>
    /// <param name="bindings">The media bindings supplying variable values.</param>
    /// <returns>The evaluated string result.</returns>
    string Evaluate(string expression, IMediaBindings bindings);

    /// <summary>
    /// Validates the syntax of the specified expression.
    /// </summary>
    /// <param name="expression">The expression to validate.</param>
    /// <param name="error">An error message if validation fails; otherwise, <see langword="null"/>.</param>
    /// <returns>A value indicating whether the expression is valid.</returns>
    bool Validate(string expression, out string? error);
}
