
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

                var textCode = Parser.LoadText(dlg.FileName);

                TextCode.Text = textCode;

                sw.Stop();
                System.Diagnostics.Debug.Print("Loaded code in {0}ms", sw.Elapsed.TotalMilliseconds);
            }
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

                string codeText = TextCode.Text;
                Parser.SaveText(dlg.FileName, codeText);

                sw.Stop();
                System.Diagnostics.Debug.Print("Saved code in {0}ms", sw.Elapsed.TotalMilliseconds);
            }
        }

        #endregion

        #region Compile Code

        private void CompileUserCode()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            var codeText = TextCode.Text;
            var lines = Parser.SplitText(codeText);
            var commands = Parser.ParseLines(lines);

            // reset canvas
            CanvasDesignArea.Children.Clear();

            var blocks = Compile(commands);

            var resourceDictionary = GetResourceDictionary(blocks);
            var formattedXaml = FormatXml(resourceDictionary);

            TextXaml.Text = formattedXaml;

            AddToDesignArea(blocks);

            sw.Stop();
            System.Diagnostics.Debug.Print("Compiled code in {0}ms", sw.Elapsed.TotalMilliseconds);

            #if !DEBUG
            MessageBox.Show("Compiled code in " + sw.Elapsed.TotalMilliseconds.ToString() + "ms");
            #endif
        }

        private void AddToDesignArea(IEnumerable<Tuple<string, object>> blocks)
        {
            double offset = 30;
            double nextBlockOffset = offset;

            var sb = new StringBuilder();
            foreach (var tuple in blocks)
            {
                // add block to designer canvas
                var ctText = GetControlTemplate(tuple.Item2, tuple.Item1);
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

                Canvas.SetLeft(thumb, offset);
                Canvas.SetTop(thumb, nextBlockOffset);

                CanvasDesignArea.Children.Add(thumb);

                nextBlockOffset += offset + (tuple.Item2 as Canvas).Height;
            }
        }

        private static IEnumerable<Tuple<string,object>> Compile(IEnumerable<dynamic> commands)
        {
            double offset = 0.0;
            StringBuilder linesStringBuilder = null;
            Path currentPath = null;
            Grid currentGrid = null;
            Canvas currentCanvas = null;
            string blockName = string.Empty;

            foreach (dynamic command in commands)
            {
                switch (command.Command as string)
                {
                    #region Execute

                    // execute <path>
                    case "execute":
                        {
                            string fileName = command.Path;
                            if (System.IO.File.Exists(fileName))
                            {
                                var lines = Parser.LoadLines(fileName);

                                var cmds = Parser.ParseLines(lines);

                                foreach (var tuple in Compile(cmds))
                                {
                                    yield return tuple;
                                }
                            }
                        }
                        break;

                    #endregion

                    #region Block

                    // block begin <name> <width> <height>
                    // block end
                    case "block":
                        {
                            switch (command.State as string)
                            {
                                case "begin":
                                    {
                                        if (currentCanvas != null)
                                            break;

                                        double width = 0.0, height = 0.0;
                                        if (double.TryParse(command.Width, out width) &&
                                            double.TryParse(command.Height, out height))
                                        {
                                            var canvas = new Canvas()
                                            {
                                                Background = Brushes.Transparent,
                                                ClipToBounds = false
                                            };

                                            canvas.Width = width;
                                            canvas.Height = height;

                                            blockName = command.Name;

                                            currentCanvas = canvas;
                                        }
                                    }
                                    break;
                                case "end":
                                    {
                                        if (currentCanvas == null)
                                            break;

                                        var tuple = new Tuple<string, object>(blockName + "ControlTemplateKey", currentCanvas);

                                        yield return tuple;

                                        currentCanvas = null;
                                    }
                                    break;
                            }
                        }
                        break;

                    #endregion

                    #region Simulation

                    // simulation <path>
                    case "simulation":
                        {

                        }
                        break;

                    #endregion

                    #region Path

                    // path begin
                    // path end
                    case "path":
                        {
                            switch (command.State as string)
                            {
                                case "begin":
                                    {
                                        if (currentCanvas == null || currentPath != null)
                                            break;

                                        var path = new Path()
                                        {
                                            StrokeThickness = 1.0,
                                            StrokeStartLineCap = PenLineCap.Square,
                                            StrokeEndLineCap = PenLineCap.Square,
                                            StrokeLineJoin = PenLineJoin.Miter,
                                            Stroke = Brushes.Red
                                        };

                                        RenderOptions.SetEdgeMode(path, EdgeMode.Aliased);
                                        path.SetValue(SnapsToDevicePixelsProperty, false);
                                        Canvas.SetLeft(path, offset);
                                        Canvas.SetTop(path, offset);

                                        currentCanvas.Children.Add(path);

                                        currentPath = path;
                                        linesStringBuilder = new StringBuilder();
                                    }
                                    break;
                                case "end":
                                    {
                                        if (currentPath == null)
                                            break;

                                        string pathData = linesStringBuilder.ToString();
                                        currentPath.Data = Geometry.Parse(pathData);

                                        currentPath = null;
                                        linesStringBuilder = null;
                                    }
                                    break;
                            }
                        }
                        break;

                    #endregion

                    #region Line

                    // Add new line to current path
                    // line <x1> <y1> <x2> <y2>
                    case "line":
                        {
                            if (currentPath == null || linesStringBuilder == null)
                                break;

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
                            if (currentCanvas == null)
                                break;

                            double x = 0.0, y = 0.0;
                            if (double.TryParse(command.X, out x) &&
                                double.TryParse(command.Y, out y))
                            {
                                string name = command.Name;

                                var ellipse = new Ellipse();

                                ellipse.SetResourceReference(StyleProperty, "BlockEllipseKey");
                                //ellipse.Name = name;
                                ellipse.ToolTip = name;

                                //ellipse.SetValue(UseLayoutRoundingProperty, false);
                                //ellipse.SetValue(SnapsToDevicePixelsProperty, true);

                                Canvas.SetLeft(ellipse, x + offset);
                                Canvas.SetTop(ellipse, y + offset);

                                currentCanvas.Children.Add(ellipse);
                            }
                        }
                        break;

                    #endregion

                    #region Grid

                    // grid begin <x> <y> <width> <height>
                    // grud end
                    case "grid":
                        {
                            switch (command.State as string)
                            {
                                case "begin":
                                    {
                                        if (currentGrid != null || currentCanvas == null)
                                            break;

                                        double x = 0.0, y = 0.0, width = 0.0, height = 0.0;
                                        if (double.TryParse(command.X, out x) &&
                                            double.TryParse(command.Y, out y) &&
                                            double.TryParse(command.Width, out width) &&
                                            double.TryParse(command.Height, out height))
                                        {
                                            var grid = new Grid()
                                            {
                                                Width = width,
                                                Height = height,
                                                Background = Brushes.Transparent
                                            };

                                            Canvas.SetLeft(grid, x + offset);
                                            Canvas.SetTop(grid, y + offset);

                                            currentCanvas.Children.Add(grid);

                                            currentGrid = grid;
                                        }
                                    }
                                    break;
                                case "end":
                                    {
                                        if (currentGrid == null)
                                            break;

                                        currentGrid = null;
                                    }
                                    break;
                            }
                        }
                        break;

                    #endregion

                    #region Row

                    // row <height>
                    case "row":
                        {
                            if (currentGrid == null)
                                break;

                            double height;
                            if (double.TryParse(command.Height, out height))
                            {
                                currentGrid.RowDefinitions.Add(new RowDefinition()
                                {
                                    Height = new GridLength(height)
                                });
                            }
                        }
                        break;

                    #endregion

                    #region Column

                    // column <width>
                    case "column":
                        {
                            if (currentGrid == null)
                                break;

                            double width;
                            if (double.TryParse(command.Width, out width))
                            {
                                currentGrid.ColumnDefinitions.Add(new ColumnDefinition
                                {
                                    Width = new GridLength(width)
                                });
                            }
                        }
                        break;

                    #endregion

                    #region Text

                    // text <row> <column> <row-span> <column-span> <v-alignment> <h-alignment> <font-family> <font-size> <bold> <text>
                    //where:
                    // current grid: row, column
                    // current grid: row-span, column-span
                    // current grid: v-alignment -> top, bottom, center, stretch
                    // current grid: h-alignment -> left, right, center, stretch
                    // font-family, font-size
                    // bold -> true, false
                    // text
                    //
                    // example: text 0 0 1 1 center center Arial 10 false SomeText
                    case "text":
                        {
                            if (currentGrid == null)
                                break;

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

                            currentGrid.Children.Add(tb);
                        }
                        break;

                    #endregion
                }
            }
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

                string xamlText = TextXaml.Text;

                ExportXamlToFile(dlg.FileName, xamlText);

                sw.Stop();
                System.Diagnostics.Debug.Print("Exported xaml in {0}ms", sw.Elapsed.TotalMilliseconds);
            }
        }

        private static void ExportXamlToFile(string fileName, string text)
        {
            using (var stream = new System.IO.StreamWriter(fileName))
            {
                stream.Write(text);
            }
        }

        private static string GetXaml(object obj, string indent)
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

        private static string GetControlTemplate(object obj, string key)
        {
            string objXaml = GetXaml(obj, "    ");

            string openTag = string.Concat("<ControlTemplate x:Key=\"", 
                                           key, 
                                           //"\" xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">\n");
                                           "\" xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">\n");
                      
            string closeTag = "</ControlTemplate>";

            return string.Concat(openTag, objXaml, closeTag); ;
        }

        private static string GetBlockEllipseStyle()
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

        private static string GetResourceDictionary(IEnumerable<Tuple<string, object>> blocks)
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
            foreach(var tuple in blocks)
            {
                string objText = GetControlTemplate(tuple.Item2, tuple.Item1);

                sb.AppendLine(objText);
            }

            // add resource dictionary close tag
            sb.AppendLine(closeTag);

            // return resource dictionary xaml string
            return sb.ToString();
        }

        private static string FormatXml(string xml)
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
