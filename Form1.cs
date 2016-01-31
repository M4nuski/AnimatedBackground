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
        #region BG Fields
        private Bitmap backgroundBitmap = new Bitmap("reddish bubble background.jpg");
        private Bitmap resizedBackgroundBitmap;

        private bubbleInfoStruct[] bubblesInfo;
        private ImageAttributes[] bubblesAttributes;

        private Bitmap bubbleBitmap = new Bitmap("1024RedCircle.png");
        private Bitmap[] bubblesBitmaps;

        private const int NumDepths = 10;
        private const int NumBubbles = 256;
        private const float MaxBubbleSize = 0.1f;
        private const float BubbleWiggle = 0.01f;
        private const float BubbleLimit = (MaxBubbleSize+BubbleWiggle)*NumDepths;
        private const float BubbleAdvanceRate = 0.0015f;
        private const float BubbleAngleRate = 0.01f;

        private Random rand = new Random();
        #endregion

        private bool fullScreen;

        #region Main Form Methods
        public Form1()
        {   
            InitializeComponent();

            #region BG Setup
            //Resize Bitmaps
            backgroundBitmap = new Bitmap(backgroundBitmap, Width, Height);
            bubbleBitmap = new Bitmap(bubbleBitmap, 256, 256);

            //Create ColorMatrices relating depth
            bubblesAttributes = new ImageAttributes[NumDepths];
            for (var i = 0; i < NumDepths; i++)
            {
                bubblesAttributes[i] = new ImageAttributes();
                bubblesAttributes[i].SetColorMatrix(new ColorMatrix(CreateBubbleMatrix(i)),
               ColorMatrixFlag.Default,
               ColorAdjustType.Bitmap);
            }

            //Create bubbles and resize all bitmaps relative to form width/height
            bubblesInfo = new bubbleInfoStruct[NumBubbles];
            for (var i = 0; i < NumBubbles; i++)
            {
                bubblesInfo[i] = newBubble(i);
            }
            
            bubblesBitmaps = new Bitmap[NumDepths];

            Resize += Form1_Resize;
            Form1_Resize(this, new EventArgs());
            #endregion 
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape){ Close(); }
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            #region BG Resize
            for (var i = 0; i < NumDepths; i++)
            {
                var BubSize = getBubbleSize(i);
                bubblesBitmaps[i] = new Bitmap(BubSize.Width, BubSize.Height);
                using (var graph = Graphics.FromImage(bubblesBitmaps[i]))
                {
                    //Resize bitmaps and apply colormatrices
                    graph.DrawImage(bubbleBitmap, new Rectangle(Point.Empty, BubSize), 0, 0, 256, 256, GraphicsUnit.Pixel, bubblesAttributes[i]);
                }
            }
            resizedBackgroundBitmap = new Bitmap(backgroundBitmap, Width, Height);
            #endregion
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            Refresh();
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            #region BG Paint
            e.Graphics.DrawImageUnscaled(resizedBackgroundBitmap, 0, 0);
            for (var i = 0; i < NumBubbles; i++)
            {
                e.Graphics.DrawImageUnscaled(bubblesBitmaps[bubblesInfo[i].Depth], getBubblePoint(i));
            }            
            #endregion
        }

        private void Form1_DoubleClick(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Maximized)
            {
                //reset state to normal otherwise going borderless/fullscreen dosent cover up taskbar
                WindowState = FormWindowState.Normal;
            }
            if (fullScreen)
            {
                FormBorderStyle = FormBorderStyle.Sizable;
                TopMost = false;
                fullScreen = false;
            }
            else
            {
                FormBorderStyle = FormBorderStyle.None;
                WindowState = FormWindowState.Maximized;
                TopMost = true;
                fullScreen = true;
            }
        }
        #endregion

        #region BG Methods
        private static float[][] CreateBubbleMatrix(int depth)
        {
            //Color modifiers relative to depth
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
            bubblesInfo[index].PosY = (float)rand.NextDouble()*1.25f;
        }

        private Point getBubblePoint(int index)
        {
            //Move and wiggle bubbles
            bubblesInfo[index].Ang += BubbleAngleRate * (NumDepths - bubblesInfo[index].Depth);
            var offset = (BubbleAdvanceRate * (bubblesInfo[index].Depth + 1));
            bubblesInfo[index].PosX += offset;
            bubblesInfo[index].PosY -= 0.5f * offset;

            if (bubblesInfo[index].Ang > Math.PI * 2) { bubblesInfo[index].Ang -= (float)(Math.PI * 2); }

            if ((bubblesInfo[index].PosX > (1.0f + MaxBubbleSize)) | (bubblesInfo[index].PosY < -BubbleLimit))
            {
                //out of frame bubble
                resetBubble(index);
            }

            var sinOffset = getBubbleSineOffset(bubblesInfo[index].Ang, BubbleWiggle * (1 + bubblesInfo[index].Depth));

            return new Point((int)((sinOffset.X + bubblesInfo[index].PosX) * Width), (int)((sinOffset.Y + bubblesInfo[index].PosY) * Height));
        }

        private static PointF getBubbleSineOffset(float Angle, float Radius)
        {
            return new PointF((float)Math.Sin(Angle) * Radius * 0.5f, (float)(Math.Sin(Angle) * Radius));
        }
        #endregion

    }
}
