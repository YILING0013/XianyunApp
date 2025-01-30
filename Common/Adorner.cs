using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

public class DragAdorner : Adorner
{
    private readonly UIElement _child;
    private readonly double XCenter;
    private readonly double YCenter;
    private double _leftOffset;
    private double _topOffset;

    public double scale = 1;

    public DragAdorner(UIElement owner) : base(owner) { }

    public DragAdorner(UIElement owner, UIElement adornElement, bool useVisualBrush, double opacity)
        : base(owner)
    {
        if (adornElement == null) return;

        var brush = new VisualBrush
        {
            Opacity = opacity,
            Visual = adornElement
        };

        var dropShadow = new DropShadowEffect
        {
            Color = Colors.Black,
            BlurRadius = 15,
            Opacity = opacity
        };

        var r = new Rectangle
        {
            RadiusX = 3,
            RadiusY = 3,
            Fill = brush,
            Effect = dropShadow,
            Width = adornElement.DesiredSize.Width * scale,
            Height = adornElement.DesiredSize.Height * scale
        };

        XCenter = adornElement.DesiredSize.Width / 2 * scale;
        YCenter = adornElement.DesiredSize.Height / 2 * scale;
        _child = r;
    }

    protected override Visual GetVisualChild(int index) => _child;
    protected override int VisualChildrenCount => 1;

    protected override Size MeasureOverride(Size finalSize)
    {
        _child.Measure(finalSize);
        return _child.DesiredSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        _child.Arrange(new Rect(_child.DesiredSize));
        return finalSize;
    }

    public override GeneralTransform GetDesiredTransform(GeneralTransform transform)
    {
        var group = new GeneralTransformGroup();
        group.Children.Add(base.GetDesiredTransform(transform));
        group.Children.Add(new TranslateTransform(_leftOffset, _topOffset));
        return group;
    }

    private void UpdatePosition()
    {
        var adorner = Parent as AdornerLayer;
        adorner?.Update(AdornedElement);
    }

    public double LeftOffset
    {
        get => _leftOffset;
        set
        {
            _leftOffset = value - XCenter;
            UpdatePosition();
        }
    }

    public double TopOffset
    {
        get => _topOffset;
        set
        {
            _topOffset = value - YCenter;
            UpdatePosition();
        }
    }
}
