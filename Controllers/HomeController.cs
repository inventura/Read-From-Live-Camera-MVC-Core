using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BarcodeReader;
using Bytescout.BarCodeReader;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Read_From_Live_Camera_MVC_Core.Models;

namespace Read_From_Live_Camera_MVC_Core.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        public ActionResult UploadHtml5(string image, string type)
        {
            try
            {
                StringBuilder send = new StringBuilder();

                // Lock by send variable 
                lock (send)
                {
                    // Convert base64 string from the client side into byte array
                    byte[] bitmapArrayOfBytes = Convert.FromBase64String(image);
                    // Create Bytescout.BarCodeReader.Reader object
                    Reader reader = new Reader();
                    // Get the barcode type from user's selection in the combobox
                    reader.BarcodeTypesToFind = Barcode.GetBarcodeTypeToFindFromCombobox(type);
                    // Read barcodes from image bytes
                    reader.ReadFromMemory(bitmapArrayOfBytes);
                    // Check whether the barcode is decoded
                    if (reader.FoundBarcodes != null)
                    {
                        // Add each decoded barcode into the string 
                        foreach (FoundBarcode barcode in reader.FoundBarcodes)
                        {
                            // Add barcodes as text into the output string
                            send.AppendLine(String.Format("{0} : {1}", barcode.Type, barcode.Value));
                        }
                    }

                    // Return the output string as JSON
                    return Json(new { d = send.ToString() });
                }
            }
            catch (Exception ex)
            {
                // return the exception instead
                return Json(new { d = ex.Message + "\r\n" + ex.StackTrace });
            }
        }

        [HttpPost]
        public ActionResult UploadFlash(string type)
        {
            try
            {
                String send = "";
                System.Drawing.Image originalimg;
                // read barcode type set 
                MemoryStream log = new MemoryStream();
                byte[] buffer = new byte[1024];
                int c;
                // read input buffer with image and saving into the "log" memory stream
                while ((c = Request.Body.Read(buffer, 0, buffer.Length)) > 0)
                {
                    log.Write(buffer, 0, c);
                }
                // create image object
                originalimg = System.Drawing.Image.FromStream(log);
                // resample image
                originalimg = originalimg.GetThumbnailImage(640, 480, new System.Drawing.Image.GetThumbnailImageAbort(() => { return false; }), IntPtr.Zero);

                Bitmap bp = new Bitmap(originalimg);

                bp.Save("c:\\temp\\barcode.jpg", ImageFormat.Jpeg);

                // create bytescout barcode reader object
                Reader reader = new Reader();
                // set barcode type to read
                reader.BarcodeTypesToFind = Barcode.GetBarcodeTypeToFindFromCombobox(type);
                // read barcodes from image
                reader.ReadFrom(bp);
                // if there are any result then convert them into a single stream
                if (reader.FoundBarcodes != null)
                {
                    foreach (FoundBarcode barcode in reader.FoundBarcodes)
                    {
                        // form the output string
                        send = send + (String.Format("{0} : {1}", barcode.Type, barcode.Value));
                    }
                }
                // close the memory stream
                log.Close();
                // dispose the image object
                originalimg.Dispose();
                // write output 
                return Content("<d>" + send + "</d>");
            }
            catch (Exception ex)
            {
                // write the exception if any
                return Content("<d>" + ex + "</d>");
            }
        }
    }
}
