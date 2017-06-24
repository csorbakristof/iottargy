using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Gpio;
using Windows.Devices.I2c;

namespace RgbDemo
{
    //Create a class for the raw color data (Red, Green, Blue, Clear)
    public class ColorData
    {
        public UInt16 Red { get; set; }
        public UInt16 Green { get; set; }
        public UInt16 Blue { get; set; }
        public UInt16 Clear { get; set; }
    }

    //Create a class for the RGB data (Red, Green, Blue)
    public class RgbData
    {
        public int Red { get; set; }
        public int Green { get; set; }
        public int Blue { get; set; }
    }

    class ColorSensorTcs34725
    {
        #region Hardver parameters, registers, and enums
        struct TCS34725Params
        {
            //Address values set according to the datasheet: http://www.adafruit.com/datasheets/TCS34725.pdf
            public const byte Address = 0x29;

            public const byte ENABLE = 0x00;
            public const byte ENABLE_PON = 0x01; //Power on: 1 activates the internal oscillator, 0 disables it
            public const byte ENABLE_AEN = 0x02; //RGBC Enable: 1 actives the ADC, 0 disables it 

            public const byte ID = 0x12;
            public const byte CDATAL = 0x14;  //Clear channel data 
            public const byte CDATAH = 0x15;
            public const byte RDATAL = 0x16;  //Red channel data
            public const byte RDATAH = 0x17;
            public const byte GDATAL = 0x18;  //Green channel data
            public const byte GDATAH = 0x19;
            public const byte BDATAL = 0x1A;  //Blue channel data */
            public const byte BDATAH = 0x1B;
            public const byte ATIME = 0x01;   //Integration time
            public const byte CONTROL = 0x0F; //Set the gain level for the sensor

            public const byte COMMAND_BIT = 0x80; // Have to | addresses with this value when asking for values
        }


        //An enum for the sensor intergration time, based on the values from the datasheet
        enum IntegrationTime
        {
            _2_4MS = 0xFF,   //2.4ms - 1 cycle    - Max Count: 1024
            _24MS = 0xF6,    //24ms  - 10 cycles  - Max Count: 10240
            _50MS = 0xEB,    //50ms  - 20 cycles  - Max Count: 20480 
            _101MS = 0xD5,   //101ms - 42 cycles  - Max Count: 43008
            _154MS = 0xC0,   //154ms - 64 cycles  - Max Count: 65535 
            _700MS = 0x00    //700ms - 256 cycles - Max Count: 65535 
        };

        //An enum for the sensor gain, based on the values from the datasheet
        enum Gain
        {
            _1X = 0x00,   // No gain 
            _4X = 0x01,   // 2x gain
            _16X = 0x02,  // 16x gain
            _60X = 0x03   // 60x gain 
        };
        #endregion

        //String for the friendly name of the I2C bus 
        const string I2CControllerName = "I2C1";
        //Create an I2C device
        private I2cDevice colorSensor = null;

        //Create a GPIO Controller for the LED pin on the sensor
        private GpioController gpio;
        //Create a GPIO pin for the LED pin on the sensor
        private GpioPin LedControlGPIOPin;
        //Create a variable to store the GPIO pin number for the sensor LED
        private int LedControlPin;
        //Variable to check if device is initialized
        bool initialized = false;

        // We will default the led control pin to GPIO12 (Pin 32)
        public ColorSensorTcs34725(int ledControlPin = 12)
        {
            Debug.WriteLine("New TCS34725");
            //Set the LED control pin
            LedControlPin = ledControlPin;
        }

        public async Task Init()
        {
            Debug.WriteLine("TCS34725::Initialize");

            try
            {
                //Instantiate the I2CConnectionSettings using the device address of the TCS34725
                I2cConnectionSettings settings = new I2cConnectionSettings(TCS34725Params.Address);

                //Set the I2C bus speed of connection to fast mode
                settings.BusSpeed = I2cBusSpeed.FastMode;

                //Use the I2CBus device selector to create an advanced query syntax string
                string aqs = I2cDevice.GetDeviceSelector(I2CControllerName);

                //Use the Windows.Devices.Enumeration.DeviceInformation class to create a 
                //collection using the advanced query syntax string
                DeviceInformationCollection dis = await DeviceInformation.FindAllAsync(aqs);

                //Instantiate the the TCS34725 I2C device using the device id of the I2CBus 
                //and the I2CConnectionSettings
                colorSensor = await I2cDevice.FromIdAsync(dis[0].Id, settings);

                //Create a default GPIO controller
                gpio = GpioController.GetDefault();
                //Open the LED control pin using the GPIO controller
                LedControlGPIOPin = gpio.OpenPin(LedControlPin);
                //Set the pin to output
                LedControlGPIOPin.SetDriveMode(GpioPinDriveMode.Output);
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception: " + e.Message + "\n" + e.StackTrace);
                throw;
            }

        }

        //Enum for the LED state
        public enum eLedState { On, Off };
        //Default state is ON
        private eLedState _LedState = eLedState.On;
        public eLedState LedState
        {
            get { return _LedState; }
            set
            {
                Debug.WriteLine("TCS34725::LedState::set");
                //To set the LED state, first check for a valid LED control pin
                if (LedControlGPIOPin != null)
                {
                    //Set the GPIO pin value to the new value
                    GpioPinValue newValue = (value == eLedState.On ? GpioPinValue.High : GpioPinValue.Low);
                    LedControlGPIOPin.Write(newValue);
                    //Update the LED state variable
                    _LedState = value;
                }
            }
        }

        // Enable the sensor
        public async Task Enable()
        {
            Debug.WriteLine("TCS34725::enable");
            if (!initialized) await begin();

            byte[] WriteBuffer = new byte[] { 0x00, 0x00 };

            //Enable register 
            WriteBuffer[0] = TCS34725Params.ENABLE | TCS34725Params.COMMAND_BIT;

            //Send power on
            WriteBuffer[1] = TCS34725Params.ENABLE_PON;
            colorSensor.Write(WriteBuffer);

            //Pause between commands
            await Task.Delay(3);

            //Send ADC Enable
            WriteBuffer[1] = (TCS34725Params.ENABLE_PON | TCS34725Params.ENABLE_AEN);
            colorSensor.Write(WriteBuffer);
        }

        // Disable the sensor
        public async Task Disable()
        {
            Debug.WriteLine("TCS34725::disable");
            if (!initialized) await begin();

            //Read the enable buffer
            byte[] WriteBuffer = new byte[] { TCS34725Params.ENABLE | TCS34725Params.COMMAND_BIT };
            byte[] ReadBuffer = new byte[] { 0xFF };
            colorSensor.WriteRead(WriteBuffer, ReadBuffer);

            //Turn the device off to save power by reversing the on conditions
            byte onState = (TCS34725Params.ENABLE_PON | TCS34725Params.ENABLE_AEN);
            byte offState = (byte)~onState;
            offState &= ReadBuffer[0];
            byte[] OffBuffer = new byte[] { TCS34725Params.ENABLE, offState };
            colorSensor.Write(OffBuffer);
        }

        //Method to read the RGB data
        public async Task<RgbData> GetRgbData()
        {
            //Create an object to store the raw color data
            RgbData rgbData = new RgbData();

            //First get the raw color data
            ColorData colorData = await getRawColorData();
            //Check if clear data is received
            if (colorData.Clear > 0)
            {
                //Find the RGB values from the raw data using the clear data as reference
                rgbData.Red = (colorData.Red * 255 / colorData.Clear);
                rgbData.Blue = (colorData.Blue * 255 / colorData.Clear);
                rgbData.Green = (colorData.Green * 255 / colorData.Clear);
            }
            //Write the RGB values to the debug console
            Debug.WriteLine("RGB Data - red: {0}, green: {1}, blue: {2}", rgbData.Red, rgbData.Green, rgbData.Blue);

            //Return the data
            return rgbData;
        }

        #region Lower level init functions
        //Set the default integration time as 700ms
        IntegrationTime integrationTime = IntegrationTime._700MS;

        //Set the default integration time as no gain
        Gain gain = Gain._16X;

        private async Task begin()
        {
            Debug.WriteLine("TCS34725::Begin");
            byte[] WriteBuffer = new byte[] { TCS34725Params.ID | TCS34725Params.COMMAND_BIT };
            byte[] ReadBuffer = new byte[] { 0xFF };

            //Read and check the device signature
            colorSensor.WriteRead(WriteBuffer, ReadBuffer);
            Debug.WriteLine("TCS34725 Signature: " + ReadBuffer[0].ToString());

            if (ReadBuffer[0] != 0x44)
            {
                Debug.WriteLine("TCS34725::Begin Signature Mismatch.");
                return;
            }

            //Set the initalize variable to true
            initialized = true;

            //Set the default integration time
            setIntegrationTime(integrationTime);

            //Set default gain
            setGain(gain);

            //Note: By default the device is in power down mode on bootup so need to enable it.
            await Enable();
        }

        //Method to write the gain value to the control register
        private async void setGain(Gain gain)
        {
            if (!initialized) await begin();
            this.gain = gain;
            byte[] WriteBuffer = new byte[] { TCS34725Params.CONTROL | TCS34725Params.COMMAND_BIT,
                                              (byte)gain };
            colorSensor.Write(WriteBuffer);
        }

        //Method to write the integration time value to the ATIME register
        private async void setIntegrationTime(IntegrationTime integrationTime)
        {
            if (!initialized) await begin();
            this.integrationTime = integrationTime;
            byte[] WriteBuffer = new byte[] { TCS34725Params.ATIME | TCS34725Params.COMMAND_BIT,
                                              (byte)integrationTime };
            colorSensor.Write(WriteBuffer);
        }
        #endregion

        #region Low level color retrieval functions
        private async Task<ColorData> getRawColorData()
        {
            ColorData colorData = new ColorData();
            if (!initialized) await begin();

            colorData.Clear = readColor(TCS34725Params.CDATAL);
            colorData.Red = readColor(TCS34725Params.RDATAL);
            colorData.Green = readColor(TCS34725Params.GDATAL);
            colorData.Blue = readColor(TCS34725Params.BDATAL);

            Debug.WriteLine("Raw Data - red: {0}, green: {1}, blue: {2}, clear: {3}",
                            colorData.Red, colorData.Green, colorData.Blue, colorData.Clear);
            return colorData;
        }

        private ushort readColor(byte register)
        {
            byte[] WriteBuffer = new byte[] { 0x00 };
            byte[] ReadBuffer = new byte[] { 0x00, 0x00 };

            WriteBuffer[0] = (byte)(register | TCS34725Params.COMMAND_BIT);
            colorSensor.WriteRead(WriteBuffer, ReadBuffer);
            return ColorFromBuffer(ReadBuffer);
        }

        // Get the 16-bit color from 2 8-bit buffers
        private UInt16 ColorFromBuffer(byte[] buffer)
        {
            UInt16 color = 0x00;

            color = buffer[1];
            color <<= 8;
            color |= buffer[0];

            return color;
        }
        #endregion
    }
}
