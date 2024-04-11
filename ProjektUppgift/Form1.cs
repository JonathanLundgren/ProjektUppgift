using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Rebar;

namespace ProjektUppgift
{
    public partial class Form1 : Form
    {
        const int width = 1200;
        const int height = 800;
        const int resolution = 4;
        const int fovHorizontal = 90;
        const int fovVertical = 90;
        readonly int newWidth = width / resolution;
        readonly int newHeight = height / resolution;
        Pixel[,] pixels = new Pixel[width / resolution, height / resolution];
        int[] testRoom = new int[25] { 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 1, 0, 0, 0, 1, 1, 0, 0, 0, 1, 1, 1, 1, 1, 1 };
        double angle = 0;
        double positionX = 0;
        double positionY = 0;
        public Form1()
        {
            InitializeComponent();
            pictureBox1.ClientSize = new Size(width, height);
            this.Size = new Size(width, height);
            for (int i = 0; i < newWidth; i++) 
            {
                for (int j = 0; j < newHeight; j++) 
                {
                    pixels[i, j] = new Pixel(i, j, fovHorizontal * ((newWidth / 2) - i) / newWidth, fovVertical * ((newHeight / 2) - i) / newHeight);
                }
            }
        }

        private List<Face> GenerateRoom()
        {
            List<Face> room = new List<Face>();

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            UpdateImage();
        }

        public void UpdateImage()
        {
            Bitmap bmp = new Bitmap(newWidth, newHeight);
            for (int i = 0; i < newWidth; i++)
            {
                for (int j = 0; j < newHeight; j++)
                {
                    Color color = pixels[i, j].GetColor();
                    bmp.SetPixel(i, j, color);
                }
            }
            pictureBox1.Image = bmp;
        }
    }

    public class Pixel
    {
        int posX;
        int posY;
        double angleHorizontal;
        double angleVertical;
        public Label pictureBox;
        Random rand;

        public Pixel(int x, int y, double angleHorizontal, double angleVertical) 
        {
            posX = x;
            posY = y;
            this.angleHorizontal = angleHorizontal;
            this.angleVertical = angleVertical;
            rand = new Random(posX + posY * 1000);
        }

        public Color GetColor()
        {
             return Color.FromArgb(rand.Next(256), rand.Next(256), rand.Next(256));
        }
    }

    public class Face
    {
        public bool isDirectionX;
        public double leftBound;
        public double rightBound;

        Face(bool isDirectionX, double leftBound, double rightBound)
        {
            this.isDirectionX = isDirectionX;
            this.leftBound = leftBound;
            this.rightBound = rightBound;
        }
    }
}
