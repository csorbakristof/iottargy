// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Diagnostics;
using Windows.Devices.Gpio;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace RgbDemo
{
    public sealed partial class MainPage : Page
    {
        private DispatcherTimer timer;

        private Leds leds = new Leds();
        private LedShapes ledShapes = new LedShapes();
        private ColorSensorTcs34725 colorSensor = new ColorSensorTcs34725();

        public MainPage()
        {
            InitializeComponent();
        }


        // Async init functions
        protected override async void OnNavigatedTo(NavigationEventArgs navArgs)
        {
            Debug.WriteLine("OnNavigatedTo...");
            try
            {
                colorSensor = new ColorSensorTcs34725();
                await colorSensor.Init();

                timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromMilliseconds(500);
                timer.Tick += Timer_Tick;
                leds.Init(true);
                ledShapes.Init(redLedShape, greenLedShape, blueLedShape);
                GpioStatus.Text = "GPIO pin initialized correctly.";

                timer.Start();
                Debug.WriteLine("Started...");

            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }


        private async void Timer_Tick(object sender, object e)
        {
            invertLed(0);
            invertLed(1);
            invertLed(2);

            var rgb = await colorSensor.GetRgbData();
            Debug.WriteLine(string.Format("R{0} G{1} B{2}", rgb.Red, rgb.Green, rgb.Blue));
        }

        private void invertLed(int index)
        {
            leds.SetLed(index, !leds.GetLed(index));
            ledShapes.SetLed(index, leds.GetLed(index));
        }
    }
}
