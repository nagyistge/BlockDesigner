﻿
namespace BlockDesigner
{
    #region References

    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Markup;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Navigation;
    using System.Windows.Shapes;
    using System.Xml;

    #endregion

    public partial class MainWindow : Window
    {
        #region Contructor

        public MainWindow()
        {
            InitializeComponent();
        }

        #endregion

        #region Events

        private void ButtonLoadCodeFromFile_Click(object sender, RoutedEventArgs e)
        {
            LoadCodeFromFile();
        }

        private void ButtonSaveCodeToFile_Click(object sender, RoutedEventArgs e)
        {
            SaveCodeToFile();
        }

        private void ButtonCompileCode_Click(object sender, RoutedEventArgs e)
        {
            CompileUserCode();
        }

        private void ButtonExportXaml_Click(object sender, RoutedEventArgs e)
        {
            ExportXamlToFile();
        }

        #endregion

        #region Load Code

        private void LoadCodeFromFile()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog()
            {
                DefaultExt = "txt",
                Filter = "Block Files (*.txt;*.block)|*.txt;*.block|TXT Files (*.txt)|*.txt|Block Files (*.block)|*.block|All Files (*.*)|*.*",
                FilterIndex = 1,
                FileName = ""
            };

            if (dlg.ShowDialog() == true)
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();

                LoadCodeFromFile(dlg.FileName);

                sw.Stop();
                System.Diagnostics.Debug.Print("Loaded code in {0}ms", sw.Elapsed.TotalMilliseconds);
            }
        }

        private void LoadCodeFromFile(string fileName)
        {
            var sb = new StringBuilder();

            using (var stream = new System.IO.StreamReader(fileName))
            {
                string line;
                while ((line = stream.ReadLine()) != null)
                    sb.AppendLine(line);
            }

            TextCode.Text = sb.ToString();
        }

        #endregion

        #region Save Code

        private void SaveCodeToFile()
        {
            var dlg = new Microsoft.Win32.SaveFileDialog()
            {
                DefaultExt = "txt",
                Filter = "Block Files (*.txt;*.block)|*.txt;*.block|TXT Files (*.txt)|*.txt|Block Files (*.block)|*.block|All Files (*.*)|*.*",
                FilterIndex = 1,
                FileName = "script1"
            };

            if (dlg.ShowDialog() == true)
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();

                SaveCodeToFile(dlg.FileName);

                sw.Stop();
                System.Diagnostics.Debug.Print("Saved code in {0}ms", sw.Elapsed.TotalMilliseconds);
            }
        }

        private void SaveCodeToFile(string fileName)
        {
            string codeText = TextCode.Text;

            using (var stream = new System.IO.StreamWriter(fileName))
            {
                stream.Write(codeText);
            }
        }

        #endregion

        #region Compile Code

        private void CompileUserCode()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            var codeText = TextCode.Text;

            var lines = Compiler.GetLines(codeText);

            var commands = Parser.GetCommands(lines);

            CompileUserCode(commands);

            sw.Stop();
            System.Diagnostics.Debug.Print("Compiled code in {0}ms", sw.Elapsed.TotalMilliseconds);

            #if !DEBUG
            MessageBox.Show("Compiled code in " + sw.Elapsed.TotalMilliseconds.ToString() + "ms");
            #endif
        }

        private void CompileUserCode(IEnumerable<dynamic> commands)
        {
            double offset = 0.0;

            var linesStringBuilder = new StringBuilder();
            var pinEllipses = new List<Ellipse>();
            var textBlocks = new List<TextBlock>();
            var grids = new Dictionary<string, Grid>();

            var canvas = new Canvas()
            {
                Background = Brushes.Transparent,
                ClipToBounds = false
            };

            string block_name = "BlockName";

            foreach (dynamic command in commands)
            {
                switch (command.Command as string)
                {
                    #region Block

                    // block <name> <width> <height>
                    case "block":
                        {
                            block_name = command.Name;

                            double width = 0.0, height = 0.0;
                            if (double.TryParse(command.Width, out width) &&
                                double.TryParse(command.Height, out height))
                            {
                                canvas.Width = width;
                                canvas.Height = height;
                            }
                        }
                        break;

                    #endregion

                    #region Line

                    // line <x1> <y1> <x2> <y2>
                    case "line":
                        {
                            double x1 = 0.0, y1 = 0.0, x2 = 0.0, y2 = 0.0;
                            if (double.TryParse(command.X1, out x1) &&
                                double.TryParse(command.Y1, out y1) &&
                                double.TryParse(command.X2, out x2) &&
                                double.TryParse(command.Y2, out y2))
                            {
                                linesStringBuilder.AppendFormat("M{0},{1}", x1, y1);
                                linesStringBuilder.AppendFormat("L{0},{1}\n", x2, y2);
                            }
                        }
                        break;

                    #endregion

                    #region Pin

                    // pin <name> <x> <y>
                    case "pin":
                        {
                            double x = 0.0, y = 0.0;
                            if (double.TryParse(command.X, out x) &&
                                double.TryParse(command.Y, out y))
                            {
                                string name = command.Name;

                                var ellipse = new Ellipse();

                                ellipse.SetResourceReference(StyleProperty, "BlockEllipseKey");
                                ellipse.Name = name;
                                ellipse.ToolTip = name;

                                //ellipse.SetValue(UseLayoutRoundingProperty, false);
                                ellipse.SetValue(SnapsToDevicePixelsProperty, true);

                                Canvas.SetLeft(ellipse, x + offset);
                                Canvas.SetTop(ellipse, y + offset);

                                pinEllipses.Add(ellipse);
                            }
                        }
                        break;

                    #endregion

                    #region Grid

                    // grid <name> <x> <y> <width> <height>
                    case "grid":
                        {
                            string name = command.Name;
                            if (grids.ContainsKey(name) == false)
                            {
                                double x = 0.0, y = 0.0, width = 0.0, height = 0.0;
                                if (double.TryParse(command.X, out x) &&
                                    double.TryParse(command.Y, out y) &&
                                    double.TryParse(command.Width, out width) &&
                                    double.TryParse(command.Height, out height))
                                {
                                    var grid = new Grid()
                                    {
                                        Name = name,
                                        Width = width,
                                        Height = height,
                                        Background = Brushes.Transparent
                                    };

                                    Canvas.SetLeft(grid, x + offset);
                                    Canvas.SetTop(grid, y + offset);

                                    grids.Add(name, grid);
                                }
                            }
                        }
                        break;

                    #endregion

                    #region Row

                    // row <grid-name> <height>
                    case "row":
                        {
                            if (grids.ContainsKey(command.GridName))
                            {
                                double height;
                                if (double.TryParse(command.Height, out height))
                                {
                                    Grid grid = grids[command.GridName];

                                    grid.RowDefinitions.Add(new RowDefinition()
                                    {
                                        Height = new GridLength(height)
                                    });
                                }
                            }
                        }
                        break;

                    #endregion

                    #region Column

                    // column <grid-name> <width>
                    case "column":
                        {
                            if (grids.ContainsKey(command.GridName))
                            {
                                double width;
                                if (double.TryParse(command.Width, out width))
                                {
                                    Grid grid = grids[command.GridName];

                                    grid.ColumnDefinitions.Add(new ColumnDefinition
                                    {
                                        Width = new GridLength(width)
                                    });
                                }
                            }
                        }
                        break;

                    #endregion

                    #region Text

                    // text <grid> <grid-name> <row> <column> <row-span> <column-span> <v-alignment> <h-alignment> <font-family> <font-size> <bold> <text>
                    //where:
                    // grid -> layout type for text is grid
                    // grid-name -> target layout grid name
                    // row, column
                    // row-span, column-span
                    // v-alignment -> top, bottom, center, stretch
                    // h-alignment -> left, right, center, stretch
                    // font-family, font-size
                    // bold -> true, false
                    // text
                    case "text":
                        {
                            switch (command.Layout as string)
                            {
                                // eg.: text grid g1 1 0 1 1 center center Arial 10 false SA
                                case "grid":
                                    {
                                        if (grids.ContainsKey(command.GridName) == false)
                                            break;

                                        Grid grid = grids[command.GridName];

                                        int r = 0, c = 0, rs = 1, cs = 1;
                                        if (int.TryParse(command.Row, out r) == false ||
                                            int.TryParse(command.Column, out c) == false ||
                                            int.TryParse(command.RowSpan, out rs) == false ||
                                            int.TryParse(command.ColumnSpan, out cs) == false)
                                            break;

                                        double fontSize;
                                        if (double.TryParse(command.FontSize, out fontSize) == false)
                                            break;

                                        bool isBold;
                                        if (bool.TryParse(command.IsBold, out isBold) == false)
                                            break;

                                        var tb = new TextBlock()
                                        {
                                            Text = command.Text,
                                            FontFamily = new FontFamily(command.FontFamily),
                                            FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal,
                                            FontSize = fontSize,
                                            Foreground = Brushes.Red
                                        };

                                        RenderOptions.SetEdgeMode(tb, EdgeMode.Aliased);
                                        tb.SetValue(SnapsToDevicePixelsProperty, false);

                                        Grid.SetColumn(tb, c);
                                        Grid.SetRow(tb, r);
                                        Grid.SetColumnSpan(tb, cs);
                                        Grid.SetRowSpan(tb, rs);

                                        switch (command.VerticalAlignment as string)
                                        {
                                            case "top":
                                                tb.SetValue(VerticalAlignmentProperty, VerticalAlignment.Top);
                                                break;
                                            case "bottom":
                                                tb.SetValue(VerticalAlignmentProperty, VerticalAlignment.Bottom);
                                                break;
                                            case "center":
                                                tb.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
                                                break;
                                            case "stretch":
                                                tb.SetValue(VerticalAlignmentProperty, VerticalAlignment.Stretch);
                                                break;
                                        }

                                        switch (command.HorizontalAlignment as string)
                                        {
                                            case "left":
                                                tb.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Left);
                                                break;
                                            case "right":
                                                tb.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Right);
                                                break;
                                            case "center":
                                                tb.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
                                                break;
                                            case "stretch":
                                                tb.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
                                                break;
                                        }

                                        grid.Children.Add(tb);
                                    }
                                    break;
                            }
                        }
                        break;

                    #endregion
                }
            }

            // reset canvas
            //CanvasDesignArea.Children.Clear();

            // lines
            var path = new Path() { StrokeThickness = 1.0, Stroke = Brushes.Red };
            RenderOptions.SetEdgeMode(path, EdgeMode.Aliased);
            path.SetValue(SnapsToDevicePixelsProperty, false);
            Canvas.SetLeft(path, offset);
            Canvas.SetTop(path, offset);
            string pathData = linesStringBuilder.ToString();
            path.Data = Geometry.Parse(pathData);
            canvas.Children.Add(path);

            // pins
            foreach (var ellipse in pinEllipses)
                canvas.Children.Add(ellipse);

            // grids
            foreach (var grid in grids)
                canvas.Children.Add(grid.Value);

            // generate Xaml

            var objects = new Dictionary<string, object>();

            objects.Add(block_name + "ControlTemplateKey", canvas);

            TextXaml.Text = FormatXml(GetResourceDictionary(objects));

            // add block to designer canvas

            var ctText = GetControlTemplate(canvas, block_name + "ControlTemplateKey");
            var ct = (ControlTemplate)XamlReader.Parse(ctText);

            var thumb = new Thumb()
            {
                Template = ct
            };

            thumb.DragDelta += (sender, e) =>
            {
                var t = sender as Thumb;
                double x = Canvas.GetLeft(t) + e.HorizontalChange;
                double y = Canvas.GetTop(t) + e.VerticalChange;
                Canvas.SetLeft(t, x);
                Canvas.SetTop(t, y);
            };

            Canvas.SetLeft(thumb, 30);
            Canvas.SetTop(thumb, 30);

            CanvasDesignArea.Children.Add(thumb);
        }

        #endregion

        #region Export Xaml

        private void ExportXamlToFile()
        {
            var dlg = new Microsoft.Win32.SaveFileDialog()
            {
                DefaultExt = "xaml",
                Filter = "Xaml Files (*.xaml)|*.xaml;|All Files (*.*)|*.*",
                FilterIndex = 1,
                FileName = "Dictionary1"
            };

            if (dlg.ShowDialog() == true)
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();

                ExportXamlToFile(dlg.FileName);

                sw.Stop();
                System.Diagnostics.Debug.Print("Exported xaml in {0}ms", sw.Elapsed.TotalMilliseconds);
            }
        }

        private void ExportXamlToFile(string fileName)
        {
            string xamlText = TextXaml.Text;

            using (var stream = new System.IO.StreamWriter(fileName))
            {
                stream.Write(xamlText);
            }
        }

        private string GetXaml(object obj, string indent)
        {
            var sb = new StringBuilder();
            var writer = System.Xml.XmlWriter.Create(sb, new System.Xml.XmlWriterSettings
            {
                Indent = true,
                IndentChars = indent,
                ConformanceLevel = ConformanceLevel.Fragment,
                OmitXmlDeclaration = true
            });

            var mgr = new XamlDesignerSerializationManager(writer)
            {
                XamlWriterMode = XamlWriterMode.Expression
            };

            System.Windows.Markup.XamlWriter.Save(obj, mgr);

            return sb.ToString();
        }

        private string GetControlTemplate(object obj, string key)
        {
            string objXaml = GetXaml(obj, "    ");

            string openTag = string.Concat("<ControlTemplate x:Key=\"", 
                                           key, 
                                           //"\" xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">\n");
                                           "\" xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">\n");
                      
            string closeTag = "</ControlTemplate>";

            return string.Concat(openTag, objXaml, closeTag); ;
        }

        private string GetBlockEllipseStyle()
        {
            return
                "<Style x:Key=\"BlockEllipseKey\" TargetType=\"Ellipse\" xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">" +
                    "<Setter Property=\"Stroke\" Value=\"Red\"/>" +
                    "<Setter Property=\"Fill\" Value=\"Red\"/>" +
                    "<Setter Property=\"StrokeThickness\" Value=\"1.0\"/>" +
                    "<Setter Property=\"Width\" Value=\"8.0\"/>" +
                    "<Setter Property=\"Height\" Value=\"8.0\"/>" +
                    "<Setter Property=\"Margin\" Value=\"-4,-4,0,0\"/>" +
                    "<Setter Property=\"SnapsToDevicePixels\" Value=\"True\"/>" +
                "</Style>";
        }

        private string GetResourceDictionary(Dictionary<string, object> objects)
        {
            var sb = new StringBuilder();

            string openTag = "<ResourceDictionary xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">";
            string closeTag = "</ResourceDictionary>";

            // add resource dictionary open tag
            sb.AppendLine(openTag);

            // create control styles

            string ellipseStyleText = GetBlockEllipseStyle();
            sb.AppendLine(ellipseStyleText);

            // create control templates
            foreach(var item in objects)
            {
                 string objText = GetControlTemplate(item.Value, item.Key);

                sb.AppendLine(objText);
            }

            // add resource dictionary close tag
            sb.AppendLine(closeTag);

            // return resource dictionary xaml string
            return sb.ToString();
        }

        public static string FormatXml(string xml)
        {
            XmlDocument doc = new XmlDocument();
            
            doc.LoadXml(xml);

            StringBuilder sb = new StringBuilder();
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = "    ";
            settings.NewLineChars = "\r\n";
            settings.NewLineHandling = NewLineHandling.Replace;
            settings.OmitXmlDeclaration = true;

            using (XmlWriter writer = XmlWriter.Create(sb, settings))
            {
                doc.Save(writer);
            }

            return sb.ToString();
        }

        #endregion
    }
}
