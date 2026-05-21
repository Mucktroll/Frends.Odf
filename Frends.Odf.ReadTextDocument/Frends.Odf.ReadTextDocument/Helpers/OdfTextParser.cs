using System;
using System.Text;
using System.Xml.Linq;

namespace Frends.Odf.ReadTextDocument.Helpers
{
    internal static class OdfTextParser
    {
        /// <summary>
        /// Parses an ODF paragraph or heading, converting ODF XML formatting tags into standard strings.
        /// </summary>
        /// <param name="element">XML element to parse.</param>
        /// <param name="textNamespace">Standard ODF text namespace.</param>
        /// <returns>A string containing the extracted text with converted whitespaces, tabs, and line breaks.</returns>
        internal static string ParseOdfElements(XElement element, XNamespace textNamespace)
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
