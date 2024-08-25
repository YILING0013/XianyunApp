using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

public class DragAdorner : Adorner
{
    public DragAdorner(UIElement owner) : base(owner) { }

    public DragAdorner(UIElement owner, UIElement adornElement, bool useVisualBrush, double opacity)
        : base(owner)
    {
        _owner = owner;
        VisualBrush _brush = new VisualBrush
        {
            Opacity = opacity,
            Visual = adornElement
        };

        DropShadowEffect dropShadowEffect = new DropShadowEffect
        {
            Color = Colors.Black,
            BlurRadius = 15,
            Opacity = opacity
        };

        Rectangle r = new Rectangle
        {
            RadiusX = 3,
            RadiusY = 3,
            Fill = _brush,
            Effect = dropShadowEffect,
            Width = adornElement.DesiredSize.Width * scale,
            Height = adornElement.DesiredSize.Height * scale
        };


        XCenter = adornElement.DesiredSize.Width / 2 * scale;
        YCenter = adornElement.DesiredSize.Height / 2 * scale;

        _child = r;
    }

    private void UpdatePosition()
    {
        AdornerLayer adorner = (AdornerLayer)Parent;
        if (adorner != null)
        {
            adorner.Update(AdornedElement);
        }
    }

    #region Overrides

    protected override Visual GetVisualChild(int index)
    {
        return _child;
    }
    protected override int VisualChildrenCount
    {
        get
        {
            return 1;
        }
    }
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
        GeneralTransformGroup result = new GeneralTransformGroup();

        result.Children.Add(base.GetDesiredTransform(transform));
        result.Children.Add(new TranslateTransform(_leftOffset, _topOffset));
        return result;
    }

    #endregion

    #region Field & Properties

    public double scale = 1;
    protected UIElement _child;
    protected VisualBrush _brush;
    protected UIElement _owner;
    protected double XCenter;
    protected double YCenter;
    private double _leftOffset;
    public double LeftOffset
    {
        get { return _leftOffset; }
        set
        {
            _leftOffset = value - XCenter;
            UpdatePosition();
        }
    }
    private double _topOffset;
    public double TopOffset
    {
        get { return _topOffset; }
        set
        {
            _topOffset = value - YCenter;

            UpdatePosition();
        }
    }

    #endregion
}