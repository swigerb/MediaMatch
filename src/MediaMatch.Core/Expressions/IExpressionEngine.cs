namespace MediaMatch.Core.Expressions;

public interface IExpressionEngine
{
    string Evaluate(string expression, IMediaBindings bindings);

    bool Validate(string expression, out string? error);
}
