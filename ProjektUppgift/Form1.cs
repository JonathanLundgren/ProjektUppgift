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
        const int size = 75;
        Picture[,] pictures = new Picture[size, size];
        Random rand = new Random();
        public Form1()
        {
            InitializeComponent();

            for (int i = 0; i < size; i++) 
            {
                for (int j = 0; j < size; j++) 
                {
                    pictures[i, j] = new Picture(i, j);
                    Controls.Add(pictures[i, j].pictureBox);
                }
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    pictures[i,j].pictureBox.BackColor = Color.FromArgb(rand.Next(256), rand.Next(256), rand.Next(256));
                }
            }
        }
    }

    public class Picture
    {
        const int size = 10;
        int posX;
        int posY;
        public Label pictureBox;
        Random rand;

        public Picture(int x, int y) 
        {
            posX = x;
            posY = y;
            pictureBox = new Label();
            pictureBox.Location = new Point(posX*size, posY*size);
            pictureBox.Size = new Size(size, size);
            rand = new Random(posX + posY * 1000);
        }

        public void ColorChange()
        {
            pictureBox.BackColor = Color.FromArgb(rand.Next(256), rand.Next(256), rand.Next(256));
        }
    }
}
