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
        double positionX = 1.5;
        double positionY = 0.5;
        double positionZ = 1.5;
        double roomHeight = 1;
        Color roomColor = Color.DarkGreen;
        Color roomColorPattern = Color.DarkGray;
        Color roofColor = Color.DarkBlue;
        double lineSize = 0.03;
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
                    pixels[i, j] = new Pixel(fovHorizontal * (i - (newWidth / 2)) / newWidth, fovVertical * (j -(newHeight / 2)) / newHeight);
                }
            }
            currentRoom = GenerateRoom(testRoomCode);

        }

        private List<Face> GenerateRoom(int[,] roomCode)
        {
            List<Face> room = new List<Face>();
            for(int i = 0; i < roomCode.GetLength(0);  i++)
            {
                if (roomCode[i, 0] == 0)
                {
                    room.Add(new Face(false, i, 0, i + 1, 0));
                }
                if (roomCode[i, roomCode.GetLength(1) - 1] == 0)
                {
                    room.Add(new Face(false, i, roomCode.GetLength(1), i + 1, roomCode.GetLength(1)));
                }
            }
            for (int i = 0; i < roomCode.GetLength(1); i++)
            {
                if (roomCode[0, i] == 0)
                {
                    room.Add(new Face(true, 0, i, 0, i + 1));
                }
                if (roomCode[roomCode.GetLength(0) - 1, i] == 0)
                {
                    room.Add(new Face(true, roomCode.GetLength(0), i, roomCode.GetLength(0), i + 1));
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
                    Color color = CalculatePixel(pixels[i, j]);
                    bmp.SetPixel(i, j, color);
                }
            }
            pictureBox1.Image = bmp;
        }

        public Color CalculatePixel(Pixel pixel)
        {
            CalculateRatio(pixel.angleHorizontal, pixel.angleVertical, out double x, out double y, out double z);
            Face currentClosest = null;
            double proximity = 10000;
            double relativeHitFromLower = 0;
            double relativeHitY = 0;
            foreach (Face face in currentRoom)
            {
                if (face.isDirectionX)
                {
                    double hitZ = (face.LowerBoundX - positionX) * z / x + positionZ;
                    if (face.LowerBoundZ <= hitZ && hitZ <= face.HigherBoundZ)
                    {
                        double hitY = (face.LowerBoundX - positionX) * y / x + positionY;
                        if (0 <= hitY && hitY <= roomHeight)
                        {
                            if (Math.Pow(face.midX - positionX, 2) + Math.Pow(face.midZ - positionZ, 2) < proximity)
                            {
                                currentClosest = face;
                                proximity = Math.Pow(face.midX - positionX, 2) + Math.Pow(face.midZ - positionZ, 2);
                                relativeHitFromLower = hitZ - face.LowerBoundZ;
                                relativeHitY = hitY;
                            }
                        }
                    }
                }
                else
                {
                    double hitX = (face.LowerBoundZ - positionZ) * x / z + positionX;
                    if (face.LowerBoundX <= hitX && hitX <= face.HigherBoundX)
                    {
                        double hitY = (face.LowerBoundZ - positionZ) * y / z + positionY;
                        if (0 <= hitY && hitY <= roomHeight)
                        {
                            if (Math.Pow(face.midX - positionX, 2) + Math.Pow(face.midZ - positionZ, 2) < proximity)
                            {
                                currentClosest = face;
                                proximity = Math.Pow(face.midX - positionX, 2) + Math.Pow(face.midZ - positionZ, 2);
                                relativeHitFromLower = hitX - face.LowerBoundX;
                                relativeHitY = hitY;
                            }
                        }
                    }
                }
            }
            if (currentClosest != null)
            {
                if (currentClosest.isDirectionX)
                {
                    if (Math.Abs(relativeHitFromLower - currentClosest.LowerBoundZ) < lineSize || Math.Abs(currentClosest.HigherBoundZ - relativeHitFromLower) < lineSize)
                    {
                        return roomColorPattern;
                    }
                }
                else
                {
                    if (Math.Abs(relativeHitFromLower - currentClosest.LowerBoundX) < lineSize || Math.Abs(currentClosest.HigherBoundX - relativeHitFromLower) < lineSize)
                    {
                        return roomColorPattern;
                    }
                }
                if (Math.Abs(relativeHitY) < lineSize || Math.Abs(roomHeight - relativeHitY) < lineSize)
                {
                    return roomColorPattern;
                }
                else if (Math.Abs(relativeHitY - relativeHitFromLower) < lineSize || Math.Abs(roomHeight - relativeHitY - relativeHitFromLower) < lineSize)
                {
                    return roomColorPattern;
                }
                else
                {
                    return roomColor;
                }
            }
            else
            {
                return roofColor;
            }
        }

        public void CalculateRatio(double horizontalAngle, double verticalAngle, out double x, out double y, out double z)
        {
            double a = Math.Tan(verticalAngle * Math.PI / 180);
            double c = Math.Tan(horizontalAngle * Math.PI / 180);
            double a2 = a * a;
            double c2 = c * c;
            double d = c2 + a2 * (c2 + 1) + 1;
            z = Math.Sqrt(1 / d);
            y = a * Math.Sqrt(z * z * (c2 + 1));
            x = c * z;
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
        public double LowerBoundX;
        public double LowerBoundZ;
        public double HigherBoundX;
        public double HigherBoundZ;
        public double midX;
        public double midZ;

        public Face(bool isDirectionX, double LowerBoundX, double LowerBoundZ , double HigherBoundX, double HigherBoundZ)
        {
            this.isDirectionX = isDirectionX;
            this.LowerBoundX = LowerBoundX;
            this.LowerBoundZ = LowerBoundZ;
            this.HigherBoundX = HigherBoundX;
            this.HigherBoundZ = HigherBoundZ;
            midX = (HigherBoundX + LowerBoundX) / 2;
            midZ = (HigherBoundZ + LowerBoundZ) / 2;
        }
    }
}
