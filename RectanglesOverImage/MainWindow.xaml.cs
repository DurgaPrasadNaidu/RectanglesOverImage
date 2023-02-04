using Microsoft.Win32;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RectanglesOverImage
{
    /// <summary>
    /// Main window for the application "Rectangles over Image" managing loading 
    /// and saving of the images and including drawing, moving, resizing and removing rectangles.
    /// </summary>
    public partial class MainWindow : Window
    {
        // specifies whether the image is loaded or not
        // when the application starts, there is no image loaded
        // this variable helps prevent unnecessary rectangle drawings when there is no image
        private bool _isImageLoaded = false;

        // start point of the rectangle in canvas (left and top or x and y)
        private Point _startPoint;

        // set to true during rectangle drawing
        private bool _isMouseDown = false;

        // holds the latest rectangle being drawn
        private Rectangle _currentRectangle = null;

        /// <summary>
        /// Initializes the window and its components.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Shows an image open dialog box and loads the selected image.
        /// This method is called when "Load Image" button is clicked.
        /// </summary>
        private async void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            // show open file dialogbox allowing only BMP or JPG or PNG files to be selected
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image Files|*.bmp;*.jpg;*.png";
            openFileDialog.FilterIndex = 1;

            // the user has specified a file
            if ((bool)openFileDialog.ShowDialog())
            {
                // load the image into the Image control
                img.Source = new BitmapImage(new Uri(openFileDialog.FileName));
                _isImageLoaded = true;

                // clear any rectangles alread drawn on the canvas
                canvas.Children.Clear();

                // wait for some milliseconds and then set the canvas size to match the image size
                // waiting to make sure the image is loaded before we try to find its size
                await Task.Delay(100);
                canvas.Width = img.ActualWidth;
                canvas.Height = img.ActualHeight;

                // Note: the actual image may be bigger as compared to what is displayed
                // in the application because the "Border" container specified in XAML
                // fits the image and maintains the ratio
            }

        }

        /// <summary>
        /// Shows image save dialog box and saves the image with any rectangles drawn over it.
        /// This method is called when "Save Image" button is clicked.
        /// </summary>
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            // show open file dialogbox allowing only BMP or JPG or PNG files to be selected
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Image Files|*.bmp;*.jpg;*.png";

            // the user proceeds with dialogbox (clicks on Save)
            if ((bool)saveFileDialog.ShowDialog())
            {
                // as we know the image can be different in size than that is visible in the application
                // we need to find the X and Y scalers to save the image with its original size
                double scalerX = img.Source.Width / img.ActualWidth;
                double scalerY = img.Source.Height / img.ActualHeight;

                // create new canvas in memory that is as big as the original image
                // we will draw scaled rectangles on this new canvas
                // (as the size of the image changes, we need to resize the rectangles as well)
                Canvas newCanvas = new Canvas();
                newCanvas.Width = img.Source.Width;
                newCanvas.Height = img.Source.Height;

                // fetch rectangles from visible canvas and draw on canvas in memory
                foreach (Rectangle rect in canvas.Children)
                {
                    // creating new rectangle is necessary
                    // because the same rectangle cannot be child of multiple canvases
                    // create new (scaled) rectangle and calculate its X and Y (left and top)
                    Rectangle newRect = CloneRectangle(rect, scalerX, scalerY);
                    double newLeft = Canvas.GetLeft(rect) * scalerX;
                    double newTop = Canvas.GetTop(rect) * scalerY;

                    // draw this scaled rectangle on canvas in memory
                    newCanvas.Children.Add(newRect);
                    Canvas.SetTop(newRect, newTop);
                    Canvas.SetLeft(newRect, newLeft);
                }

                // as our canvas is in memory and is not visible,
                // following lines are necessary to let the canvas arrange all the components
                newCanvas.Arrange(new Rect(newCanvas.RenderSize));
                newCanvas.UpdateLayout();

                // prepare image visual for rendering
                DrawingVisual dw = new DrawingVisual();
                using (DrawingContext dc = dw.RenderOpen())
                    dc.DrawImage(img.Source, new Rect(0, 0, img.Source.Width, img.Source.Height));

                // identify the DPI (Device Independent Units) so that we render 
                // the image with the same DPI as the source
                DpiScale scale = VisualTreeHelper.GetDpi(img);

                // render the image and canvas from memory (that includes scaled and repositioned rectangles)
                RenderTargetBitmap bmp = new RenderTargetBitmap((int)newCanvas.Width, (int)newCanvas.Height, scale.PixelsPerInchX, scale.PixelsPerInchY, PixelFormats.Default);
                bmp.Render(dw);
                bmp.Render(newCanvas);

                // add rendered frames to PNG
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bmp));

                // save the PNG file to storage
                System.IO.FileStream stream = null;
                try
                {
                    stream = System.IO.File.Create(saveFileDialog.FileName);
                    encoder.Save(stream);
                    MessageBox.Show("Image saved successfully.", "Image Save");
                }
                catch (Exception ex)
                {
                    // any error occurred preventing file saving
                    MessageBox.Show("Could not save image.", "Image Save");
                    Console.WriteLine(ex.ToString());
                }
                finally
                {
                    // must close the stream if it is still open (specially in case of error)
                    if (stream != null)
                    {
                        stream.Close();
                    }
                }
            }
        }

        /// <summary>
        /// Starts drawing a rectangle on the canvas.
        /// This method is called when mouse is clicked on the canvas.
        /// </summary>
        private void canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // if there is no image loaded yet, no need to draw any rectangle
            if (!_isImageLoaded)
            {
                return;
            }

            // specify that mouse is now down, this will be used in other methods
            _isMouseDown = true;

            // create a rectangle
            if (_currentRectangle == null)
            {
                // store the mouse click position
                _startPoint = e.GetPosition(canvas);

                // create and add rectangle to canvas
                _currentRectangle = new Rectangle() { Fill = Brushes.GreenYellow, Stroke = Brushes.Black };
                Canvas.SetLeft(_currentRectangle, _startPoint.X);
                Canvas.SetTop(_currentRectangle, _startPoint.Y);
                canvas.Children.Add(_currentRectangle);
            }
        }

        /// <summary>
        /// Gives the new rectangle (that is being drawn) size as the mouse moves over the canvas.
        /// This method is called when the mouse moves over the canvas.
        /// </summary>
        private void canvas_MouseMove(object sender, MouseEventArgs e)
        {
            // to give the rectangle size, the mouse button must be pressed
            // here if the button is released, cancel the operation
            if (e.LeftButton == MouseButtonState.Released)
            {
                _isMouseDown = false;
                _currentRectangle = null;
            }

            // mouse moving without prior click, cancel the operation
            if (!_isMouseDown || _currentRectangle == null)
            {
                return;
            }

            // get the current mouse position
            // finalize the rectangle if the mouse is moving outside the canvas
            Point currentPoint = e.GetPosition(canvas);
            if (currentPoint.X >= canvas.Width - 2 || currentPoint.Y >= canvas.Height - 2)
            {
                canvas_MouseUp(sender, null);
                return;
            }

            // while drawing, the mouse can move in positive or negative direction
            // if it is positive direction, we need to update width and height of the rectangle
            // but if it is negative direction, we need to reset the top-left position
            // of the rectangle on the canvas as well

            // calculate new top-left position
            double x = Math.Min(_startPoint.X, currentPoint.X);
            double y = Math.Min(_startPoint.Y, currentPoint.Y);

            // calculate new width and height
            double width = Math.Max(_startPoint.X, currentPoint.X) - x;
            double height = Math.Max(_startPoint.Y, currentPoint.Y) - y;

            // update rectangle on canvas
            Canvas.SetLeft(_currentRectangle, x);
            Canvas.SetTop(_currentRectangle, y);
            _currentRectangle.Width = width;
            _currentRectangle.Height = height;
        }

        /// <summary>
        /// Finalizes the rectangle drawn on the canvas.
        /// This method is called when the mouse button is release.
        /// </summary>
        private void canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // finalize the rectangle and apply adorns (move, resize, delete icons) to the rectangle
            if (_currentRectangle != null)
            {
                AdornerLayer.GetAdornerLayer(canvas).Add(new RectangleAdorner(_currentRectangle, canvas));
            }

            // reset is necessary so that user can draw next rectangle
            _isMouseDown = false;
            _currentRectangle = null;
        }

        /// <summary>
        /// Create a new scaled rectangle from received rectangle.
        /// This method clones the rectangle and then applies scalers to it.
        /// The rectangle returned by this method is used to put on the canvas in memory
        /// which is then saved in the image file.
        /// </summary>
        /// <param name="rect">source rectangle</param>
        /// <param name="scalerX">horizontal scaler</param>
        /// <param name="scalerY">vertical scaler</param>
        /// <returns>scaled rectangle</returns>
        public Rectangle CloneRectangle(Rectangle rect, double scalerX = 1.0, double scalerY = 1.0)
        {
            // clone the rectangle
            Rectangle newRect = (Rectangle)XamlReader.Parse(XamlWriter.Save(rect));

            // scale the rectangle
            newRect.Width *= scalerX;
            newRect.Height *= scalerY;

            return newRect;
        }
    }
}
