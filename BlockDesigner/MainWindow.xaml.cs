
namespace BlockDesigner
{
    #region References

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
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

        private void ButtonCompileCode_Click(object sender, RoutedEventArgs e)
        {
            CompileUserCode();
        }

        private void ButtonLoadCodeFromFile_Click(object sender, RoutedEventArgs e)
        {
            LoadCodeFromFile();
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

        #region Compile Code

        private void CompileUserCode()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            var text = TextCode.Text;
            var lines = new List<string[]>();

            using (System.IO.StringReader reader = new System.IO.StringReader(text))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                    lines.Add(line.Split(' '));
            }

            CompileUserCode(lines);

            sw.Stop();
            System.Diagnostics.Debug.Print("Compiled code in {0}ms", sw.Elapsed.TotalMilliseconds);
        }

        private void CompileUserCode(List<string[]> lines)
        {
            double offset = 30.0;

            var linesStringBuilder = new StringBuilder();
            var pinEllipses = new List<Ellipse>();
            var textBlocks = new List<TextBlock>();
            var grids = new Dictionary<string, Grid>();

            foreach (var l in lines)
            {
                var command = l[0];

                switch (command)
                {
                    #region Line

                    // line x1 y1 x2 y2
                    case "line":
                        {
                            if (l.Length != 5)
                                break;

                            double x1, y1, x2, y2;
                            if (double.TryParse(l[1], out x1) &&
                                double.TryParse(l[2], out y1) &&
                                double.TryParse(l[3], out x2) &&
                                double.TryParse(l[4], out y2))
                            {
                                linesStringBuilder.AppendFormat("M{0},{1}", x1, y1);
                                linesStringBuilder.AppendFormat("L{0},{1}\n", x2, y2);
                            }
                        }
                        break;

                    #endregion

                    #region Pin

                    // pin name x y
                    case "pin":
                        {
                            if (l.Length != 4)
                                break;

                            double x, y;
                            if (double.TryParse(l[2], out x) &&
                                double.TryParse(l[3], out y))
                            {
                                string name = l[1];

                                var ellipse = new Ellipse()
                                {
                                    Stroke = Brushes.Red,
                                    Fill = Brushes.Red,
                                    StrokeThickness = 1.0,
                                    Width = 8,
                                    Height = 8,
                                    Margin = new Thickness(-4.0, -4.0, 0.0, 0.0)
                                };

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

                    // grid name x y width height
                    case "grid":
                        {
                            if (l.Length != 6)
                                break;

                            string name = l[1];
                            if (grids.ContainsKey(name) == false)
                            {
                                double x, y;
                                double width, height;
                                if (double.TryParse(l[2], out x) &&
                                    double.TryParse(l[3], out y) &&
                                    double.TryParse(l[4], out width) &&
                                    double.TryParse(l[5], out height))
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

                    // row grid-name height
                    case "row":
                        {
                            if (l.Length != 3)
                                break;

                            string gridName = l[1];

                            if (grids.ContainsKey(gridName))
                            {
                                double height;
                                if (double.TryParse(l[2], out height))
                                {
                                    Grid grid = grids[gridName];

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

                    // column grid-name width
                    case "column":
                        {
                            if (l.Length != 3)
                                break;

                            string gridName = l[1];

                            if (grids.ContainsKey(gridName))
                            {
                                double width;
                                if (double.TryParse(l[2], out width))
                                {
                                    Grid grid = grids[gridName];

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

                    // 0    1    2         3   4      5        6           7           8           9           10        11   12                
                    // text grid grid-name row column row-span column-span v-alignment h-alignment font-family font-size bold text
                    //where:
                    // grid -> layout type grid
                    // grid-name -> target grid
                    // row, column
                    // row-span, column-span
                    // v-alignment -> top, bottom, center, stretch
                    // h-alignment -> left, right, center, stretch
                    // font-family, font-size
                    // bold -> true, false
                    // text
                    case "text":
                        {
                            if (l.Length != 13)
                                break;

                            string layout = l[1];

                            switch (layout)
                            {
                                // eg.: text grid g1 1 0 1 1 center center Arial 10 false SA
                                case "grid":
                                    {
                                        string gridName = l[2];

                                        if (grids.ContainsKey(gridName) == false)
                                            break;

                                        Grid grid = grids[gridName];

                                        int r, c, rs, cs;
                                        if (int.TryParse(l[3], out r) == false ||
                                            int.TryParse(l[4], out c) == false ||
                                            int.TryParse(l[5], out rs) == false ||
                                            int.TryParse(l[6], out cs) == false)
                                            break;

                                        double fontSize;
                                        if (double.TryParse(l[10], out fontSize) == false)
                                            break;

                                        bool isBold;
                                        if (bool.TryParse(l[11], out isBold) == false)
                                            break;

                                        var tb = new TextBlock()
                                        {
                                            Text = l[12],
                                            FontFamily = new FontFamily(l[9]),
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

                                        switch (l[7])
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

                                        switch (l[8])
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
            CanvasBlock.Children.Clear();

            // lines
            var path = new Path() { StrokeThickness = 1.0, Stroke = Brushes.Red };
            RenderOptions.SetEdgeMode(path, EdgeMode.Aliased);
            path.SetValue(SnapsToDevicePixelsProperty, false);
            Canvas.SetLeft(path, offset);
            Canvas.SetTop(path, offset);
            string pathData = linesStringBuilder.ToString();
            path.Data = Geometry.Parse(pathData);
            CanvasBlock.Children.Add(path);

            // pins
            foreach (var ellipse in pinEllipses)
                CanvasBlock.Children.Add(ellipse);

            // grids
            foreach (var grid in grids)
                CanvasBlock.Children.Add(grid.Value);

            // generate Xaml
            TextXaml.Text = GenerateResourceDictionary(CanvasBlock as object);
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
                OmitXmlDeclaration = true,
            });
            var mgr = new XamlDesignerSerializationManager(writer);
            mgr.XamlWriterMode = XamlWriterMode.Expression;
            System.Windows.Markup.XamlWriter.Save(obj, mgr);

            return sb.ToString();
        }

        private ControlTemplate GetControlTemplate(object obj)
        {
            string objXaml = GetXaml(obj, "    ");

            string templatetagOpen = "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>\n";
            string templatetagClose = "</ControlTemplate>";

            ControlTemplate ct = (ControlTemplate)XamlReader.Parse(string.Concat(templatetagOpen, objXaml, templatetagClose));

            return ct;
        }

        private string GenerateResourceDictionary(object obj)
        {
            var rd = new ResourceDictionary();

            /*
            string styleText = "<Style TargetType='Ellipse' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>\n" +
                                    "<Setter Property='Stroke' Value='Red'/>\n" +
                                    "<Setter Property='Fill' Value='Red'/>\n" +
                                    "<Setter Property='StrokeThickness' Value='1.0'/>\n" +
                                    "<Setter Property='Width' Value='8.0'/>\n" +
                                    "<Setter Property='Height' Value='8.0'/>\n" +
                                    "<Setter Property='Margin' Value='-4,-4,0,0'/>\n" +
                               "</Style>";
            Style ellipseStyle = (Style)XamlReader.Parse(styleText);
            

            //Style ellipseStyle = new System.Windows.Style(typeof(Ellipse));

            //ellipseStyle.Setters.Add(new Setter(Ellipse.StrokeProperty, Brushes.Red));
            //ellipseStyle.Setters.Add(new Setter(Ellipse.FillProperty, Brushes.Red));
            //ellipseStyle.Setters.Add(new Setter(Ellipse.StrokeThicknessProperty, 1.0));
            //ellipseStyle.Setters.Add(new Setter(Ellipse.WidthProperty, 8.0));
            //ellipseStyle.Setters.Add(new Setter(Ellipse.HeightProperty, 8.0));
            //ellipseStyle.Setters.Add(new Setter(Ellipse.MarginProperty, new Thickness(-4.0, -4.0, 0.0, 0.0)));

            rd.Add("BlockEllipseKey", ellipseStyle);
            */

            ControlTemplate ct = GetControlTemplate(obj);
            rd.Add("BlockNameControlTemplateKey", ct);
     
            string rdXaml = GetXaml(rd, "    ");

            return rdXaml;
        }

        #endregion
    }
}
