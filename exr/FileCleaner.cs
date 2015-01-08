using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.IO;

namespace exr
{
    enum ErrorCode : int
    {
        NoError = 0,
        Unknown = -1,
        NoAccess = -2,
        FileNotFound = -3,
        FileCorrupt = -4
    }

    static class FileCleaner
    {
        public static void ProcessFile(String fileName)
        {
            FileInfo fileInfo;

            try
            {
                fileInfo = new FileInfo(fileName);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(String.Format("Can't open file '{0}'. Reason: {1}", fileName, ex.Message));
                return;
            }

            if ((fileInfo.Extension.ToLower() != ".jpg") && (fileInfo.Extension.ToLower() != ".jpeg"))
            {
                System.Console.WriteLine(String.Format("{0} - wrong file extension", fileName));
                return;
            }

            System.Console.Write(String.Format("Cleaning file '{0}' - ", fileInfo.Name));

            ErrorCode err = CleanFile(fileInfo);
            ConsoleColor oldColor;
            switch (err)
            {
                case ErrorCode.NoError:
                    oldColor = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Green;
                    System.Console.WriteLine("OK");
                    Console.ForegroundColor = oldColor;
                    break;
                default:
                    oldColor = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    System.Console.WriteLine("ERROR");
                    Console.ForegroundColor = oldColor;
                    break;
            }
        }

        static ErrorCode CleanFile(FileInfo fileInfo)
        {
            FileStream inputFile;
            try
            {
                inputFile = fileInfo.OpenRead();
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(String.Format("Can't read file '{0}'. Reason: {1}", fileInfo.Name, ex.Message));
                return ErrorCode.NoAccess;
            }

            long inputFileLength = fileInfo.Length;

            String outputFileName = fileInfo.Name.Substring(0, fileInfo.Name.Length - fileInfo.Extension.Length) + ".new";
            FileStream outputFile;
            try
            {
                outputFile = new FileStream(outputFileName, FileMode.Create, FileAccess.Write);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(String.Format("Can't create file '{0}'. Reason: {1}", outputFileName, ex.Message));
                return ErrorCode.NoAccess;
            }

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

                    outputFile.Flush();
                    outputFile.Close();
                    inputFile.Close();
                    try
                    {
                        System.IO.File.Delete(inputFile.Name);
                    }
                    catch (Exception ex)
                    {
                        System.Console.WriteLine(String.Format("Can't delete file '{0}'. Reason: {1}", inputFile.Name, ex.Message));
                        return ErrorCode.NoAccess;
                    }

                    try
                    {
                        System.IO.File.Move(outputFile.Name, inputFile.Name);
                    }
                    catch (Exception ex)
                    {
                        System.Console.WriteLine(String.Format("Can't rename file '{0}'. Reason: {1}", outputFile.Name, ex.Message));
                        return ErrorCode.NoAccess;
                    }
                    //outputFile.Dispose();
                    //inputFile.Dispose();
                    return ErrorCode.NoError;
                }

            }
            while (true);

            return 0;
        }
    }
}
