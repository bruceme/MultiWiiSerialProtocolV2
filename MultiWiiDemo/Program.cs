using MultiWiiSerialProtocalV2;
using System;
using System.IO.Ports;

namespace MultiWiiDemo
{
    class Program
    {
        static SerialPort serialPort = new SerialPort("COM11", 115200);

        static MSPv2 msp = new MSPv2(serialPort);

        static void Main(string[] args)
        {
            serialPort.Open();
            serialPort.ReadTimeout = 200;

            var version = msp.Get<MultiWiiSerialProtocalV2.Version>(MSP.MSP_IDENT);
            var status = msp.Get<Status2>(MSP.MSP2_INAV_STATUS);
            var imu = msp.Get<RawIMU>(MSP.MSP_RAW_IMU);
            var altitude = msp.Get<Altitude>(MSP.MSP_ALTITUDE);
            var attitude = msp.Get<Attitude>(MSP.MSP_ATTITUDE);
            var airspeed = msp.Get<float>(MSP.MSP2_INAV_AIR_SPEED);

            serialPort.Close();
        }

        static void HexDump(byte[] buff)
        {
            foreach (var b in buff)
                Console.Write(b.ToString("X") + " ");
            Console.WriteLine();
        }
    }
}