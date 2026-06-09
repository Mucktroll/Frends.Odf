using System;
using System.Collections.Generic;
using System.Threading;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;

namespace Frends.Odf.WriteSpreadsheet.Helpers;

internal static class OdfSpreadsheetWriter
{
    /// <summary>
    /// Iterates through the JSON array, extracts unique headers, and generates ODF table rows and cells.
    /// </summary>
    /// <param name="firstTable">The first sheet of the ODF spreadsheet as an XElement.</param>
    /// <param name="jsonArray">The input JSON array to be injected.</param>
    /// <param name="includeHeaderRow">Boolean indicating whether the first row should be treated as JSON keys.</param>
    /// <param name="tableNamespace">Standard ODF table namespace.</param>
    /// <param name="textNamespace">Standard ODF text namespace.</param>
    /// <param name="cancellationToken">A cancellation token provided by the Frends Platform.</param>
    internal static void InjectData(
        XElement firstTable,
        JArray jsonArray,
        bool includeHeaderRow,
        XNamespace tableNamespace,
        XNamespace textNamespace,
        CancellationToken cancellationToken)
    {
        var headers = new List<string>();

        firstTable.Elements(tableNamespace + "table-row").Remove();

        foreach (var item in jsonArray)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (item is JObject jObject)
            {
                foreach (var property in jObject.Properties())
                {
                    if (!headers.Contains(property.Name))
                    {
                        headers.Add(property.Name);
                    }
                }
            }
            else
            {
                throw new ArgumentException("The JSON payload must contain valid JSON objects.");
            }
        }

        if (includeHeaderRow)
        {
            var headerRow = new XElement(tableNamespace + "table-row");

            foreach (var header in headers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var cell = new XElement(tableNamespace + "table-cell", new XElement(textNamespace + "p", header));

                headerRow.Add(cell);
            }

            firstTable.Add(headerRow);
        }

        foreach (var item in jsonArray)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (item is not JObject jObject) 
                throw new ArgumentException("The JSON payload must contain valid JSON objects.");

            var dataRow = new XElement(tableNamespace + "table-row");

            foreach (var header in headers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var property = jObject.Property(header);
                var value = string.Empty;

                if (property != null && property.Value.Type != JTokenType.Null)
                {
                    value = property.Value.ToString();
                    value = HandleFormulaInjection(value);
                }

                var cell = new XElement(tableNamespace + "table-cell", new XElement(textNamespace + "p", value));

                dataRow.Add(cell);
            }

            firstTable.Add(dataRow);
        }
    }

    /// <summary>
    /// Escapes strings starting with formula characters to write them as plain text.
    /// </summary>
    private static string HandleFormulaInjection(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        var trimmedInput = input.TrimStart();

        if (trimmedInput.Length == 0)
            return input;

        var firstChar = trimmedInput[0];

        if (firstChar == '=' || firstChar == '+' || firstChar == '-' || firstChar == '@')
        {
            return "'" + input;
        }

        return input;
    }
}