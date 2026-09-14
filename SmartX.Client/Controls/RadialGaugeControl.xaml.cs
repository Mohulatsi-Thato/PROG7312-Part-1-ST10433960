using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SmartX.Client.Controls;

/// <summary>
/// The Part 1 engagement widget's visual centrepiece: an animated radial
/// gauge (not a plain <see cref="ProgressBar"/>) that sweeps from 0-360
/// degrees based on <see cref="Score"/> and colours itself green/amber/red
/// to match the Sensor Health Card states described in the research report.
/// </summary>
public partial class RadialGaugeControl : UserControl
{
    public static readonly DependencyProperty ScoreProperty =
        DependencyProperty.Register(
            nameof(Score),
            typeof(int),
            typeof(RadialGaugeControl),
            new PropertyMetadata(100, OnScoreChanged));

    public int Score
    {
        get => (int)GetValue(ScoreProperty);
        set => SetValue(ScoreProperty, value);
    }

    public RadialGaugeControl()
    {
        InitializeComponent();
        Loaded += (_, _) => Redraw();
    }

    private static void OnScoreChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RadialGaugeControl gauge)
            gauge.Redraw();
    }

    private void Redraw()
    {
        if (ScoreText is null || ArcPath is null) return;

        var clamped = Math.Clamp(Score, 0, 100);
        ScoreText.Text = clamped.ToString();

        ArcPath.Stroke = new SolidColorBrush(clamped switch
        {
            >= 80 => Color.FromRgb(0x22, 0xC5, 0x5E), // green - healthy
            >= 40 => Color.FromRgb(0xF5, 0x9E, 0x0B), // amber - anomalous
            _ => Color.FromRgb(0xEF, 0x44, 0x44),      // red - disconnected
        });

        if (clamped <= 0)
        {
            ArcPath.Data = null;
            return;
        }

        const double radius = 60;
        const double center = 60;
        const double startAngle = -90; // start at 12 o'clock

        var sweepAngle = 360.0 * clamped / 100.0;
        var endAngle = startAngle + Math.Min(sweepAngle, 359.999); // avoid a degenerate full-circle arc

        var startPoint = PointOnCircle(center, center, radius, startAngle);
        var endPoint = PointOnCircle(center, center, radius, endAngle);
        var isLargeArc = sweepAngle > 180.0;

        var figure = new PathFigure { StartPoint = startPoint, IsClosed = false };
        figure.Segments.Add(new ArcSegment(
            endPoint,
            new Size(radius, radius),
            0,
            isLargeArc,
            SweepDirection.Clockwise,
            isStroked: true));

        ArcPath.Data = new PathGeometry(new[] { figure });
    }

    private static Point PointOnCircle(double centerX, double centerY, double radius, double angleDegrees)
    {
        var radians = angleDegrees * Math.PI / 180.0;
        return new Point(centerX + radius * Math.Cos(radians), centerY + radius * Math.Sin(radians));
    }
}
