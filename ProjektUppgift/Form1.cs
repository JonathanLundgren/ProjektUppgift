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



/*

Styrspel i 3D

av Jonathan Lundgren vårterminen 2024

Kravspec: 

Spelaren ska kunna röra sig fram- och baklänges eller i sidled, och snurra runt.
Mönster ska finnas på väggar, golv och tak.
Fiender som anfaller spelaren ska finnas i banan.
spelaren ska kunna anfalla fiender.
Det ska finnas power-ups utspridda i banan.
spelaren ska ha ett HUD som visar liv och power-ups.
Spelaren ska inte kunna gå genom väggar.

Om jag hinner:
Fler banor, och ett läge där man kan designa egna banor.
Oändligt läge?

Log :
04/10 Kollade hur man kan sätta varje pixel på skärmen. Kollade på matematik som behövs.
04/11 Gjorde så att spelaren befinner sig i ett litet rum. Det går nu att gå och titta runt, och väggarna har mönster. Det finns en viss "Fisheye" effekt, som gör att väggarna ser runda ut och större mot mitten av skärmen.
04/12 Golv och tak har nu mönster.

*/
namespace ProjektUppgift
{
    public partial class Form1 : Form
    {
        //Storleken på formsen
        const int width = 1200;
        const int height = 800;
        //Hur hög upplösning det är. Mindre värde ger högre upplösning.
        const int resolution = 4;
        double imageSize = 1;
        double imageScale = 2;
        //I hur stor vinkel spelaren kan se.
        const int fovHorizontal = 120;
        const int fovVertical = 75;
        //Höjd och bredd som används för beräkningar.
        readonly int newWidth = width / resolution;
        readonly int newHeight = height / resolution;
        //En två-dimensionell array med alla pixlar som kan ändras på.
        Pixel[,] pixels = new Pixel[width / resolution, height / resolution];
        //"RoomCodes" är arrayer som beskriver hur rummen ska se ut. Beräkningar utförs senare för att generera rummen på ett sätt som fungerar med resten av koden.
        int[,] testRoomCode = new int[,] { { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 } };
        //Vilken riktning spelaren tittar mot.
        double angle = 0;
        //Spelarens position.
        double playerPositionX = 1.5;
        double playerPositionY = 0.5;
        double playerPositionZ = 1.5;
        //Hur högt upp taket är.
        const double roomHeight = 1;
        Color roomColor = Color.DarkGreen;
        Color roomColorPattern = Color.DarkGray;
        Color roofColor = Color.DarkBlue;
        double lineSize = 0.1;
        //Alla ytor som finns i det genererade rummet.
        List<Face> currentRoom = new List<Face>();
        int isWDown = 0;
        int isSDown = 0;
        int isADown = 0;
        int isDDown = 0;
        double playerSpeed = 0.1;
        public Form1()
        {
            InitializeComponent();
            pictureBox1.ClientSize = new Size(width, height);
            this.Size = new Size(width, height);
            //Genererar alla pixlar som behövs.
            for (int i = 0; i < newWidth; i++) 
            {
                for (int j = 0; j < newHeight; j++) 
                {
                    pixels[i, j] = new Pixel(imageSize * (i - (newWidth / 2)) / newWidth, imageSize * (j -(newHeight / 2)) / newWidth);
                }
            }
            currentRoom = GenerateRoom(testRoomCode);

        }

        //Metod som genererar rum utifrån en "RoomCode".
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

        //En timer används för att generera nästa frame.
        private void timer1_Tick(object sender, EventArgs e)
        {
            UpdateImage();
            MovePlayer();
        }

        public void MovePlayer()
        {
            double horizontal = isDDown - isADown;
            double vertical = isWDown - isSDown;
            if (!(horizontal == 0 && vertical == 0))
            {
                double movementDirection = Math.Atan2(vertical, horizontal) * 180 / Math.PI - angle;

                double movementX = Math.Cos(movementDirection * Math.PI / 180);
                double movementZ = Math.Sin(movementDirection * Math.PI / 180);

                playerPositionX += movementX * playerSpeed;
                playerPositionZ += movementZ * playerSpeed;
            }
        }

        //Genererar en bild utifrån alla pixlar som används, och sätter den sedan som den bild som syns.
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

        //Räknar ut vilken punkt som träffas om man drar en linje från spelarens position med vinklar beroende på vilken pixel som kollas.
        public Color CalculatePixel(Pixel pixel)
        {
            CalculateRatio(pixel.xPos, pixel.yPos, angle, out double xDirection, out double yDirection, out double zDirection, out double xPosition, out double yPosition, out double zPosition);
            Face currentClosest = null;
            double proximity = 10000;
            double relativeHitFromLower = 0;
            double relativeHitY = 0;
            Color color = Color.Black;
            foreach (Face face in currentRoom)
            {
                if (face.isDirectionX)
                {
                    double hitZ = (face.LowerBoundX - xPosition) * zDirection / xDirection + zPosition;
                    double direction = (180 / Math.PI) * Math.Atan2(hitZ - zPosition, face.LowerBoundX - xPosition);
                    if (Math.Abs(direction + angle - 90) < fovHorizontal / 2 || Math.Abs(direction + 360 + angle - 90) < fovHorizontal / 2)
                    {
                        if (face.LowerBoundZ <= hitZ && hitZ <= face.HigherBoundZ)
                        {
                            double hitY = (face.LowerBoundX - xPosition) * yDirection / xDirection + yPosition;
                            if (0 <= hitY && hitY <= roomHeight)
                            {
                                if (Math.Pow(face.midX - xPosition, 2) + Math.Pow(face.midZ - zPosition, 2) < proximity)
                                {
                                    currentClosest = face;
                                    color = face.color;
                                    proximity = Math.Pow(face.midX - xPosition, 2) + Math.Pow(face.midZ - zPosition, 2);
                                    relativeHitFromLower = hitZ - face.LowerBoundZ;
                                    relativeHitY = hitY;
                                }
                            }
                        }
                    }
                }
                else
                {
                    double hitX = (face.LowerBoundZ - zPosition) * xDirection / zDirection + xPosition;
                    double direction = (180 / Math.PI) * Math.Atan2(face.LowerBoundZ - zPosition, hitX - xPosition);
                    if (Math.Abs(direction + angle - 90) < fovHorizontal / 2 || Math.Abs(direction + 360 + angle - 90) < fovHorizontal / 2)
                    {
                        if (face.LowerBoundX <= hitX && hitX <= face.HigherBoundX)
                        {
                            double hitY = (face.LowerBoundZ - zPosition) * yDirection / zDirection + yPosition;
                            if (0 <= hitY && hitY <= roomHeight)
                            {
                                if (Math.Pow(face.midX - xPosition, 2) + Math.Pow(face.midZ - zPosition, 2) < proximity)
                                {
                                    currentClosest = face;
                                    color = face.color;
                                    proximity = Math.Pow(face.midX - xPosition, 2) + Math.Pow(face.midZ - zPosition, 2);
                                    relativeHitFromLower = hitX - face.LowerBoundX;
                                    relativeHitY = hitY;
                                }
                            }
                        }
                    }
                }
            }
            //Kod som gör olika mönster.
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
                double hitX = (roomHeight - yPosition) * xDirection / yDirection + xPosition;
                double hitZ = (roomHeight - yPosition) * zDirection / yDirection + zPosition;
                double direction = (180 / Math.PI) * Math.Atan2(hitZ - zPosition, hitX - xPosition);
                if (!(Math.Abs(direction + angle - 90) < fovHorizontal / 2 || Math.Abs(direction + 360 + angle - 90) < fovHorizontal / 2))
                {
                    hitX = -yPosition * xDirection / yDirection + xPosition;
                    hitZ = -yPosition * zDirection / yDirection + zPosition;
                }
                if (Math.Abs(Math.Round(hitX) - hitX) <= lineSize || Math.Abs(Math.Round(hitZ) - hitZ) <= lineSize)
                {
                    return roomColorPattern;
                }
                else
                {
                    return roofColor;
                }
            }
        }

        //Metod för att räkna ut i vilken riktning linjen ska dras utifrån givna vinklar.
        public void CalculateRatio(double localXPos, double localYPos, double angle, out double xDirection, out double yDirection, out double zDirection, out double xPosition, out double yPosition, out double zPosition)
        {
            double verticalAngle = Math.Atan(localYPos * (imageScale - 1)) * 180 / Math.PI;
            double horizontalAngle = Math.Atan(localXPos * (imageScale - 1)) * 180 / Math.PI + angle;
            double a = Math.Tan(verticalAngle * Math.PI / 180);
            double c = Math.Tan(horizontalAngle * Math.PI / 180);
            double a2 = a * a;
            double c2 = c * c;
            double d = c2 + a2 * (c2 + 1) + 1;
            zDirection = Math.Sqrt(1 / d);
            yDirection = a * Math.Sqrt(zDirection * zDirection * (c2 + 1));
            xDirection = c * zDirection;
            xPosition = localXPos * Math.Sin(angle) + playerPositionX;
            zPosition = localXPos * Math.Cos(angle) + playerPositionZ;
            yPosition = localYPos + playerPositionY;
        }

        //Kollar vilka knappar som trycks ned och flyttar eller roterar spelaren.
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.W) 
            {
                isWDown = 1;
            }
            if (e.KeyCode == Keys.S)
            {
                isSDown = 1;
            }
            if (e.KeyCode == Keys.D)
            {
                isDDown = 1;
            }
            if (e.KeyCode == Keys.A)
            {
                isADown = 1;
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

        //Metod som ser till att vinkeln håller sig mellan -180 och 180.
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

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.W)
            {
                isWDown = 0;
            }
            if (e.KeyCode == Keys.S)
            {
                isSDown = 0;
            }
            if (e.KeyCode == Keys.D)
            {
                isDDown = 0;
            }
            if (e.KeyCode == Keys.A)
            {
                isADown = 0;
            }
        }
    }

    //Varje pixel förvarar data om åt vilket håll de ska "titta" mot.
    public class Pixel
    {
        public double angleHorizontal;
        public double angleVertical;
        public double xPos;
        public double yPos;

        public Pixel(double xPos, double yPos) 
        {

            this.xPos = xPos;
            this.yPos = yPos;
        }
    }

    //Data om varje vägg.
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
