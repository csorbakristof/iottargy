// Copyright (c) Microsoft. All rights reserved.

using System;
using Windows.Devices.Gpio;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;

namespace RgbDemo
{
    public sealed partial class MainPage : Page
    {
        private DispatcherTimer timer;

        private Leds leds = new Leds();
        private LedShapes ledShapes = new LedShapes();

        public MainPage()
        {
            InitializeComponent();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(500);
            timer.Tick += Timer_Tick;
            leds.Init(true);
            ledShapes.Init(redLedShape, greenLedShape, blueLedShape);
            GpioStatus.Text = "GPIO pin initialized correctly.";

            timer.Start();
        }

        private void Timer_Tick(object sender, object e)
        {
            invertLed(0);
            invertLed(1);
            invertLed(2);
        }

        private void invertLed(int index)
        {
            leds.SetLed(index, !leds.GetLed(index));
            ledShapes.SetLed(index, leds.GetLed(index));
        }
    }
}
