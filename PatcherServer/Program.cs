using System;
using System.IO;
using System.Net;

using SevenZip;
using LZ = SevenZip.Compression.LZMA;

namespace PatcherServer
{
    public class Program
    {
        private const Int32 Dictionary = 1 << 21; //No dictionary
        private const Int32 PosStateBits = 2;
        private const Int32 LitContextBits = 3; // for normal files  // UInt32 litContextBits = 0; // for 32-bit data                                             
        private const Int32 LitPosBits = 0; // UInt32 litPosBits = 2; // for 32-bit data
        private const Int32 Algorithm = 2;
        private const Int32 NumFastBytes = 128;
        private const Boolean Eos = false;
        private const String Mf = "bt4";

        private static readonly CoderPropID[] PropIDs = 
        {
            CoderPropID.DictionarySize,
            CoderPropID.PosStateBits,  
            CoderPropID.LitContextBits,
            CoderPropID.LitPosBits,
            CoderPropID.Algorithm,
            CoderPropID.NumFastBytes,
            CoderPropID.MatchFinder,
            CoderPropID.EndMarker
        };

        private static readonly Object[] Properties = 
        {
            Dictionary,
            PosStateBits,  
            LitContextBits,
            LitPosBits,
            Algorithm,
            NumFastBytes,
            Mf,
            Eos
        };

        public static void Main()
        {
            var l = new HttpListener();
            l.Prefixes.Add("http://*:80/");
            l.Start();

            Console.WriteLine("HttpListener is listening...");

            while (l.IsListening)
            {
                var cont = l.GetContext();

                Console.WriteLine("Incoming request for: {0}", cont.Request.RawUrl.Replace("/", @"\"));

                var fname = Environment.CurrentDirectory + cont.Request.RawUrl.Replace("/", @"\");
                if (!File.Exists(fname))
                {
                    var modfname = fname.Substring(0, fname.Length - 4);
                    if (Path.GetExtension(fname) == ".7zl" && File.Exists(modfname))
                    {
                        var outStream = new FileStream(fname, FileMode.Create, FileAccess.Write);
                        var inStream = new FileStream(modfname, FileMode.Open, FileAccess.Read);

                        var encoder = new LZ.Encoder();
                        encoder.SetCoderProperties(PropIDs, Properties);
                        encoder.WriteCoderProperties(outStream);

                        var instreamLen = inStream.Length;

                        outStream.Write(BitConverter.GetBytes(instreamLen), 0, 8);

                        encoder.Code(inStream, outStream, -1, -1, null);

                        outStream.Close();
                        inStream.Close();
                    }
                    else
                    {
                        cont.Response.Close();
                        continue;
                    }
                }

                var buff = File.ReadAllBytes(fname);

                var mime = "text/html";

                switch (Path.GetExtension(fname))
                {
                    case "txt":
                        mime = "text/plain";
                        break;

                    case "tga":
                        mime = "image/targa";
                        break;

                    case "bmp":
                    case "png":
                    case "gif":
                    case "tiff":
                    case "jpeg":
                        mime = String.Format("image/{0}", Path.GetExtension(fname));
                        break;

                    case "tif":
                        mime = "image/tiff";
                        break;

                    case "jpg":
                    case "jpe":
                        mime = "image/jpeg";
                        break;

                    case "js":
                        mime = "text/javascript";
                        break;
                        
                    case "css":
                        mime = "text/css";
                        break;
                }
                try
                {
                    cont.Response.ContentType = mime;
                    cont.Response.ContentLength64 = buff.Length;
                    cont.Response.OutputStream.Write(buff, 0, buff.Length);
                    //cont.Response.Close();
                }
                catch (HttpListenerException)
                {
                    
                }
            }
        }
    }
}
