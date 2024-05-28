namespace ProjektUppgift
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.gameTimer = new System.Windows.Forms.Timer(this.components);
            this.HUD = new System.Windows.Forms.PictureBox();
            this.hpLabel = new System.Windows.Forms.Label();
            this.powerupLabel = new System.Windows.Forms.Label();
            this.Crosshair = new System.Windows.Forms.PictureBox();
            this.gameScreen = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.HUD)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.Crosshair)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gameScreen)).BeginInit();
            this.SuspendLayout();
            // 
            // gameTimer
            // 
            this.gameTimer.Interval = 10;
            this.gameTimer.Tick += new System.EventHandler(this.gameTimer_Tick);
            // 
            // HUD
            // 
            this.HUD.BackColor = System.Drawing.Color.Gray;
            this.HUD.ErrorImage = null;
            this.HUD.InitialImage = null;
            this.HUD.Location = new System.Drawing.Point(0, 798);
            this.HUD.Name = "HUD";
            this.HUD.Size = new System.Drawing.Size(1200, 202);
            this.HUD.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.HUD.TabIndex = 1;
            this.HUD.TabStop = false;
            // 
            // hpLabel
            // 
            this.hpLabel.AutoSize = true;
            this.hpLabel.BackColor = System.Drawing.Color.Red;
            this.hpLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 64.2F);
            this.hpLabel.Location = new System.Drawing.Point(58, 850);
            this.hpLabel.Name = "hpLabel";
            this.hpLabel.Size = new System.Drawing.Size(256, 97);
            this.hpLabel.TabIndex = 2;
            this.hpLabel.Text = "HP: 5";
            // 
            // powerupLabel
            // 
            this.powerupLabel.AutoSize = true;
            this.powerupLabel.BackColor = System.Drawing.Color.Aqua;
            this.powerupLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 64.2F);
            this.powerupLabel.Location = new System.Drawing.Point(411, 850);
            this.powerupLabel.Name = "powerupLabel";
            this.powerupLabel.Size = new System.Drawing.Size(727, 97);
            this.powerupLabel.TabIndex = 3;
            this.powerupLabel.Text = "Powerup: Inactive";
            // 
            // Crosshair
            // 
            this.Crosshair.BackColor = System.Drawing.Color.Transparent;
            this.Crosshair.BackgroundImage = global::ProjektUppgift.Properties.Resources.Crosshair64;
            this.Crosshair.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.Crosshair.Location = new System.Drawing.Point(568, 368);
            this.Crosshair.Name = "Crosshair";
            this.Crosshair.Size = new System.Drawing.Size(64, 64);
            this.Crosshair.TabIndex = 4;
            this.Crosshair.TabStop = false;
            this.Crosshair.Click += new System.EventHandler(this.gameScreen_Click);
            this.Crosshair.MouseMove += new System.Windows.Forms.MouseEventHandler(this.Form1_MouseMove);
            // 
            // gameScreen
            // 
            this.gameScreen.BackColor = System.Drawing.SystemColors.Highlight;
            this.gameScreen.ErrorImage = null;
            this.gameScreen.InitialImage = null;
            this.gameScreen.Location = new System.Drawing.Point(0, 0);
            this.gameScreen.Name = "gameScreen";
            this.gameScreen.Size = new System.Drawing.Size(500, 500);
            this.gameScreen.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.gameScreen.TabIndex = 0;
            this.gameScreen.TabStop = false;
            this.gameScreen.Click += new System.EventHandler(this.gameScreen_Click);
            this.gameScreen.MouseMove += new System.Windows.Forms.MouseEventHandler(this.Form1_MouseMove);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(1200, 1000);
            this.ControlBox = false;
            this.Controls.Add(this.Crosshair);
            this.Controls.Add(this.powerupLabel);
            this.Controls.Add(this.hpLabel);
            this.Controls.Add(this.HUD);
            this.Controls.Add(this.gameScreen);
            this.KeyPreview = true;
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Form1";
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Form1_KeyDown);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.Form1_KeyUp);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.Form1_MouseMove);
            ((System.ComponentModel.ISupportInitialize)(this.HUD)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.Crosshair)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gameScreen)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Timer gameTimer;
        private System.Windows.Forms.PictureBox gameScreen;
        private System.Windows.Forms.PictureBox HUD;
        private System.Windows.Forms.Label hpLabel;
        private System.Windows.Forms.Label powerupLabel;
        private System.Windows.Forms.PictureBox Crosshair;
    }
}

