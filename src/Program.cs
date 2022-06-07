using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using toolbelt;
using webgrep;

namespace websnapshot
{
    class Program
    {
        public static void Main(string[] args)
        {
            Console.Error.WriteLine("websnapshot");
            using (FirefoxInstance browser = new FirefoxInstance())
            {
                Console.Error.WriteLine($"navigate to {args[0]}");
                if (browser.NavigateTo(args[0]))
                {
                    string title = browser.AttachedElements("title").First().GetText();
                    Console.Error.WriteLine($"title is {title}");
                    foreach (char elem in Path.GetInvalidFileNameChars().Union(Path.GetInvalidPathChars()))
                    {
                        title = title.Replace(elem, '_');
                    }
                    string filename = $"{title}.jpg";
                    Console.Error.WriteLine($"store snapshot in {filename}");
                    using (MemoryStream mem = new MemoryStream())
                    {
                        if (browser.TakeScreenshot(mem))
                        {
                            mem.Seek(0, SeekOrigin.Begin);
                            using (var image = new Bitmap(System.Drawing.Image.FromStream(mem)))
                            {
                                image.Save(filename, ImageFormat.Jpeg);
                            }
                        }
                    }
                }
            }
        }
    }
}