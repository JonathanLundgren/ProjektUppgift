using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
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

När programmet startas ska en meny där man kan välja bana dyka upp.
Varje bana ska bestå av flera rum, med ett sätt att gå mellan rummen.
Spelaren ska kunna röra sig fram- och baklänges eller i sidled, och snurra runt.
Mönster ska finnas på väggar, golv och tak.
Fiender som anfaller spelaren ska finnas i banan.
Fienderna ska finnas i färgerna grön, gul och röd beroende på svårighetsgrad.
Vissa fiender ska sitta fast i golvet och andra ska kunna röra på sig.
Spelaren ska kunna anfalla fiender.
När spelaren träffar fienderna ska en effekt spelas.
Det ska finnas power-ups utspridda i banan.
Spelaren ska ha ett HUD som visar liv och power-ups.
Spelaren ska inte kunna gå genom väggar.

Om jag hinner:
Fler banor, och ett läge där man kan designa egna banor.
Oändligt läge?

Log :
04/10 Kollade hur man kan sätta varje pixel på skärmen. Kollade på matematik som behövs.
04/11 Gjorde så att spelaren befinner sig i ett litet rum. Det går nu att gå och titta runt, och väggarna har mönster. Det finns en viss "Fisheye" effekt, som gör att väggarna ser runda ut och större mot mitten av skärmen.
04/12 Golv och tak har nu mönster.
04/13 Fixade "Fisheye"-effekten. Det kan nu finnas väggar inuti rummen och sänkte mängden lagg. Förbättrade koden. Spelaren kan inte längre gå genom väggar.
04/17 Gjorde början till en meny.

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
        double imageSize = 0.25;
        double imageScale = 8;
        double imageScaleY = 1.5;
        //Höjd och bredd som används för beräkningar.
        readonly int newWidth = width / resolution;
        readonly int newHeight = height / resolution;
        //En två-dimensionell array med alla pixlar som kan ändras på.
        Pixel[,] pixels = new Pixel[width / resolution, height / resolution];
        //"RoomCodes" är arrayer som beskriver hur rummen ska se ut. Beräkningar utförs senare för att generera rummen på ett sätt som fungerar med resten av koden.
        public Level[] levels = new Level[]
        {
            new Level
            (
                "Level 1",
                new int[,] { { 0, 1, 0, 1, 0 }, { 0, 0, 0, 0, 0 }, { 0, 1, 0, 0, 1 }, { 0, 0, 0, 0, 0 }, { 0, 1, 0, 1, 0 }, { 0, 0, 0, 0, 0 }, { 0, 1, 0, 1, 0 }, { 0, 0, 10, 0, 0 } },
                new int[,] { { 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0 } },
                null,
                null,
                null
            ),
            new Level
            (
                "Level 2",
                new int[,] { { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 } },
                new int[,] { { 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0 } },
                null,
                null,
                null
            )
        };
        int[,] testRoomCode = new int[,] { { 0, 1, 0, 1, 0 }, { 0, 0, 0, 0, 0 }, { 0, 1, 0, 0, 1 }, { 0, 0, 0, 0, 0 }, { 0, 1, 0, 1, 0 }, { 0, 0, 0, 0, 0 }, { 0, 1, 0, 1, 0 }, { 0, 0, 2, 0, 0 } };
        int[,] currentRoomCode;
        //Vilken riktning spelaren tittar mot.
        double angle = 0;
        //Spelarens position.
        double playerPositionX = 0.5;
        double playerPositionY = 0.5;
        double playerPositionZ = 0.5;
        double wallHitboxSize = 0.2;
        //Hur högt upp taket är.
        const double roomHeight = 1;
        Color roomColor = Color.DarkGreen;
        Color roomColorPattern = Color.DarkGray;
        Color roofColor = Color.DarkBlue;
        double lineSize = 0.1;
        //Alla ytor som finns i det genererade rummet.
        List<Face> currentRoom = new List<Face>();
        List<Object> objects = new List<Object>();
        int isWDown = 0;
        int isSDown = 0;
        int isADown = 0;
        int isDDown = 0;
        double playerSpeed = 0.1;
        bool isGameActive = false;
        List<ButtonData> buttons = new List<ButtonData>();
        public Form1()
        {
            InitializeComponent();
            currentRoomCode = testRoomCode;
            gameScreen.ClientSize = new Size(width, height);
            Size = new Size(width, height);
            //Genererar alla pixlar som behövs.
            for (int i = 0; i < newWidth; i++)
            {
                for (int j = 0; j < newHeight; j++)
                {
                    pixels[i, j] = new Pixel(imageSize * (i - (newWidth / 2)) / newWidth, imageSize * (j - (newHeight / 2)) / newWidth);
                }
            }
            //StartRoom(testRoomCode);
            CreateStartButtons();
            gameScreen.Hide();
        }

        public void CreateStartButtons()
        {
            RemoveButtons();
            ButtonData tempButton = new ButtonData(0, 0, this);
            tempButton.button = new Button();
            tempButton.button.Text = "Start Game";
            tempButton.button.Location = new Point(0, 0);
            tempButton.button.Click += tempButton.OnClick;
            Controls.Add(tempButton.button);
            buttons.Add(tempButton);
        }

        public void CreateLevelButtons()
        {
            RemoveButtons();
            for (int i = 0; i < levels.Length; i++)
            {
                ButtonData tempButton = new ButtonData(i, 1, this);
                tempButton.button = new Button();
                tempButton.button.Text = levels[i].name;
                tempButton.button.Location = new Point(0, (int)(25d * i));
                tempButton.button.Click += tempButton.OnClick;
                Controls.Add(tempButton.button);
                buttons.Add(tempButton);
            }
        }

        public void RemoveButtons()
        {
            foreach (ButtonData button in buttons)
            {
                button.button.Dispose();
            }
            buttons.Clear();
        }

        public void StartLevel(Level level)
        {
            RemoveButtons();
            gameScreen.Show();
            isGameActive = true;
            gameTimer.Start();
            StartRoom(level.room1);
        }

        public void StartRoom(int[,] roomCode)
        {
            currentRoom = GenerateRoom(roomCode);
            objects = GenerateObjects(roomCode);
        }

        public List<Object> GenerateObjects(int[,] roomCode)
        {
            List<Object> objectsToReturn = new List<Object>();
            for (int i = 0; i < roomCode.GetLength(0); i++)
            {
                for (int j = 0; j < roomCode.GetLength(1); j++)
                {
                    if (roomCode[i, j] >= 2)
                    {
                        Object tempObject = new Object(roomCode[i, j], i, j);
                        objectsToReturn.Add(tempObject);
                    }
                }
            }
            return objectsToReturn;
        }

        //Metod som genererar rum utifrån en "RoomCode".
        private List<Face> GenerateRoom(int[,] roomCode)
        {
            List<Face> room = new List<Face>();
            for (int i = 0; i < roomCode.GetLength(0); i++)
            {
                if (roomCode[i, 0] != 1)
                {
                    room.Add(new Face(Math.PI / 2, i, 0, 0, i + 1, roomHeight, 0, Color.Yellow));
                }
                if (roomCode[i, roomCode.GetLength(1) - 1] != 1)
                {
                    room.Add(new Face(Math.PI * 1.5, i, 0, roomCode.GetLength(1), i + 1, roomHeight, roomCode.GetLength(1), Color.Red));
                }
            }
            for (int i = 0; i < roomCode.GetLength(1); i++)
            {
                if (roomCode[0, i] != 1)
                {
                    room.Add(new Face(0, 0, 0, i, 0, roomHeight, i + 1, Color.Green));
                }
                if (roomCode[roomCode.GetLength(0) - 1, i] != 1)
                {
                    room.Add(new Face(Math.PI, roomCode.GetLength(0), 0, i, roomCode.GetLength(0), roomHeight, i + 1, Color.Purple));
                }
            }
            for (int i = 0; i < roomCode.GetLength(0); i++)
            {
                for (int j = 0; j < roomCode.GetLength(1); j++)
                {
                    if (roomCode[i, j] == 1)
                    {
                        if (i > 0)
                        {
                            if (roomCode[i - 1, j] != 1)
                            {
                                room.Add(new Face(Math.PI, i, 0, j, i, roomHeight, j + 1, Color.DarkRed));
                            }
                        }
                        if (i < roomCode.GetLength(0) - 1)
                        {
                            if (roomCode[i + 1, j] != 1)
                            {
                                room.Add(new Face(0, i + 1, 0, j, i + 1, roomHeight, j + 1, Color.DarkRed));
                            }
                        }
                        if (j > 0)
                        {
                            if (roomCode[i, j - 1] != 1)
                            {
                                room.Add(new Face(Math.PI * 1.5, i, 0, j, i + 1, roomHeight, j, Color.DarkRed));
                            }
                        }
                        if (j < roomCode.GetLength(1) - 1)
                        {
                            if (roomCode[i, j + 1] != 1)
                            {
                                room.Add(new Face(Math.PI / 2, i, 0, j + 1, i + 1, roomHeight, j + 1, Color.DarkRed));
                            }
                        }
                    }
                }
            }
            //int k = 0;
            //while (k < room.Count)
            //{
            //    int l = k;
            //    while (l < room.Count)
            //    {
            //        if (room[l].lowerBoundX == room[k].higherBoundX && room[l].lowerBoundZ == room[k].higherBoundZ && room[k].direction == room[l].direction)
            //        {
            //            room[k].higherBoundX = room[l].higherBoundX;
            //            room[k].higherBoundZ = room[l].higherBoundZ;
            //            room.RemoveAt(l);
            //            if (l < k)
            //            {
            //                k -= 1;
            //            }
            //            l -= 1;
            //        }
            //        l++;
            //    }
            //    k++;
            //}
            return room;
        }

        //En timer används för att generera nästa frame.
        private void gameTimer_Tick(object sender, EventArgs e)
        {
            UpdateImage();
            MovePlayer();
        }

        public void MovePlayer()
        {
            double horizontal = isADown - isDDown;
            double vertical = isWDown - isSDown;
            if (!(horizontal == 0 && vertical == 0))
            {
                double movementDirection = Math.Atan2(horizontal, vertical) + angle;

                double movementX = Math.Cos(movementDirection);
                double movementZ = Math.Sin(movementDirection);

                double newXPos = playerPositionX + movementX * playerSpeed;
                double newZPos = playerPositionZ + movementZ * playerSpeed;

                int playerGridPosX = (int)Math.Floor(playerPositionX);
                int playerGridPosZ = (int)Math.Floor(playerPositionZ);
                int newGridPosX = (int)Math.Floor(newXPos + wallHitboxSize * Math.Sign(movementX));
                int newGridPosZ = (int)Math.Floor(newZPos + wallHitboxSize * Math.Sign(movementZ));

                if ((!(newGridPosX >= currentRoomCode.GetLength(0))) && (!(newGridPosX < 0)))
                {
                    if (!(currentRoomCode[newGridPosX, playerGridPosZ] == 1))
                    {
                        playerPositionX = newXPos;
                    }
                }
                if ((!(newGridPosZ >= currentRoomCode.GetLength(1))) && (!(newGridPosZ < 0)))
                {
                    if (!(currentRoomCode[playerGridPosX, newGridPosZ] == 1))
                    {
                        playerPositionZ = newZPos;
                    }
                }
            }
        }

        //Genererar en bild utifrån alla pixlar som används, och sätter den sedan som den bild som syns.
        public void UpdateImage()
        {
            Bitmap bmp = new Bitmap(newWidth, newHeight);
            List<Face> simplifiedRoom = SimplifyRoom(currentRoom);
            for (int i = 0; i < newWidth; i++)
            {
                for (int j = 0; j < newHeight; j++)
                {
                    Color color = CalculatePixel(pixels[i, j], simplifiedRoom).Item1;
                    bmp.SetPixel(i, j, color);
                }
            }
            //pictureBox1.Image = bmp;
            gameScreen.Image = bmp;
        }

        //Räknar ut vilken punkt som träffas om man drar en linje från spelarens position med vinklar beroende på vilken pixel som kollas.
        public (Color, Face) CalculatePixel(Pixel pixel, List<Face> room)
        {
            CalculateRatio(pixel.xPos, pixel.yPos, angle, out double xDirection, out double yDirection, out double zDirection, out double xPosition, out double yPosition, out double zPosition);
            return CalculateLine(xDirection, yDirection, zDirection, xPosition, yPosition, zPosition, room);
        }

        public List<Face> SimplifyRoom(List<Face> room)
        {
            List<Face> result = new List<Face>();
            for (int i = 0; i < newWidth; i += 3)
            {
                Face face = CalculatePixel(pixels[i, newHeight / 2], room).Item2;
                if ((!result.Contains(face)) && face != null)
                {
                    result.Add(face);
                }
            }
            return result;
        }


        public (Color, Face) CalculateLine(double xDirection, double yDirection, double zDirection, double xPosition, double yPosition, double zPosition, List<Face> faces)
        {
            Face currentClosest = null;
            double proximity = 10000;
            double relativeHitFromLower = 0;
            double relativeHitY = 0;
            Color color = Color.Black;
            foreach (Face face in faces)
            {
                if ((zDirection == face.zxRatio * xDirection) || xDirection == 0)
                {

                }
                //else if (face.direction == 0 || face.direction == Math.PI)
                //{

                //}
                else
                {
                    double hitX = (face.lowerBoundZ - zPosition) / ((zDirection / xDirection) - face.zxRatio);
                    if (hitX <= face.higherBoundX && hitX >= face.lowerBoundX)
                    {
                        double hitZ = (hitX - face.lowerBoundX) * face.zxRatio + face.lowerBoundX + zPosition;
                        if (hitZ <= face.higherBoundZ && hitZ >= face.lowerBoundZ)
                        {
                            double hitY = (yDirection / xDirection) * (hitX - xPosition) + yPosition;
                            if (hitY <= face.higherBoundY && hitY >= face.lowerBoundY)
                            {
                                double direction = Math.Atan2(hitZ - playerPositionZ, hitX - playerPositionX);
                                if (Math.Abs(direction - angle) < Math.PI / 2 || Math.Abs(direction + Math.PI * 2 - angle) < Math.PI / 2)
                                {
                                    if (Math.Pow(hitX - xPosition, 2) + Math.Pow(hitZ - zPosition, 2) < proximity)
                                    {
                                        currentClosest = face;
                                        color = face.color;
                                        proximity = Math.Pow(hitX - xPosition, 2) + Math.Pow(hitZ - zPosition, 2);
                                        relativeHitFromLower = Math.Sqrt(Math.Pow(hitX - face.lowerBoundX, 2) + Math.Pow(hitZ - face.lowerBoundZ, 2));
                                        relativeHitY = hitY - face.lowerBoundY;
                                    }
                                }
                            }
                        }
                    }
                }
                //a + bx = c + dx
                //bx = c + dx - a
                //(b - d)x = c - a
                //x = (c - a) / (b - d)
                //if (face.isDirectionX)
                //{
                //    double hitZ = (face.lowerBoundX - xPosition) * zDirection / xDirection + zPosition;
                //    double direction = Math.Atan2(hitZ - playerPositionZ, face.lowerBoundX - playerPositionX);
                //    if (Math.Abs(direction - angle) < Math.PI / 2 || Math.Abs(direction + Math.PI * 2 - angle) < Math.PI / 2)
                //    {
                //        if (face.lowerBoundZ <= hitZ && hitZ <= face.higherBoundZ)
                //        {
                //            double hitY = (face.lowerBoundX - xPosition) * yDirection / xDirection + yPosition;
                //            if (0 <= hitY && hitY <= roomHeight)
                //            {
                //                if (Math.Pow(face.lowerBoundX - xPosition, 2) + Math.Pow(hitZ - zPosition, 2) < proximity)
                //                {
                //                    currentClosest = face;
                //                    color = face.color;
                //                    proximity = Math.Pow(face.lowerBoundX - xPosition, 2) + Math.Pow(hitZ - zPosition, 2);
                //                    relativeHitFromLower = hitZ - face.lowerBoundZ;
                //                    relativeHitY = hitY;
                //                }
                //            }
                //        }
                //    }
                //}
                //else
                //{
                //    double hitX = (face.lowerBoundZ - zPosition) * xDirection / zDirection + xPosition;
                //    double direction = Math.Atan2(face.lowerBoundZ - playerPositionZ, hitX - playerPositionX);
                //    if (Math.Abs(direction - angle) < Math.PI / 2 || Math.Abs(direction + Math.PI * 2 - angle) < Math.PI / 2)
                //    {
                //        if (face.lowerBoundX <= hitX && hitX <= face.higherBoundX)
                //        {
                //            double hitY = (face.lowerBoundZ - zPosition) * yDirection / zDirection + yPosition;
                //            if (0 <= hitY && hitY <= roomHeight)
                //            {
                //                if (Math.Pow(hitX - xPosition, 2) + Math.Pow(face.lowerBoundZ - zPosition, 2) < proximity)
                //                {
                //                    currentClosest = face;
                //                    color = face.color;
                //                    proximity = Math.Pow(hitX - xPosition, 2) + Math.Pow(face.lowerBoundZ - zPosition, 2);
                //                    relativeHitFromLower = hitX - face.lowerBoundX;
                //                    relativeHitY = hitY;
                //                }
                //            }
                //        }
                //    }
                //}
            }
            //Kod som gör olika mönster.
            if (currentClosest != null)
            {
                //if (currentClosest.isDirectionX)
                //{
                    //if (Math.Abs(relativeHitFromLower - currentClosest.lowerBoundZ) < lineSize || Math.Abs(currentClosest.higherBoundZ - relativeHitFromLower) < lineSize)
                    //{
                        //return (roomColorPattern, currentClosest);
                    //}
                //}
                if (Math.Abs(relativeHitFromLower - currentClosest.lowerBoundX) < lineSize || Math.Abs(currentClosest.higherBoundX - relativeHitFromLower) < lineSize)
                {
                        return (roomColorPattern, currentClosest);
                }
                if (Math.Abs(relativeHitY) < lineSize || Math.Abs(roomHeight - relativeHitY) < lineSize)
                {
                    return (roomColorPattern, currentClosest);
                }
                else if (Math.Abs(relativeHitY - relativeHitFromLower) < lineSize || Math.Abs(roomHeight - relativeHitY - relativeHitFromLower) < lineSize)
                {
                    return (roomColorPattern, currentClosest);
                }
                else
                {
                    return (color, currentClosest);
                }
            }
            else
            {
                double hitX = (roomHeight - yPosition) * xDirection / yDirection + xPosition;
                double hitZ = (roomHeight - yPosition) * zDirection / yDirection + zPosition;
                double direction = Math.Atan2(hitZ - playerPositionZ, hitX - playerPositionX);
                if (!(Math.Abs(direction - angle) < Math.PI / 2 || Math.Abs(direction + Math.PI * 2 - angle) < Math.PI / 2))
                {
                    hitX = -yPosition * xDirection / yDirection + xPosition;
                    hitZ = -yPosition * zDirection / yDirection + zPosition;
                }
                if (Math.Abs(Math.Round(hitX) - hitX) <= lineSize || Math.Abs(Math.Round(hitZ) - hitZ) <= lineSize)
                {
                    return (roomColorPattern, currentClosest);
                }
                else
                {
                    return (roofColor, currentClosest);
                }
            }
        }

        //Metod för att räkna ut i vilken riktning linjen ska dras utifrån givna vinklar.
        public void CalculateRatio(double localXPos, double localYPos, double angle, out double xDirection, out double yDirection, out double zDirection, out double xPosition, out double yPosition, out double zPosition)
        {
            double baseXPosition = localXPos * Math.Sin(angle);
            double baseZPosition = localXPos * -Math.Cos(angle);
            double baseYPosition = localYPos;
            double projectedXPosition = Math.Cos(angle) + baseXPosition * imageScale;
            double projectedZPosition = Math.Sin(angle) + baseZPosition * imageScale;
            double projectedYPosition = baseYPosition * imageScale * imageScaleY;
            xDirection = projectedXPosition - baseXPosition;
            yDirection = projectedYPosition - baseYPosition;
            zDirection = projectedZPosition - baseZPosition;
            xPosition = baseXPosition + playerPositionX;
            yPosition = baseYPosition + playerPositionY;
            zPosition = baseZPosition + playerPositionZ;
            //double verticalAngle = Math.Atan2(localYPos * (imageScale - 1), 1) * 180 / Math.PI;
            //double horizontalAngle = Math.Atan2(localXPos * (imageScale - 1), 1) * 180 / Math.PI + angle;
            //double a = Math.Tan(verticalAngle * Math.PI / 180);
            //double c = Math.Tan(horizontalAngle * Math.PI / 180);
            //double a2 = a * a;
            //double c2 = c * c;
            //double d = c2 + a2 * (c2 + 1) + 1;
            //zDirection = Math.Sqrt(1 / d);
            //yDirection = a * Math.Sqrt(zDirection * zDirection * (c2 + 1));
            //xDirection = c * zDirection;
            //xPosition = localXPos * Math.Cos(angle * Math.PI / 180) + playerPositionX;
            //zPosition = localXPos * Math.Sin(angle * Math.PI / 180) + playerPositionZ;
            //yPosition = localYPos + playerPositionY;
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
                angle += Math.PI / 18;
                fixAngle();
            }
            if (e.KeyCode == Keys.Right)
            {
                angle -= Math.PI / 18;
                fixAngle();
            }
        }

        //Metod som ser till att vinkeln håller sig mellan -180 och 180.
        private void fixAngle()
        {
            if (angle < 0)
            {
                angle += 2 * Math.PI;
            }
            else if (angle > Math.PI * 2)
            {
                angle -= 2 * Math.PI;
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
        public double direction;
        public double lowerBoundX;
        public double lowerBoundY;
        public double lowerBoundZ;
        public double higherBoundX;
        public double higherBoundY;
        public double higherBoundZ;
        public double midX;
        public double midZ;
        public Color color;
        public double zxRatio;

        public Face(double direction, double lowerBoundX, double lowerBoundY, double lowerBoundZ, double higherBoundX, double higherBoundY, double higherBoundZ, Color color)
        {
            this.direction = direction;
            this.lowerBoundX = lowerBoundX;
            this.lowerBoundY = lowerBoundY;
            this.lowerBoundZ = lowerBoundZ;
            this.higherBoundX = higherBoundX;
            this.higherBoundY = higherBoundY;
            this.higherBoundZ = higherBoundZ;
            midX = (higherBoundX + lowerBoundX) / 2;
            midZ = (higherBoundZ + lowerBoundZ) / 2;
            this.color = color;
            if (higherBoundX == lowerBoundX)
            {
                zxRatio = 0;
            }
            else
            {
                zxRatio = (higherBoundZ - lowerBoundZ) / (higherBoundX - lowerBoundX);
            }
        }
    }

    public class Object
    {
        public Image image;
        public double positionX;
        public double positionZ;
        public bool isEnemy;
        public double height;

        public Object(int type, double positionX, double positionZ)
        {
            this.positionX = positionX;
            this.positionZ = positionZ;
            switch (type)
            {
                case 10:
                    break;
            }
        }
    }

    public class Level
    {
        public string name;
        public int[,] room1;
        public int[,] room2;
        public int[,] room3;
        public int[,] room4;
        public int[,] room5;
        public Level(string name, int[,] room1, int[,] room2, int[,] room3, int[,] room4, int[,] room5)
        {
            this.name = name;
            this.room1 = room1;
            this.room2 = room2;
            this.room3 = room3;
            this.room4 = room4;
            this.room5 = room5;
        }
    }

    public class ButtonData
    {
        public int id;
        public int type;
        public Form1 main;
        public Button button;

        public ButtonData(int id, int type, Form1 main)
        {
            this.id = id;
            this.type = type;
            this.main = main;
        }

        public void OnClick(object sender, EventArgs e)
        {
            switch (type)
            {
                case 0:
                    main.CreateLevelButtons();
                    break;
                case 1:
                    main.StartLevel(main.levels[id]);
                    break;
            }
        }
    }
}
