
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Xabe.FFmpeg;

namespace Spine {
    static class Program {

        static void Main (string[] args)
        {
            var race = new RaceGame();

            race.Run();

            /*
            var device = CreateDevice();

            RenderTarget2D target = new RenderTarget2D(device, 800, 600);
            var skel = new SpineContainer(device, "spine/100131");

            for (int i = 0; i < 100; ++i)
            {
                device.SetRenderTarget(target);

                skel.Update(10);
                skel.Draw(400, 500);

                device.SetRenderTarget(null);
                using (var fs = File.OpenWrite($"{i}.jpg"))
                    target.SaveAsJpeg(fs, target.Width, target.Height);
            }*/

        }
    }
}