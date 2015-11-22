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
        static ErrorCode PngCleaner(FileStream inputFile, FileStream outputFile)
        {
            if ((inputFile == null) || (outputFile == null))
                throw new ArgumentNullException();

            UInt32 controlDword;

            //check PNG 8 bytes header
            controlDword = ReadDWORD(inputFile);
            if (controlDword != 0x89504e47)
                return ErrorCode.FileCorrupt;
            controlDword = ReadDWORD(inputFile);
            if (controlDword != 0x0d0a1a0a)
                return ErrorCode.FileCorrupt;

            WriteDword(outputFile, 0x89504e47);
            WriteDword(outputFile, 0x0d0a1a0a);

            //process next chunk
            UInt32 chunkLength, chunkType, chunkCRC;
            do
            {
                bool skipChunk;
                chunkLength = ReadDWORD(inputFile);
                chunkType = ReadDWORD(inputFile);
                if ((chunkType == 0x69545874) /* iTXt */ ||
                    (chunkType == 0x74455874) /* tEXt */ ||
                    (chunkType == 0x7a545874) /* zTXt */ ||
                    (chunkType == 0x74494d45) /* tIME */
                    )
                    skipChunk = true;
                else
                    skipChunk = false;

                if (skipChunk == false)
                {
                    //copy chunk to the output file
                    WriteDword(outputFile, chunkLength);
                    WriteDword(outputFile, chunkType);

                    //chunk data
                    for (UInt32 i = 0; i < chunkLength; i++)
                    {
                        int inputByte = inputFile.ReadByte();
                        if (inputByte < 0)
                            throw new Exception("Unexpected end of file");
                        outputFile.WriteByte((byte)inputByte);
                    }

                    chunkCRC = ReadDWORD(inputFile);
                    WriteDword(outputFile, chunkCRC);
                }
                else
                {
                    //chunk data
                    for (UInt32 i = 0; i < chunkLength; i++)
                    {
                        int inputByte = inputFile.ReadByte();
                        if (inputByte < 0)
                            throw new Exception("Unexpected end of file");
                    }

                    chunkCRC = ReadDWORD(inputFile);
                }

                if (chunkType == 0x49454e44) /* IEND */
                    break;
            } while (true);

            return ErrorCode.NoError;
        }

        static UInt32 ReadDWORD(FileStream file)
        {
            UInt32 result = 0;
            int inputByte;
            
            for (int i = 0; i < 4; i++)
            {
                inputByte = file.ReadByte();
                if (inputByte < 0)
                    throw new Exception("Unexpected end of file");
                result = (result << 8) + (byte)(inputByte);
            }

            return result;
        }

        static void WriteDword(FileStream file, UInt32 dw)
        {
            file.WriteByte((byte)(dw >> 24));
            file.WriteByte((byte)((dw >> 16) & 0xff));
            file.WriteByte((byte)((dw >> 8) & 0xff));
            file.WriteByte((byte)(dw & 0xff));
        }
    }
}
