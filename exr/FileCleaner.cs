using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace exr
{
    enum ErrorCode
    {
        NoError = 0,
        Unknown = -1,
        NoAccess = -2,
        FileNotFound = -3,
        FileCorrupt = -4
    }

    static partial class FileCleaner
    {
        public static void ProcessFile(String fileName)
        {
            string[] validExtensions = { ".jpg", ".jpeg", ".png" };

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

            if (string.IsNullOrEmpty(validExtensions.FirstOrDefault(x => x == fileInfo.Extension.ToLower())))
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

            //long inputFileLength = fileInfo.Length;

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

            try
            {
                ErrorCode result;
                switch (fileInfo.Extension.ToLower())
                {
                    case ".jpg":
                    case ".jpeg":
                        result = JpegCleaner(inputFile, outputFile);
                        break;
                    case ".png":
                        result = PngCleaner(inputFile, outputFile);
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(String.Format("Error: {0}", ex.Message));
                return ErrorCode.Unknown;
            }
            finally
            {
                outputFile.Flush();
                outputFile.Close();
                inputFile.Close();
            }

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


            return 0;
        }
    }
}
