using Microsoft.Xna.Framework.Graphics;
using Spine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spine
{
    public class SpineContainer
    {

        private readonly Atlas atlas;
        private readonly Skeleton skeleton;
        private readonly AnimationState state;
        private readonly SkeletonRenderer renderer;
        private readonly GraphicsDevice device;

        public string aniprefix { get; private set; }

        public string Animation
        {
            get => state.GetCurrent(0)?.Animation?.Name;
            set
            {
                if (value != Animation)
                    state.SetAnimation(0, value, true);
            }
        }

        public float X
        {
            get => skeleton.X;
            set => skeleton.X = value;
        }
        public float Y
        {
            get => skeleton.Y;
            set => skeleton.Y = value;
        }

        public string Skin
        {
            get => skeleton.Skin.Name;
            set => skeleton.SetSkin(value);
        }

        public SpineContainer(GraphicsDevice device, string filename, float scale)
        {
            atlas = new Atlas(filename + ".atlas.asset", new XnaTextureLoader(device));
            skeleton = new Skeleton(new SkeletonBinary(atlas)
            {
                Scale = scale
            }.ReadSkeletonData(filename + ".skel"));

            state = new AnimationState(new AnimationStateData(skeleton.Data));

            foreach (var ani in skeleton.Data.Animations) Console.WriteLine(ani.Name);

            aniprefix = skeleton.Data.Animations.First().Name.Split('_').First();


            renderer = new SkeletonRenderer(device);
            renderer.PremultipliedAlpha = false;

            this.device = device;

            Skin = "default";
        }

        public void Draw(float x, float y)
        {
            X = x;
            Y = y;
            Draw();
        }
        
        public void Draw()
        {
            state.Apply(skeleton);
            skeleton.UpdateWorldTransform();

            renderer.Begin();
            renderer.Draw(skeleton);
            renderer.End();
        }

        public void Update(float time) => state.Update(time);
    }
}
