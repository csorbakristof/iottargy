using System;
using Windows.Devices.Gpio;

namespace RgbDemo
{
    public sealed class Leds
    {
        private readonly int[] ledPins = new int[3] { 5, 6, 13 };
        private GpioPin[] pins = new GpioPin[3];
        private bool[] ledStates = new bool[3];
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
