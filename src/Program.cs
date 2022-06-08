using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using toolbelt;
using webgrep;

namespace websnapshot
{
    class Program
    {
        public static void Main(string[] args)
        {
            List<Uri> links = new List<Uri>();
            ParseUriArguments(args, links);

            Console.Error.WriteLine("websnapshot");
            using (FirefoxInstance browser = new FirefoxInstance())
            {
                int pageCount = 4;
                while (browser.Pages.Count < pageCount)
                    browser.NewPage();

                PageWrapper[] pages = browser.Pages.Cast<PageWrapper>().ToArray();
                IList<IList<Uri>> parts = links.PartitionCount<Uri>(pageCount);
                Thread[] worker = new Thread[pageCount];

                for (int i = 0; i < parts.Count; i++)
                {
                    worker[i] = new Thread(new ParameterizedThreadStart((idx) =>
                    {
                        IPageWrapper page = pages[(int)idx];
                        foreach (Uri uri in parts[(int)idx])
                        {
                            try
                            {
                                Console.Error.WriteLine($"navigate to {uri}");
                                if (page.NavigateTo(uri.ToString()))
                                {
                                    string title = GetTitle(page);
                                    string filename = GetFilenameForTitle(title);
                                    Console.Error.WriteLine($"store snapshot in {filename}");
                                    SaveSnapshotInFile(page, filename);
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.Error.WriteLine($"error: {ex.ToString()}");
                            }
                        }
                    }));
                    worker[i].Start(i);
                }

                for (int i = 0; i < pageCount; i++)
                    worker[i]?.Join();
                for (int i = 0; i < pageCount; i++)
                    pages[i].ClosePage();
            }
        }

        private static void SaveSnapshotInFile(IPageWrapper page, string filename)
        {
            using (MemoryStream mem = new MemoryStream())
            {
                if (page.TakeScreenshot(mem))
                {
                    mem.Seek(0, SeekOrigin.Begin);
                    using (var image = new Bitmap(System.Drawing.Image.FromStream(mem)))
                    {
                        image.Save(filename, ImageFormat.Jpeg);
                    }
                }
            }
        }

        private static string GetFilenameForTitle(string title)
        {
            string filename = $"{title}.jpg";
            int i = 1;
            while (File.Exists(filename))
            {
                filename = $"{title}.{i}.jpg";
                i++;
            }

            return filename;
        }

        private static string GetTitle(IPageWrapper page)
        {
            string title = page.AttachedElements("title").First().GetText();
            Console.Error.WriteLine($"title is {title}");
            foreach (char elem in Path.GetInvalidFileNameChars().Union(Path.GetInvalidPathChars()))
            {
                title = title.Replace(elem, '_');
            }

            return title;
        }

        private static void ParseUriArguments(string[] args, List<Uri> links)
        {
            using (Stream stdin = Console.OpenStandardInput())
            {
                if (stdin.CanRead && stdin.CanSeek && stdin.Length > 0)
                {
                    using (StreamReader sr = new StreamReader(stdin))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            if (Uri.IsWellFormedUriString(line, UriKind.Absolute))
                            {
                                links.Add(new Uri(line, UriKind.Absolute));
                            }
                        }
                    }
                }
            }
            foreach (var line in args)
            {
                if (Uri.IsWellFormedUriString(line, UriKind.Absolute))
                {
                    links.Add(new Uri(line, UriKind.Absolute));
                }
            }
        }
    }
}