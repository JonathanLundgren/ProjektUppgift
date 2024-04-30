using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography.X509Certificates;
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
04/19 Refaktoriserade en del av koden.
04/24 Uppdaterade systemet som används för att göra mönster på väggarna.
04/26 Golv och Tak använder nu också det uppdaterade systemet. Påbörjade system för fiender.
04/30 Påbörjade rendring av fiender.

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
                new int[,] { { 0, 0, 0 }, { 0, 0, 0 }, { 0, 10, 0 } },
                new int[,] { { 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0 } },
                null,
                null,
                null
            )
        };
        int[,] testRoomCode = new int[,] { { 0, 1, 0, 1, 0 }, { 0, 0, 0, 0, 0 }, { 0, 1, 0, 0, 1 }, { 0, 0, 0, 0, 0 }, { 0, 1, 0, 1, 0 }, { 0, 0, 0, 0, 0 }, { 0, 1, 0, 1, 0 }, { 0, 0, 2, 0, 0 } };
        int[,] currentRoomCode;
        //Vilken riktning spelaren tittar mot.
        public double angle = 0;
        //Spelarens position.
        public double playerPositionX = 0.5;
        public double playerPositionY = 0.5;
        public double playerPositionZ = 0.5;
        double wallHitboxSize = 0.2;
        //Hur högt upp taket är.
        public const double roomHeight = 1;
        Color roomColor = Color.DarkGreen;
        Color roomColorPattern = Color.DarkGray;
        Color roofColor = Color.DarkBlue;
        double lineSize = 0.1;
        //Alla ytor som finns i det genererade rummet.
        public List<Face> currentRoom = new List<Face>();
        List<Object> objects = new List<Object>();
        int isWDown = 0;
        int isSDown = 0;
        int isADown = 0;
        int isDDown = 0;
        double playerSpeed = 0.1;
        bool isGameActive = false;
        List<ButtonData> buttons = new List<ButtonData>();
        public Picture colorPatternWall1 = new Picture(Properties.Resources.Wall_1);
        public Picture colorPatternRoof1 = new Picture(Properties.Resources.Roof1);
        public Picture colorPatternFloor1 = new Picture(Properties.Resources.Floor1);
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
                tempButton.button.Location = new Point(0, 25 * i);
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
            currentRoomCode = level.room1;
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
                        Object tempObject = new Object(roomCode[i, j], i, j, this);
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
                    room.Add(new Face(Math.PI / 2, i, 0, 0, i + 1, roomHeight, 0, colorPatternWall1));
                }
                if (roomCode[i, roomCode.GetLength(1) - 1] != 1)
                {
                    room.Add(new Face(Math.PI * 1.5, i, 0, roomCode.GetLength(1), i + 1, roomHeight, roomCode.GetLength(1), colorPatternWall1));
                }
            }
            for (int i = 0; i < roomCode.GetLength(1); i++)
            {
                if (roomCode[0, i] != 1)
                {
                    room.Add(new Face(0, 0, 0, i, 0, roomHeight, i + 1, colorPatternWall1));
                }
                if (roomCode[roomCode.GetLength(0) - 1, i] != 1)
                {
                    room.Add(new Face(Math.PI, roomCode.GetLength(0), 0, i, roomCode.GetLength(0), roomHeight, i + 1, colorPatternWall1));
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
                                room.Add(new Face(Math.PI, i, 0, j, i, roomHeight, j + 1, colorPatternWall1));
                            }
                        }
                        if (i < roomCode.GetLength(0) - 1)
                        {
                            if (roomCode[i + 1, j] != 1)
                            {
                                room.Add(new Face(0, i + 1, 0, j, i + 1, roomHeight, j + 1, colorPatternWall1));
                            }
                        }
                        if (j > 0)
                        {
                            if (roomCode[i, j - 1] != 1)
                            {
                                room.Add(new Face(Math.PI * 1.5, i, 0, j, i + 1, roomHeight, j, colorPatternWall1));
                            }
                        }
                        if (j < roomCode.GetLength(1) - 1)
                        {
                            if (roomCode[i, j + 1] != 1)
                            {
                                room.Add(new Face(Math.PI / 2, i, 0, j + 1, i + 1, roomHeight, j + 1, colorPatternWall1));
                            }
                        }
                    }
                }
            }
            int k = 0;
            while (k < room.Count)
            {
                int l = k;
                while (l < room.Count)
                {
                    if (room[l].x1 == room[k].x2 && room[l].z1 == room[k].z2 && room[k].direction == room[l].direction)
                    {
                        room[k].x2 = room[l].x2;
                        room[k].z2 = room[l].z2;
                        room.RemoveAt(l);
                        if (l < k)
                        {
                            k -= 1;
                        }
                        l -= 1;
                    }
                    l++;
                }
                k++;
            }
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
            foreach (Object o in objects)
            {
                foreach (Face face in o.GetFaces())
                {
                    simplifiedRoom.Add(face);
                }
            }
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
            double hitX;
            double hitY;
            double hitZ;
            Color color = Color.Black;

            void AfterXYZ(Face face)
            {
                if (hitY <= face.y2 && hitY >= face.y1)
                {
                    double direction = Math.Atan2(hitZ - playerPositionZ, hitX - playerPositionX);
                    if (Math.Abs(direction - angle) <= Math.PI / 2 || Math.Abs(direction + Math.PI * 2 - angle) <= Math.PI / 2)
                    {
                        if (Math.Pow(hitX - xPosition, 2) + Math.Pow(hitZ - zPosition, 2) < proximity)
                        {
                            currentClosest = face;
                            proximity = Math.Pow(hitX - xPosition, 2) + Math.Pow(hitZ - zPosition, 2);
                            relativeHitFromLower = Math.Sqrt((hitX - face.x1) * (hitX - face.x1) + (hitZ - face.z1) * (hitZ - face.z1));
                            relativeHitY = hitY - face.y1;
                        }
                    }
                }
            }

            foreach (Face face in faces)
            {
                if (zDirection == face.zxRatio * xDirection)
                {

                }
                else if (Math.Abs(xDirection) <= 0.00001d)
                {
                    //MessageBox.Show("BBBBBBBBBBBB");
                    if (face.zxRatio <= 0.01d && face.zxRatio >= -0.01d)
                    {
                        //MessageBox.Show("AAAAAAAAAAAAAA");
                        hitX = xPosition;
                        if (hitX <= face.x2 && hitX >= face.x1)
                        {
                            hitZ = face.z1;
                            hitY = (yDirection / zDirection) * (hitZ - zPosition) + yPosition;
                            AfterXYZ(face);
                        }
                    }
                    else
                    {
                        hitZ = (face.x1 - ((1 / face.zxRatio) * face.z1) - (xPosition - (xDirection / zDirection * zPosition))) / ((xDirection / zDirection) - (1 / face.zxRatio));
                        if (hitZ <= face.z2 && hitZ >= face.z1)
                        {
                            hitX = (hitZ * (xDirection / zDirection) + xPosition) - ((xDirection / zDirection) * zPosition);
                            if (hitX <= face.x2 + 0.01d && hitX >= face.x1 - 0.01d)
                            {
                                hitY = (yDirection / zDirection) * (hitZ - zPosition) + yPosition;
                                AfterXYZ(face);
                            }
                        }
                    }
                }
                else if (face.x1 == face.x2)
                {
                    if (Math.Abs(zDirection) <= 0.00001d)
                    {
                        hitX = face.x1;
                        hitZ = zPosition;
                        if (hitZ <= face.z2 && hitZ >= face.z1)
                        {
                            hitY = (yDirection / xDirection) * (hitX - xPosition) + yPosition;
                            AfterXYZ(face);
                        }
                    }
                    else
                    {
                        hitZ = (face.x1 - ((0 / face.zxRatio) * face.z1) - (xPosition - (xDirection / zDirection * zPosition))) / ((xDirection / zDirection) - (0 / face.zxRatio));
                        if (hitZ <= face.z2 && hitZ >= face.z1)
                        {
                            hitX = (hitZ * (xDirection / zDirection) + xPosition) - ((xDirection / zDirection) * zPosition);
                            if (hitX <= face.x2 + 0.01d && hitX >= face.x1 - 0.01d)
                            {
                                hitY = (yDirection / zDirection) * (hitZ - zPosition) + yPosition;
                                AfterXYZ(face);
                            }
                        }
                    }
                }
                else
                {
                    hitX = (face.z1 - (face.zxRatio * face.x1) - (zPosition - (zDirection / xDirection * xPosition))) / ((zDirection / xDirection) - face.zxRatio);
                    if (hitX <= face.x2 && hitX >= face.x1)
                    {
                        hitZ = (hitX * (zDirection / xDirection) + zPosition) - ((zDirection / xDirection) * xPosition);
                        if (hitZ <= face.z2 + 0.01d && hitZ >= face.z1 - 0.01d)
                        {
                            hitY = (yDirection / xDirection) * (hitX - xPosition) + yPosition;
                            AfterXYZ(face);
                        }
                    }
                }
                //a + bx = c + dx
                //bx = c + dx - a
                //(b - d)x = c - a
                //x = (c - a) / (b - d)
                //if (face.isDirectionX)
                //{
                //    double hitZ = (face.x1 - xPosition) * zDirection / xDirection + zPosition;
                //    double direction = Math.Atan2(hitZ - playerPositionZ, face.x1 - playerPositionX);
                //    if (Math.Abs(direction - angle) < Math.PI / 2 || Math.Abs(direction + Math.PI * 2 - angle) < Math.PI / 2)
                //    {
                //        if (face.z1 <= hitZ && hitZ <= face.z2)
                //        {
                //            double hitY = (face.x1 - xPosition) * yDirection / xDirection + yPosition;
                //            if (0 <= hitY && hitY <= roomHeight)
                //            {
                //                if (Math.Pow(face.x1 - xPosition, 2) + Math.Pow(hitZ - zPosition, 2) < proximity)
                //                {
                //                    currentClosest = face;
                //                    color = face.color;
                //                    proximity = Math.Pow(face.x1 - xPosition, 2) + Math.Pow(hitZ - zPosition, 2);
                //                    relativeHitFromLower = hitZ - face.z1;
                //                    relativeHitY = hitY;
                //                }
                //            }
                //        }
                //    }
                //}
                //else
                //{
                //    double hitX = (face.z1 - zPosition) * xDirection / zDirection + xPosition;
                //    double direction = Math.Atan2(face.z1 - playerPositionZ, hitX - playerPositionX);
                //    if (Math.Abs(direction - angle) < Math.PI / 2 || Math.Abs(direction + Math.PI * 2 - angle) < Math.PI / 2)
                //    {
                //        if (face.x1 <= hitX && hitX <= face.x2)
                //        {
                //            double hitY = (face.z1 - zPosition) * yDirection / zDirection + yPosition;
                //            if (0 <= hitY && hitY <= roomHeight)
                //            {
                //                if (Math.Pow(hitX - xPosition, 2) + Math.Pow(face.z1 - zPosition, 2) < proximity)
                //                {
                //                    currentClosest = face;
                //                    color = face.color;
                //                    proximity = Math.Pow(hitX - xPosition, 2) + Math.Pow(face.z1 - zPosition, 2);
                //                    relativeHitFromLower = hitX - face.x1;
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
                double patternX = relativeHitFromLower % currentClosest.length;
                double patternY = relativeHitY % currentClosest.height;
                int pixelX = (int)(currentClosest.picture.pattern.GetLength(0) * patternX / currentClosest.length);
                int pixelY = (int)(currentClosest.picture.pattern.GetLength(1) * patternY / currentClosest.height);
                int colorIndex = currentClosest.picture.pattern[pixelX, pixelY];
                return (currentClosest.picture.colors[colorIndex], currentClosest);
                //if (currentClosest.isDirectionX)
                //{
                //if (Math.Abs(relativeHitFromLower - currentClosest.z1) < lineSize || Math.Abs(currentClosest.z2 - relativeHitFromLower) < lineSize)
                //{
                //return (roomColorPattern, currentClosest);
                //}
                //}
                //if (Math.Abs(patternX) < lineSize || Math.Abs(1 - patternX) < lineSize)
                //{
                //    return (roomColorPattern, currentClosest);
                //}
                //if (Math.Abs(patternY) < lineSize || Math.Abs(roomHeight - patternY) < lineSize)
                //{
                //    return (roomColorPattern, currentClosest);
                //}
                //else if (Math.Abs(patternY - patternX) < lineSize || Math.Abs(roomHeight - patternY - patternX) < lineSize)
                //{
                //    return (roomColorPattern, currentClosest);
                //}
                //else
                //{
                //    return (color, currentClosest);
                //}
            }
            else
            {
                hitX = (roomHeight - yPosition) * xDirection / yDirection + xPosition;
                hitZ = (roomHeight - yPosition) * zDirection / yDirection + zPosition;
                Picture pattern = colorPatternRoof1;
                double direction = Math.Atan2(hitZ - playerPositionZ, hitX - playerPositionX);
                if (!(Math.Abs(direction - angle) < Math.PI / 2 || Math.Abs(direction + Math.PI * 2 - angle) < Math.PI / 2))
                {
                    hitX = -yPosition * xDirection / yDirection + xPosition;
                    hitZ = -yPosition * zDirection / yDirection + zPosition;
                    pattern = colorPatternFloor1;
                }
                if (Math.Abs(hitX) == double.PositiveInfinity)
                {
                    hitX = 0;
                    hitZ = 0;
                }
                double patternX = Math.Abs(hitX) % 1;
                double patternY = Math.Abs(hitZ) % 1;
                int pixelX = (int)((pattern.pattern.GetLength(0) - 1) * patternX);
                int pixelY = (int)((pattern.pattern.GetLength(1) - 1) * patternY);
                int colorIndex = pattern.pattern[pixelX, pixelY];
                return (pattern.colors[colorIndex], currentClosest);
                //if (Math.Abs(Math.Round(hitX) - hitX) <= lineSize || Math.Abs(Math.Round(hitZ) - hitZ) <= lineSize)
                //{
                //    return (roomColorPattern, currentClosest);
                //}
                //else
                //{
                //    return (roofColor, currentClosest);
                //}
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
        public double x1;
        public double y1;
        public double z1;
        public double x2;
        public double y2;
        public double z2;
        public double midX;
        public double midZ;
        public double zxRatio;
        public double length;
        public double height;
        public Picture picture;

        public Face(double direction, double x1, double y1, double z1, double x2, double y2, double z2, Picture picture)
        {
            this.direction = direction;
            this.x1 = x1;
            this.y1 = y1;
            this.z1 = z1;
            this.x2 = x2;
            this.y2 = y2;
            this.z2 = z2;
            midX = (x2 + x1) / 2;
            midZ = (z2 + z1) / 2;
            this.picture = picture;
            if (x2 == x1)
            {
                zxRatio = 1;
            }
            else
            {
                zxRatio = (z2 - z1) / Math.Abs(x2 - x1);
            }
            length = Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(z2 - z1, 2));
            height = y2 - y1;
        }
    }

    public class Object
    {
        public double positionX;
        public double positionZ;
        public double angle = 0;
        public int difficulty;
        public bool isEnemy;
        public bool isImmobile;
        public Form1 main;

        public Object(int type, double positionX, double positionZ, Form1 main)
        {
            this.positionX = positionX + 0.5;
            this.positionZ = positionZ + 0.5;
            switch (type)
            {
                case 10:
                    difficulty = 1;
                    isEnemy = true;
                    isImmobile = true;
                    break;
            }

            this.main = main;
        }

        public void Move()
        {
            if (isEnemy)
            {
                if (main.CalculateLine(main.playerPositionX - positionX, 0, main.playerPositionZ - positionZ, positionX, Form1.roomHeight / 2, positionZ, main.currentRoom).Item2 != null)
                {
                    
                }
            }
        }

        public Face[] GetFaces()
        {
            if (isEnemy)
            {
                switch (difficulty)
                {
                    case 1:
                        return GenerateCuboid(positionX, 0.5, positionZ, 0.5, 0.5, angle, new Picture[] { main.colorPatternFloor1, main.colorPatternRoof1, main.colorPatternWall1, main.colorPatternRoof1 });
                }
            }
            return null;
        }

        public Face[] GenerateCuboid(double xPos, double yPos, double zPos, double height, double length, double angle, Picture[] pictures)
        {
            Face[] toReturn = new Face[4];
            for (int i = 0; i < 4; i++)
            {
                double relativeXPosition1 = (Math.Cos(angle) + Math.Sin(angle)) * 0.5 * length; //1  //1  //-1
                double relativeZPosition1 = (Math.Sin(angle) - Math.Cos(angle)) * 0.5 * length; //-1 //1  //1
                double relativeXPosition2 = (Math.Cos(angle) - Math.Sin(angle)) * 0.5 * length; //1  //-1 //-1
                double relativeZPosition2 = (Math.Sin(angle) + Math.Cos(angle)) * 0.5 * length; //1  //1  //-1

                toReturn[i] = new Face(angle, relativeXPosition1 + xPos, yPos - 0.5 * height, relativeZPosition1 + zPos, relativeXPosition2 + xPos, yPos + 0.5 * height, relativeZPosition2 + zPos, pictures[i]);
                angle += Math.PI / 2;
                if (angle >= Math.PI * 2)
                {
                    angle -= Math.PI * 2;
                }
            }
            return toReturn;
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

    public class Picture
    {
        public List<Color> colors = new List<Color>();
        public int[,] pattern;
        public Picture(Bitmap baseImage)
        {
            pattern = new int[baseImage.Width, baseImage.Height];
            for (int i = 0; i < baseImage.Width; i++)
            {
                for (int j = 0; j < baseImage.Height; j++)
                {
                    Color color = baseImage.GetPixel(i, j);
                    if (!colors.Contains(color))
                    {
                        colors.Add(color);
                    }
                    pattern[i, j] = colors.IndexOf(color);
                }
            }
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
