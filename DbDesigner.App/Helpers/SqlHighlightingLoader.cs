using System;
using System.Reflection;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace DbDesigner.App.Helpers;

public static class SqlHighlightingLoader
{
    private static IHighlightingDefinition? _sqlHighlighting;

    public static IHighlightingDefinition GetSqlHighlighting()
    {
        if (_sqlHighlighting == null)
        {
            using var stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("DbDesigner.App.Resources.Sql.xshd");
            if (stream == null)
            {
                throw new InvalidOperationException("Nie znaleziono zasobu Sql.xshd.");
            }

            using var reader = new XmlTextReader(stream);
            _sqlHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
        }

        return _sqlHighlighting;
    }
}
