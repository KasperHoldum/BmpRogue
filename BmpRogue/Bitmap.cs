using System;
using System.Drawing;
using System.IO;
namespace BitmapRogue
{
    public class Bitmap
    {
        private BmpFileType fileType;
        private HeaderType headerType;
        private CompressionType compressionType;
        private int fileSize;
        private Int16[] reservedValues;
        private int bitmapDataOffset;

        private WindowsV3Header bitmapInfo = new WindowsV3Header();

        public WindowsV3Header BitmapInfo
        {
            get { return bitmapInfo; }
            set { bitmapInfo = value; }
        }
        private Color[,] bitmapData;

        public Color[,] BitmapData
        {
            get { return bitmapData; }
        }

        private byte[] hiddenData;

        /// <summary>
        /// The length of the string that can be hidden in the current image.
        /// </summary>
        public int maxHiddenSpaceLength
        {
            get
            {
                int padBytes = 4 - ((int)bitmapInfo.width * 3) % 4;
                if (padBytes == 4)
                {
                    padBytes = 0;
                }
                return ((int)this.bitmapInfo.height) * padBytes;
            }
        }

        /// <summary>
        /// Returns a string representing the hidden data contained in the currently loaded image.
        /// </summary>
        public string HiddenData
        {
            get
            {
                return System.Text.ASCIIEncoding.ASCII.GetString(hiddenData); //.Replace("\0", "").Reverse();
            }
        }

        private static int PadBytes(int bits, int width)
        {
            int padBytes = 4 - (width * 3) % 4;
            if (padBytes == 4)
            {
                padBytes = 0;
            }

            return padBytes;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public Bitmap()
        {
            this.fileType = BmpFileType.BM;
            this.headerType = HeaderType.WindowsV3;
            this.compressionType = CompressionType.BI_RBG; // none
            this.fileSize = 0;
            this.reservedValues = new Int16[2];

            this.reservedValues[0] = (byte)65;
            this.reservedValues[1] = (byte)65;

            this.bitmapDataOffset = 0;
        }

        /// <summary>
        /// Saves the currently loaded bitmap to a file
        /// </summary>
        /// <param name="path">the file to save the bitmap to</param>
        public void Save(string path)
        {
            this.Save(path, null);
        }

        /// <summary>
        /// Saves the currently loaded bitmap to a file with a given hiddenDataString
        /// </summary>
        /// <param name="path">the file to save the bitmap to</param>
        /// <param name="hiddenDataString">the datastring to be hidden in image</param>
        public void Save(string path, string hiddenDataString)
        {
            BinaryWriter bw = null;

            /* Try to gain write access to the file */
            try
            {
                bw = new BinaryWriter(new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read));

            }
            catch (IOException)
            {
                throw;
            }
            byte[] hiddenData = null;

            if (hiddenDataString != null)
            {
                if (System.Text.ASCIIEncoding.ASCII.GetByteCount(hiddenDataString) > this.maxHiddenSpaceLength)
                {
                    throw new ArgumentException(string.Format("You tried to hide {} bytes in the file, even though max hideable bytes is {}", System.Text.ASCIIEncoding.ASCII.GetByteCount(hiddenDataString), this.maxHiddenSpaceLength));
                }

                hiddenData = System.Text.ASCIIEncoding.ASCII.GetBytes(hiddenDataString);
            }

            // write the fileheader
            System.Text.ASCIIEncoding encoder = new System.Text.ASCIIEncoding();
            byte[] test = encoder.GetBytes(this.fileType.ToString());
            bw.Write(encoder.GetBytes(this.fileType.ToString()));
            bw.Write(this.fileSize);
            bw.Write(this.reservedValues[0]);
            bw.Write(this.reservedValues[1]);
            bw.Write(this.bitmapDataOffset);

            // write the bmp header
            bw.Write(this.bitmapInfo.size);
            bw.Write(this.bitmapInfo.width);
            bw.Write(this.bitmapInfo.height);
            bw.Write(this.bitmapInfo.planes);
            bw.Write(this.bitmapInfo.depth);
            bw.Write((UInt32)this.bitmapInfo.compression);
            //bm.bitmapInfo.compression = (CompressionType)Enum.ToObject(typeof(CompressionType), (int)br.ReadUInt32());
            bw.Write(this.bitmapInfo.imagesize);
            bw.Write(this.bitmapInfo.horizontalResolution);
            bw.Write(this.bitmapInfo.verticalResolution);
            bw.Write(this.bitmapInfo.coloursInPalette);
            bw.Write(this.bitmapInfo.importantColours);

            int padBytes = Bitmap.PadBytes(4, (int)this.bitmapInfo.width);

            // write the actual data
            for (int i = 0 ; i < this.bitmapInfo.height; i++)
            {
                for (int j = 0; j < this.bitmapInfo.width; j++)
                {
                    switch (this.bitmapInfo.depth)
                    {
                        case 24:
                            // this is fblabla
                            bw.Write(this.bitmapData[j, i].B);
                            bw.Write(this.bitmapData[j, i].G);
                            bw.Write(this.bitmapData[j, i].R);
                            break;
                        default:
                            throw new NotImplementedException("Currently the parser only support 24-bit bitmaps");
                    }
                }

                // write extra bytes - hidden data if possible
                for (int k = 0; k < padBytes; k++)
                {
                    if (hiddenData != null && hiddenData.Length -1 > i * padBytes + k)
                    {
                        bw.Write(hiddenData[i * padBytes + k]);
                    }
                    else
                    {
                        /* user did not want to save any hidden data */
                        bw.Write((byte)0);
                    }
                }
            }
            bw.Close();
        }

        /// <summary>
        /// Loads a bitmap from a file
        /// </summary>
        /// <param name="file">the file to load bitmap from</param>
        /// <returns>Returns the bitmap</returns>
        public static Bitmap Parse(string file)
        {
            BinaryReader br;
            try
            {
                br = new BinaryReader(new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read));
            }
            catch (FileNotFoundException)
            {

                throw;
            }
            catch (Exception)
            {
                throw new Exception("Error while initiating the binaryReader in Parse()");
            }
            Bitmap bm = new Bitmap();

            /* First we'll parse the BMP file header!
             * --------------------------------------
             */

            /* in a bmp the first 2 bytes should read two letters.
             * in windows it should be BM for BitMap
             */

            byte[] read2 = new byte[2];
            br.BaseStream.Read(read2, 0, 2);

            string bitmapType = System.Text.ASCIIEncoding.ASCII.GetString(read2, 0, 2);

            /* Determining the file type */
            switch (bitmapType)
            {
                case "BM":
                    //Console.WriteLine("windows BitMap!");

                    /* NOTE: YOU CAN't QUITE CONCLUDE THIS; BUT FOR NOW IT*LL DO! */
                    bm.headerType = HeaderType.WindowsV3;
                    bm.fileType = BmpFileType.BM;
                    break;
                case "BA":
                case "CI":
                case "CP":
                case "IC":
                case "PT":
                    throw new NotImplementedException("Can only parse windows bitmap files!");
                    break;
                default:
                    throw new ArgumentException("The selected file was not a bitmap file");
                    break;
            }


            /* The next 4 bytes are the size of the BMP in bytes */
            bm.fileSize = br.ReadInt32();

            /* The next 2x2 bytes are reserved. The value in these depends on the program that created the file */
            bm.reservedValues[0] = br.ReadInt16();
            bm.reservedValues[1] = br.ReadInt16();

            /* The next 4 bytes describes the offset , ie starting address, where the bitmap data can be found */
            bm.bitmapDataOffset = br.ReadInt32();



            /* End of BMP header file.
             * 
             * now starts:
             * Bitmap information (DIB header)
             * 
             * Offset # 	Size 	Purpose
                Eh 	        4 	    the size of this header (40 bytes)
                12h 	    4 	    the bitmap width in pixels (signed integer).
                16h 	    4 	    the bitmap height in pixels (signed integer).
                1Ah 	    2 	    the number of color planes being used. Must be set to 1.
                1Ch 	    2 	    the number of bits per pixel, which is the color depth of the image. Typical values are 1, 4, 8, 16, 24 and 32.
                1Eh 	    4 	    the compression method being used. See the next table for a list of possible values.
                22h 	    4 	    the image size. This is the size of the raw bitmap data (see below), and should not be confused with the file size.
                26h 	    4 	    the horizontal resolution of the image. (pixel per meter, signed integer)
                2Ah 	    4 	    the vertical resolution of the image. (pixel per meter, signed integer)
                2Eh 	    4 	    the number of colors in the color palette, or 0 to default to 2n.
                32h 	    4 	    the number of important colors used, or 0 when every color is important; generally ignored.
             */


            bm.bitmapInfo.size = br.ReadUInt32();
            bm.bitmapInfo.width = br.ReadUInt32();
            bm.bitmapInfo.height = br.ReadUInt32();
            bm.bitmapInfo.planes = br.ReadUInt16();
            bm.bitmapInfo.depth = br.ReadUInt16();
            bm.bitmapInfo.compression = (CompressionType)Enum.ToObject(typeof(CompressionType), (int)br.ReadUInt32());

            bm.bitmapInfo.imagesize = br.ReadUInt32();

            bm.bitmapInfo.horizontalResolution = (uint)br.ReadInt32();
            bm.bitmapInfo.verticalResolution = (uint)br.ReadInt32();
            bm.bitmapInfo.coloursInPalette = br.ReadUInt32();
            bm.bitmapInfo.importantColours = br.ReadUInt32();

            /* End of DIB header
             * 
             * Now if bitmap is less than 16-bit than there now comes a color palette that describes all of the colours
             */
            //Console.WriteLine("Color Palette");
            if (bm.bitmapInfo.depth < 16)
            {
                // there is a color palette present!
                //Console.WriteLine("\tHERE GOES COLOR PALETTE DETAILS!");
                throw new NotImplementedException("Parser does not support less than 16-bit Bitmaps");
            }
            else
            {
                //Console.WriteLine("\tThere is no color palette present since the image has a depth bigger than or equal to 16-bit");
            }

            /* End of optional color palette
             * 
             * This block of bytes describes the image, pixel by pixel. Pixels are stored "upside-down" with respect to normal image
             * raster scan order, starting in the lower left corner, going from left to right, and then row by row from the bottom to
             * the top of the image. Uncompressed Windows bitmaps can also be stored from the top row to the bottom, if the image
             * height value is negative.
             * 
             * 
             * */

            //Console.WriteLine("Reading bitmap data");
            try
            {
                bm.bitmapData = Bitmap.Read24bitImageData(br, bm.bitmapInfo, out bm.hiddenData);

            }
            catch (Exception)
            {

                throw;
            }
            finally
            {
                br.Close();
            }

            return bm;
        }

        private static Color[,] Read1bitImageData(BinaryReader br, WindowsV3Header header, out byte[] hiddenData)
        {
            if (header.depth != 1)
            {
                throw new Exception("This method only parses 1-bit data");
            }
            hiddenData = null;
            return null;
        }

        private static Color[,] Read4bitImageData(BinaryReader br, WindowsV3Header header, out byte[] hiddenData)
        {
            if (header.depth != 4)
            {
                throw new Exception("This method only parses 4-bit data");
            }
            hiddenData = null;
            return null;
        }

        private static Color[,] Read8bitImageData(BinaryReader br, WindowsV3Header header, out byte[] hiddenData)
        {
            if (header.depth != 8)
            {
                throw new Exception("This method only parses 8-bit data");
            }

            hiddenData = null;
            return null;
        }


        private static Color[,] Read24bitImageData(BinaryReader br, WindowsV3Header header, out byte[] hiddenData)
        {
            if (header.depth != 24)
            {
                throw new Exception("This method only parses 24-bit data");
            }

            Color[,] bitmapData = new Color[header.width, header.height];

            int headerWidth = (int)header.width;
            int padBytes = 4 - (headerWidth * 3) % 4;

            if (padBytes == 4)
            {
                padBytes = 0;
            }

            hiddenData = new byte[header.height * padBytes];


            int i = 0; int j = 0;

            for (i = 0; i < header.height; i++)
            {
                byte[] bytes = br.ReadBytes(headerWidth * 3);

                for (j = 0; j < bytes.Length / 3; j++)
                {
                    // colour values come in 3 bytes formatted [B G R]
                    bitmapData[j, i] = Color.FromArgb(255, bytes[j * 3 + 2], bytes[j * 3 + 1], bytes[j * 3]);
                }

                /* the number of bytes in each row is always dividable by 4
                 * which means that if only 9 bytes was used on a line, we have to burn 1 byte more
                 * before next row starts!
                */

                for (int k = 0; k < padBytes; k++)
                {
                    hiddenData[i * padBytes + k] = br.ReadByte();
                }
            }

            return bitmapData;
        }

        #region "enums"
        /// <summary>
        /// Header that describes the Bitmap file
        /// </summary>
        public enum BmpFileType
        {
            BM, // Windows 3.1x, 95, NT, ... etc
            BA, // OS/2 Bitmap Array
            CI, // OS/2 Color Icon
            CP, // CP - OS/2 Color Pointer
            IC, // OS/2 Icon
            PT // OS/2 Pointer
        }

        /// <summary>
        /// BMP Header type, desribes which format the bmp header is
        /// </summary>
        public enum HeaderType
        {
            WindowsV3, // 40 bytes 	all Windows versions since Windows 3.0
            Os2V1, // 12 bytes OS/2 and also all Windows versions since Windows 3.0
            Os2V2, // 64 bytes
            WindowsV4, // 108 bytes all Windows versions since Windows 95/NT4
            WindowsV5 // 124 bytes Windows 98/2000 and newer
        }

        /// <summary>
        /// enum of different compressions types
        /// </summary>
        public enum CompressionType : uint
        {
            BI_RBG = 1, // none
            BI_RLE8 = 2, // Can be used only with 8-bit/pixel bitmaps
            BI_RLE4 = 3, // 	Can be used only with 4-bit/pixel bitmaps
            BI_BITFIELDS = 4, //	Can be used only with 16 and 32-bit/pixel bitmaps
            BI_JPEG = 5, // The bitmap contains a JPEG image
            BI_PNG = 6 // The bitmap contains a PNG image
        }

        public enum ImageDepth
        {
            OneBit = 1,
            FourBit = 2,
            EightBit = 8,
            TwentyFourBit = 24
        }
        #endregion

        #region "header structs"

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct WindowsV3Header
        {
            public uint size;
            public uint width;
            public uint height;
            public UInt16 planes;
            public UInt16 depth;
            public CompressionType compression;
            public uint imagesize;
            public uint horizontalResolution;
            public uint verticalResolution;
            public uint coloursInPalette;
            public uint importantColours; // generally ignored.

        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct Os2V1Header
        {
            public uint size;
            public UInt16 width;
            public UInt16 height;
            public UInt16 planes;
            public UInt16 depth;
        }
        #endregion
    }
}
