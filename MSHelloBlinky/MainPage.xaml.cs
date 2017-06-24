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
        private SolidColorBrush redBrush = new SolidColorBrush(Windows.UI.Colors.Red);
        private SolidColorBrush grayBrush = new SolidColorBrush(Windows.UI.Colors.LightGray);

        private Leds leds = new Leds();

        public MainPage()
        {
            InitializeComponent();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(500);
            timer.Tick += Timer_Tick;
            leds.Init(true);
            GpioStatus.Text = "GPIO pin initialized correctly.";

            timer.Start();
        }

        private void Timer_Tick(object sender, object e)
        {
            if (leds.GetLed(0))
            {
                leds.SetLed(0, false);
                LED.Fill = redBrush;
            }
            else
            {
                leds.SetLed(0, true);
                LED.Fill = grayBrush;
            }
        }
             

    }
}
