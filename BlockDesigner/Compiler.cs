
namespace BlockDesigner
{
    #region References

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    #endregion

    #region Compiler

    public class Compiler
    {
        public static List<string[]> GetLines(string text)
        {
            var lines = new List<string[]>();

            using (System.IO.StringReader reader = new System.IO.StringReader(text))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                    lines.Add(line.Split(' '));
            }

            return lines;
        }
    }

    #endregion
}
