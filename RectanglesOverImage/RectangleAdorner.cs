using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RectanglesOverImage
{
    /// <summary>
    /// This class adorns the rectangles drawn on the canvas.
    /// The functionalities by this class provided are move the rectangle,
    /// resize the rectangle and remove it from the canvas.
    /// </summary>
    public class RectangleAdorner : Adorner
    {
        // rectangle and canvas we are working with
        private Rectangle _rect;
        private Canvas _canvas;

        // collection of adorns
        private VisualCollection _adornerVisuals;
        private Thumb _resizer;
        private Thumb _mover;
        private Image _remover;

        /// <summary>
        /// Initializes the adorns for the rectangle.
        /// </summary>
        /// <param name="rect">rectangle</param>
        /// <param name="canvas">canvas</param>
        public RectangleAdorner(Rectangle rect, Canvas canvas) : base(rect)
        {
            _rect = rect;
            _canvas = canvas;

            // create adorns (for moving, resizing and removing rectangle)
            _mover = new Thumb() { Background = Brushes.RoyalBlue, Height = 10, Width = 10 };
            _resizer = new Thumb() { Background = Brushes.Firebrick, Height = 10, Width = 10 };
            _remover = new Image()
            {
                Source = new BitmapImage(new Uri("/delete.png", UriKind.Relative)),
                Stretch = Stretch.None
            };

            // register handlers to adorns
            _mover.DragDelta += mover_DragDelta;
            _resizer.DragDelta += resizer_DragDelta;
            _remover.MouseDown += _remover_MouseDown;

            // populate collection of adorns
            _adornerVisuals = new VisualCollection(this);
            _adornerVisuals.Add(_mover);
            _adornerVisuals.Add(_remover);
            _adornerVisuals.Add(_resizer);
        }

        /// <summary>
        /// Removes the rectangle from canvas.
        /// This method is called when "Remover" adorn is clicked.
        /// </summary>
        private void _remover_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _canvas.Children.Remove(_rect);
        }

        /// <summary>
        /// Moves the rectangle with the mouse drag operation.
        /// This method is called when "Mover" adorn is clicked and dragged.
        /// </summary>
        private void mover_DragDelta(object sender, DragDeltaEventArgs e)
        {
            // identify current location of mouse
            double x = Canvas.GetLeft(_rect) + e.HorizontalChange;
            double y = Canvas.GetTop(_rect) + e.VerticalChange;

            // don't allow rectangle moving past the canvas boundaries
            if (x < 0)
            {
                x = 0;
            }
            else if (x > _canvas.Width - _rect.Width)
            {
                x = _canvas.Width - _rect.Width;
            }

            if (y < 0)
            {
                y = 0;
            }
            else if (y > _canvas.Height - _rect.Height)
            {
                y = _canvas.Height - _rect.Height;
            }

            // update rectangle position on the canvas
            Canvas.SetLeft(_rect, x);
            Canvas.SetTop(_rect, y);
        }

        /// <summary>
        /// Resizes the rectangle on the canvas.
        /// This method is called when "Resizer" adorn is clicked and dragged.
        /// </summary>
        private void resizer_DragDelta(object sender, DragDeltaEventArgs e)
        {
            // identify current size of rectangle
            double width = _rect.Width + e.HorizontalChange;
            double height = _rect.Height + e.VerticalChange;

            // don't allow rectangle being really tiny or bigger than canvas
            if (width < 5)
            {
                width = 5;
            }
            else if (width > _canvas.Width - Canvas.GetLeft(_rect))
            {
                width = _canvas.Width - Canvas.GetLeft(_rect);
            }

            if (height < 5)
            {
                height = 5;
            }
            else if (height > _canvas.Height - Canvas.GetTop(_rect))
            {
                height = _canvas.Height - Canvas.GetTop(_rect);
            }

            // update rectangle size on the canvas
            _rect.Width = width;
            _rect.Height = height;
        }


        /// <summary>
        /// This overridden method is used internally to get specified adorn.
        /// </summary>
        /// <param name="index">index of adorn</param>
        /// <returns>adorn (as Visual)</returns>
        protected override Visual GetVisualChild(int index)
        {
            return _adornerVisuals[index];
        }

        /// <summary>
        /// This overridden method is used internally to get the count of adorns.
        /// </summary>
        protected override int VisualChildrenCount => _adornerVisuals.Count;

        /// <summary>
        /// This overridden method is used internally to arrange the adorns.
        /// This method sets the size and positions of adorns (thumbs and image).
        /// </summary>
        protected override Size ArrangeOverride(Size finalSize)
        {
            // place mover adorn (thumb) at the middle of the rectangle
            _mover.Arrange(new Rect(AdornedElement.DesiredSize.Width / 2 - 5, AdornedElement.DesiredSize.Height / 2 - 5, 10, 10));

            // place remover adorn (image) at top-right corner
            _remover.Arrange(new Rect(AdornedElement.DesiredSize.Width - 8, -8, 16, 16));

            // place resizer adorn (thumb) at bottom-right corner
            _resizer.Arrange(new Rect(AdornedElement.DesiredSize.Width - 5, AdornedElement.DesiredSize.Height - 5, 10, 10));

            return base.ArrangeOverride(finalSize);
        }

    }


}
