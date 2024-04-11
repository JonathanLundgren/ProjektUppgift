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
        const int fovHorizontal = 120;
        const int fovVertical = 75;
        readonly int newWidth = width / resolution;
        readonly int newHeight = height / resolution;
        Pixel[,] pixels = new Pixel[width / resolution, height / resolution];
        int[,] testRoomCode = new int[,] { { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 } };
        double angle = 0;
        double positionX = 1.5;
        double positionY = 0.5;
        double positionZ = 1.5;
        double roomHeight = 1;
        Color roomColor = Color.DarkGreen;
        Color roomColorPattern = Color.DarkGray;
        Color roofColor = Color.DarkBlue;
        double lineSize = 0.1;
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
                    room.Add(new Face(false, i, 0, i + 1, 0, Color.Yellow));
                }
                if (roomCode[i, roomCode.GetLength(1) - 1] == 0)
                {
                    room.Add(new Face(false, i, roomCode.GetLength(1), i + 1, roomCode.GetLength(1), Color.Red));
                }
            }
            for (int i = 0; i < roomCode.GetLength(1); i++)
            {
                if (roomCode[0, i] == 0)
                {
                    room.Add(new Face(true, 0, i, 0, i + 1, Color.Green));
                }
                if (roomCode[roomCode.GetLength(0) - 1, i] == 0)
                {
                    room.Add(new Face(true, roomCode.GetLength(0), i, roomCode.GetLength(0), i + 1, Color.Purple));
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
            CalculateRatio(pixel.angleHorizontal + angle, pixel.angleVertical, out double x, out double y, out double z);
            Face currentClosest = null;
            double proximity = 10000;
            double relativeHitFromLower = 0;
            double relativeHitY = 0;
            Color color = Color.Black;
            foreach (Face face in currentRoom)
            {
                if (face.isDirectionX)
                {
                    double hitZ = (face.LowerBoundX - positionX) * z / x + positionZ;
                    double direction = (180 / Math.PI) * Math.Atan2((hitZ - positionZ), (face.LowerBoundX - positionX));
                    if ((face.LowerBoundX - positionX < 0 && hitZ - positionZ < 0) || (face.LowerBoundX - positionX < 0 && hitZ - positionZ > 0))
                    {
                        direction += 180;
                    }
                    if (Math.Abs(direction + angle - 90) < fovHorizontal / 2 || Math.Abs(direction + 360 + angle - 90) < fovHorizontal / 2)
                    {
                        if (face.LowerBoundZ <= hitZ && hitZ <= face.HigherBoundZ)
                        {
                            double hitY = (face.LowerBoundX - positionX) * y / x + positionY;
                            if (0 <= hitY && hitY <= roomHeight)
                            {
                                if (Math.Pow(face.midX - positionX, 2) + Math.Pow(face.midZ - positionZ, 2) < proximity)
                                {
                                    currentClosest = face;
                                    color = face.color;
                                    proximity = Math.Pow(face.midX - positionX, 2) + Math.Pow(face.midZ - positionZ, 2);
                                    relativeHitFromLower = hitZ - face.LowerBoundZ;
                                    relativeHitY = hitY;
                                }
                            }
                        }
                    }
                }
                else
                {
                    double hitX = (face.LowerBoundZ - positionZ) * x / z + positionX;
                    double direction = (180 / Math.PI) * Math.Atan((face.LowerBoundZ - positionZ) / (hitX - positionX));
                    if ((face.LowerBoundZ - positionZ < 0 && hitX - positionX < 0) || (hitX - positionX < 0 && face.LowerBoundZ - positionZ > 0))
                    {
                        direction += 180;
                    }
                    if (Math.Abs(direction + angle - 90) < fovHorizontal / 2 || Math.Abs(direction + 360 + angle - 90) < fovHorizontal / 2)
                    {
                        if (face.LowerBoundX <= hitX && hitX <= face.HigherBoundX)
                        {
                            double hitY = (face.LowerBoundZ - positionZ) * y / z + positionY;
                            if (0 <= hitY && hitY <= roomHeight)
                            {
                                if (Math.Pow(face.midX - positionX, 2) + Math.Pow(face.midZ - positionZ, 2) < proximity)
                                {
                                    currentClosest = face;
                                    color = face.color;
                                    proximity = Math.Pow(face.midX - positionX, 2) + Math.Pow(face.midZ - positionZ, 2);
                                    relativeHitFromLower = hitX - face.LowerBoundX;
                                    relativeHitY = hitY;
                                }
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
                    return color;
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

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.W) 
            {
                positionZ += 0.1;
            }
            if (e.KeyCode == Keys.S)
            {
                positionZ -= 0.1;
            }
            if (e.KeyCode == Keys.D)
            {
                positionX += 0.1;
            }
            if (e.KeyCode == Keys.A)
            {
                positionX -= 0.1;
            }
            if (e.KeyCode == Keys.Left)
            {
                angle -= 10;
                fixAngle();
            }
            if (e.KeyCode == Keys.Right)
            {
                angle += 10;
                fixAngle();
            }
        }

        private void fixAngle()
        {
            if (angle < -180) 
            {
                angle += 360;
            }
            else if (angle > 180)
            {
                angle -= 360;
            }
        }
    }

    public class Pixel
    {
        public double angleHorizontal;
        public double angleVertical;

        public Pixel(double angleHorizontal, double angleVertical) 
        {

            this.angleHorizontal = angleHorizontal;
            this.angleVertical = angleVertical;
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
        public Color color;

        public Face(bool isDirectionX, double LowerBoundX, double LowerBoundZ , double HigherBoundX, double HigherBoundZ, Color color)
        {
            this.isDirectionX = isDirectionX;
            this.LowerBoundX = LowerBoundX;
            this.LowerBoundZ = LowerBoundZ;
            this.HigherBoundX = HigherBoundX;
            this.HigherBoundZ = HigherBoundZ;
            midX = (HigherBoundX + LowerBoundX) / 2;
            midZ = (HigherBoundZ + LowerBoundZ) / 2;
            this.color = color;
        }
    }
}
