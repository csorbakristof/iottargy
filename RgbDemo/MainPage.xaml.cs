using System;
using System.Diagnostics;
using System.Net.Http;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
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
            try
            {
                timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromMilliseconds(500);
                timer.Tick += Timer_Tick;
                leds.Init(true);
                ledShapes.Init(redLedShape, greenLedShape, blueLedShape);

                colorSensor = new ColorSensorTcs34725();
                await colorSensor.Init();

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
            var rgb = await colorSensor.GetRgbData();
            Debug.WriteLine(string.Format("R:{0} G:{1} B:{2}", rgb.Red, rgb.Green, rgb.Blue));
            SetLedsAccordingToSensorValue(rgb);
            SendReportToServer(rgb);
        }

        private async void SendReportToServer(RgbData rgb)
        {
            const string serverBaseUrl = "http://avalon.aut.bme.hu/kristof/rgbdemo/add.php";
            string url = string.Format("{0}?R={1}&G={2}&B={3}", serverBaseUrl, rgb.Red, rgb.Green, rgb.Blue);
            try
            {
                using (HttpClient http = new HttpClient())
                {
                    string response = await http.GetStringAsync(url);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format("SendReportToServer error:\n(URL: {0})\n{1}",url,ex.ToString()));
            }
        }

        private void SetLedsAccordingToSensorValue(RgbData rgb)
        {
            // Active LEDs corresponding to not significant color components.
            // (This way, most LEDs will be active most of the time.)
            int max = rgb.Red > rgb.Green ?
                (rgb.Red > rgb.Blue ? rgb.Red : rgb.Blue) :
                (rgb.Green > rgb.Blue ? rgb.Green : rgb.Blue);
            int min = rgb.Red < rgb.Green ?
                (rgb.Red < rgb.Blue ? rgb.Red : rgb.Blue) :
                (rgb.Green < rgb.Blue ? rgb.Green : rgb.Blue);
            int threshold = (min + max) / 2 + 20;
            setLed(0, rgb.Red < threshold);
            setLed(1, rgb.Green < threshold);
            setLed(2, rgb.Blue < threshold);
        }

        private void setLed(int index, bool value)
        {
            leds.SetLed(index, value);
            ledShapes.SetLed(index, value);
        }

        private void invertLed(int index)
        {
            leds.SetLed(index, !leds.GetLed(index));
            ledShapes.SetLed(index, leds.GetLed(index));
        }
    }
}
