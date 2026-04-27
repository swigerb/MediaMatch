using System.Text.RegularExpressions;
using MediaMatch.Core.Expressions;
using Scriban;
using Scriban.Runtime;

namespace MediaMatch.Application.Expressions;

/// <summary>
/// Expression engine using Scriban templates.
/// Supports FileBot-compatible expressions like "{n} ({y})/Season {s00}/{n} - {s00e00} - {t}"
/// which are auto-converted to Scriban syntax "{{n}} ({{y}})/Season {{s00}}/{{n}} - {{s00e00}} - {{t}}".
/// </summary>
public partial class ScribanExpressionEngine : IExpressionEngine
{
    // Matches single-brace variable references like {n}, {s00e00}, {mm.clean_filename t}
    // but NOT double-brace (already Scriban) or content that looks like literal JSON/code.
    [GeneratedRegex(@"(?<!\{)\{([a-zA-Z_][\w.]*(?:\s[^}]*)?)\}(?!\})", RegexOptions.Compiled)]
    private static partial Regex FileBotVarPattern();

    /// <inheritdoc />
    public string Evaluate(string expression, IMediaBindings bindings)
    {
        var scribanExpr = ConvertFromFileBotSyntax(expression);
        var template = Template.Parse(scribanExpr);

        if (template.HasErrors)
            return string.Empty;

        var context = BuildContext(bindings);
        return template.Render(context).Trim();
    }

    /// <inheritdoc />
    public bool Validate(string expression, out string? error)
    {
        var scribanExpr = ConvertFromFileBotSyntax(expression);
        var template = Template.Parse(scribanExpr);

        if (template.HasErrors)
        {
            error = string.Join("; ", template.Messages.Select(m => m.Message));
            return false;
        }

        error = null;
        return true;
    }

    /// <summary>
    /// Convert FileBot-style {var} expressions to Scriban {{var}} syntax.
    /// Only converts single-brace patterns that look like variable references.
    /// Already-doubled braces are left untouched.
    /// </summary>
    public static string ConvertFromFileBotSyntax(string fileBotExpression) =>
        FileBotVarPattern().Replace(fileBotExpression, "{{$1}}");

    private static TemplateContext BuildContext(IMediaBindings bindings)
    {
        var context = new TemplateContext { MemberRenamer = member => member.Name };

        // If the bindings are already a ScriptObject, push directly
        if (bindings is ScriptObject so)
        {
            context.PushGlobal(so);
        }
        else
        {
            var global = new ScriptObject();
            foreach (var kvp in bindings.GetAllBindings())
                global.SetValue(kvp.Key, kvp.Value, readOnly: false);
            context.PushGlobal(global);
        }

        // Register mm.* helper functions
        var helpers = new ExpressionFormatHelper();
        context.CurrentGlobal!.SetValue("mm", helpers, readOnly: false);

        return context;
    }
}
