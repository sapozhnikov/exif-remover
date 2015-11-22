using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace exr
{
    static partial class FileCleaner
    {
        static ErrorCode JpegCleaner(FileStream inputFile, FileStream outputFile)
        {
            if ((inputFile == null) || (outputFile == null))
                throw new ArgumentNullException();

            int markerHead;
            int inputByte = inputFile.ReadByte();
            markerHead = inputByte;
            inputByte = inputFile.ReadByte();
            markerHead = (markerHead << 8) + inputByte;

            if (markerHead != 0xFFD8)
            {
                System.Console.WriteLine("Missed SOI marker");
                return ErrorCode.FileCorrupt;
            }

            outputFile.WriteByte(0xFF);
            outputFile.WriteByte(0xD8);

            int blockLength;
            do
            {
                inputByte = inputFile.ReadByte();
                //block header must be 0xFFXX
                if (inputByte != 0xFF)
                {
                    return ErrorCode.FileCorrupt;
                }
                markerHead = inputByte;
                inputByte = inputFile.ReadByte();
                if (inputByte < 0)
                    return ErrorCode.FileCorrupt;
                markerHead = (markerHead << 8) + inputByte;

                if (markerHead == 0xFFD9) //EOI marker
                {
                    break;
                }

                inputByte = inputFile.ReadByte();
                blockLength = inputByte;
                inputByte = inputFile.ReadByte();
                blockLength = (blockLength << 8) + inputByte;

                //System.Console.WriteLine(String.Format("Block {0:X} length {1} position {2:X}", markerHead, blockLength, inputFile.Position - 4));

                if (
                    ((markerHead >= 0xFFE0) && (markerHead <= 0xFFEF)) || //application segments
                    (markerHead == 0xFFFE) // JPEG comment
                    )
                {
                    //skip unnesessary data
                    for (int i = 0; (i < blockLength - 2) && (inputByte >= 0); i++)
                    {
                        inputByte = inputFile.ReadByte();
                    }
                    continue;
                }

                outputFile.WriteByte((byte)(markerHead >> 8));
                outputFile.WriteByte((byte)(markerHead & 0xFF));
                outputFile.WriteByte((byte)(blockLength >> 8));
                outputFile.WriteByte((byte)(blockLength & 0xFF));

                for (int i = 0; (i < blockLength - 2) && (inputByte >= 0); i++)
                {
                    inputByte = inputFile.ReadByte();
                    outputFile.WriteByte((byte)inputByte);
                }

                if (markerHead == 0xFFDA) //SOS marker
                {
                    //begin of the image stream
                    inputByte = inputFile.ReadByte();
                    while (inputByte >= 0)
                    {
                        outputFile.WriteByte((byte)inputByte);
                        inputByte = inputFile.ReadByte();
                    }

                    return ErrorCode.NoError;
                }

            }
            while (true);

            return ErrorCode.NoError;
        }
    }
}
