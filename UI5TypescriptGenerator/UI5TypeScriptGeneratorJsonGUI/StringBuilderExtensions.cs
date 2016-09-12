using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI5TypeScriptGeneratorJsonGUI
{
    public static class StringBuilderExtensions
    {
        /// <summary>
        /// Append string as comment (fill in string without any comment prefixes.
        /// </summary>
        /// Result will be
        /// <example>
        /// <code>
        ///  /** <br/>
        ///  * Your Comment lines <br/>
        ///  */
        /// </code>
        /// </example>
        /// <param name="sb"></param>
        /// <param name="commenttext"></param>
        public static void AppendComment(this StringBuilder sb, string commenttext, int tablevel = 0)
        {
            sb.AppendLine("/**", tablevel);
            using (StringReader sr = new StringReader(commenttext))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                    sb.AppendLine(" * " + line, tablevel);
            }
            sb.AppendLine(" */", tablevel);
        }

        /// <summary>
        /// Appens a new line with beginning tabs.
        /// </summary>
        /// <param name="sb">extension</param>
        /// <param name="value">string to append</param>
        /// <param name="tabs">tabs to put in front</param>
        public static void AppendLine(this StringBuilder sb, string value, int tabs)
        {
            using (StringReader sr = new StringReader(value))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                    sb.AppendLine(new String('\t', tabs) + line);
            }
        }
    }
}
