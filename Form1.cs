using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Text.Json;
using System.IO;

namespace WatermelonTime2024_editor
{

    public enum PlacingMode
    { 
        TILES,
        OBJECTS,
        NONE
    }


    public partial class Form1 : Form
    {
        public const int TILE_SIZE = 32;
        public const int FIXED_HEIGHT = 15;
        public const int TERRAIN_SET_HEIGHT = 5;

        public bool GridOn { get; set; } = true;

        public Image Tileset { get; set; }
        public Image[] backgrounds;


        public Level CurrentLevel { get; set; } = new Level(30, FIXED_HEIGHT);

        public short TileChooseX { get; set; } = 0;
        public short TileChooseY { get; set;} = 0;

        public PlacingMode placingMode = PlacingMode.TILES;

        private string currentFilePath = "";

        public List<EntityData> entityData = new List<EntityData>();
        public List<Entity> entities = new List<Entity>();      // < ---- this should have gone into the level class... oh well!
        public BindingSource bindingSource = new BindingSource();
        public Dictionary<string, Image> entityImages = new Dictionary<string, Image>();

        Pen placepen = new Pen(Color.Black);
        Pen placepen2 = new Pen(Color.White);

        public Form1()
        {
            InitializeComponent();

            ResizeLevelDisplay();

            UpdateControls();

            backgrounds = new Image[cb_background.Items.Count];

            Tileset = Image.FromFile("resources/backgrounds/watermelon_tileset.png");
            backgrounds[0] = Image.FromFile("resources/backgrounds/clouds.png");
            backgrounds[1] = Image.FromFile("resources/backgrounds/stars.png");
            backgrounds[2] = Image.FromFile("resources/backgrounds/cave.png");
            backgrounds[3] = Image.FromFile("resources/backgrounds/water.png");
            backgrounds[4] = Image.FromFile("resources/backgrounds/cave_2.png");

            tileChooser.Width = Tileset.Width;
            tileChooser.Height = Tileset.Height;


            placepen2.DashPattern = new float[] { 5, 5 };

            LoadEntityData();

            bindingSource.DataSource = entities;
            entityListBox.DisplayMember = "InfoString";
            entityListBox.ValueMember = "InfoString";
            entityListBox.DataSource = bindingSource;
        }

        private void myDisplay1_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            g.Clear(Color.DarkGray);

            // draw BG
            for (int y = 0; y < levelDisplay.Height; y += backgrounds[CurrentLevel.BackgroundIdx].Height)
            {
                for (int x = 0; x < levelDisplay.Width; x += backgrounds[CurrentLevel.BackgroundIdx].Width)
                {
                    g.DrawImage(backgrounds[CurrentLevel.BackgroundIdx], x, y);
                }
            }
            

            // draw level
            Brush tb = new SolidBrush(Color.DarkOliveGreen);
            for (int y = 0; y < CurrentLevel.Height; y++)
            {
                for (int x = 0; x < CurrentLevel.Width; x++)
                {
                    Tile currentTile = CurrentLevel.GetTile(x, y);
                    if (currentTile.idx != 0)
                    {
                        //g.FillRectangle(tb, x * TILE_SIZE, y * TILE_SIZE, TILE_SIZE, TILE_SIZE);
                        //g.DrawImage(
                        //    Tileset, 
                        //    new Rectangle(TILE_SIZE * x, TILE_SIZE * y, TILE_SIZE, TILE_SIZE), 
                        //    new Rectangle(TILE_SIZE * 3, TILE_SIZE * 4, TILE_SIZE, TILE_SIZE), 
                        //    GraphicsUnit.Pixel
                        //);
                        int terrain_offset = currentTile.terrain ? cb_tileset.SelectedIndex * TERRAIN_SET_HEIGHT * TILE_SIZE : 0;
                        int sheetX = (currentTile.idx % TILE_SIZE) * TILE_SIZE;
                        int sheetY = (currentTile.idx / TILE_SIZE) * TILE_SIZE + terrain_offset;

                        g.DrawImage(
                            Tileset,
                            new Rectangle(TILE_SIZE * x, TILE_SIZE * y, TILE_SIZE, TILE_SIZE),
                            new Rectangle(sheetX, sheetY, TILE_SIZE, TILE_SIZE),
                            GraphicsUnit.Pixel
                        );
                    }
                }
            }

            


            if (GridOn)
                DrawGrid(g);

            // draw entities
            foreach (Entity entity in entities)
            {
                EntityData data = entityData[entity.id];
                Rectangle src = new Rectangle();
                src.X = 0;
                src.Y = 0;
                src.Width = data.w;
                src.Height = data.h;

                g.DrawImage(entityImages[data.imgKey], entity.x, entity.y, src, GraphicsUnit.Pixel);
                Pen entityOutline = new Pen(Color.Red);
                g.DrawRectangle(entityOutline, entity.x, entity.y, data.w, data.h);
                Font f = new Font(FontFamily.GenericSansSerif, 8);
                Brush fontbrush = new SolidBrush(Color.White);
                Brush fontbrush2 = new SolidBrush(Color.Black);
                
                string displayname = data.name;
                if (data.name == "sign")
                {
                    displayname += " Index: " + ((entity.x / TILE_SIZE) % 10);
                }

                SizeF stringsize = g.MeasureString(displayname, f);

                g.FillRectangle(new SolidBrush(Color.FromArgb(128, Color.Black)), entity.x, entity.y, stringsize.Width, stringsize.Height);
                g.DrawString(displayname, f, fontbrush, entity.x , entity.y);
            }

        }


        public void DrawGrid(Graphics g)
        {
            Pen gridpen = new Pen(Color.Black);

            for (int i = 0; i < levelDisplay.Width; i += 32)
            {
                g.DrawLine(gridpen, i, 0, i, levelDisplay.Height);
            }

            for (int i = 0; i < levelDisplay.Height; i += 32)
            {
                g.DrawLine(gridpen, 0, i, levelDisplay.Width, i);
            }
        }

        private void tsb_ToggleGrid_Click(object sender, EventArgs e)
        {
            GridOn = !GridOn;
            levelDisplay.Invalidate();
        }

        private void levelDisplay_MouseClick(object sender, MouseEventArgs e)
        {
            PlaceItem(e);

        }

        private void num_LevelWidth_ValueChanged(object sender, EventArgs e)
        {
            CurrentLevel.Resize((short)num_LevelWidth.Value, FIXED_HEIGHT);
            tb_pixeldisplay.Text = string.Format("{0}", CurrentLevel.Width * TILE_SIZE);
            ResizeLevelDisplay();
        }


        public void ResizeLevelDisplay()
        {
            levelDisplay.Width = TILE_SIZE * CurrentLevel.Width;
            levelDisplay.Height = TILE_SIZE * CurrentLevel.Height;
            levelDisplay.Invalidate();
        }

        private void levelDisplay_MouseMove(object sender, MouseEventArgs e)
        {
            ts_Tile.Text = string.Format("Tile: ({0}, {1})", e.X / TILE_SIZE, e.Y / TILE_SIZE);
            ts_Pixel.Text = string.Format("Pixel: ({0}, {1})", e.X, e.Y);

            if (placingMode == PlacingMode.TILES && (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right))
            {
                PlaceItem(e);
            }

            
        }

        private void cb_music_SelectedIndexChanged(object sender, EventArgs e)
        {
            CurrentLevel.MusicIdx = (byte)cb_music.SelectedIndex;
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.InitialDirectory = ".";
                saveFileDialog.Filter = "lvl files (*.lvl)|*.lvl";
                saveFileDialog.RestoreDirectory = true;

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    //SaveAsJson(saveFileDialog.FileName);
                    SaveAsBin(saveFileDialog.FileName);
                    currentFilePath = saveFileDialog.FileName;
                }
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = ".";
                openFileDialog.Filter = "lvl files (*.lvl)|*.lvl";
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    //CurrentLevel = Level.FromJSONFile(openFileDialog.FileName);
                    OpenAsBin(openFileDialog.FileName);
                    ResizeLevelDisplay();
                    UpdateControls();

                    currentFilePath = openFileDialog.FileName;
                }
            }
        }


        public void PlaceItem(MouseEventArgs e)
        {
            Tile t = new Tile();

            if (e.Button == MouseButtons.Left)
            {
                switch (placingMode)
                {
                    case PlacingMode.TILES:
                        if (TileChooseX == 0 && TileChooseY == 0)
                        {
                            t.idx = 131;
                            t.terrain = true;
                        }
                        else
                        {
                            short tx = (short)(TileChooseX + 22);
                            short ty = (short)(TileChooseY - 1);
                            t.idx = (short)(tx + ty * 32);
                            t.terrain = false;
                        }
                        CurrentLevel.SetTile(e.X / TILE_SIZE, e.Y / TILE_SIZE, t);
                        break;

                    case PlacingMode.OBJECTS:
                        {

                            if (CheckIfEntityPlaceFree(e.X / TILE_SIZE * TILE_SIZE, 
                                e.Y / TILE_SIZE * TILE_SIZE, cb_objectselect.SelectedIndex))
                            {
                                Entity entity = new Entity();
                                entity.x = (short)(e.X / TILE_SIZE * TILE_SIZE);
                                entity.y = (short)(e.Y / TILE_SIZE * TILE_SIZE);
                                entity.id = (short)cb_objectselect.SelectedIndex;
                                entity.data = entityData[entity.id];
                                entities.Add(entity);
                                bindingSource.ResetBindings(false);
                            }

                            
                        }
                        break;

                    default:
                        break;
                }
                

                
            }
            else if (e.Button == MouseButtons.Right)
            {
                switch (placingMode)
                {
                    case PlacingMode.TILES:
                        t.idx = 0;
                        t.terrain = false;

                        CurrentLevel.SetTile(e.X / TILE_SIZE, e.Y / TILE_SIZE, t);
                        break;

                    case PlacingMode.OBJECTS:
                        for (int i = 0; i < entities.Count; i++)
                        {
                            Entity entity = entities[i];
                            EntityData edata = entityData[entity.id];
                            if (e.X > entity.x && e.X < entity.x + edata.w &&
                                e.Y > entity.y && e.Y < entity.y + edata.h)
                            {
                                entities.RemoveAt(i);

                                bindingSource.ResetBindings(false);
                            }
                        }
                        break;

                    default:
                        break;
                }

                
            }

            CurrentLevel.UpdateAutoTiles();
            levelDisplay.Invalidate();
        }


        public bool CheckForEntityAtPosition(int x, int y)
        {
            foreach (Entity e in entities)
            {
                EntityData data = entityData[e.id];
                if (x >= e.x && x <= e.x + data.w && y > e.y && y < e.y + data.h)
                {
                    return true;
                }
                
            }
            return false;
        }


        public bool CheckIfEntityPlaceFree(int x, int y, int entityId)
        {
            foreach (Entity e in entities)
            {
                EntityData data = entityData[e.id];
                EntityData data2 = entityData[entityId];

                Rectangle r1 = new Rectangle(e.x, e.y, 32, 32);
                Rectangle r2 = new Rectangle(x, y, 32, 32);

                

                if (r1.IntersectsWith(r2))
                    return false;
            }
            return true;
        }


        public void UpdateControls()
        {
            num_LevelWidth.Value = CurrentLevel.Width;
            tb_pixeldisplay.Text = string.Format("{0}", CurrentLevel.Width * TILE_SIZE);
            cb_music.SelectedIndex = CurrentLevel.MusicIdx;
            cb_background.SelectedIndex = CurrentLevel.BackgroundIdx;
            cb_tileset.SelectedIndex = CurrentLevel.TilesetIdx;
            check_LockCamY.Checked = (CurrentLevel.flags & (byte)LevelFlags.CAMERA_Y_LOCK) == (byte)LevelFlags.CAMERA_Y_LOCK;
            check_Autoscroll.Checked = (CurrentLevel.flags & (byte)LevelFlags.AUTOSCROLL) == (byte)LevelFlags.AUTOSCROLL;
        }

        public void SaveAsJson(string filename)
        {
            using (StreamWriter outfile = new StreamWriter(filename))
            {
                outfile.Write(CurrentLevel.ToJSON());
            }
        }

        public void SaveAsBin(string filename)
        {
            using (var stream = File.OpenWrite(filename))
            {
                using (BinaryWriter bw = new BinaryWriter(stream))
                {

                    byte[] clearheader = new byte[32];
                    bw.Write(clearheader);
                    bw.Seek(0, SeekOrigin.Begin);
                    bw.Write((byte)'W');
                    bw.Write((byte)'M');
                    bw.Write((byte)'L');
                    bw.Write((byte)'V');

                    bw.Write(CurrentLevel.Width);
                    bw.Write(CurrentLevel.Height);
                    bw.Write(CurrentLevel.MusicIdx);
                    bw.Write(CurrentLevel.BackgroundIdx);
                    bw.Write(CurrentLevel.TilesetIdx);

                    if (check_LockCamY.Checked)
                        CurrentLevel.flags |= (byte)LevelFlags.CAMERA_Y_LOCK;
                    if (check_Autoscroll.Checked)
                        CurrentLevel.flags |= (byte)LevelFlags.AUTOSCROLL;

                    bw.Write(CurrentLevel.flags);

                    bw.Seek(0x20, SeekOrigin.Begin);
                    for (int i = 0; i < CurrentLevel.tiles.Length; i++)
                    {
                        if (CurrentLevel.tiles[i].terrain)
                            bw.Write((short)-1);
                        else
                            bw.Write((short)CurrentLevel.tiles[i].idx);
                    }

                    bw.Write((byte)'O');
                    bw.Write((byte)'B');
                    bw.Write((byte)'J');
                    bw.Write((byte)'S');
                    bw.Write(entities.Count);

                    foreach (Entity e in entities)
                    {
                        bw.Write(e.id);
                        bw.Write(e.x);
                        bw.Write(e.y);
                        bw.Write(e.flags);
                    }

                    bw.Write((byte)'S');
                    bw.Write((byte)'I');
                    bw.Write((byte)'G');
                    bw.Write((byte)'N');

                    for (int i = 0; i < 10; i++)
                    {
                        byte[] msgBytes = new byte[128];
                        for (int j = 0; j < CurrentLevel.signMsg[i].Length; j++)
                        {
                            byte[] ascii = Encoding.ASCII.GetBytes(CurrentLevel.signMsg[i]);
                            msgBytes[j] = ascii[j];
                        }

                        bw.Write(msgBytes);
                    }
                }
            }
        }

        public void OpenAsBin(string filename)
        {
            
            using (var stream = File.OpenRead(filename))
            {
                using (BinaryReader br = new BinaryReader(stream))
                {

                    byte[] wmlv = br.ReadBytes(4);
                    if (wmlv[0] != 'W' && wmlv[1] != 'M' && wmlv[2] != 'L' && wmlv[3] != 'V')
                    {
                        MessageBox.Show("Level is not the correct format!", "Alert!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        return;
                    }


                    CurrentLevel = new Level();
                    entities.Clear();

                    CurrentLevel.Width = br.ReadInt16();
                    CurrentLevel.Height = br.ReadInt16();
                    CurrentLevel.MusicIdx = br.ReadByte();
                    CurrentLevel.BackgroundIdx = br.ReadByte();
                    CurrentLevel.TilesetIdx = br.ReadByte();
                    CurrentLevel.flags = br.ReadByte();

                    br.BaseStream.Seek(0x20, SeekOrigin.Begin);
                    CurrentLevel.tiles = new Tile[CurrentLevel.Width * CurrentLevel.Height];
                    for (int i = 0; i < CurrentLevel.tiles.Length; i++)
                    {
                        CurrentLevel.tiles[i].idx = br.ReadInt16();
                        if (CurrentLevel.tiles[i].idx == -1)
                        {
                            CurrentLevel.tiles[i].terrain = true;
                        }
                    }
                    byte[] objs = br.ReadBytes(4);
                    int maxEntitiesToRead = br.ReadInt32();

                    for (int i = 0; i < maxEntitiesToRead; i++)
                    {
                        Entity e = new Entity();
                        e.id = br.ReadInt16();
                        e.x = br.ReadInt16();
                        e.y = br.ReadInt16();
                        e.flags = br.ReadByte();
                        e.data = entityData[e.id];

                        entities.Add(e);
                    }

                    

                    // old style level compatibility
                    if (br.BaseStream.Position != br.BaseStream.Length)
                    {
                        byte[] sign = br.ReadBytes(4);

                        for (int i = 0; i < 10; i++)
                        {
                            byte[] strBytes = br.ReadBytes(128);

                            CurrentLevel.signMsg[i] = System.Text.Encoding.ASCII.GetString(strBytes);
                        }
                    }

                    


                    bindingSource.ResetBindings(false);
                    CurrentLevel.UpdateAutoTiles();

                    tabControl1.SelectedIndex = 2;
                    placingMode = PlacingMode.NONE;
                }
            }
        }

        private void tileChooser_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            const int TERRAIN_TILE_END = 704;
            int terrain_offset = cb_tileset.SelectedIndex * TERRAIN_SET_HEIGHT * TILE_SIZE;

            g.DrawImage(
                Tileset,
                0, 0,
                new Rectangle(3 * TILE_SIZE, 4 * TILE_SIZE + terrain_offset, TILE_SIZE, TILE_SIZE),
                GraphicsUnit.Pixel
            );

            g.DrawImage(
                Tileset,
                0, TILE_SIZE,
                new Rectangle(704, 0, Tileset.Width - 704, Tileset.Height),
                GraphicsUnit.Pixel
            );



            DrawDashedRect(g, TileChooseX * TILE_SIZE, TileChooseY * TILE_SIZE, TILE_SIZE, TILE_SIZE);


        }

        private void tileChooser_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                TileChooseX = (short)(e.X / TILE_SIZE);
                TileChooseY = (short)(e.Y / TILE_SIZE);

                tileChooser.Invalidate();
            }
        }

        private void DrawDashedRect(Graphics g, int x, int y, int w, int h)
        {
            
            g.DrawRectangle(placepen, x, y, w, h);
            g.DrawRectangle(placepen2, x, y, w, h);
        }

        private void cb_tileset_SelectedIndexChanged(object sender, EventArgs e)
        {
            CurrentLevel.TilesetIdx = (byte)cb_tileset.SelectedIndex;
            levelDisplay.Invalidate();
            tileChooser.Invalidate();
        }

        private void cb_background_SelectedIndexChanged(object sender, EventArgs e)
        {
            CurrentLevel.BackgroundIdx = (byte)cb_background.SelectedIndex;
            levelDisplay.Invalidate();
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult res= MessageBox.Show("Any unsaved changes will be lost. Do you want to continue?", "Confirmation",
                MessageBoxButtons.OKCancel, MessageBoxIcon.Question);

            if (res == DialogResult.OK)
            {
                CurrentLevel = new Level();
                entities.Clear();
                currentFilePath = "";
                UpdateControls();
                ResizeLevelDisplay();
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(currentFilePath))
                SaveAsBin(currentFilePath);
            else
                saveAsToolStripMenuItem_Click(sender, e);
        }

        private void LoadEntityData()
        {
            short currentIdToAssign = 0;
            string err = "";
            FileStream fs = new FileStream("resources/entities/entity_data.dat", FileMode.Open);
            using (StreamReader sr = new StreamReader(fs))
            {
                EntityData e;
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    string[] tokens = line.Split(',');

                    e.name = tokens[0];
                    e.id = currentIdToAssign++;
                    e.w = int.Parse(tokens[2]);
                    e.h = int.Parse(tokens[3]);
                    e.imgKey = tokens[4].Substring(0, tokens[4].Length - 4);

                    // legacy support with old level editor
                    e.pixelColorKey = uint.Parse(tokens[1], System.Globalization.NumberStyles.HexNumber);

                    try
                    {
                        if (!entityImages.ContainsKey(e.imgKey))
                        {
                            entityImages[e.imgKey] = Image.FromFile("resources/entities/" + tokens[4]);
                        }

                        entityData.Add(e);

                        
                        cb_objectselect.Items.Add(e.name);
                    }
                    catch (FileNotFoundException ex)
                    {
                        if (!err.Contains(tokens[4]))
                            err += tokens[4] + "\n";
                    }
                }

                if (!String.IsNullOrEmpty(err))
                {
                    MessageBox.Show("Could not load the following entity images!\n" + err, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                
            }

            cb_objectselect.SelectedIndex = 0;
            
        }

        public void UpdateEntityDisplay()
        { 
            objectDisplay.Invalidate();
        }

        private void objectDisplay_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            EntityData currentEntity = entityData[cb_objectselect.SelectedIndex];
            g.DrawImage(entityImages[currentEntity.imgKey], 0, 0, 
                new Rectangle(0, 0, currentEntity.w, currentEntity.h), GraphicsUnit.Pixel);
        }

        private void cb_objectselect_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateEntityDisplay();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult res = MessageBox.Show("All unsaved changes will be lost! Continue?", "Confirmation", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation);

            if (res == DialogResult.OK)
            {
                Application.Exit();
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            DialogResult res = MessageBox.Show("All unsaved changes will be lost! Continue?", "Confirmation", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation);

            if (res == DialogResult.Cancel)
            {
                e.Cancel = true;
            }
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (tabControl1.SelectedIndex)
            {
                case 0:
                    placingMode = PlacingMode.TILES;
                    break;

                case 1:
                    placingMode = PlacingMode.OBJECTS;
                    break;

                default:
                case 2:
                    placingMode = PlacingMode.NONE;
                    break;
            }
        }

        private void btn_remove_Click(object sender, EventArgs e)
        {
            if (entities.Count == 0)
                return;


            entities.RemoveAt(entityListBox.SelectedIndex);
            bindingSource.ResetBindings(false);
            levelDisplay.Invalidate();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutBox1 aboutBox = new AboutBox1();
            aboutBox.ShowDialog();
        }


        private void ImportBMPLevel(string filepath)
        {
            CurrentLevel = new Level();
            entities.Clear();

            Bitmap bmp = new Bitmap(filepath);

            Color tileset = bmp.GetPixel(0, 0);
            Color background = bmp.GetPixel(1, 0);
            Color music = bmp.GetPixel(2, 0);
            Color lock_y = bmp.GetPixel(3, 0);
            Color autoscroll = bmp.GetPixel(4, 0);

            if ((uint)lock_y.ToArgb() == 0xFFAAAAAA)
            {
                CurrentLevel.flags |= (byte)LevelFlags.CAMERA_Y_LOCK;
            }

            if ((uint)autoscroll.ToArgb() == 0xFFAAAAAA)
            {
                CurrentLevel.flags |= (byte)LevelFlags.AUTOSCROLL;
            }

            switch ((uint)tileset.ToArgb())
            {
                // cave
                case 0xFF804000:
                    CurrentLevel.TilesetIdx = 1;
                    break;

                // water
                case 0xFF000080:
                    CurrentLevel.TilesetIdx = 2;
                    break;

                // normal
                case 0xFF008000:
                default:
                    CurrentLevel.TilesetIdx = 0;
                    break;
            }


            switch ((uint)background.ToArgb())
            {
                // clouds
                default:
                case 0xFF0080FF:
                    CurrentLevel.BackgroundIdx = 0;
                    break;

                // Stars
                case 0xFF000080:
                    CurrentLevel.BackgroundIdx = 1;
                    break;

                // cave
                case 0xFF804000:
                    CurrentLevel.BackgroundIdx = 2;
                    break;

                // water
                case 0xFF0000FF:
                    CurrentLevel.BackgroundIdx= 3;
                    break;

                // cave boss
                case 0xFF804040:
                    CurrentLevel.BackgroundIdx = 4;
                    break;


            }


            switch ((uint)music.ToArgb())
            {
                // normal
                default:
                case 0xFF008000:
                    CurrentLevel.MusicIdx = 0;
                    break;

                // cave
                case 0xFF804000:
                    CurrentLevel.MusicIdx = 1;
                    break;

                // water
                case 0xFF000080:
                    CurrentLevel.MusicIdx = 2;
                    break;

                // boss 1
                case 0xFFFF0100:
                    CurrentLevel.MusicIdx = 3;
                    break;

                // boss 2
                case 0xFFFF0200:
                    CurrentLevel.MusicIdx = 4;
                    break;
            }

            CurrentLevel.Resize((short)Math.Max(bmp.Width, 15), FIXED_HEIGHT);

            // determine placements
            for (int j = 1; j < bmp.Height; j++)
            {
                for (int i = 0; i < bmp.Width; i++)
                {
                    int ty = j - 1;
                    int tx = i;

                    uint pixel = (uint)bmp.GetPixel(i, j).ToArgb();

                    Tile terrain;
                    terrain.idx = -1;
                    terrain.terrain = true;

                    Tile t;
                    t.idx = 0;
                    t.terrain = false;
                    switch (pixel)
                    {
                        // terrain tile
                        case 0xFF008000:
                            CurrentLevel.SetTile(tx, ty, terrain);
                            break;

                        case 0xFFAC3232:
                            t.idx = 54;
                            CurrentLevel.SetTile(tx, ty, t);
                            break;

                        case 0xFFFFBB00:
                            t.idx = 86;
                            CurrentLevel.SetTile(tx, ty, t);
                            break;

                        case 0xFF811111:
                            t.idx = 26;
                            CurrentLevel.SetTile(tx, ty, t);
                            break;

                        case 0xFF800000:
                            t.idx = 25;
                            CurrentLevel.SetTile(tx, ty, t);
                            break;

                        case 0xFF500000:
                            t.idx = 22;
                            CurrentLevel.SetTile(tx, ty, t);
                            break;

                        case 0xFFFFFFFF:
                            CurrentLevel.SetTile(tx, ty, t);
                            break;

                        default:
                            for (int ei = 0; ei < entityData.Count; ei++)
                            {
                                if (entityData[ei].pixelColorKey == pixel)
                                {
                                    Entity entity = new Entity();
                                    entity.x = (short)(tx * TILE_SIZE);
                                    entity.y = (short)(ty * TILE_SIZE);
                                    entity.id = (short)ei;
                                    entity.data = entityData[entity.id];
                                    entities.Add(entity);
                                    bindingSource.ResetBindings(false);
                                }
                            }
                            break;

                    }

                }
            }
            // end determine placements
            CurrentLevel.UpdateAutoTiles();


            tabControl1.SelectedIndex = 2;
            placingMode = PlacingMode.NONE;

        }

        private void importBMPLevelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = ".";
                openFileDialog.Filter = "bmp lvl files (*.bmp)|*.bmp";
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    //CurrentLevel = Level.FromJSONFile(openFileDialog.FileName);
                    ImportBMPLevel(openFileDialog.FileName);
                    ResizeLevelDisplay();
                    UpdateControls();

                    currentFilePath = "";
                }
            }
        }

        private void check_Autoscroll_CheckedChanged(object sender, EventArgs e)
        {
            ;//CurrentLevel.flags |= (byte)LevelFlags.AUTOSCROLL;
        }

        private void exportBMPLevelToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void ExportBMPLevel(string path)
        {
            Bitmap bmp = new Bitmap(CurrentLevel.Width, FIXED_HEIGHT + 1);
            for (int x = 0; x < CurrentLevel.Width; x++)
            {
                bmp.SetPixel(x, 0, Color.Black);
            }


        }

        private void signMessagesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SignMessagesForm f = new SignMessagesForm();

            f.Sign0 = CurrentLevel.signMsg[0];
            f.Sign1 = CurrentLevel.signMsg[1];
            f.Sign2 = CurrentLevel.signMsg[2];
            f.Sign3 = CurrentLevel.signMsg[3];
            f.Sign4 = CurrentLevel.signMsg[4];
            f.Sign5 = CurrentLevel.signMsg[5];
            f.Sign6 = CurrentLevel.signMsg[6];
            f.Sign7 = CurrentLevel.signMsg[7];
            f.Sign8 = CurrentLevel.signMsg[8];
            f.Sign9 = CurrentLevel.signMsg[9];



            if (f.ShowDialog() == DialogResult.OK)
            {
                CurrentLevel.signMsg[0] = f.Sign0;
                CurrentLevel.signMsg[1] = f.Sign1;
                CurrentLevel.signMsg[2] = f.Sign2;
                CurrentLevel.signMsg[3] = f.Sign3;
                CurrentLevel.signMsg[4] = f.Sign4;
                CurrentLevel.signMsg[5] = f.Sign5;
                CurrentLevel.signMsg[6] = f.Sign6;
                CurrentLevel.signMsg[7] = f.Sign7;
                CurrentLevel.signMsg[8] = f.Sign8;
                CurrentLevel.signMsg[9] = f.Sign9;
            }
        }
    }

    
    
}
