using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Media.Imaging;
using Xabe.FFmpeg;

namespace Spine
{
    public class RaceInfo
    {
        public class Status
        {
            public int[] pos, skills;
            public string msg;
            public bool[] abnormal;
        }

        public int[] unit;
        public List<Status> status;
    }
    class RaceGame : Game
    {
        GraphicsDeviceManager graphics;
        private RaceInfo info;
        private SpineContainer[] units;
        private float progress;
        private Texture2D bg;
        private IAudioStream bgm;
        private GraphicsDevice device => GraphicsDevice;

        const int width = 640, height = 480;
        const float delta = .1f;

        private static float scale => height / 1400f;

        public RaceGame()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.IsFullScreen = false;
            graphics.PreferredBackBufferWidth = width;
            graphics.PreferredBackBufferHeight = height;
        }

        protected override void LoadContent()
        {
            using (var fs = File.OpenRead("tex/race_bg.png"))
                bg = Texture2D.FromStream(device, fs);
            //SetInfo(JsonConvert.DeserializeObject<RaceInfo>(File.ReadAllText("test.json")));
            new Thread(() => bgm = FFmpeg.GetMediaInfo("bgm/bgm.mp3").Result.AudioStreams.First()).Start();
        }

        public void SetInfo(RaceInfo info)
        {
            this.info = info;
            info.status.Add(new RaceInfo.Status
            {
                pos = new int[] { 1, 1, 1, 1, 1 },
                msg = "",
            });
            units = info.unit.Select(unit => new SpineContainer(device, $"spine/{unit + 30}", scale)).ToArray();
            progress = 0f;
            win = new bool[5];

            for (int i = 0; i < units.Length; ++i)
                units[i].Y = height * (0.5f + 0.12f * i);
        }

        private static float Progress(float a, float b, float p) => a * (1 - p) + b * p;

        private static float ToScreenPos(float pos)
        {
            return width * (1f - pos / 15f * 0.9f);
        }

        private static readonly Font font = new Font(FontFamily.GenericMonospace, 13, FontStyle.Bold);

        private static bool[] win;
        private RaceInfo.Status DrawFrame()
        {
            var sb = new SpriteBatch(device);

            sb.Begin();
            sb.Draw(bg, new Microsoft.Xna.Framework.Rectangle(0, 0, width, height), new Microsoft.Xna.Framework.Rectangle(128, 0, 1024, 768 + 128), Microsoft.Xna.Framework.Color.White);
            sb.End();
            progress += delta;
            var now = info.status[Math.Min((int)progress, info.status.Count - 1)];
            var next = info.status[Math.Min((int)progress + 1, info.status.Count - 1)];

            for (int i = 0; i < units.Length; ++i)
            {
                var skill = next.skills?[i] ?? 0;
                var ab = next.abnormal?[i] ?? false;
                var pos = Progress(now.pos[i], next.pos[i], progress - (int)progress);
                win[i] |= pos == 1;
                units[i].X = ToScreenPos(win[i] ? 1 : pos);
                var speed = next.pos[i] - now.pos[i];
                units[i].Animation = skill > 0 ? $"{info.unit[i]}_skill1" :
                    ab || speed > 0 ? $"{units[i].aniprefix}_damage" :
                    win[i] ? $"000000_smile" :
                    speed < -1 ? "000000_noWeapon_run_super" :
                    speed < 0 ? "000000_noWeapon_run" : "000000_noWeapon_idle";
                units[i].Update(delta);
                units[i].Draw();
            }

            return next;
        }

        private IEnumerator<object> Waiter()
        {
            HttpListener listener = new HttpListener();

            listener.Prefixes.Add("http://+:1/");
            listener.Start();

            while (true)
            {
                HttpListenerContext context = null;

                listener.BeginGetContext(state =>
                {
                    context = listener.EndGetContext(state);
                }, null);

                while (context == null) yield return null;

                var sr = new StreamReader(context.Request.InputStream);
                SetInfo(JsonConvert.DeserializeObject<RaceInfo>(sr.ReadToEnd()));

                //SetInfo(JsonConvert.DeserializeObject<RaceInfo>(File.ReadAllText("test.json")));

                foreach (var obj in 
                    //DrawToScreen()
                    SaveTo("pic")
                ) yield return obj;

                var output = $"output.mp4";

                if (File.Exists(output))
                    File.Delete(output);

                var task = FFmpeg.Conversions.New()
                    .AddStream(bgm)
                    .SetInputFrameRate(5)
                    .SetPixelFormat(Xabe.FFmpeg.PixelFormat.yuv420p)
                    .SetOutput(output)
                    .BuildVideoFromImages(Directory.EnumerateFiles("pic"))
                    .UseShortest(true)
                    .Start();

                while (!task.IsCompleted) yield return null;

                foreach (var file in Directory.EnumerateFiles("pic"))
                    File.Delete(file);

                var sw = new StreamWriter(context.Response.OutputStream);

                context.Response.Close(Encoding.UTF8.GetBytes(Path.GetFullPath(output)), false);
            }
        }

        private IEnumerator<object> waiter;

        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            if (waiter == null) waiter = Waiter();
            waiter.MoveNext();
        }

        public IEnumerable<object> DrawToScreen()
        {
            Console.WriteLine($"frame {progress}/{info.status.Count}");
            while (progress < info.status.Count)
            {
                DrawFrame();
                yield return null;
            }
        }

        public IEnumerable<object> SaveTo(string root)
        {
            var target = new RenderTarget2D(device, width, height);
            var j = 0;

            while (progress < info.status.Count + 1)
            {
                Console.WriteLine($"frame {progress}/{info.status.Count}");
                device.SetRenderTarget(target);

                var next = DrawFrame();

                device.SetRenderTarget(null);
                Image img;

                using (var ms = new MemoryStream())
                {
                    target.SaveAsPng(ms, target.Width, target.Height);
                    using (var ms2 = new MemoryStream(ms.ToArray()))
                        img = Image.FromStream(ms2);
                }
                using (var g = Graphics.FromImage(img))
                {
                    var l = 0;
                    foreach (var line in next.msg.Split('\n')
                        .SelectMany(line => line.Select((c, i)=> (c, i)).GroupBy(p => p.i / 30)
                        .Select(gr => new string(gr.Select(p => p.c).ToArray()))))
                        g.DrawString(line, font, Brushes.Black, new PointF(5, 20 * (l++) + 5));
                }
                img.Save($"{root}/{++j:D4}.png");

                yield return null;
            }
        }
    }
}
