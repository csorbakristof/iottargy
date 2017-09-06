using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;

namespace RgbDemo
{
    public class RgbData
    {
        public int Red { get; set; }
        public int Green { get; set; }
        public int Blue { get; set; }
    }

    class ColorSensorTcs34725
    {
        #region Hardver parameters, registers, and enums (from the datasheet)
        struct TCS34725Params
        {
            // Address values set according to the datasheet:
            //   http://www.adafruit.com/datasheets/TCS34725.pdf
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
            public const byte BDATAL = 0x1A;  //Blue channel data
            public const byte BDATAH = 0x1B;
            public const byte ATIME = 0x01;   //Integration time
            public const byte CONTROL = 0x0F; //Set the gain level for the sensor

            public const byte COMMAND_BIT = 0x80; // Have to | addresses with this value when asking for values
        }

        // An enum for the sensor intergration time, based on the values from the datasheet
        enum IntegrationTime
        {
            _2_4MS = 0xFF,   //2.4ms - 1 cycle    - Max Count: 1024
            _24MS = 0xF6,    //24ms  - 10 cycles  - Max Count: 10240
            _50MS = 0xEB,    //50ms  - 20 cycles  - Max Count: 20480 
            _101MS = 0xD5,   //101ms - 42 cycles  - Max Count: 43008
            _154MS = 0xC0,   //154ms - 64 cycles  - Max Count: 65535 
            _700MS = 0x00    //700ms - 256 cycles - Max Count: 65535 
        };

        // An enum for the sensor gain, based on the values from the datasheet
        enum Gain
        {
            _1X = 0x00,   // No gain 
            _4X = 0x01,   // 2x gain
            _16X = 0x02,  // 16x gain
            _60X = 0x03   // 60x gain 
        };
        #endregion

        const string I2CControllerName = "I2C1";
        private I2cDevice colorSensor = null;
        bool initialized = false;

        public async Task Init()
        {
            try
            {
                // Instantiate the I2CConnectionSettings using the device address of the TCS34725
                I2cConnectionSettings settings = new I2cConnectionSettings(TCS34725Params.Address);
                settings.BusSpeed = I2cBusSpeed.FastMode;
                // Use the I2CBus device selector to create an advanced query syntax string
                string aqs = I2cDevice.GetDeviceSelector(I2CControllerName);

                // Use the Windows.Devices.Enumeration.DeviceInformation class to create a 
                // collection using the advanced query syntax string
                DeviceInformationCollection dis = await DeviceInformation.FindAllAsync(aqs);

                // Instantiate the the TCS34725 I2C device using the device id of the I2CBus 
                // and the I2CConnectionSettings
                colorSensor = await I2cDevice.FromIdAsync(dis[0].Id, settings);
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception: " + e.Message + "\n" + e.StackTrace);
                throw;
            }

        }

        public async Task Enable()
        {
            if (!initialized) await begin();

            byte[] WriteBuffer = new byte[] { 0x00, 0x00 };
            WriteBuffer[0] = TCS34725Params.ENABLE | TCS34725Params.COMMAND_BIT;

            // Send power on
            WriteBuffer[1] = TCS34725Params.ENABLE_PON;
            colorSensor.Write(WriteBuffer);

            // Pause between commands
            await Task.Delay(3);

            // Send ADC Enable
            WriteBuffer[1] = (TCS34725Params.ENABLE_PON | TCS34725Params.ENABLE_AEN);
            colorSensor.Write(WriteBuffer);
        }

        public async Task Disable()
        {
            if (!initialized) await begin();

            // Read the enable buffer
            byte[] WriteBuffer = new byte[] { TCS34725Params.ENABLE | TCS34725Params.COMMAND_BIT };
            byte[] ReadBuffer = new byte[] { 0xFF };
            colorSensor.WriteRead(WriteBuffer, ReadBuffer);

            // Turn the device off to save power by reversing the on conditions
            // (Turn off the bits ENABLE_PON (enable power) and ENABLE_AEN (enable ADC).)
            byte onState = (TCS34725Params.ENABLE_PON | TCS34725Params.ENABLE_AEN);
            byte offState = (byte)~onState; // Mask to clear these bits
            offState &= ReadBuffer[0];
            byte[] OffBuffer = new byte[] { TCS34725Params.ENABLE, offState };
            colorSensor.Write(OffBuffer);   // Write back the new settings
        }

        public async Task<RgbData> GetRgbData()
        {
            RgbData rgbData = new RgbData();
            ColorData colorData = await getRawColorData();
            if (colorData.Clear > 0)
            {
                // Find the RGB values from the raw data using the clear data as reference
                rgbData.Red = (colorData.Red * 255 / colorData.Clear);
                rgbData.Blue = (colorData.Blue * 255 / colorData.Clear);
                rgbData.Green = (colorData.Green * 255 / colorData.Clear);
            }
            return rgbData;
        }

        #region Lower level init functions
        // Set the default integration time as 700ms
        IntegrationTime integrationTime = IntegrationTime._700MS;

        // Set the default integration time as no gain
        Gain gain = Gain._16X;

        // Init the sensor
        private async Task begin()
        {
            Debug.WriteLine("TCS34725::Begin");
            byte[] WriteBuffer = new byte[] { TCS34725Params.ID | TCS34725Params.COMMAND_BIT };
            byte[] ReadBuffer = new byte[] { 0xFF };

            // Read and check the device signature
            colorSensor.WriteRead(WriteBuffer, ReadBuffer);
            Debug.WriteLine("TCS34725 Signature: " + ReadBuffer[0].ToString());
            if (ReadBuffer[0] != 0x44)
            {
                // This device does not seem to be the expected one.
                Debug.WriteLine("TCS34725::Begin Signature Mismatch.");
                return;
            }

            initialized = true;

            // Set the default values
            setIntegrationTime(integrationTime);
            setGain(gain);

            // Note: By default the device is in power down mode on bootup so need to enable it.
            await Enable();
        }

        private async void setGain(Gain gain)
        {
            if (!initialized) await begin();
            this.gain = gain;
            byte[] WriteBuffer = new byte[] { TCS34725Params.CONTROL | TCS34725Params.COMMAND_BIT,
                                              (byte)gain };
            colorSensor.Write(WriteBuffer);
        }

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
        // Raw color data (Red, Green, Blue, Clear)
        public class ColorData
        {
            public UInt16 Red { get; set; }
            public UInt16 Green { get; set; }
            public UInt16 Blue { get; set; }
            public UInt16 Clear { get; set; }
        }

        private async Task<ColorData> getRawColorData()
        {
            ColorData colorData = new ColorData();
            if (!initialized) await begin();

            colorData.Clear = readColor(TCS34725Params.CDATAL);
            colorData.Red = readColor(TCS34725Params.RDATAL);
            colorData.Green = readColor(TCS34725Params.GDATAL);
            colorData.Blue = readColor(TCS34725Params.BDATAL);

            Debug.WriteLine("Raw Color Data - R:{0}, G:{1}, B:{2}, Clear:{3}",
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
