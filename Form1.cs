using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace AnimatedBackgroundTest1
{
    public struct bubbleInfoStruct
    {
        public int Depth;
        public float Ang;
        public float PosX, PosY;
    }

    public partial class Form1 : Form
    {
        private Bitmap backgroundBitmap = new Bitmap("reddish bubble background.jpg");
        private Bitmap resizedBackgroundBitmap;

        private bubbleInfoStruct[] bubblesInfo;
        private ImageAttributes[] bubblesAttributes;

        private Bitmap bubbleBitmap = new Bitmap("1024RedCircle.png");
        private Bitmap[] bubblesBitmaps;

        private const int NumDepths = 10;
        private const int NumBubbles = 100;
        private const float MaxBubbleSize = 0.1f;

        private Random rand = new Random();

        public Form1()
        {
            InitializeComponent();

            backgroundBitmap = new Bitmap(backgroundBitmap, Width, Height);
            bubbleBitmap = new Bitmap(bubbleBitmap, 256, 256);

            bubblesAttributes = new ImageAttributes[NumDepths];
            for (var i = 0; i < NumDepths; i++)
            {
                bubblesAttributes[i] = new ImageAttributes();
                bubblesAttributes[i].SetColorMatrix(new ColorMatrix(CreateBubbleMatrix(i)),
               ColorMatrixFlag.Default,
               ColorAdjustType.Bitmap);
            }

            bubblesInfo = new bubbleInfoStruct[NumBubbles];
            for (var i = 0; i < NumBubbles; i++)
            {
                bubblesInfo[i] = newBubble(i);
            }

            bubblesBitmaps = new Bitmap[NumDepths];
            Form1_Resize(this, new EventArgs());
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                Close();
            }
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            for (var i = 0; i < NumDepths; i++)
            {
                var BubSize = getBubbleSize(i);
                bubblesBitmaps[i] = new Bitmap(BubSize.Width, BubSize.Height);
                using (var graph = Graphics.FromImage(bubblesBitmaps[i]))
                {
                    graph.DrawImage(bubbleBitmap, new Rectangle(Point.Empty, BubSize), 0, 0, 256, 256, GraphicsUnit.Pixel, bubblesAttributes[i]);
                }
            }

            resizedBackgroundBitmap = new Bitmap(backgroundBitmap, Width, Height);
        }


        private void timer1_Tick(object sender, EventArgs e)
        {
            Refresh();
        }

        private static float[][] CreateBubbleMatrix(int depth)
        {
            var ratio = (float)depth / NumDepths;
            var alpha = 0.6f * ratio + 0.2f;
            var red = ratio - 1.0f;
            var blue = 1.0f - ratio;

            return new[]  {
                new [] {0.8f, 0f, 0f, 0f, 0f},
                new [] {0f, 1f, 0f, 0f, 0f},
                new [] {0f, 0f, 1f, 0f, 0f},
                new [] {0f, 0f, 0f, alpha, 0f}, 
                new [] {red, 0f, blue, 0f, 1.0f}
            };
        }


        private Size getBubbleSize(int index)
        {
            var ratio = ((float)index + 1) / NumDepths;
            var w = Math.Max(ratio * Width * MaxBubbleSize * 0.5625f, 5);
            var h = Math.Max(ratio * Height * MaxBubbleSize, 5);
            return new Size((int)w, (int)h);
        }

        private bubbleInfoStruct newBubble(int index)
        {
            return new bubbleInfoStruct
            {
                Depth = NumDepths * index / NumBubbles,
                Ang = (float)(rand.NextDouble() * 2 * Math.PI),
                PosX = (float)rand.NextDouble(),
                PosY = (float)rand.NextDouble()
            };
        }

        private void resetBubble(int index)
        {
            bubblesInfo[index].PosX = -MaxBubbleSize;
            bubblesInfo[index].PosY = (float)rand.NextDouble();
        }

        private Point getBubblePoint(int index)
        {
            bubblesInfo[index].Ang = bubblesInfo[index].Ang + (0.01f * (NumDepths - bubblesInfo[index].Depth));
            var offset = (0.0015f * (bubblesInfo[index].Depth + 1));
            bubblesInfo[index].PosX = bubblesInfo[index].PosX + offset;
            bubblesInfo[index].PosY = bubblesInfo[index].PosY - (0.5f * offset);

            if (bubblesInfo[index].Ang > Math.PI * 2) { bubblesInfo[index].Ang -= (float)(Math.PI * 2); }


            if ((bubblesInfo[index].PosX > (1.2f + MaxBubbleSize)) | (bubblesInfo[index].PosY < -MaxBubbleSize - 0.2f))
            {
                resetBubble(index);
            }

            var sinOffset = getBubbleSineOffset(bubblesInfo[index].Ang, 0.01f * (1 + bubblesInfo[index].Depth));

            return new Point((int)((sinOffset.X + bubblesInfo[index].PosX) * Width), (int)((sinOffset.Y + bubblesInfo[index].PosY) * Height));
        }

        private static PointF getBubbleSineOffset(float Angle, float Radius)
        {
            return new PointF((float)Math.Sin(Angle) * Radius * 0.5f, (float)(Math.Sin(Angle) * Radius));
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.DrawImageUnscaled(resizedBackgroundBitmap, 0, 0);
            for (var i = 0; i < NumBubbles; i++)
            {
                e.Graphics.DrawImageUnscaled(bubblesBitmaps[bubblesInfo[i].Depth], getBubblePoint(i));
            }
        }



    }
}
