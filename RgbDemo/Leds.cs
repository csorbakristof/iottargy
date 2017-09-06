using System;
using Windows.Devices.Gpio;

namespace RgbDemo
{
    // Wraps 3 LEDs
    public sealed class Leds
    {
        // Mapping of LED indices to hardware pin indices
        private readonly int[] ledPins = new int[3] { 5, 6, 13 };

        // GpioPin instances associated with the 3 LEDs
        private GpioPin[] pins = new GpioPin[3];

        // Stored status of the 3 LEDs.
        private bool[] ledStates = new bool[3];

        // Set true if LEDs use inverse logic on hardware level
        private bool isInverted = false;

        public void Init(bool isInverted=false)
        {
            this.isInverted = isInverted;
            var gpio = GpioController.GetDefault();
            if (gpio == null)
                throw new Exception("No GPIO controller found...");

            for (int i = 0; i < 3; i++)
            {
                pins[i] = gpio.OpenPin(ledPins[i]);
                SetLed(i, false);
                pins[i].SetDriveMode(GpioPinDriveMode.Output);
            }
        }

        public void SetLed(int index, bool value)
        {
            ledStates[index] = value;
            bool pinState = isInverted ? !value : value;
            pins[index].Write(pinState ? GpioPinValue.High : GpioPinValue.Low);
        }

        public bool GetLed(int index)
        {
            return ledStates[index];
        }
    }
}
