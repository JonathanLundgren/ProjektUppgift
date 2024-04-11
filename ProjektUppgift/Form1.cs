using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.Remoting.Messaging;
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
        int[,] testRoomCode = new int[3,3] {{0, 0, 0}, {0, 0, 0}, {0, 0, 0}};
        double angle = 0;
        double positionX = 0;
        double positionY = 0;
        List<Face> currentRoom = new List<Face>();
        public Form1()
        {
            InitializeComponent();
            pictureBox1.ClientSize = new Size(width, height);
            this.Size = new Size(width, height);
            for (int i = 0; i < newWidth; i++) 
            {
                for (int j = 0; j < newHeight; j++) 
                {
                    pixels[i, j] = new Pixel( fovHorizontal * ((newWidth / 2) - i) / newWidth, fovVertical * ((newHeight / 2) - i) / newHeight);
                }
            }
            currentRoom = GenerateRoom(testRoomCode);

        }

        private List<Face> GenerateRoom(int[,] roomCode)
        {
            List<Face> room = new List<Face>();
            for(int i = 0; i < roomCode.GetLength(0);  i++)
            {
                if (roomCode[i, roomCode.GetLength(1) - 1] == 0)
                {
                    room.Add(new Face(false, i, 0, i + 1, 0));
                }
                if (roomCode[i, 0] == 0)
                {
                    room.Add(new Face(false, i, roomCode.GetLength(1) - 1, i + 1, roomCode.GetLength(1) - 1));
                }
            }
            for (int i = 0; i < roomCode.GetLength(1); i++)
            {
                if (roomCode[roomCode.GetLength(0) - 1, i] == 0)
                {
                    room.Add(new Face(true, 0, i, 0, i + 1));
                }
                if (roomCode[0, i] == 0)
                {
                    room.Add(new Face(true, roomCode.GetLength(0) - 1, i, roomCode.GetLength(0) - 1, i + 1));
                }
            }
            return room;
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
        public double angleHorizontal;
        public double angleVertical;
        Random rand = new Random();

        public Pixel(double angleHorizontal, double angleVertical) 
        {

            this.angleHorizontal = angleHorizontal;
            this.angleVertical = angleVertical;
        }

        public Color GetColor()
        {
             return Color.FromArgb(rand.Next(256), rand.Next(256), rand.Next(256));
        }
    }

    public class Face
    {
        public bool isDirectionX;
        public double LeftBoundX;
        public double LeftBoundZ;
        public double RightBoundX;
        public double RightBoundZ;

        public Face(bool isDirectionX, double LeftBoundX, double LeftBoundZ , double RightBoundX, double RightBoundZ)
        {
            this.isDirectionX = isDirectionX;
            this.LeftBoundX = LeftBoundX;
            this.LeftBoundZ = LeftBoundZ;
            this.RightBoundX = RightBoundX;
            this.RightBoundZ = RightBoundZ;
        }
    }
}
