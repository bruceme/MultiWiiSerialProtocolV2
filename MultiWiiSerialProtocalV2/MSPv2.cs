using System;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;

namespace MultiWiiSerialProtocalV2
{
    public class MSPv2
    {
        private SerialPort serialPort;

        public MSPv2(SerialPort port)
        {
            serialPort = port;
        }

        public T Get<T>(ushort mspFunction) where T : unmanaged
        {
            SendRequest(mspFunction);
            return Serializer.Deserialize<T>(ReceiveResponse());
        }


        private byte[] ReceiveResponse()
        {
            var letsRead = serialPort.BytesToRead;

            if (serialPort.ReadChar() != '$')
                return new byte[] { };

            var ident = serialPort.ReadChar();
            if (ident != 'M' && ident != 'X')
                return new byte[] { };

            var direction = (byte)serialPort.ReadByte();
            var flag = (byte)serialPort.ReadByte(); // zero
            var id0 = (byte)serialPort.ReadByte();
            var id1 = (byte)serialPort.ReadByte();
            var len0 = (byte)serialPort.ReadByte();
            var len1 = (byte)serialPort.ReadByte();
            var msgLen = len0 + (len1 << 8);

            var buff = new byte[msgLen];
            var stuff = serialPort.Read(buff, 0, msgLen);

            var crc = serialPort.ReadByte();

            var crcMsg = crc8_dvb_s2(new byte[] { flag, id0, id1, len0, len1 }.Concat(buff).ToArray(), 0, 5 + msgLen);

            if (crc != crcMsg)
                Console.WriteLine("bad crc");

            return buff;
        }

        static byte crc8_dvb_s2(byte crc, byte a)
        {
            crc ^= a;
            for (int i = 0; i < 8; i++)
            {
                var crcShiftAndMask = (byte)((crc << 1) & 0xFF);
                crc = (0 != (crc & 0x80)) ? (byte)(crcShiftAndMask ^ 0xD5) : crcShiftAndMask;
            }
            return crc;
        }

        byte crc8_dvb_s2(byte[] buffer, int start, int count)
        {
            byte ck2 = 0; // initialise CRC
            for (int i = start; i < count + start; i++)
                ck2 = crc8_dvb_s2(ck2, buffer[i]);
            return ck2;
        }

        private void SendRequest(ushort command)
        {
            SendRequest(command, new byte[] { });
        }

        private void SendRequest(ushort command, byte[] buffer)
        {
            byte[] o;
            if (null == buffer)
                buffer = new byte[] { };
            var len = buffer.Length;
            o = new byte[9 + len];
            int i = 0;
            // with checksum 
            o[i++] = (byte)'$';
            o[i++] = (byte)'X';
            o[i++] = (byte)'<';
            o[i++] = 0; // flag (always zero)
            o[i++] = (byte)(command & 0xFF);
            o[i++] = (byte)(command >> 8);
            o[i++] = (byte)(len & 0xFF);
            o[i++] = (byte)(len >> 8);
            foreach (var b in buffer)
                o[i++] = b;
            o[i++] = crc8_dvb_s2(o, 3, 5 + len);
            serialPort.Write(o, 0, i);
        }
    }

    // Really gotta force this one... cs compiler wants to put a phantom byte in there to make the first dword
    [StructLayout(LayoutKind.Explicit, Pack = 0, Size = 7)]
    public struct Version
    {
        [FieldOffset(0)]
        byte version;
        [FieldOffset(1)]
        byte multitype;
        [FieldOffset(2)]
        byte msp_version;
        [FieldOffset(3)]
        uint capability;
    }

    [StructLayout(LayoutKind.Sequential)]

    public struct Status2
    {
        ushort cycle_time;
        ushort i2c_errors_count;
        ushort sensor;
        ushort systemLoadPerc;
        byte configProfile;
        uint armingFlags;
        uint modeFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RawIMU
    {
        ushort accx;
        ushort accy;
        ushort accz;
        ushort gyrx;
        ushort gyry;
        ushort gyrz;
        ushort magx;
        ushort magy;
        ushort magz;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RawGPS
    {
        byte fix;
        byte num_sat;
        uint coord_lat;
        uint coord_lon;
        ushort altitude;
        ushort speed;
        ushort ground_course;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Attitude
    {
        ushort angx;
        ushort angy;
        ushort heading;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Altitude
    {
        uint altitude;
        ushort vario;
    }
}
