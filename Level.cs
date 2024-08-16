using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Text.Json;
using System.IO;
using System.Text.Json.Nodes;

namespace WatermelonTime2024_editor
{
    [Flags]
    public enum LevelFlags
    {
        CAMERA_Y_LOCK = 1 << 0,
        AUTOSCROLL = 1 << 1
    }


    [Flags]
    public enum TileNeighbor
    {
        TL = 1 << 0,
        T = 1 << 1,
        TR = 1 << 2,
        L = 1 << 3,
        R = 1 << 4,
        BL = 1 << 5,
        B = 1 << 6,
        BR = 1 << 7,
    }

    public struct Tile
    {
        public Int16 idx;
        public bool terrain;
    }

    public struct Entity
    {
        public Int16 id;
        public short x;
        public short y;
        public byte flags;
        public EntityData data;

        public string InfoString
        {
            get
            {
                return data.name + string.Format(" ({0},{1})", x, y);
            }
        }

    }

    public struct EntityData
    {
        public string name;
        public string imgKey;
        public uint pixelColorKey; // legacy support with old level editor
        public int w;
        public int h;
        public Int16 id;
    }

    // Memory address list
    // 0x0  -- level width
    // 0x2  -- level height
    // 0x4  -- level music
    // 0x5  -- level bg
    // 0x6  -- level tileset
    // 0x7  -- level flags
    // 0x10 -- 
    // 0x20 -- tiles
    // after tiles -- OBJS
    // 4 bytes -- entity count
    // entity bytes:
    // 2 bytes -- id
    // 2 bytes -- x
    // 2 bytes -- y
    // 1 byte -- flags
    public class Level
    {
        public Tile[] tiles;
        public short Width = 30;
        public short Height = 15;
        public byte MusicIdx { get; set; } = 0;
        public byte BackgroundIdx { get; set; } = 0;
        public byte TilesetIdx { get; set; } = 0;
        public byte flags = 0;

        public Level(short width, short height)
        {
            this.Width = width;
            this.Height = height;
            tiles = new Tile[width * height];
        }

        public Level()
        {
            this.Width = 30;
            this.Height = 15;
            tiles = new Tile[Width * Height];
        }


        public static Level FromJSONFile(string filepath)
        {
            Level lvl = new Level();
            string jsonString;

            using (StreamReader sr = new StreamReader(filepath))
            {
                jsonString = sr.ReadToEnd();

                var obj = JsonSerializer.Deserialize<JsonNode>(jsonString);
                var json = JsonDocument.Parse(jsonString).RootElement;

                // TODO: find out how to  parse array
                Int16[] tiles_read = new Int16[lvl.Width * lvl.Height];

                lvl.Width = json.GetProperty("width").GetInt16();
                lvl.Height = json.GetProperty("height").GetInt16();
                lvl.MusicIdx = json.GetProperty("music_index").GetByte();

                lvl.tiles = new Tile[lvl.Width * lvl.Height];

                int tidx = 0;
                foreach (var tile_num in json.GetProperty("tiles").EnumerateArray())
                {
                    lvl.tiles[tidx].idx = tile_num.GetInt16();
                    lvl.tiles[tidx].terrain = (lvl.tiles[tidx].idx == -1) ? true : false;
                    tidx++;
                }


                for (int i = 0; i < lvl.tiles.Length; i++)
                {
                    lvl.tiles[i].idx = tiles_read[i];
                    lvl.tiles[i].terrain = tiles_read[i] == -1 ? true : false;
                }
                
            }

            return lvl;
        }


        public static Level FromBinaryFile(string filepath)
        {
            Level lvl = new Level();


            return lvl;
        }

        
        public void SetTile(int x, int y, Tile t)
        {
            if (x < 0)
                x = 0;
            if (y < 0)
                y = 0;
            if (x >= Width - 1)
                x = Width - 1;
            if (y >= Height - 1)
                y = Height - 1;

            tiles[y * Width + x] = t;
        }


        public Tile GetTile(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
            {
                Tile outsidetile;
                outsidetile.idx = 0;
                outsidetile.terrain = true;

                return outsidetile;
            }

            return tiles[y * Width + x];
        }


        public void Resize(short newWidth, short newHeight)
        {
            Tile[] remember = tiles;
            tiles = new Tile[newWidth * newHeight];

            for (int y = 0; y < Math.Min(Height, newHeight); y++)
            {
                for (int x = 0; x < Math.Min(Width, newWidth); x++)
                {
                    tiles[(y * newWidth) + x] = remember[y * Width + x];
                }
            }

            Width = newWidth;
            Height = newHeight;
        }

        public string ToJSON()
        {
            short [] tile_indices = new short[tiles.Length];
            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i].terrain)
                    tile_indices[i] = -1;
                else
                    tile_indices[i] = tiles[i].idx;
            }

            var obj = new
            {
                width = Width,
                height = Height,
                music_index = MusicIdx,
                tiles = tile_indices
            };

            return JsonSerializer.Serialize(obj).ToString();
        }


        public void UpdateAutoTiles()
        {
            if (tiles.Length == 0)
                return;

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Tile updatedTile = GetTile(x, y);

                    if (!updatedTile.terrain)
                        continue;

                    TileNeighbor result = 0;

                    Tile tl = GetTile(x - 1, y - 1);
                    Tile t = GetTile(x, y - 1);
                    Tile tr = GetTile(x + 1, y - 1);

                    Tile l = GetTile(x - 1, y);
                    Tile r = GetTile(x + 1, y);

                    Tile bl = GetTile(x - 1, y + 1);
                    Tile b = GetTile(x, y + 1);
                    Tile br = GetTile(x + 1, y + 1);

                    if (tl.terrain)
                        result |= TileNeighbor.TL;
                    if (t.terrain)
                        result |= TileNeighbor.T;
                    if (tr.terrain)
                        result |= TileNeighbor.TR;

                    if (l.terrain)
                        result |= TileNeighbor.L;
                    if (r.terrain)
                        result |= TileNeighbor.R;

                    if (bl.terrain)
                        result |= TileNeighbor.BL;
                    if (b.terrain)
                        result |= TileNeighbor.B;
                    if (br.terrain)
                        result |= TileNeighbor.BR;

                    if ((result & (TileNeighbor.L | TileNeighbor.T)) != (TileNeighbor.L | TileNeighbor.T))
                        result &= ~TileNeighbor.TL;

                    if ((result & (TileNeighbor.R | TileNeighbor.T)) != (TileNeighbor.R | TileNeighbor.T))
                        result &= ~TileNeighbor.TR;

                    if ((result & (TileNeighbor.L | TileNeighbor.B)) != (TileNeighbor.L | TileNeighbor.B))
                        result &= ~TileNeighbor.BL;

                    if ((result & (TileNeighbor.R | TileNeighbor.B)) != (TileNeighbor.R | TileNeighbor.B))
                        result &= ~TileNeighbor.BR;

                    updatedTile.idx = TranslateTileIdx(result);
                    updatedTile.terrain = true;
                    SetTile(x, y, updatedTile);
                }
            }
        }

        public short TranslateTileIdx(TileNeighbor tileflags)
        {
            switch (tileflags)
            {
                default:
                case 0: return 131;

                case TileNeighbor.T: return 99;
                case TileNeighbor.R: return 128;
                case TileNeighbor.L: return 130;
                case TileNeighbor.B: return 35;

                case TileNeighbor.T | TileNeighbor.B: return 67;
                case TileNeighbor.L | TileNeighbor.R: return 129;

                case TileNeighbor.T | TileNeighbor.B | TileNeighbor.L: return 167;
                case TileNeighbor.T | TileNeighbor.B | TileNeighbor.R: return 164;
                case TileNeighbor.T | TileNeighbor.L | TileNeighbor.R: return 136;
                case TileNeighbor.B | TileNeighbor.L | TileNeighbor.R: return 40;
                case TileNeighbor.T | TileNeighbor.B | TileNeighbor.L | TileNeighbor.R: return 168;


                case TileNeighbor.L | TileNeighbor.R | TileNeighbor.BL | TileNeighbor.B | TileNeighbor.BR: return 33;
                case TileNeighbor.R | TileNeighbor.BR | TileNeighbor.B: return 32;
                case TileNeighbor.L | TileNeighbor.BL | TileNeighbor.B: return 34;

                case TileNeighbor.TL | TileNeighbor.T | TileNeighbor.TR | TileNeighbor.L | TileNeighbor.R | TileNeighbor.BL | TileNeighbor.B | TileNeighbor.BR: return 65;
                case TileNeighbor.T | TileNeighbor.TR | TileNeighbor.R | TileNeighbor.BR | TileNeighbor.B: return 64;
                case TileNeighbor.T | TileNeighbor.TL | TileNeighbor.L | TileNeighbor.BL | TileNeighbor.B: return 66;

                case TileNeighbor.T | TileNeighbor.TR | TileNeighbor.R: return 96;
                case TileNeighbor.L | TileNeighbor.TL | TileNeighbor.T | TileNeighbor.TR | TileNeighbor.R: return 97;
                case TileNeighbor.L | TileNeighbor.TL | TileNeighbor.T: return 98;

                case TileNeighbor.B | TileNeighbor.R: return 36;
                case TileNeighbor.B | TileNeighbor.L: return 39;
                case TileNeighbor.T | TileNeighbor.R: return 132;
                case TileNeighbor.T | TileNeighbor.L: return 135;


                case TileNeighbor.T | TileNeighbor.TR | TileNeighbor.R | TileNeighbor.B: return 68;
                case TileNeighbor.T | TileNeighbor.R | TileNeighbor.BR | TileNeighbor.B: return 100;

                case TileNeighbor.L | TileNeighbor.BL | TileNeighbor.B | TileNeighbor.R: return 37;
                case TileNeighbor.L | TileNeighbor.B | TileNeighbor.BR | TileNeighbor.R: return 38;

                case TileNeighbor.T | TileNeighbor.TL | TileNeighbor.L | TileNeighbor.B: return 71;
                case TileNeighbor.T | TileNeighbor.L | TileNeighbor.BL | TileNeighbor.B: return 103;

                case TileNeighbor.L | TileNeighbor.TL | TileNeighbor.T | TileNeighbor.R: return 133;
                case TileNeighbor.L | TileNeighbor.T | TileNeighbor.TR | TileNeighbor.R: return 134;

                case TileNeighbor.TL | TileNeighbor.T | TileNeighbor.TR | TileNeighbor.L | TileNeighbor.R | TileNeighbor.BL | TileNeighbor.B: return 69;
                case TileNeighbor.TL | TileNeighbor.T | TileNeighbor.TR | TileNeighbor.L | TileNeighbor.R | TileNeighbor.B | TileNeighbor.BR: return 70;
                case TileNeighbor.TL | TileNeighbor.T | TileNeighbor.L | TileNeighbor.R | TileNeighbor.BL | TileNeighbor.B | TileNeighbor.BR: return 101;
                case TileNeighbor.T | TileNeighbor.TR | TileNeighbor.L | TileNeighbor.R | TileNeighbor.BL | TileNeighbor.B | TileNeighbor.BR: return 102;

                case TileNeighbor.TL | TileNeighbor.T | TileNeighbor.L | TileNeighbor.R | TileNeighbor.BL | TileNeighbor.B: return 165;
                case TileNeighbor.T | TileNeighbor.TR | TileNeighbor.L | TileNeighbor.R | TileNeighbor.B | TileNeighbor.BR: return 166;

                case TileNeighbor.TL | TileNeighbor.T | TileNeighbor.L | TileNeighbor.R | TileNeighbor.B | TileNeighbor.BR: return 41;
                case TileNeighbor.T | TileNeighbor.TR | TileNeighbor.L | TileNeighbor.R | TileNeighbor.BL | TileNeighbor.B: return 73;

                case TileNeighbor.TL | TileNeighbor.T | TileNeighbor.TR | TileNeighbor.L | TileNeighbor.R | TileNeighbor.B: return 72;
                case TileNeighbor.T | TileNeighbor.L | TileNeighbor.R | TileNeighbor.BL | TileNeighbor.B | TileNeighbor.BR: return 104;

                case TileNeighbor.T | TileNeighbor.L | TileNeighbor.R | TileNeighbor.B | TileNeighbor.BR: return 105;
                case TileNeighbor.T | TileNeighbor.L | TileNeighbor.R | TileNeighbor.BL | TileNeighbor.B: return 106;
                case TileNeighbor.T | TileNeighbor.TR | TileNeighbor.L | TileNeighbor.R | TileNeighbor.B: return 137;
                case TileNeighbor.TL | TileNeighbor.T | TileNeighbor.L | TileNeighbor.R | TileNeighbor.B: return 138;
            }
        }
    }
}
