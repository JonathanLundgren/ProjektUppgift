﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Media;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
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
Finderna ska sitta fast i golvet.
Spelaren ska kunna anfalla fiender.
När spelaren träffar fienderna ska en effekt spelas.
Det ska finnas power-ups utspridda i banan.
Spelaren ska ha ett HUD som visar liv och power-ups.
Spelaren ska inte kunna gå genom väggar.

Om jag hinner:
Fiender som rör på sig.
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
05/03 Fixade rendrings-bugg. Väggar kan nu ha fler vinklar än multiplar av 90 grader, och det går nu göra topp och botten på kuber.
05/04 Det går nu att titta lite uppåt och nedåt.
05/08 Jobbade med matematik som kommer att behövas.
05/15 Jobbade med optimiseringar.
05/16 Fortsatte med optimiseringar.
05/24 Fortsatte med optimiseringar.

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
        float imageSize = 0.25f;
        float imageScale = 8;
        //Höjd och bredd som används för beräkningar.
        readonly int newWidth = width / resolution;
        readonly int newHeight = height / resolution;
        //En två-dimensionell array med alla pixlar som kan ändras på.
        Pixel[,] pixels = new Pixel[width / resolution, height / resolution];
        (float, float) yxRatioUp;
        (float, float) yxRatioDown;
        (float, float) zxRatioLeft;
        (float, float) zxRatioRight;
        //"RoomCodes" är arrayer som beskriver hur rummen ska se ut. Beräkningar utförs senare för att generera rummen på ett sätt som fungerar med resten av koden.
        public Level[] levels = new Level[]
        {
            new Level
            (
                "Level 1",
                new int[,] { { 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0 }, { 31, 32, 0, 33, 34 }, { 1, 1, 0, 1, 1 }, { 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0 }, { 12, 0, 11, 0, 13 } },
                new int[,] { { 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0 } },
                null,
                null,
                null
            ),
            new Level
            (
                "Level 2",
                new int[,] { { 11, 0, 11 }, { 0, 0, 0 }, { 11, 0, 11 } },
                new int[,] { { 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0 } },
                null,
                null,
                null
            )
        };
        int[,] testRoomCode = new int[,] { { 0, 1, 0, 1, 0 }, { 0, 0, 0, 0, 0 }, { 0, 1, 0, 0, 1 }, { 0, 0, 0, 0, 0 }, { 0, 1, 0, 1, 0 }, { 0, 0, 0, 0, 0 }, { 0, 1, 0, 1, 0 }, { 0, 0, 2, 0, 0 } };
        public int[,] currentRoomCode;
        //Vilken riktning spelaren tittar mot.
        public float angle = 0;
        public float angleVertical = 0;
        //Spelarens position.
        public float playerPositionX = 0;
        public float playerPositionY = 0;
        public float playerPositionZ = 0;
        float wallHitboxSize = 0.2f;
        //Hur högt upp taket är.
        public const float roomHeight = 1;
        Color roomColor = Color.DarkGreen;
        Color roomColorPattern = Color.DarkGray;
        Color roofColor = Color.DarkBlue;
        float lineSize = 0.1f;
        //Alla ytor som finns i det genererade rummet.
        public List<Face> currentRoom = new List<Face>();
        public List<Object> objects = new List<Object>();
        public List<Object> objectsToAdd = new List<Object>();
        public List<Object> objectsToRemove = new List<Object>();
        int isWDown = 0;
        int isSDown = 0;
        int isADown = 0;
        int isDDown = 0;
        float playerSpeed = 2f;
        float verticalVelocity = 0;
        float jumpForce = 0.5f;
        float gravity = 0.1f;
        int startHP = 5;
        public int hp;
        public float hitBox = 0.15f;
        float shotCooldown = 1;
        float remainingShotCoolDown = 0;
        float powerupActive;
        public bool isEnemiesInRoom = false;
        Stopwatch stopwatch = new Stopwatch();
        public long timeElapsed;
        bool isGameActive = false;
        bool controlCursor = false;
        int roomIndex;
        Level currentLevel;
        List<ButtonData> buttons = new List<ButtonData>();
        public Picture colorPatternWall1 = new Picture(Properties.Resources.Wall_1);
        public Picture colorPatternRoof1 = new Picture(Properties.Resources.Roof1);
        public Picture colorPatternFloor1 = new Picture(Properties.Resources.Floor1);
        public Picture colorPatternProjectile1 = new Picture(Properties.Resources.Projectile1);
        public Picture colorPatternEnemyFace1 = new Picture(Properties.Resources.EnemyFront1);
        public Picture colorPatternEnemyFace2 = new Picture(Properties.Resources.EnemyFront2);
        public Picture colorPatternEnemyFace3 = new Picture(Properties.Resources.EnemyFront3);
        public Picture colorPatternEnemySide1 = new Picture(Properties.Resources.EnemySide1);
        public Picture colorPatternEnemySide2 = new Picture(Properties.Resources.EnemySide2);
        public Picture colorPatternEnemySide3 = new Picture(Properties.Resources.EnemySide3);
        public Picture colorPatternGoal = new Picture(Properties.Resources.Goal);
        public Picture colorPatternLocked = new Picture(Properties.Resources.Lock);
        public Picture colorPatternYellow = new Picture (Properties.Resources.Yellow);
        public Picture colorPatternGreen = new Picture(Properties.Resources.Green);
        public Picture colorPatternHeal = new Picture (Properties.Resources.Heal);


        public SoundPlayer shotSound = new SoundPlayer("ShotSound.wav");

        public Form1()
        {

            InitializeComponent();
            currentRoomCode = testRoomCode;
            gameScreen.Size = new Size(width, height);
            ClientSize = new Size(width - 2, height + 200);
            //Genererar alla pixlar som behövs.
            for (int i = 0; i < newWidth; i++)
            {
                for (int j = 0; j < newHeight; j++)
                {
                    pixels[i, j] = new Pixel(imageSize * (i - (newWidth / 2)) / newWidth, -imageSize * (j - (newHeight / 2)) / newHeight);
                }
            }
            //StartRoom(testRoomCode);
            CalculateRatio(pixels[0, newHeight - 1].xPos, pixels[0, newHeight - 1].yPos, 0, 0, out float xDirectionLeftUp, out float yDirectionLeftUp, out float zDirectionLeftUp, out _, out float yPositionleftUp, out float zPositionLeftUp);
            CalculateRatio(pixels[newWidth - 1, 0].xPos, pixels[newWidth - 1, 0].yPos, 0, 0, out float xDirectionRightDown, out float yDirectionRightDown, out float zDirectionRightDown, out _, out float yPositionRightDown, out float zPositionRightDown);
            yxRatioUp = (yDirectionLeftUp / xDirectionLeftUp, yPositionleftUp);
            yxRatioDown = (yDirectionRightDown / xDirectionRightDown, yPositionRightDown);
            zxRatioLeft = (zDirectionLeftUp / xDirectionLeftUp, zPositionLeftUp);
            zxRatioRight = (zDirectionRightDown / xDirectionRightDown, zPositionRightDown);
            CreateStartButtons();
            gameScreen.Hide();
            playerPositionX = 0.5f;
            playerPositionY = 0.5f;
            playerPositionZ = 0.5f;

            Crosshair.Parent = gameScreen;
        }

        public void CreateStartButtons()
        {
            ReleaseCursor();
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
            roomIndex = 1;
            currentLevel = level;
            stopwatch.Restart();
            ResetPlayer();
            FixCursor();
            RemoveButtons();
            gameScreen.Show();
            isGameActive = true;
            gameTimer.Start();
            StartRoom(level.room1);
            currentRoomCode = level.room1;
            isEnemiesInRoom = CheckForEnemies();
        }

        public void StartNextRoom()
        {
            switch (roomIndex)
            {
                case 1:
                    StartRoom(currentLevel.room2);
                    break;
                case 2:
                    StartRoom(currentLevel.room3);
                    break;
                case 3:
                    StartRoom(currentLevel.room4);
                    break;
                case 4:
                    StartRoom(currentLevel.room5);
                    break;
            }
        }

        private void ResetPlayer()
        {
            hp = startHP;
            powerupActive = 0;
        }

        public void TakeDamage(int amount)
        {
            if (powerupActive == 0 || amount <= 0)
            {
                hp -= amount;
                hpLabel.Text = "HP: " + hp;
            }
        }

        public void ActivatePowerup()
        {
            powerupActive = 20f;
            powerupLabel.Text = "Powerup: Active";
            powerupLabel.BackColor = Color.Yellow;
        }

        public void DeactivatePowerup()
        {
            powerupActive = 0f;
            powerupLabel.Text = "Powerup: Inactive";
            powerupLabel.BackColor = Color.Aqua;
        }

        public bool CheckForEnemies()
        {
            byte foundEnemies = 0;
            {
                foreach (Object o in objects)
                {
                    if (o.isEnemy)
                    {
                        foundEnemies++;
                        if (foundEnemies == 2)
                        {
                            break;
                        }
                    }
                }
            }
            if (foundEnemies == 2)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void Shoot()
        {

            if (remainingShotCoolDown <= 0)
            {
                shotSound.Play();
                CalculateRatio(0, 0, angle, angleVertical, out float xDirection, out float yDirection, out float zDirection, out float xPosition, out float yPosition, out float zPosition);
                var lineHit = CalculateLine(xDirection, yDirection, zDirection, xPosition, yPosition, zPosition, GetPartRoom(currentRoom, false), angle);
                if (lineHit.Item2 != null)
                {
                    if (lineHit.Item2.parent != null)
                    {
                        Object hitObject = lineHit.Item2.parent;
                        if (lineHit.Item2.parent.isEnemy)
                        {
                            if (powerupActive == 0)
                            {
                                hitObject.TakeDamage(1);
                            }
                            else
                            {
                                hitObject.TakeDamage(3);
                            }
                        }
                    }
                }
                remainingShotCoolDown = shotCooldown;
            }
        }

        public void StartRoom(int[,] roomCode)
        {
            playerPositionX = 0.5f;
            playerPositionY = 0.5f;
            playerPositionZ = 0.5f;
            currentRoom = GenerateRoom(roomCode);
            currentRoomCode = roomCode;
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
                        Object tempObject = new Object(roomCode[i, j], i + 0.5f, j + 0.5f, 0, this);
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
                    room.Add(new Face((float)Math.PI / 2, i, 0, 0, i + 1, roomHeight, 0, colorPatternWall1, null));
                }
                if (roomCode[i, roomCode.GetLength(1) - 1] != 1)
                {
                    room.Add(new Face((float)Math.PI * 1.5f, i, 0, roomCode.GetLength(1), i + 1, roomHeight, roomCode.GetLength(1), colorPatternWall1, null));
                }
            }
            for (int i = 0; i < roomCode.GetLength(1); i++)
            {
                if (roomCode[0, i] != 1)
                {
                    room.Add(new Face(0, 0, 0, i, 0, roomHeight, i + 1, colorPatternWall1, null));
                }
                if (roomCode[roomCode.GetLength(0) - 1, i] != 1)
                {
                    room.Add(new Face((float)Math.PI, roomCode.GetLength(0), 0, i, roomCode.GetLength(0), roomHeight, i + 1, colorPatternWall1, null));
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
                                room.Add(new Face((float)Math.PI, i, 0, j, i, roomHeight, j + 1, colorPatternWall1, null));
                            }
                        }
                        if (i < roomCode.GetLength(0) - 1)
                        {
                            if (roomCode[i + 1, j] != 1)
                            {
                                room.Add(new Face(0, i + 1, 0, j, i + 1, roomHeight, j + 1, colorPatternWall1, null));
                            }
                        }
                        if (j > 0)
                        {
                            if (roomCode[i, j - 1] != 1)
                            {
                                room.Add(new Face((float)Math.PI * 1.5f, i, 0, j, i + 1, roomHeight, j, colorPatternWall1, null));
                            }
                        }
                        if (j < roomCode.GetLength(1) - 1)
                        {
                            if (roomCode[i, j + 1] != 1)
                            {
                                room.Add(new Face((float)Math.PI / 2, i, 0, j + 1, i + 1, roomHeight, j + 1, colorPatternWall1, null));
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
                    if (room[l].x1 == room[k].x3 && room[l].z1 == room[k].z3 && room[k].direction == room[l].direction)
                    {
                        room[k].x3 = room[l].x3;
                        room[k].z3 = room[l].z3;
                        room[k].x4 = room[l].x4;
                        room[k].z4 = room[l].z4;
                        room[k].SetValues();
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
            timeElapsed = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();
            UpdateImage();
            MovePlayer();
            MoveObjects();
            objects.AddRange(objectsToAdd);
            objectsToAdd.Clear();
            foreach (Object obj in objectsToRemove)
            {
                objects.Remove(obj);
            }
            remainingShotCoolDown -= (float)timeElapsed / 1000;

            if (powerupActive >= 0)
            {
                powerupActive -= (float)timeElapsed / 1000;
            }
            else
            {
                DeactivatePowerup();
            }
        }

        public void MoveObjects()
        {
            foreach (Object obj in objects)
            {
                obj.Move();
            }
        }

        public void MovePlayer()
        {
            float horizontal = isADown - isDDown;
            float vertical = isWDown - isSDown;
            if (!(horizontal == 0 && vertical == 0))
            {
                float movementDirection = (float)Math.Atan2(horizontal, vertical) + angle;

                float movementX = (float)Math.Cos(movementDirection);
                float movementZ = (float)Math.Sin(movementDirection);

                float newXPos = playerPositionX + movementX * playerSpeed * timeElapsed / 1000;
                float newZPos = playerPositionZ + movementZ * playerSpeed * timeElapsed / 1000;

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

            playerPositionY += verticalVelocity;
            verticalVelocity -= gravity;
            if (playerPositionY <= 0.5)
            {
                playerPositionY = 0.5f;
                verticalVelocity = 0;
            }
        }

        private List<Face> GetPartRoom(List<Face> room, bool getEverything)
        {
            List<Face> partRoom = new List<Face>();
            foreach (Face face in room)
            {
                partRoom.Add(face);
            }
            foreach (Object o in objects)
            {
                if (!o.isProjectile || getEverything)
                foreach (Face face in o.GetFaces())
                {
                    partRoom.Add(face);
                }
            }
            return partRoom;
        }
        //Genererar en bild utifrån alla pixlar som används, och sätter den sedan som den bild som syns.
        public void UpdateImage()
        {
            Bitmap bmp = new Bitmap(newWidth, newHeight);
            
            Line[] simplifiedRoom = SimplifyRoom(GetPartRoom(currentRoom, true));
            //Color[] previouslayer = null;
            for (int i = 0; i < newWidth; i++)
            {
                for (int j = 0; j < newHeight; j++)
                {
                    List<Face> faces = new List<Face>();
                    foreach (FaceOnLine face in simplifiedRoom[j].faces)
                    {
                        if (face.pos1 - 1 <= i && face.pos2 + 1 >= i)
                        {
                            faces.Add(face.face);
                        }
                    }

                    //För testning
                    //Color color;
                    //if (faces.Count > 0)
                    //{
                    //    color = Color.Red;
                    //    if (CalculatePixel(pixels[i, j], faces).Item2 != null)
                    //    {
                    //        color = Color.Purple;
                    //    }
                    //}
                    //else
                    //{
                    //    color = CalculatePixel(pixels[i, j], faces).Item1;
                    //}
                    //bmp.SetPixel(i, j, color);

                    //Vanlig kod som inte används under testning
                    Color color = CalculatePixel(pixels[i, j], faces).Item1;
                    bmp.SetPixel(i, j, color);
                }
                //Color[] colors = new Color[newHeight];
                //for (int j = 0; j < newHeight; j ++)
                //{
                //    Color color = CalculatePixel(pixels[i, j], simplifiedRoom).Item1;
                //    colors[j] = color;
                //    if (j >= 2)
                //    {
                //        if (colors[j - 2] == color)
                //        {
                //            colors[j - 1] = color;
                //        }
                //        else
                //        {
                //            colors[j - 1] = CalculatePixel(pixels[i, j - 1], simplifiedRoom).Item1;
                //        }
                //    }
                //    //bmp.SetPixel(i, j, color);
                //}
                //for (int j = 0; j < newHeight; j++)
                //{
                //    bmp.SetPixel(i, j, colors[j]);
                //}
                //if (previouslayer != null)
                //{
                //    for (int j = 0; j < newHeight; j += 2)
                //    {
                //        if (previouslayer[j] == colors[j])
                //        {
                //            bmp.SetPixel(i - 1, j, colors[j]);
                //        }
                //        else
                //        {
                //            Color color = CalculatePixel(pixels[i - 1, j], simplifiedRoom).Item1;
                //            bmp.SetPixel(i - 1, j, color);
                //        }
                //        if (j >= 2)
                //        {
                //            if (previouslayer[j - 2] == colors[j])
                //            {
                //                bmp.SetPixel(i - 1, j - 1, colors[j]);
                //            }
                //            else
                //            {
                //                Color color = CalculatePixel(pixels[i - 1, j - 1], simplifiedRoom).Item1;
                //                bmp.SetPixel(i - 1, j - 1, color);
                //            }
                //        }
                //    }
                //}
                //previouslayer = colors;
            }
            //pictureBox1.Image = bmp;
            gameScreen.Image = bmp;
        }

        //Räknar ut vilken punkt som träffas om man drar en linje från spelarens position med vinklar beroende på vilken pixel som kollas.
        public (Color, Face, float, (float, float, float)) CalculatePixel(Pixel pixel, List<Face> room)
        {
            CalculateRatio(pixel.xPos, pixel.yPos, angle, angleVertical, out float xDirection, out float yDirection, out float zDirection, out float xPosition, out float yPosition, out float zPosition);
            return CalculateLine(xDirection, yDirection, zDirection, xPosition, yPosition, zPosition, room, angle);
        }

        public int RotateInList (int newIndex, int length)
        {
            while (newIndex < 0) 
            {
                newIndex += length;
            }
            while (newIndex >= length) 
            {
                newIndex -= length;
            }
            return newIndex;
        }

        public Line[] SimplifyRoom(List<Face> room)
        {
            //List<Face> result = new List<Face>();
            //for (int i = 0; i < newWidth; i += 3)
            //{
            //    Face face = CalculatePixel(pixels[i, newHeight / 2], room).Item2;
            //    if ((!result.Contains(face)) && face != null)
            //    {
            //        result.Add(face);
            //    }
            //}
            //return result;
            //y = kx + m
            //m = y - kx
            Line[] toreturn = new Line[newHeight];
            for (int i = 0; i < newHeight; i++)
            {
                toreturn[i] = new Line(new List<FaceOnLine>());
            }
            foreach (Face face in room)
            {
                (float, float, float) corner1;
                (float, float, float) corner2;
                (float, float, float) corner3;
                (float, float, float) corner4;
                corner1.Item1 = face.x1;
                corner1.Item2 = face.y1;
                corner1.Item3 = face.z1;
                corner2.Item1 = face.x2;
                corner2.Item2 = face.y2;
                corner2.Item3 = face.z2;
                corner3.Item1 = face.x3;
                corner3.Item2 = face.y3;
                corner3.Item3 = face.z3;
                corner4.Item1 = face.x4;
                corner4.Item2 = face.y4;
                corner4.Item3 = face.z4;
                bool isFaceHidden = false;
                List<pointOnScreen> points = new List<pointOnScreen>
                {
                    new pointOnScreen(GetPosOnScreen(corner1.Item1, corner1.Item2, corner1.Item3, false)),
                    new pointOnScreen(GetPosOnScreen(corner2.Item1, corner2.Item2, corner2.Item3, false)),
                    new pointOnScreen(GetPosOnScreen(corner3.Item1, corner3.Item2, corner3.Item3, false)),
                    new pointOnScreen(GetPosOnScreen(corner4.Item1, corner4.Item2, corner4.Item3, false))
                };

                int xLessThanZeroCount = 0;
                List<pointOnScreen> postiveXPoints = new List<pointOnScreen>();
                List<pointOnScreen> negativeXPoints = new List<pointOnScreen>();
                foreach (pointOnScreen pointOnScreen in points)
                {
                    if (pointOnScreen.relativeCamPosX < 0)
                    {
                        xLessThanZeroCount++;
                        negativeXPoints.Add(pointOnScreen);
                    }
                    else
                    {
                        postiveXPoints.Add(pointOnScreen);
                    }
                }
                if (xLessThanZeroCount >= 4)
                {
                    isFaceHidden = true;
                }
                else if (xLessThanZeroCount == 3)
                {
                    //int previousPoint = RotateInList(points.IndexOf(postiveXPoints[0]) - 1, points.Count);
                    //int nextPoint = RotateInList(points.IndexOf(postiveXPoints[0]) + 1, points.Count);
                    //(float, float, float) newPoint = CalculateZeroXBetweenPoints(postiveXPoints[0], points[previousPoint]);
                    //points.Add(new pointOnScreen(GetPosOnScreen(newPoint.Item1, newPoint.Item2, newPoint.Item3, true)));
                    //newPoint = CalculateZeroXBetweenPoints(postiveXPoints[0], points[nextPoint]);
                    //points.Add(new pointOnScreen(GetPosOnScreen(newPoint.Item1, newPoint.Item2, newPoint.Item3, true)));

                    int index = points.IndexOf(postiveXPoints[0]);
                    int previousPoint = RotateInList(index - 1, points.Count);
                    int nextPoint = RotateInList(index + 1, points.Count);

                    (float, float, float) newPoint = CalculateZeroXBetweenPoints(postiveXPoints[0], points[nextPoint]);

                    points[nextPoint] = new pointOnScreen(GetPosOnScreen(newPoint.Item1, newPoint.Item2, newPoint.Item3, true));
                    newPoint = CalculateZeroXBetweenPoints(postiveXPoints[0], points[previousPoint]);
                    points[previousPoint] = new pointOnScreen(GetPosOnScreen(newPoint.Item1, newPoint.Item2, newPoint.Item3, true));
                }
                else if (xLessThanZeroCount == 2)
                {
                    (float, float, float)[] newPoints = new (float, float, float)[2];
                    for (int i = 0; i < 2; i++) 
                    {
                        pointOnScreen point = negativeXPoints[i];
                        pointOnScreen connectingPoint;
                        if (points[RotateInList(points.IndexOf(point) - 1, points.Count)].relativeCamPosX >= 0)
                        {
                            connectingPoint = points[RotateInList(points.IndexOf(point) - 1, points.Count)];
                        }
                        else
                        {
                            connectingPoint = points[RotateInList(points.IndexOf(point) + 1, points.Count)];
                        }
                        newPoints[i] = CalculateZeroXBetweenPoints(point, connectingPoint);
                        //points.Add(new pointOnScreen(GetPosOnScreen(newPoint.Item1, newPoint.Item2, newPoint.Item3)));
                        //int index = points.IndexOf(point);
                        //points.RemoveAt(index);
                        //points.Insert(index, new pointOnScreen(GetPosOnScreen(newPoint.Item1, newPoint.Item2, newPoint.Item3)));
                    }
                    for (int i = 0; i < 2; i++)
                    {
                        int index = points.IndexOf(negativeXPoints[i]);
                        points[index] = new pointOnScreen(GetPosOnScreen(newPoints[i].Item1, newPoints[i].Item2, newPoints[i].Item3, true));
                    }
                }
                else if (xLessThanZeroCount == 1)
                {
                    int index = points.IndexOf(negativeXPoints[0]);
                    pointOnScreen previousPoint = points[RotateInList(index - 1, points.Count)];
                    pointOnScreen nextPoint = points[RotateInList(index + 1, points.Count)];
                    (float, float, float) newPoint = CalculateZeroXBetweenPoints(negativeXPoints[0], nextPoint);
                    points.Insert(index, new pointOnScreen(GetPosOnScreen(newPoint.Item1, newPoint.Item2, newPoint.Item3, true)));
                    newPoint = CalculateZeroXBetweenPoints(negativeXPoints[0], previousPoint);
                    points.Insert(index, new pointOnScreen(GetPosOnScreen(newPoint.Item1, newPoint.Item2, newPoint.Item3, true)));
                }
                if (!isFaceHidden)
                {
                    foreach (pointOnScreen point in negativeXPoints)
                    {
                        points.Remove(point);
                    }
                    negativeXPoints.Clear();
                    postiveXPoints.Clear();


                    StraightLine[] sides = new StraightLine[points.Count];
                    for (int i = 0; i < points.Count - 1; i++)
                    {
                        sides[i] = new StraightLine(points[i].x, points[i].y, points[i + 1].x, points[i + 1].y);
                    }
                    sides[points.Count - 1] = new StraightLine(points[points.Count - 1].x, points[points.Count - 1].y, points[0].x, points[0].y);
                    int lowest = int.MaxValue;
                    int highest = int.MinValue;
                    foreach (StraightLine side in sides)
                    {
                        lowest = (int)Math.Min(lowest, side.lowerY - 1);
                        highest = (int)Math.Max(highest, side.upperY + 1);
                    }
                    if (lowest < 0) { lowest = 0; }
                    if (highest > toreturn.Length) { highest = toreturn.Length; }
                    for (int i = lowest; i < highest; i++)
                    {
                        float leftBound = float.NaN;
                        float rightBound = float.NaN;
                        foreach (StraightLine side in sides)
                        {
                            if (i >= side.lowerY && i <= side.upperY)
                            {
                                if (leftBound != leftBound)
                                {
                                    leftBound = side.k * i + side.m;
                                }
                                else if (rightBound != rightBound)
                                {
                                    {
                                        if (side.k * i + side.m != leftBound)
                                        {
                                            rightBound = side.k * i + side.m;
                                        }
                                    }
                                }
                            }
                        }
                        if (leftBound > rightBound)
                        {
                            (leftBound, rightBound) = (rightBound, leftBound);
                        }
                        //float distance = ((face.x1 - playerPositionX) * (face.x1 - playerPositionX) + (face.y1 - playerPositionY) * (face.y1 - playerPositionY) + (face.z1 - playerPositionZ) * (face.z1 - playerPositionZ) + (face.x2 - playerPositionX) * (face.x2 - playerPositionX) + (face.y2 - playerPositionY) * (face.y2 - playerPositionY) + (face.z2 - playerPositionZ) * (face.z2 - playerPositionZ)) / 2;
                        float distance = 0;
                        if (rightBound == rightBound)
                        {
                            toreturn[i].faces.Add(new FaceOnLine(leftBound, rightBound, face, distance));
                        }
                    }
                }
            }
            return toreturn;

            (float, float, float, float, float) GetPosOnScreen(float x, float y, float z, bool alreadymodified)
            {
                float newX;
                float newY;
                float newZ;

                if (alreadymodified)
                {
                    newX = x;
                    newY = y;
                    newZ = z;
                }
                else
                {
                    float relativeX = x - playerPositionX;
                    float relativeY = -y + playerPositionY;
                    float relativeZ = z - playerPositionZ;
                    //float newY = (float)(relativeY * Math.Cos(-angleVertical) + Math.Sqrt(relativeX * relativeX + relativeZ * relativeZ) * Math.Sin(-angleVertical));
                    //float halfNewZ = (float)Math.Sqrt((relativeX * relativeX + relativeY * relativeY + relativeZ * relativeZ - newY * newY) / (1 + (relativeX * relativeX) / (relativeZ * relativeZ)));
                    //halfNewZ = Math.Abs(halfNewZ) * Math.Sign(relativeZ);
                    //float halfNewX = halfNewZ * relativeX / relativeZ;
                    //float newX = (float)(halfNewX * Math.Cos(-angle) - halfNewZ * Math.Sin(-angle));
                    //float newZ = (float)(halfNewZ * Math.Cos(-angle) +  halfNewX * Math.Sin(-angle));
                    float halfNewX = (float)(relativeX * Math.Cos(-angle) - relativeZ * Math.Sin(-angle));
                    newZ = (float)(relativeX * Math.Sin(-angle) + relativeZ * Math.Cos(-angle));
                    newX = (float)(halfNewX * Math.Cos(angleVertical) - relativeY * Math.Sin(angleVertical));
                    newY = (float)(halfNewX * Math.Sin(angleVertical) + relativeY * Math.Cos(angleVertical));
                }

                //if (newX > 0)
                //{
                //    newZ = (float)(newZ / (imageScale * newX));
                //    newY = (float)(newY / (imageScale * newX));
                //}
                //else if (newX < 0)
                //{
                //    newZ = (float)(newZ * (imageScale * -newX));
                //    newY = (float)(newY * (imageScale * -newX));
                //}
                //newX = 0;
                float leftBound = zxRatioLeft.Item1 * newX + zxRatioLeft.Item2;
                float rightBound = zxRatioRight.Item1 * newX + zxRatioRight.Item2;

                float partHorizontal;
                float partVertical;
                partHorizontal = (newZ - leftBound) / (rightBound - leftBound);
                partVertical = (newY - (yxRatioUp.Item1 * newX + yxRatioUp.Item2)) / ((yxRatioDown.Item1 * newX + yxRatioDown.Item2) - (yxRatioUp.Item1 * newX + yxRatioUp.Item2));
                return (partHorizontal * newWidth, partVertical * newHeight, newX, newY, newZ);
            }

            (float, float, float) CalculateZeroXBetweenPoints(pointOnScreen point1, pointOnScreen point2)
            {
                float k = (point2.relativeCamPosZ - point1.relativeCamPosZ) / (point2.relativeCamPosX - point1.relativeCamPosX);
                float zPos = point1.relativeCamPosZ - k * point1.relativeCamPosX;
                k = (point2.relativeCamPosY - point1.relativeCamPosY) / (point2.relativeCamPosX - point1.relativeCamPosX);
                float yPos = point1.relativeCamPosY - k * point1.relativeCamPosX;
                return (0, yPos, zPos);
            }
        }


        public (Color, Face, float, (float, float, float)) CalculateLine(float xDirection, float yDirection, float zDirection, float xPosition, float yPosition, float zPosition, List<Face> faces, float angle)
        {
            Face currentClosest = null;
            float proximity = 1000000;
            float relativeHitFromLower = 0;
            float relativeHitY = 0;
            float hitX = 0;
            float hitY = 0;
            float hitZ = 0;

            void AfterXYZ(Face face)
            {
                if (hitY <= face.y3 && hitY >= face.y1)
                {
                    float direction = (float)Math.Atan2(hitZ - zPosition, hitX - xPosition);
                    if (Math.Abs(direction - angle) <= Math.PI / 2 || Math.Abs(direction + Math.PI * 2 - angle) <= Math.PI / 2)
                    {
                        if (Math.Pow(hitX - xPosition, 2) + Math.Pow(hitZ - zPosition, 2) + Math.Pow(hitY - yPosition, 2) < proximity)
                        {
                            currentClosest = face;
                            proximity = (float)Math.Pow(hitX - xPosition, 2) + (float)Math.Pow(hitZ - zPosition, 2) + (float)Math.Pow(hitY - yPosition, 2);
                            relativeHitFromLower = (float)Math.Sqrt((hitX - face.x1) * (hitX - face.x1) + (hitZ - face.z1) * (hitZ - face.z1));
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
                else if (face.y1 == face.y3)
                {
                    if (face.higherFunction1.Item1 == 0 || face.higherFunction2.Item1 == 0 || face.lowerFunction1.Item1 == 0 || face.lowerFunction2.Item1 == 0)
                    {
                        hitY = face.y1;
                        hitX = (hitY * (xDirection / yDirection) + xPosition) - ((xDirection / yDirection) * yPosition);
                        if (hitX <= face.UpperX && hitX >= face.lowerX)
                        {
                            hitZ = (hitY * (zDirection / yDirection) + zPosition) - ((zDirection / yDirection) * yPosition);
                            if (hitZ <= face.UpperZ && hitZ >= face.lowerZ)
                            {
                                float direction = (float)Math.Atan2(hitZ - zPosition, hitX - xPosition);
                                if (Math.Abs(direction - angle) <= Math.PI / 2 || Math.Abs(direction + Math.PI * 2 - angle) <= Math.PI / 2)
                                {
                                    if (Math.Pow(hitX - xPosition, 2) + Math.Pow(hitZ - zPosition, 2) + Math.Pow(hitY - yPosition, 2) < proximity)
                                    {
                                        currentClosest = face;
                                        proximity = (float)Math.Pow(hitX - xPosition, 2) + (float)Math.Pow(hitZ - zPosition, 2) + (float)Math.Pow(hitY - yPosition, 2);
                                        if (face.lowerFunction1.Item1 == 0)
                                        {
                                            relativeHitFromLower = Math.Abs(hitZ - face.z2);
                                            relativeHitY = Math.Abs(hitX - face.x3);
                                        }
                                        else
                                        {
                                            relativeHitFromLower = Math.Abs(hitX - face.x3);
                                            relativeHitY = Math.Abs(hitZ - face.z2);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        hitY = face.y1;
                        hitX = (hitY * (xDirection / yDirection) + xPosition) - ((xDirection / yDirection) * yPosition);
                        hitZ = (hitY * (zDirection / yDirection) + zPosition) - ((zDirection / yDirection) * yPosition);
                        if (hitZ <= face.higherFunction1.Item1 * hitX + face.higherFunction1.Item2)
                        {
                            if (hitZ <= face.higherFunction2.Item1 * hitX + face.higherFunction2.Item2)
                            {
                                if (hitZ >= face.lowerFunction1.Item1 * hitX + face.lowerFunction1.Item2)
                                {
                                    if (hitZ >= face.lowerFunction2.Item1 * hitX + face.lowerFunction2.Item2)
                                    {
                                        float direction = (float)Math.Atan2(hitZ - zPosition, hitX - xPosition);
                                        if (Math.Abs(direction - angle) <= Math.PI / 2 || Math.Abs(direction + Math.PI * 2 - angle) <= Math.PI / 2)
                                        {
                                            if (Math.Pow(hitX - xPosition, 2) + Math.Pow(hitZ - zPosition, 2) + Math.Pow(hitY - yPosition, 2) < proximity)
                                            {
                                                currentClosest = face;
                                                proximity = (float)Math.Pow(hitX - xPosition, 2) + (float)Math.Pow(hitZ - zPosition, 2) + (float)Math.Pow(hitY - yPosition, 2);
                                                float tempk = face.lowerFunction2.Item1;
                                                float tempm = hitZ - tempk * hitX;
                                                float intersectX = (face.lowerFunction1.Item2 - tempm) / (tempk - face.lowerFunction1.Item1);
                                                float intersectZ = tempk * intersectX + tempm;
                                                relativeHitFromLower = (float)Math.Sqrt(Math.Pow(intersectX - hitX, 2) + Math.Pow(intersectZ - hitZ, 2));
                                                tempk = face.lowerFunction1.Item1;
                                                tempm = hitZ - tempk * hitX;
                                                intersectX = (face.lowerFunction2.Item2 - tempm) / (tempk - face.lowerFunction2.Item1);
                                                intersectZ = tempk * intersectX + tempm;
                                                relativeHitY = (float)Math.Sqrt(Math.Pow(intersectX - hitX, 2) + Math.Pow(intersectZ - hitZ, 2));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else if (Math.Abs(xDirection) <= 0.00001d)
                {
                    //MessageBox.Show("BBBBBBBBBBBB");
                    if (face.zxRatio <= 0.01d && face.zxRatio >= -0.01d)
                    {
                        //MessageBox.Show("AAAAAAAAAAAAAA");
                        hitX = xPosition;
                        if (hitX <= face.UpperX && hitX >= face.lowerX)
                        {
                            hitZ = face.z1;
                            hitY = (yDirection / zDirection) * (hitZ - zPosition) + yPosition;
                            AfterXYZ(face);
                        }
                    }
                    else
                    {
                        hitZ = (face.lowerX - ((1 / face.zxRatio) * face.LowerXZ) - (xPosition - (xDirection / zDirection * zPosition))) / ((xDirection / zDirection) - (1 / face.zxRatio));
                        if (hitZ <= face.UpperZ && hitZ >= face.lowerZ)
                        {
                            hitX = (hitZ * (xDirection / zDirection) + xPosition) - ((xDirection / zDirection) * zPosition);
                            if (hitX <= face.UpperX + 0.01d && hitX >= face.lowerX - 0.01d)
                            {
                                hitY = (yDirection / zDirection) * (hitZ - zPosition) + yPosition;
                                AfterXYZ(face);
                            }
                        }
                    }
                }
                else if (face.x1 == face.x3)
                {
                    if (Math.Abs(zDirection) <= 0.00001d)
                    {
                        hitX = face.x1;
                        hitZ = zPosition;
                        if (hitZ <= face.UpperZ && hitZ >= face.lowerZ)
                        {
                            hitY = (yDirection / xDirection) * (hitX - xPosition) + yPosition;
                            AfterXYZ(face);
                        }
                    }
                    else
                    {
                        hitZ = (face.lowerX - (xPosition - (xDirection / zDirection * zPosition))) / ((xDirection / zDirection));
                        if (hitZ <= face.UpperZ && hitZ >= face.lowerZ)
                        {
                            //hitX = (hitZ * (xDirection / zDirection) + xPosition) - ((xDirection / zDirection) * zPosition);
                            hitX = face.x1;
                            if (hitX <= face.UpperX + 0.01d && hitX >= face.lowerX - 0.01d)
                            {
                                hitY = (yDirection / zDirection) * (hitZ - zPosition) + yPosition;
                                AfterXYZ(face);
                            }
                        }
                    }
                }
                else
                {
                    hitX = (face.lowerZ - (face.zxRatio * face.LowerZX) - (zPosition - (zDirection / xDirection * xPosition))) / ((zDirection / xDirection) - face.zxRatio);
                    if (hitX <= face.UpperX && hitX >= face.lowerX)
                    {
                        hitZ = (hitX * (zDirection / xDirection) + zPosition) - ((zDirection / xDirection) * xPosition);
                        if (hitZ <= face.UpperZ + 0.01d && hitZ >= face.lowerZ - 0.01d)
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
                //    float hitZ = (face.x1 - xPosition) * zDirection / xDirection + zPosition;
                //    float direction = Math.Atan2(hitZ - playerPositionZ, face.x1 - playerPositionX);
                //    if (Math.Abs(direction - angle) < Math.PI / 2 || Math.Abs(direction + Math.PI * 2 - angle) < Math.PI / 2)
                //    {
                //        if (face.z1 <= hitZ && hitZ <= face.z2)
                //        {
                //            float hitY = (face.x1 - xPosition) * yDirection / xDirection + yPosition;
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
                //    float hitX = (face.z1 - zPosition) * xDirection / zDirection + xPosition;
                //    float direction = Math.Atan2(face.z1 - playerPositionZ, hitX - playerPositionX);
                //    if (Math.Abs(direction - angle) < Math.PI / 2 || Math.Abs(direction + Math.PI * 2 - angle) < Math.PI / 2)
                //    {
                //        if (face.x1 <= hitX && hitX <= face.x3)
                //        {
                //            float hitY = (face.z1 - zPosition) * yDirection / zDirection + yPosition;
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
                if (currentClosest.height == 0)
                {
                    currentClosest.height = 1;
                }
                if (currentClosest.length == 0)
                {
                    currentClosest.length = 1;
                }
                float patternX = relativeHitFromLower % currentClosest.length;
                float patternY = relativeHitY % currentClosest.height;
                int pixelX = (int)(currentClosest.picture.pattern.GetLength(0) * patternX / currentClosest.length);
                int pixelY = (int)(currentClosest.picture.pattern.GetLength(1) * patternY / currentClosest.height);
                int colorIndex = currentClosest.picture.pattern[pixelX, pixelY];
                Color color = currentClosest.picture.colors[colorIndex];
                if (currentClosest.parent != null)
                {
                    if (currentClosest.parent.isHurt > 0)
                    {
                        int newAlpha = color.A - 150;
                        if (newAlpha < 0)
                        {
                            newAlpha = 0;
                        }
                        color = Color.FromArgb(newAlpha, color.R, color.G, color.B);
                    }
                }
                return (color, currentClosest, proximity, (hitX, hitY, hitZ));
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
                float direction = (float)Math.Atan2(hitZ - playerPositionZ, hitX - playerPositionX);
                if (!(Math.Abs(direction - angle) < Math.PI / 2 || Math.Abs(direction + Math.PI * 2 - angle) < Math.PI / 2))
                {
                    hitX = -yPosition * xDirection / yDirection + xPosition;
                    hitZ = -yPosition * zDirection / yDirection + zPosition;
                    pattern = colorPatternFloor1;
                }
                if (Math.Abs(hitX) == float.PositiveInfinity)
                {
                    hitX = 0;
                    hitZ = 0;
                }
                float patternX = Math.Abs(hitX) % 1;
                float patternY = Math.Abs(hitZ) % 1;
                int pixelX = (int)((pattern.pattern.GetLength(0) - 1) * patternX);
                int pixelY = (int)((pattern.pattern.GetLength(1) - 1) * patternY);
                int colorIndex = pattern.pattern[pixelX, pixelY];
                return (pattern.colors[colorIndex], currentClosest, float.MaxValue, (0, 0, 0));
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
        public void CalculateRatio(float localXPos, float localYPos, float angle, float angleVertical, out float xDirection, out float yDirection, out float zDirection, out float xPosition, out float yPosition, out float zPosition)
        {
            float baseYPosition = (float)Math.Cos(angleVertical) * localYPos;
            float baseXPosition;
            float baseZPosition;
            //baseXPosition = localYPos * Math.Sin(angleVertical) / Math.Sqrt(1 + Math.Tan(angle) * Math.Tan(angle));
            //baseZPosition = localYPos * Math.Sin(angleVertical) / Math.Sqrt(1 + (1 / (Math.Tan(angle) * Math.Tan(angle))));
            baseXPosition = -(float)Math.Cos(angle) * (float)Math.Sin(angleVertical) * localYPos;
            baseZPosition = -(float)Math.Sin(angle) * (float)Math.Sin(angleVertical) * localYPos;
            baseXPosition += localXPos * (float)Math.Sin(angle);
            baseZPosition += localXPos * -(float)Math.Cos(angle);
            float projectedXPosition = baseXPosition * imageScale;
            float projectedYPosition = baseYPosition * imageScale;
            float projectedZPosition = baseZPosition * imageScale;
            float y = (float)Math.Sin(angleVertical);
            float h = (float)Math.Sqrt(1 - y * y);
            float x = (float)Math.Cos(angle) * h;
            //if (angle < 180)
            //{
            //    x = -x;
            //}
            float z = (float)Math.Sin(angle) * h;
            //if (angle < 90)
            //{
            //    z = -z;
            //}
            //float x = 1 / ((1 + a2) * (c2 - 1));
            //float y = c * x * x * Math.Sqrt(1 + a2);
            //float z = a * x;
            projectedXPosition += x;
            projectedYPosition += y;
            projectedZPosition += z;
            xDirection = projectedXPosition - baseXPosition;
            yDirection = projectedYPosition - baseYPosition;
            zDirection = projectedZPosition - baseZPosition;
            xPosition = baseXPosition + playerPositionX;
            yPosition = baseYPosition + playerPositionY;
            zPosition = baseZPosition + playerPositionZ;
            //float baseXPosition = localXPos * Math.Sin(angle);
            //float baseZPosition = localXPos * -Math.Cos(angle);
            //float baseYPosition = localYPos;
            //float projectedXPosition = Math.Cos(angle) + baseXPosition * imageScale;
            //float projectedZPosition = Math.Sin(angle) + baseZPosition * imageScale;
            //float projectedYPosition = baseYPosition * imageScale;
            //xDirection = projectedXPosition - baseXPosition;
            //yDirection = projectedYPosition - baseYPosition;
            //zDirection = projectedZPosition - baseZPosition;
            //xPosition = baseXPosition + playerPositionX;
            //yPosition = baseYPosition + playerPositionY;
            //zPosition = baseZPosition + playerPositionZ;
            //float verticalAngle = Math.Atan2(localYPos * (imageScale - 1), 1) * 180 / Math.PI;
            //float horizontalAngle = Math.Atan2(localXPos * (imageScale - 1), 1) * 180 / Math.PI + angle;
            //float a = Math.Tan(verticalAngle * Math.PI / 180);
            //float c = Math.Tan(horizontalAngle * Math.PI / 180);
            //float a2 = a * a;
            //float c2 = c * c;
            //float d = c2 + a2 * (c2 + 1) + 1;
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
                angle += (float)Math.PI / 16;
                FixPlayerRotation();
            }
            if (e.KeyCode == Keys.Right)
            {
                angle -= (float)Math.PI / 16;
                FixPlayerRotation();
            }
            if (e.KeyCode == Keys.Escape)
            {
                if (controlCursor)
                {
                    ReleaseCursor();
                }
                else
                {
                    FixCursor();
                }
            }
            if (e.KeyCode == Keys.Space)
            {
                if (playerPositionY == 0.5)
                {
                    verticalVelocity = jumpForce;
                }
            }

        }

        bool isCursorHidden = false;
        private void FixCursor()
        {
            Cursor.Clip = new Rectangle(Bounds.X + 30, Bounds.Y + 50, Bounds.Width - 60, Bounds.Height - 80);
            controlCursor = true;
            if (!isCursorHidden)
            {
                Cursor.Hide();
                isCursorHidden = true;
            }
        }
        private void ReleaseCursor()
        {
            Cursor.Clip = Rectangle.Empty;
            controlCursor = false;
            if (isCursorHidden)
            {
                Cursor.Show();
                isCursorHidden = false;
            }
        }

        public float FixAngle(float angle)
        {
            while (angle < 0)
            {
                angle += 2 * (float)Math.PI;
            }
            while (angle > Math.PI * 2)
            {
                angle -= 2 * (float)Math.PI;
            }
            return angle;
        }

        //Metod som ser till att vinkeln håller sig mellan 0 och 180.
        private void FixPlayerRotation()
        {
            angle = FixAngle(angle);
            angleVertical = FixAngle(angleVertical);
            if (angleVertical > Math.PI / 5 && angleVertical < Math.PI)
            {
                angleVertical = (float)Math.PI / 5;
            }
            if (angleVertical < Math.PI * 9 / 5 && angleVertical >= Math.PI)
            {
                angleVertical = (float)Math.PI * 9 / 5;
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

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            if (controlCursor)
            {
                angle -= ((float)Cursor.Position.X - Location.X - width / 2) / 300;
                angleVertical -= ((float)Cursor.Position.Y - Location.Y - height / 2) / 300;
                FixPlayerRotation();
                Cursor.Position = new Point(Location.X + width / 2, Location.Y + height / 2);
            }
        }

        private void gameScreen_Click(object sender, EventArgs e)
        {
            Shoot();
        }
    }


    //Varje pixel förvarar data om åt vilket håll de ska "titta" mot.
    public class Pixel
    {
        public float angleHorizontal;
        public float angleVertical;
        public float xPos;
        public float yPos;

        public Pixel(float xPos, float yPos)
        {

            this.xPos = xPos;
            this.yPos = yPos;
        }
    }

    //Data om varje vägg.
    public class Face
    {
        public float direction;
        public float x1;
        public float y1;
        public float z1;
        public float x2;
        public float y2;
        public float z2;
        public float x3;
        public float y3;
        public float z3;
        public float x4;
        public float y4;
        public float z4;
        public float lowerX;
        public float lowerZ;
        public float LowerXZ;
        public float LowerZX;
        public float UpperX;
        public float UpperZ;
        public float midX;
        public float midZ;
        public float zxRatio;
        public float length;
        public float height;
        public Picture picture;
        public Object parent;

        public (float, float) higherFunction1;
        public (float, float) higherFunction2;
        public (float, float) lowerFunction1;
        public (float, float) lowerFunction2;

        public Face(float direction, float x1, float y1, float z1, float x2, float y2, float z2, Picture picture, Object parent)
        {
            this.parent = parent;
            this.direction = direction;
            this.x1 = x1;
            this.y1 = y1;
            this.z1 = z1;
            this.x2 = x1;
            this.y2 = y2;
            this.z2 = z1;
            this.x3 = x2;
            this.y3 = y2;
            this.z3 = z2;
            this.x4 = x2;
            this.y4 = y1;
            this.z4 = z2;
            midX = (x2 + x1) / 2;
            midZ = (z2 + z1) / 2;
            this.picture = picture;
            if (x2 == x1)
            {
                zxRatio = 1;
            }
            else
            {
                zxRatio = (z2 - z1) / (x2 - x1);
            }
            length = (float)Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(z2 - z1, 2));
            height = y2 - y1;
            SetValues();
        }

        public Face(float y, float x1, float z1, float x2, float z2, float x3, float z3, float x4, float z4, Picture picture, Object parent)
        {
            this.parent = parent;
            this.picture = picture;
            y1 = y;
            y2 = y;
            y3 = y;
            y4 = y;
            this.x1 = x1;
            this.x2 = x2;
            this.x3 = x3;
            this.x4 = x4;
            this.z1 = z1;
            this.z2 = z2;
            this.z3 = z3;
            this.z4 = z4;
            length = (float)Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(z2 - z1, 2));
            height = length;
            float tempk = (z2 - z1) / (x2 - x1);
            float tempm = z1 - tempk * x1;
            if (z1 > z4)
            {
                higherFunction1 = (tempk, tempm);
                lowerFunction1 = (tempk, z4 - tempk * x4);
            }
            else
            {
                lowerFunction1 = (tempk, tempm);
                higherFunction1 = (tempk, z4 - tempk * x4);
            }
            tempk = (z1 - z4) / (x1 - x4);
            tempm = z4 - tempk * x4;
            if (z4 > z3)
            {
                higherFunction2 = (tempk, tempm);
                lowerFunction2 = (tempk, z3 - tempk * x3);
            }
            else
            {
                lowerFunction2 = (tempk, tempm);
                higherFunction2 = (tempk, z3 - tempk * x3);
            }
            SetValues();

        }

        public void SetValues()
        {
            if (x1 < x3)
            {
                lowerX = x1;
                LowerXZ = z1;
                UpperX = x3;
            }
            else
            {
                lowerX = x3;
                LowerXZ = z3;
                UpperX = x1;
            }
            if (z1 < z3)
            {
                lowerZ = z1;
                LowerZX = x1;
                UpperZ = z3;
            }
            else
            {
                lowerZ = z3;
                LowerZX = x3;
                UpperZ = z1;
            }
        }
    }

    public class Object
    {
        public float positionX;
        public float positionY = 0.4f;
        public float positionZ;
        public float angle;
        public float additionalShownAngle = 0;
        public float turningSpeed;
        public float movementSpeed;
        public float lifeTime;
        public int difficulty;
        public float maxCooldown;
        public float coolDownRemaining = 0;
        public float isHurt = 0;
        public int hp;
        public bool isEnemy = false;
        public bool isProjectile = false;
        public bool isPickUp = false;
        public bool isHeal = false;
        public bool isPowerup = false;
        public bool isNextLevel = false;
        public bool isGoal = false;
        public Picture pickUpPattern;
        public Form1 main;

        public Object(int type, float positionX, float positionZ, float angle, Form1 main, float positionY = 0.4f)
        {
            this.positionX = positionX;
            this.positionY = positionY;
            this.positionZ = positionZ;
            this.angle = angle;
            switch (type)
            {
                case 11:
                    difficulty = 1;
                    isEnemy = true;
                    break;
                case 12:
                    difficulty = 2;
                    isEnemy = true;
                    break;
                case 13:
                    difficulty = 3;
                    isEnemy = true;
                    break;
                case 21:
                    difficulty = 1;
                    isProjectile = true;
                    break;
                case 22:
                    difficulty = 2;
                    isProjectile = true;
                    break;
                case 23:
                    difficulty = 3;
                    isProjectile = true;
                    break;
                case 31:
                    isPickUp = true;
                    isHeal = true;
                    pickUpPattern = main.colorPatternHeal;
                    break;
                case 32:
                    isPickUp = true;
                    isPowerup = true;
                    pickUpPattern = main.colorPatternYellow;
                    break;
                case 33:
                    isPickUp = true;
                    isNextLevel = true;
                    pickUpPattern = main.colorPatternGreen;
                    break;
                case 34:
                    isPickUp = true;
                    isGoal = true;
                    pickUpPattern = main.colorPatternGoal;
                    break;


            }
            if (isEnemy)
            {
                switch (difficulty)
                {
                    case 1:
                        turningSpeed = 1.5f;
                        maxCooldown = 2f;
                        hp = 1;
                        break;
                    case 2:
                        turningSpeed = 2.5f;
                        maxCooldown = 1f;
                        hp = 3; 
                        break;
                    case 3:
                        turningSpeed = 5f;
                        maxCooldown = 0.67f;
                        hp = 5;
                        break;
                }
            }
            else if (isProjectile)
            {
                switch (difficulty)
                {
                    case 1:
                        movementSpeed = 0.5f;
                        break;
                    case 2:
                        movementSpeed = 1f;
                        break;
                    case 3:
                        movementSpeed = 1f;
                        break;
                }
            }
            else if (isPickUp)
            {
                turningSpeed = 1f;
            }

            this.main = main;
        }

        public void Shoot()
        {
            coolDownRemaining -= (float)main.timeElapsed / 1000;
            if (coolDownRemaining <= 0)
            {
                switch(difficulty)
                {
                    case 1:
                        main.objectsToAdd.Add(new Object(21, positionX, positionZ, angle, main));
                        break;
                    case 2:
                        main.objectsToAdd.Add(new Object(22, positionX, positionZ, angle, main));
                        break;
                    case 3:
                        main.objectsToAdd.Add(new Object(23, positionX, positionZ, angle, main));
                        main.objectsToAdd.Add(new Object(23, positionX, positionZ, main.FixAngle(angle + 0.3f), main));
                        main.objectsToAdd.Add(new Object(23, positionX, positionZ, main.FixAngle(angle - 0.3f), main));
                        break;

                }
                coolDownRemaining = maxCooldown;
            }
        }

        public void TakeDamage(int amount)
        {
            hp -= amount;
            isHurt = 0.3f;
            if (hp <= 0)
            {
                main.objectsToRemove.Add(this);
                main.isEnemiesInRoom = main.CheckForEnemies();
            }
        }

        public void PickUpEffect()
        {
            if (isHeal)
            {
                main.TakeDamage(-3);
            }
            else if (isPowerup)
            {
                main.ActivatePowerup();
            }
            else if (isNextLevel)
            {
                main.StartNextRoom();
            }
            main.objectsToRemove.Add(this);
        }

        public void Move()
        {
            if (isHurt > 0)
            {
                isHurt -= (float)main.timeElapsed / 1000;
            }
            else
            {
                isHurt = 0;
            }
            if (isEnemy)
            {
                float angleToPlayer = main.FixAngle((float)Math.Atan2(main.playerPositionZ - positionZ, main.playerPositionX - positionX));
                var lineHit = main.CalculateLine(main.playerPositionX - positionX, 0.0001f, main.playerPositionZ - positionZ, positionX, Form1.roomHeight / 2, positionZ, main.currentRoom, main.FixAngle(angleToPlayer));
                float distanceToWall = lineHit.Item3;
                if (distanceToWall >= (main.playerPositionX - positionX) * (main.playerPositionX - positionX) + (main.playerPositionZ - positionZ) * (main.playerPositionZ - positionZ))
                {
                    Shoot();
                    float angleDistance = Math.Abs(angleToPlayer - angle);
                    if (angleDistance < turningSpeed * main.timeElapsed / 1000 || Math.PI * 2 - angleToPlayer + angle < turningSpeed * main.timeElapsed / 1000)
                    {
                        angle = angleToPlayer;
                    }
                    else
                    {
                        if (angleToPlayer < angle)
                        {
                            if (angleDistance < Math.PI)
                            {
                                angle -= turningSpeed * main.timeElapsed / 1000;
                            }
                            else
                            {
                                angle += turningSpeed * main.timeElapsed / 1000;
                            }
                        }
                        else
                        {
                            if (angleDistance < Math.PI)
                            {
                                angle += turningSpeed * main.timeElapsed / 1000;
                            }
                            else
                            {
                                angle -= turningSpeed * main.timeElapsed / 1000;
                            }
                        }
                    }
                }
            }
            else if (isProjectile)
            {
                positionX += (float)Math.Cos(angle) * movementSpeed * main.timeElapsed / 1000;
                positionZ += (float)Math.Sin(angle) * movementSpeed * main.timeElapsed / 1000;
                if ((main.playerPositionX - positionX) * (main.playerPositionX - positionX) + (main.playerPositionY - positionY) * (main.playerPositionY - positionY) + (main.playerPositionZ - positionZ) * (main.playerPositionZ - positionZ) < main.hitBox * main.hitBox)
                {
                    main.TakeDamage(1);
                    main.objectsToRemove.Add(this);
                }
                if (positionX < 0 || positionZ < 0 || positionX > main.currentRoomCode.GetLength(0) || positionZ > main.currentRoomCode.GetLength(1))
                {
                    main.objectsToRemove.Add(this);
                }
            }
            else if (isPickUp)
            {
                angle += turningSpeed * main.timeElapsed / 1000;
                if ((main.playerPositionX - positionX) * (main.playerPositionX - positionX) + (main.playerPositionY - positionY) * (main.playerPositionY - positionY) + (main.playerPositionZ - positionZ) * (main.playerPositionZ - positionZ) < main.hitBox * main.hitBox)
                {
                    if ((isGoal || isNextLevel) && main.isEnemiesInRoom)
                    {

                    }
                    else
                    {
                        PickUpEffect();
                    }
                }
            }

            angle = main.FixAngle(angle);
        }

        public Face[] GetFaces()
        {
            if (isEnemy)
            {
                switch (difficulty)
                {
                    case 1:
                        return GenerateCuboid(positionX, positionY, positionZ, 0.3f, 0.3f, angle, new Picture[] { main.colorPatternEnemyFace1, main.colorPatternEnemySide1, main.colorPatternEnemySide1, main.colorPatternEnemySide1, main.colorPatternEnemySide1, main.colorPatternEnemySide1 });
                    case 2:
                        return GenerateCuboid(positionX, positionY, positionZ, 0.3f, 0.3f, angle, new Picture[] { main.colorPatternEnemyFace2, main.colorPatternEnemySide2, main.colorPatternEnemySide2, main.colorPatternEnemySide2, main.colorPatternEnemySide2, main.colorPatternEnemySide2 });
                    case 3:
                        return GenerateCuboid(positionX, positionY, positionZ, 0.3f, 0.3f, angle, new Picture[] { main.colorPatternEnemyFace3, main.colorPatternEnemySide3, main.colorPatternEnemySide3, main.colorPatternEnemySide3, main.colorPatternEnemySide3, main.colorPatternEnemySide3 });
                }
            }
            else if (isProjectile)
            {
                return GenerateCuboid(positionX, positionY, positionZ, 0.1f, 0.1f, angle, new Picture[] { main.colorPatternProjectile1, main.colorPatternProjectile1, main.colorPatternProjectile1, main.colorPatternProjectile1, main.colorPatternProjectile1, main.colorPatternProjectile1 });
            }
            else if (isPickUp)
            {
                if ((isGoal || isNextLevel) && main.isEnemiesInRoom)
                {
                    return GenerateCuboid(positionX, positionY, positionZ, 0.3f, 0.3f, angle, new Picture[] { main.colorPatternLocked, main.colorPatternLocked, main.colorPatternLocked, main.colorPatternLocked, main.colorPatternLocked, main.colorPatternLocked });
                }
                return GenerateCuboid(positionX, positionY, positionZ, 0.3f, 0.3f, angle, new Picture[] { pickUpPattern, pickUpPattern, pickUpPattern, pickUpPattern, pickUpPattern, pickUpPattern });
            }
            
            return null;
        }

        public Face[] GenerateCuboid(float xPos, float yPos, float zPos, float height, float length, float angle, Picture[] pictures)
        {
            Face[] toReturn = new Face[6];
            for (int i = 0; i < 4; i++)
            {
                float relativeXPosition1 = (float)(Math.Cos(angle) + (float)Math.Sin(angle)) * 0.5f * length; //1  //1  //-1
                float relativeZPosition1 = (float)(Math.Sin(angle) - (float)Math.Cos(angle)) * 0.5f * length; //-1 //1  //1
                float relativeXPosition2 = (float)(Math.Cos(angle) - (float)Math.Sin(angle)) * 0.5f * length; //1  //-1 //-1
                float relativeZPosition2 = (float)(Math.Sin(angle) + (float)Math.Cos(angle)) * 0.5f * length; //1  //1  //-1

                toReturn[i] = new Face(angle, relativeXPosition1 + xPos, yPos - 0.5f * height, relativeZPosition1 + zPos, relativeXPosition2 + xPos, yPos + 0.5f * height, relativeZPosition2 + zPos, pictures[i], this);
                angle += (float)Math.PI / 2;
                if (angle >= Math.PI * 2)
                {
                    angle -= (float)Math.PI * 2;
                }
            }
            for (int i = 4; i < 6; i++)
            {
                (float, float) relativePosition1 = GetValuesAndIncreaseAngle();
                (float, float) relativePosition2 = GetValuesAndIncreaseAngle();
                (float, float) relativePosition3 = GetValuesAndIncreaseAngle();
                (float, float) relativePosition4 = GetValuesAndIncreaseAngle();
                if (i == 4)
                {
                    toReturn[i] = new Face(yPos + 0.5f * height, xPos + relativePosition1.Item1, zPos + relativePosition1.Item2, xPos + relativePosition2.Item1, zPos + relativePosition2.Item2, xPos + relativePosition3.Item1, zPos + relativePosition3.Item2, xPos + relativePosition4.Item1, zPos + relativePosition4.Item2, pictures[i], this);
                }
                else
                {
                    toReturn[i] = new Face(yPos - 0.5f * height, xPos + relativePosition1.Item1, zPos + relativePosition1.Item2, xPos + relativePosition2.Item1, zPos + relativePosition2.Item2, xPos + relativePosition3.Item1, zPos + relativePosition3.Item2, xPos + relativePosition4.Item1, zPos + relativePosition4.Item2, pictures[i], this);
                }
            }
            (float, float) GetValuesAndIncreaseAngle()
            {
                angle += (float)Math.PI / 2;
                if (angle >= Math.PI * 2)
                {
                    angle -= (float)Math.PI * 2;
                }
                return ((float)(Math.Cos(angle) + Math.Sin(angle)) * 0.5f * length, (float)(Math.Sin(angle) - Math.Cos(angle)) * 0.5f * length);
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
                    Color color = baseImage.GetPixel(i, baseImage.Height - j - 1);
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

    public struct FaceOnLine
    {
        public float pos1;
        public float pos2;
        public Face face;
        public float distance;

        public FaceOnLine(float pos1, float pos2, Face face, float distance)
        {
            this.pos1 = pos1;
            this.pos2 = pos2;
            this.face = face;
            this.distance = distance;
        }
    }

    public struct pointOnScreen
    {
        public float x;
        public float y;
        public float relativeCamPosX;
        public float relativeCamPosY;
        public float relativeCamPosZ;
        public pointOnScreen((float, float, float, float, float) position)
        {
            x = position.Item1;
            y = position.Item2;
            relativeCamPosX = position.Item3;
            relativeCamPosY = position.Item4;
            relativeCamPosZ = position.Item5;
        }
    }

    public struct StraightLine
    {
        public float k;
        public float m;
        public float lowerX;
        public float upperX;
        public float lowerY;
        public float upperY;
        public StraightLine(float x1, float y1, float x2, float y2)
        {
            if (y2 == y1)
            {
                k = 0f;
            }
            else
            {
                k = (x2 - x1) / (y2 - y1);
            }
            m = x1 - k * y1;
            if (x1 < x2)
            {
                lowerX = x1;
                upperX = x2;
            }
            else
            {
                lowerX = x2;
                upperX = x1;
            }
            if (y1 < y2)
            {
                lowerY = y1;
                upperY = y2;
            }
            else
            {
                lowerY = y2;
                upperY = y1;
            }
        }
    }

    public struct Line
    {
        public List<FaceOnLine> faces;

        public Line(List<FaceOnLine> faces)
        {
            this.faces = faces;
        }
    }
}
