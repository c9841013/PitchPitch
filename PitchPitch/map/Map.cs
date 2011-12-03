﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using SdlDotNet.Graphics;

namespace PitchPitch.map
{
    struct View
    {
        public long X;
        public long Y;
        public int Width;
        public int Height;

        public Size Size { get { return new Size(Width, Height); } }
    }

    abstract class Map : IDisposable
    {
        protected MapInfo _mapInfo;
        public MapInfo MapInfo { get { return _mapInfo; } set { _mapInfo = value; } }

        protected MapChipData _chipData;
        public MapChipData ChipData { get { return _chipData; } set { _chipData = value; } }

        protected PitchPitch _parent;
        protected View view;

        protected List<uint[]> _chips = new List<uint[]>();

        /// <summary>列数</summary>
        protected int _columnNum = 0;
        /// <summary>行数</summary>
        protected int _rowNum = 0;
        /// <summary>1画面表示するのに必要な行数</summary>
        protected int _needRowNum = 0;
        /// <summary>1画面表示するのに必要な列数</summary>
        protected int _needColumnNum = 0;

        protected Color _backColor = Color.LightSalmon;
        public virtual Color BackColor
        {
            get { return _backColor; }
            set { _backColor = value; }
        }
        protected Color _strongColor = Color.Black;
        public virtual Color StrongColor
        {
            get { return _strongColor; }
            set { _strongColor = value; }
        }
        protected Color _foreColor = Color.Red;
        public virtual Color ForeColor
        {
            get { return _foreColor; }
            set { _foreColor = value; }
        }

        #region 表示index
        /// <summary>表示しているチップのX方向のオフセット</summary>
        protected double _xOffset = 0;
        /// <summary>表示しているチップのY方向のオフセット</summary>
        protected double _yOffset = 0;
        /// <summary>表示したいチップのX開始インデックス</summary>
        protected int _xFirstIdx = 0;
        /// <summary>表示したいチップのY開始インデックス</summary>
        protected int _yFirstIdx = 0;
        /// <summary>表示したいチップのX終了インデックスの次</summary>
        protected int _xLastIdx = 0;
        /// <summary>表示したいチップのY終了インデックスの次</summary>
        protected int _yLastIdx = 0;
        #endregion

        public virtual void Init(PitchPitch parent, Size viewSize)
        {
            _parent = parent;
        }

        public virtual bool HasEnd { get { return true; } }
        public virtual long Height
        {
            get { return _rowNum * _chipData.ChipHeight; }
        }
        public virtual long Width
        {
            get { return _columnNum * _chipData.ChipWidth; }
        }

        #region 座標変換
        /// <summary>Chip index から 表示座標への変換</summary>
        protected double convertIdx2VX(int xIdx)
        {
            return xIdx * _chipData.ChipWidth - view.X;
        }
        /// <summary>Chip index から 表示座標への変換</summary>
        protected double convertIdx2VY(int yIdx)
        {
            return yIdx * _chipData.ChipHeight - view.Y;
        }

        /// <summary>Chip index から 座標への変換</summary>
        protected double convertIdx2PX(int xIdx)
        {
            return xIdx * _chipData.ChipWidth + _xOffset;
        }
        /// <summary>Chip index から 座標への変換</summary>
        protected double convertIdx2PY(int yIdx)
        {
            return yIdx * _chipData.ChipHeight + _yOffset;
        }

        /// <summary>表示座標 から Chip index への変換</summary>
        protected int convertV2IdxX(double x)
        {
            return (int)((x + view.X - _xOffset) / (double)_chipData.ChipWidth);
        }
        /// <summary>表示座標 から Chip index への変換</summary>
        protected int convertV2IdxY(double y)
        {
            return (int)((y + view.Y - _yOffset) / (double)_chipData.ChipHeight);
        }

        /// <summary>座標 から Chip index への変換</summary>
        protected int convertP2IdxX(double x)
        {
            return (int)((x - _xOffset) / (double)_chipData.ChipWidth);
        }
        /// <summary>座標 から Chip index への変換</summary>
        protected int convertP2IdxY(double y)
        {
            return (int)((y - _yOffset) / (double)_chipData.ChipHeight);
        }
        #endregion

        #region MiniMap表示用
        private Size _surfaceSize = Size.Empty;
        private List<Rectangle> _miniMapMarginRects = new List<Rectangle>();
        private Rectangle _miniMapRect = Rectangle.Empty;
        protected virtual void updateMiniMapRect(Size size)
        {
            if (_surfaceSize == size) return;

            double cdw = size.Width / (double)_columnNum;
            double cdh = size.Height / (double)_rowNum;
            Rectangle rect = new Rectangle(Point.Empty, size);

            _miniMapMarginRects = new List<Rectangle>();
            if (cdw > cdh)
            {
                rect.Width = (int)(rect.Height * _columnNum / (double)_rowNum);
                rect.X = (int)(size.Width / 2.0 - rect.Width / 2.0);
                _miniMapMarginRects.Add(new Rectangle(0, rect.Y, rect.X, rect.Height));
                _miniMapMarginRects.Add(new Rectangle(rect.Right, rect.Y, size.Width - rect.Right, rect.Height));
            }
            else if (cdh > cdw)
            {
                rect.Height = (int)(rect.Width * _rowNum / (double)_columnNum);
                rect.Y = (int)(size.Height / 2.0 - rect.Height / 2.0);
                _miniMapMarginRects.Add(new Rectangle(rect.X, 0, rect.Width, rect.Y));
                _miniMapMarginRects.Add(new Rectangle(rect.X, rect.Bottom, rect.Width, size.Height - rect.Bottom));
            }
            _surfaceSize = size;
            _miniMapRect = rect;
        }
        #endregion

        public virtual void SetView(View view)
        {
            this.view = view;
            int vb = (int)(view.Y + view.Height);

            _xOffset = view.X % _chipData.ChipWidth;
            _yOffset = view.Y % _chipData.ChipHeight;
            _xFirstIdx = convertP2IdxX(view.X);
            _yFirstIdx = convertP2IdxY(view.Y);
            _xLastIdx = _xFirstIdx + _needColumnNum;
            _yLastIdx = _yFirstIdx + _needRowNum;
        }

        public virtual IEnumerable<Chip> EnumViewChipData()
        {
            double[] vpxs = new double[_xLastIdx - _xFirstIdx];
            double[] vpys = new double[_yLastIdx - _yFirstIdx];
            double[] ppxs = new double[_xLastIdx - _xFirstIdx];
            double[] ppys = new double[_yLastIdx - _yFirstIdx];

            int rlen = _chips.Count > 0 ? _chips[0].Length : 0;
            for (int i = _xFirstIdx, s = 0; i < _xLastIdx && i < _chips.Count; i++, s++)
            {
                if (i < 0) continue;
                vpxs[s] = convertIdx2VX(i); ppxs[s] = convertIdx2PX(i);
            }
            for (int j = _yFirstIdx, t = 0; j < _yLastIdx && j < rlen; j++, t++)
            {
                if (j < 0) continue;
                vpys[t] = convertIdx2VY(j); ppys[t] = convertIdx2PY(j);
            }

            for (int i = _xFirstIdx, s= 0; i < _xLastIdx && i < _chips.Count; i++, s++)
            {
                if (i < 0) continue;
                for (int j = _yFirstIdx, t = 0; j < _yLastIdx && j < _chips[i].Length; j++, t++)
                {
                    if (j < 0) continue;
                    Chip cd = new Chip();
                    cd.Idx = new Point(i, j);
                    cd.ViewPoint = new PointD(vpxs[s], vpys[t]);
                    cd.X = ppxs[s]; cd.Y = ppys[t];
                    cd.ChipData = _chips[i][j];
                    cd.Hardness = _chipData.Hardness[cd.ChipData];
                    yield return cd;
                }
            }
        }

        #region 描画
        protected virtual void renderBackground(Surface s, Rectangle r)
        {
            s.Fill(r, _backColor);
        }
        protected virtual void renderForeground(Surface s, Rectangle r)
        {
            foreach (Chip cd in this.EnumViewChipData())
            {
                _chipData.Draw(s, cd.ChipData, new Point((int)(r.X + cd.ViewPoint.X), (int)(r.Y + cd.ViewPoint.Y)));
            }
        }

        protected virtual void renderMiniMapBackground(Surface s, Rectangle r)
        {
            renderBackground(s, r);
        }
        protected virtual void renderMiniMapForeground(Surface s, Rectangle r)
        {
            updateMiniMapRect(r.Size);
            double cdw = _miniMapRect.Width / (double)_columnNum;
            double cdh = _miniMapRect.Height / (double)_rowNum;

            foreach (Rectangle rr in _miniMapMarginRects)
            {
                _chipData.Draw(s, _chipData.WallChip, rr);
            }

            int x0, x1;
            for (int i = 0; i < _chips.Count; i++)
            {
                x0 = (int)(i * cdw);
                if (i == _chips.Count - 1) x1 = _miniMapRect.Width;
                else x1 = (int)((i + 1) * cdw);

                int y0, y1;
                for (int j = 0; j < _chips[i].Length; j++)
                {
                    y0 = (int)(j * cdh);
                    if (j == _chips[i].Length - 1) y1 = _miniMapRect.Height;
                    else y1 = (int)((j + 1) * cdh);

                    _chipData.Draw(s, _chips[i][j],
                        new Rectangle(_miniMapRect.X + (int)x0, _miniMapRect.Y + (int)y0, (int)(x1 - x0), (int)(y1 - y0)));
                }
            }
        }
        protected virtual void renderMiniMapViewBox(Surface s, Rectangle r)
        {
            updateMiniMapRect(r.Size);
            double cdw = _miniMapRect.Width / (double)_columnNum;
            double cdh = _miniMapRect.Height / (double)_rowNum;

            Point lt = new Point((int)(_xFirstIdx * cdw) + _miniMapRect.X, (int)(_yFirstIdx * cdh) + _miniMapRect.Y);
            Point rb = new Point((int)(_xLastIdx * cdw) + _miniMapRect.X, (int)(_yLastIdx * cdh) + _miniMapRect.Y);
            lt.Offset(r.Location);
            rb.Offset(r.Location);

            SdlDotNet.Graphics.Primitives.Box viewBox = new SdlDotNet.Graphics.Primitives.Box(lt, rb);
            SdlDotNet.Graphics.Primitives.Box viewBoxIn = new SdlDotNet.Graphics.Primitives.Box(new Point(lt.X + 1, lt.Y + 1), new Point(rb.X - 1, rb.Y - 1));
            s.Draw(viewBox, _strongColor, true, false);
            s.Draw(viewBoxIn, _strongColor, true, false);
        }

        public virtual void Render(Surface s, Rectangle r)
        {
            renderBackground(s, r);
            renderForeground(s, r);
        }

        public virtual void RenderMiniMap(Surface s, Rectangle r)
        {
            renderMiniMapBackground(s, r);
            renderMiniMapForeground(s, r);
        }
        public virtual void RenderMiniMapViewBox(Surface s, Rectangle r)
        {
            renderMiniMapViewBox(s, r);
        }
        #endregion

        public virtual double GetDefaultY(double xInView)
        {
            int xidx = convertV2IdxX(xInView);
            if (xidx >= 0 && xidx < _chips.Count)
            {
                uint[] row = _chips[xidx];
                uint prev = row[0]; int inIdx = 0; int outIdx = row.Length - 1;
                for (int i = 0; i < row.Length; i++)
                {
                    uint r = row[i];
                    if (prev > 0 && r == 0)
                    {
                        // 穴に入った
                        inIdx = i;
                    }
                    else if (prev == 0 && r > 0)
                    {
                        // 穴から出た
                        outIdx = i - 1;
                        break;
                    }
                    prev = r;
                }
                double idx = (outIdx + inIdx) / 2.0;
                return convertIdx2PY((int)idx);
            }
            else
            {
                return -1;
            }
        }

        public void LoadMapImage(Bitmap mapBmp, Bitmap chipMapping = null)
        {
            _chips = new List<uint[]>();
            Dictionary<Color, uint> mapping = null;

            if (chipMapping != null)
            {
                mapping = new Dictionary<Color, uint>();
                using (Surface ms = new Surface(chipMapping))
                {
                    Color[,] tmp = ms.GetColors(new Rectangle(0, 0, ms.Width, ms.Height));
                    for (int i = 0; i < tmp.GetLength(0); i++)
                    {
                        for (int j = 0; j < tmp.GetLength(1); j++)
                        {
                            Color c = tmp[i, j];
                            uint idx = (uint)(i + j * tmp.GetLength(0));
                            if (mapping.ContainsKey(c)) mapping[c] = idx;
                            else mapping.Add(c, idx);
                        }
                    }
                }
            }

            Color[,] colors;
            using (Surface s = new Surface(mapBmp))
            {
                colors = s.GetColors(new Rectangle(0, 0, mapBmp.Width, mapBmp.Height));
            }

            _columnNum = colors.GetLength(0);
            _rowNum = colors.GetLength(1);

            if (colors == null)
            {
#warning 例外
            }
            else if (mapping != null)
            {
                for (int i = 0; i < _columnNum; i++)
                {
                    uint[] column = new uint[_rowNum];
                    for (int j = 0; j < _rowNum; j++)
                    {
                        Color c = colors[i, j];
                        if (mapping.ContainsKey(c))
                            column[j] = mapping[c];
                        else
                            column[j] = 0;
                    }
                    _chips.Add(column);
                }
            }
            else
            {
                for (int i = 0; i < _columnNum; i++)
                {
                    uint[] column = new uint[_rowNum];
                    for (int j = 0; j < _rowNum; j++)
                    {
                        Color c = colors[i, j];
                        if ((c.ToArgb() & 0x00ffffff) == 0x00ffffff) column[j] = 0;
                        else column[j] = 1;
                    }
                    _chips.Add(column);
                }
            }
        }

        public virtual void Dispose()
        {
            if (_chipData != null) _chipData.Dispose();
        }
    }
}
