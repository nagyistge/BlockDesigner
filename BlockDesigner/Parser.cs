﻿
namespace BlockDesigner
{
    #region References

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Dynamic;

    #endregion

    #region Parser

    public class Parser
    {
        public static IEnumerable<dynamic> GetCommands(IEnumerable<string[]> lines)
        {
            var commands = new List<dynamic>();
            
            foreach (var l in lines)
            {
                switch(l[0])
                {
                    // block <name> <width> <height>
                    case "block":
                        {
                            if (l.Length == 4)
                            {
                                dynamic command = new ExpandoObject();
                                command.Version = "1.0";
                                command.Command = l[0];
                                command.Name = l[1];
                                command.Width = l[2];
                                command.Height = l[3];
                                commands.Add(command);
                            }
                        }
                        break;
                    // line <x1> <y1> <x2> <y2>
                    case "line":
                        {
                            if (l.Length == 5)
                            {
                                dynamic command = new ExpandoObject();
                                command.Version = "1.0";
                                command.Command = l[0];
                                command.X1 = l[1];
                                command.Y1 = l[2];
                                command.X2 = l[3];
                                command.Y2 = l[4];
                                commands.Add(command);
                            }
                        }
                        break;
                    // pin <name> <x> <y>
                    case "pin":
                        {
                            if (l.Length == 4)
                            {
                                dynamic command = new ExpandoObject();
                                command.Version = "1.0";
                                command.Command = l[0];
                                command.Name = l[1];
                                command.X = l[2];
                                command.Y = l[3];
                                commands.Add(command);
                            }
                        }
                        break;
                    // grid <name> <x> <y> <width> <height>
                    case "grid":
                        {
                            if (l.Length == 6)
                            {
                                dynamic command = new ExpandoObject();
                                command.Version = "1.0";
                                command.Command = l[0];
                                command.Name = l[1];
                                command.X = l[2];
                                command.Y = l[3];
                                command.Width = l[4];
                                command.Height = l[5];
                                commands.Add(command);
                            }
                        }
                        break;
                    // row <grid-name> <height>
                    case "row":
                        {
                            if (l.Length == 3)
                            {
                                dynamic command = new ExpandoObject();
                                command.Version = "1.0";
                                command.Command = l[0];
                                command.GridName = l[1];
                                command.Height = l[2];
                                commands.Add(command);
                            }
                        }
                        break;
                    // column <grid-name> <width>
                    case "column":
                        {
                            if (l.Length == 3)
                            {
                                dynamic command = new ExpandoObject();
                                command.Version = "1.0";
                                command.Command = l[0];
                                command.GridName = l[1];
                                command.Width = l[2];
                                commands.Add(command);
                            }
                        }
                        break;
                    // text <grid> <grid-name> <row> <column> <row-span> <column-span> <v-alignment> <h-alignment> <font-family> <font-size> <bold> <text>
                    case "text":
                        {
                            if (l.Length == 13)
                            {
                                dynamic command = new ExpandoObject();
                                command.Version = "1.0";
                                command.Command = l[0];
                                command.Layout = l[1];
                                command.GridName = l[2];
                                command.Row = l[3];
                                command.Column = l[4];
                                command.RowSpan = l[5];
                                command.ColumnSpan = l[6];
                                command.VerticalAlignment = l[7];
                                command.HorizontalAlignment = l[8];
                                command.FontFamily = l[9];
                                command.FontSize = l[10];
                                command.IsBold = l[11];
                                command.Text = l[12];
                                commands.Add(command);
                            }
                        }
                        break;
                };
            }

            return commands;
        }
    }

    #endregion
}
