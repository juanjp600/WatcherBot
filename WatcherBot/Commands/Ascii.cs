using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using DisCatSharp.Entities;

namespace WatcherBot.Commands
{

	public class CumCommandModule : BaseCommandModule
	{
		private readonly BotMain botMain;
		public CumCommandModule(BotMain bm) => botMain = bm;

        public async Task readFuckingImage(CommandContext context, string asd)
		{
            Console.WriteLine(asd); 
            HttpClient funny = new HttpClient();
            var res = await funny.GetAsync(asd);
            MemoryStream ms = new MemoryStream(await res.Content.ReadAsByteArrayAsync());
            Image returnImage = Image.FromStream(ms);
            returnImage.RotateFlip(RotateFlipType.RotateNoneFlipY);
            ms = new MemoryStream();
            Bitmap map = new Bitmap(returnImage);
            try
            {
                map.Save(ms, ImageFormat.Bmp);
                byte[] imgData = ms.ToArray();

                char[] chars = { ' ', '.', ',', '*', '#' };
                string imposert = "";
                int w = returnImage.Width; int h = returnImage.Height;
                int downscale = (int)Math.Ceiling(Math.Sqrt(((w + 1) * h) / 2000.0));
                for (int y = 0; y <= h - downscale; y += downscale)
                {
                    for (int x = 0; x <= w - downscale; x += downscale)
                    {
                        double brrr = 0;
                        int pxl = 0;
                        for (int xd = 0; xd < downscale; xd++)
                        {
                            for (int yd = 0; yd < downscale; yd++)
                            {
                                int off = ((yd + y) * w + x + xd) * 4;
                                byte r = imgData[off];
                                byte g = imgData[off + 1];
                                byte b = imgData[off + 2];
                                byte a = imgData[off + 3];
                                pxl++;
                                brrr += Math.Sqrt(0.299 * r * r + 0.587 * g * g + 0.114 * b * b) * a;
                            }
                        }

                        // White has a brightness of 65025 with the above formula.
                        double jerma = brrr / (65025 * pxl + 1);
                        imposert += chars[(int)(jerma * chars.Length)];
                    }

                    imposert += "\n";
                }

                // Create a temporary directory
                string directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                Directory.CreateDirectory(directory);
                // Save the string to a file there
                string filepath = Path.Combine(directory, "commits.md");
                await File.WriteAllTextAsync(filepath, imposert);
                // Send the file to Discord
                await context.RespondAsync(
                    new DiscordMessageBuilder().WithFile(new FileStream(filepath, FileMode.Open)));
                // Delete the temporary directory
                Directory.Delete(directory, true);

            } catch (Exception ex)
			{
                Console.WriteLine(ex.Message);
			}
		}

        [Command("cum")]
        [Description("Ban a member and send them an appeal message via DMs, including your username for contact.")]
        public async Task Ban(
            CommandContext context,
            string reason) =>
            await readFuckingImage(context, reason);
    }

}
