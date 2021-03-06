﻿using Windows.UI.Xaml.Media;

namespace RgbDemo
{
    // Wraps 3 WPF Ellipses corresponding to 3 LEDs.
    // Shapes are set externally via Init().
    public class LedShapes
    {
        private SolidColorBrush[] activeLedBrushes = new SolidColorBrush[3];
        private Windows.UI.Xaml.Shapes.Ellipse[] ellipses = new Windows.UI.Xaml.Shapes.Ellipse[3];
        private SolidColorBrush grayBrush = new SolidColorBrush(Windows.UI.Colors.LightGray);

        public void Init(Windows.UI.Xaml.Shapes.Ellipse redEllipse,
            Windows.UI.Xaml.Shapes.Ellipse greenEllipse,
            Windows.UI.Xaml.Shapes.Ellipse blueEllipse)
        {
            activeLedBrushes[0] = new SolidColorBrush(Windows.UI.Colors.Red);
            activeLedBrushes[1] = new SolidColorBrush(Windows.UI.Colors.Green);
            activeLedBrushes[2] = new SolidColorBrush(Windows.UI.Colors.Blue);
            ellipses[0] = redEllipse;
            ellipses[1] = greenEllipse;
            ellipses[2] = blueEllipse;
            for (int i = 0; i < 3; i++)
                ellipses[i].Fill = grayBrush;
        }

        public void SetLed(int index, bool value)
        {
            ellipses[index].Fill = value ? activeLedBrushes[index] : grayBrush;
        }
    }
}