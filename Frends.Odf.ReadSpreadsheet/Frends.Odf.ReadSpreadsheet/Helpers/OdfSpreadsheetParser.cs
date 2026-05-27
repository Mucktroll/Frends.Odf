using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;

namespace Frends.Odf.ReadSpreadsheet.Helpers
{
    internal class OdfSpreadsheetParser
    {
        private const int MaxColumns = 16384; // Maximum columns of Excel spreadsheet
        private const int MaxRows = 5000; // Cap to row limit to prevent malicous file sizes.
        private const string DefaultColumnPrefix = "Column";

        /// <summary>
        /// Iterates through spreadsheet rows and columns to extract data into a JSON array, handling ODF compression attributes.
        /// </summary>
        /// <param name="firstTable">The first sheet of the ODF spreadsheet as an XElement.</param>
        /// <param name="tableNamespace">Standard ODF table namespace.</param>
        /// <param name="textNamespace">Standard ODF text namespace.</param>
        /// <param name="containsHeaderRow">Boolean indicating whether the first row should be parsed as JSON keys.</param>
        /// <param name="cancellationToken">A cancellation token provided by the Frends Platform.</param>
        /// <returns>A JArray containing the mapped JSON objects for each row.</returns>
        internal static JArray ExtractData(XElement firstTable, XNamespace tableNamespace, XNamespace textNamespace, bool containsHeaderRow, CancellationToken cancellationToken)
        {
            // Initialise variables for final data JArray, column header names, helper boolean for the first row, and processed rows tracker.
            var jsonArray = new JArray();
            var headers = new List<string>();
            bool isFirstRow = true;
            int totalProcessedRows = 0;

            // Iterate through every row in the spreadsheet.
            foreach (var rowElement in firstTable.Elements(tableNamespace + "table-row"))
            {
                // Cancellation token should be provided to methods that support it
                // and checked during long-running operations, e.g., loops.
                cancellationToken.ThrowIfCancellationRequested();

                // Initiliase variable to hold row specific data.
                var rowData = new List<string>();

                // Iterate through every cell in the specific row.
                foreach (var cell in rowElement.Elements(tableNamespace + "table-cell"))
                {
                    // Retrieve paragraph and heading tag values inside the cell and combine with a line break.
                    var paragraphElements = cell.Elements()
                        .Where(x => x.Name == textNamespace + "p" || x.Name == textNamespace + "h")
                        .Select(x => ParseOdfElements(x, textNamespace));

                    var cellValue = string.Join(Environment.NewLine, paragraphElements);

                    // Check for repeated columns attribute.
                    var repeatedAttribute = cell.Attribute(tableNamespace + "number-columns-repeated");
                    int repeatCount = 1;

                    if (repeatedAttribute != null && int.TryParse(repeatedAttribute.Value, out int repeatedValue))
                    {
                        // Check repeatedValue size and cap at maximum excel column number.
                        if (repeatedValue <= MaxColumns)
                            repeatCount = repeatedValue;
                        else
                            repeatCount = MaxColumns;
                    }

                    // Check for spanned columns attribute indicated merged cells.
                    var spanAttribute = cell.Attribute(tableNamespace + "number-columns-spanned");
                    int spanCount = 1;

                    if (spanAttribute != null && int.TryParse(spanAttribute.Value, out int spanValue))
                    {
                        // Reassign spannedCount if spannedValue is greater than 1.
                        if (spanValue > 1)
                            spanCount = spanValue;
                        else
                            spanCount = 1;
                    }

                    // Add cell content to row data list.
                    // Uses repeatCount to append the <table:table-cell table:number-columns-repeated="X" /> value X times.
                    // Uses spanCount to append the <table:table-cell table:number-columns-spanned="X" /> value X times.
                    // If a cell is merged and repeated, the merged cell block is duplicated.
                    for (int i = 0; i < repeatCount; i++)
                    {
                        // Adds the actual cell value.
                        rowData.Add(cellValue);

                        for (int j = 1; j < spanCount; j++)
                        {
                            rowData.Add(string.Empty);
                        }
                    }
                }

                // Remove possible blank cells from end of list.
                for (int i = rowData.Count - 1; i >= 0; i--)
                {
                    if (string.IsNullOrWhiteSpace(rowData[i]))
                        rowData.RemoveAt(i);
                    else
                        break;
                }

                // If the row was empty, skip to the next row.
                if (rowData.Count == 0)
                    continue;

                // Check for repeated row attribute.
                var rowRepeatedAttribute = rowElement.Attribute(tableNamespace + "number-rows-repeated");
                int rowRepeatCount = 1;

                if (rowRepeatedAttribute != null && int.TryParse(rowRepeatedAttribute.Value, out int rowRepeatedValue))
                {
                    // Check repeatedValue size and cap at MaxRows to prevent maliciously large file.
                    if (rowRepeatedValue < MaxRows)
                        rowRepeatCount = rowRepeatedValue;
                    else
                        rowRepeatCount = MaxRows;
                }

                // Add row to jsonArray.
                for (int i = 0; i < rowRepeatCount; i++)
                {
                    if (totalProcessedRows >= MaxRows)
                        break;

                    if (isFirstRow)
                    {
                        // If row is first row, check containsHeaderRow bool.
                        if (containsHeaderRow)
                        {
                            // If true, use ParseHeader helper to create headers from rowData.
                            headers = ParseHeaders(rowData);
                        }
                        else
                        {
                            // If false, map the first row as data
                            headers = new List<string>();
                            jsonArray.Add(CreateJsonRow(rowData, headers));
                        }

                        isFirstRow = false;

                        // Break to prevent duplicated header rows.
                        if (containsHeaderRow)
                            break;
                    }
                    else
                    {
                        jsonArray.Add(CreateJsonRow(rowData, headers));
                    }

                    totalProcessedRows++;
                }

                if (totalProcessedRows >= MaxRows)
                    break;
            }

            return jsonArray;
        }

        /// <summary>
        /// Parses the first row into JSON keys, handling duplicate names and missing headers.
        /// </summary>
        /// <param name="rowData">List of strings extracted from the first row of the spreadsheet.</param>
        /// <returns>Parsed list of unique string headers.</returns>
        private static List<string> ParseHeaders(List<string> rowData)
        {
            // Initialise variable for parsed header output.
            var parsedHeaders = new List<string>();

            // Iterate through every cell in header row rowData.
            for (int i = 0; i < rowData.Count; i++)
            {
                string headerName;

                // Check if header cell is empty.
                if (string.IsNullOrWhiteSpace(rowData[i]))
                {
                    // If true, create basic column name with integer ID.
                    headerName = $"{DefaultColumnPrefix}_{i + 1}";
                }
                else
                {
                    // If false, use existing header name.
                    headerName = rowData[i];
                }

                // Initialise variables to handle header duplicates.
                string header = headerName;
                int suffix = 1;

                // Add 1 to the header name ID until it is unique.
                while (parsedHeaders.Contains(header))
                {
                    header = $"{headerName}_{suffix}";
                    suffix++;
                }

                // Add header to the parsed header list.
                parsedHeaders.Add(header);
            }

            return parsedHeaders;
        }

        /// <summary>
        /// Maps row data to headers to create a JSON object.
        /// </summary>
        /// <param name="rowData">The extracted text data for a single row.</param>
        /// <param name="headers">The list of header keys.</param>
        /// <returns>A JObject representing a single mapped row.</returns>
        private static JObject CreateJsonRow(List<string> rowData, List<string> headers)
        {
            // Initialise variable for JObject output.
            var jObject = new JObject();

            // Determine number of columns needed.
            int columnCount;

            if (rowData.Count > headers.Count)
            {
                columnCount = rowData.Count;
            }
            else
            {
                columnCount = headers.Count;
            }

            for (int i = 0; i < columnCount; i++)
            {
                string key;

                // Retrieve the header name.
                if (i < headers.Count)
                {
                    // If there are header names for each column, they can be used.
                    key = headers[i];
                }
                else
                {
                    // If there are not header names for each column, create a column header name with unique ID.
                    key = $"{DefaultColumnPrefix}_{i + 1}";
                }

                // Initialise variables to handle header duplicates.
                string uniqueKey = key;
                int suffix = 1;

                // Add 1 to the header name ID until it is unique.
                while (jObject.ContainsKey(uniqueKey))
                {
                    uniqueKey = $"{key}_{suffix}";
                    suffix++;
                }

                string value;

                // Retrieve the row data.
                if (i < rowData.Count)
                {
                    // If there are data values for each row, they can be used.
                    value = rowData[i];
                }
                else
                {
                    // If there are not data values for each row, inject an empty string to maintain structure.
                    value = string.Empty;
                }

                // Add key value pair to jObject.
                jObject.Add(uniqueKey, value);
            }

            return jObject;
        }

        /// <summary>
        /// Parses an ODF paragraph or heading, converting ODF XML formatting tags into standard strings.
        /// </summary>
        /// <param name="element">XML element to parse.</param>
        /// <param name="textNamespace">Standard ODF text namespace.</param>
        /// <returns>A string containing the extracted text with converted whitespaces, tabs, and line breaks.</returns>
        private static string ParseOdfElements(XElement element, XNamespace textNamespace)
        {
            var stringBuilder = new StringBuilder();

            // Use DescendantNodes to retrieve entire content of the XElement.
            foreach (var node in element.DescendantNodes())
            {
                // Check if node is raw text
                if (node is XText textNode)
                {
                    stringBuilder.Append(textNode.Value);
                }

                // If the node is not raw text, it has an XML tag inside it, making it an XElement.
                else if (node is XElement xElement)
                {
                    // Check for whitespace tag <text:s>.
                    if (xElement.Name == textNamespace + "s")
                    {
                        // Initialise as 1 in case of malformatting in the XML.
                        int whitespaceCount = 1;

                        // Check for 'c' attribute in multiple whitespace tag <text:s text:c="X"/>.
                        var countAttribute = xElement.Attribute(textNamespace + "c");

                        // If 'c' attribute exists, and has a valid integer, set the whitespace count to the 'c' value.
                        if (countAttribute != null && int.TryParse(countAttribute.Value, out int c))
                        {
                            whitespaceCount = c;
                        }

                        // Append whitespaces to XElement value.
                        stringBuilder.Append(new string(' ', whitespaceCount));
                    }

                    // Check for tab tag <text:tab>.
                    else if (xElement.Name == textNamespace + "tab")
                    {
                        // Append tab to XElement value.
                        stringBuilder.Append('\t');
                    }

                    // Check for line break tag <text:line-break>.
                    else if (xElement.Name == textNamespace + "line-break")
                    {
                        // Append new line to XElement value.
                        stringBuilder.Append(Environment.NewLine);
                    }
                }
            }

            return stringBuilder.ToString();
        }
    }
}