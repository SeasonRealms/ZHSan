using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
// B1+：FontStashSharp 已整體移除，Bounds 改用 GameManager.Bounds。
//using FontStashSharp;
using Tools;
using Platforms;
using GameManager;
using Zhsan.GameManager;

namespace GamePanels.Scrollbar
{
    public interface IFrameContent
    {
        /// <summary>
        /// 控件的ID
        /// </summary>
        string ID { get; set; }
        /// <summary>
        /// 控件的名字
        /// </summary>
        string Name { get; set; }
        /// <summary>
        /// 在框架内的偏移量
        /// </summary>
        Vector2 OffsetPos { get; set; }
        /// <summary>
        /// 缩放的倍数
        /// </summary>
        float Scale { get; set; }
        /// <summary>
        /// 深度
        /// </summary>
        float Depth { get; set; }
        /// <summary>
        /// 控件的边界范围
        /// </summary>
        List<Bounds> bounds { get; set; }
        /// <summary>
        /// 控件的宽
        /// </summary>
        float Width { get; set; }
        /// <summary>
        /// 控件的高
        /// </summary>
        float Height { get; set; }
        /// <summary>
        /// 包含控件的框架
        /// </summary>
        Frame baseFrame { get; set; }
        /// <summary>
        /// 控件的透明度
        /// </summary>
        float Alpha { get; set; }
        /// <summary>
        /// 颜色
        /// </summary>
        Color color { get; set; }
        /// <summary>
        /// 在画布上绘制控件
        /// </summary>
        /// <param name="batch">SpriteBatch</param>
        void DrawToCanvas(SpriteBatch batch);
        /// <summary>
        /// 计算控件的尺寸
        /// </summary>
        void CalculateControlSize();
        /// <summary>
        /// 画布内控件的更新
        /// </summary>
        void UpdateCanvas();
    }

    /// <summary>
    /// 框架内滚动条设置
    /// </summary>
    public enum FrameScrollbarType
    {
        Horizontal,
        Vertical,
        Both,
        Auto,
        None
    }
    public class Frame
    {
        /// <summary>
        /// 框架的位置坐标
        /// </summary>
        public Vector2 Position;
        /// <summary>
        /// 画布的宽高
        /// </summary>
        public float CanvasWidth, CanvasHeight;
        /// <summary>
        /// 画布的背景图片
        /// </summary>
        public Texture2D BackgroundPic = null;
        /// <summary>
        /// 画布内的控件列表
        /// </summary>
        public List<IFrameContent> ContentContorls;
        // B0：離屏畫布（Canvas 貼圖，原為 renderTarget2D 的內容）隨 RenderTarget2D 一並移除，
        // 內容改為直通繪製（見 Draw 與 CanvasToScreen）；溢出裁剪已由 Draw 內的 SpriteBatch.PushClip 恢復。
        /// <summary>
        /// 控件的可视框架矩形
        /// </summary>
        public Rectangle VisualFrame;
        /// <summary>
        /// 画布默认的背景色
        /// </summary>
        public Color BackgroundColor = new Color(0, 0, 0, 0);
        /// <summary>
        /// 背景的透明度
        /// </summary>
        public float BackgroundAlpha = 1f;
        /// <summary>
        /// 绘制画布的颜色
        /// </summary>
        public Color color;
        /// <summary>
        /// 画布的深度
        /// </summary>
        public float Depth;
        // B0：原「自建 SpriteBatch + 離屏 RenderTarget2D」欄位元移除——SeasonXNA 無 RenderTarget2D/GraphicsDevice，
        // 統一使用 Session.Current.SpriteBatch 直通繪製（繪製期由宿主 MainGame 的 Begin/End 窗口承載）。
        /// <summary>
        /// 画布的透明度
        /// </summary>
        public float Aplha;
        /// <summary>
        /// 框架内是否包含水平滚动条
        /// </summary>
        public bool HasHorizontalScrollbar = false;
        /// <summary>
        /// 框架内是否包含垂直滚动条
        /// </summary>
        public bool HasVerticalScrollbar = false;
        /// <summary>
        /// 包含框架内所有滚动条的列表
        /// </summary>
        public List<Scrollbar> Scrollbars;
        /// <summary>
        /// 锁定对象
        /// </summary>
        protected static object batchlock = new object();
        /// <summary>
        /// 控件内滚动条类型的设置
        /// </summary>
        public FrameScrollbarType frameScrollbarType;
        /// <summary>
        /// 画布的右边界填充距离
        /// </summary>
        public int CanvasRightPadding;
        /// <summary>
        /// 画布的下边界填充距离
        /// </summary>
        public int CanvasBottomPadding;
        /// <summary>
        /// 是否固定背景（如果为假则背景画在画布上，随着滚动条移动而有变化）
        /// </summary>
        public bool FixedBackground;
        /// <summary>
        /// 同时有水平滚动条和垂直滚动条时，再两种滚动条交界处空白的颜色
        /// </summary>
        public Color BlankBlockColor =new Color(57,57,57,255);
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="pos">框架的位置</param>
        /// <param name="visualFrame">设置可视窗的矩形</param>
        /// <param name="bgPicPath">背景图片的路径</param>
        /// <param name="alpha">画布的透明度</param>
        /// <param name="framescrollbartype">包含滚动条的类型</param>
        public Frame(Vector2 pos, Rectangle visualFrame, string bgPicPath = null, float alpha = 1f, FrameScrollbarType framescrollbartype = FrameScrollbarType.Auto, int canvasRightPadding = 0, int canvasBottomPadding = 0, bool fixBackground = true)
        {
            ContentContorls = new List<IFrameContent>();
            Position = pos;
            VisualFrame = visualFrame;
            if (bgPicPath != null)
                BackgroundPic = Platform.Current.LoadTexture(bgPicPath, false);

            color = Color.White;
            Aplha = alpha;
            Depth = 0f;
            frameScrollbarType = framescrollbartype;
            CanvasWidth = CanvasHeight = 0;
            CanvasRightPadding = canvasBottomPadding;
            CanvasBottomPadding = canvasBottomPadding;
            FixedBackground = fixBackground;

            // B0：原自建 SpriteBatch（Platform.GraphicsDevice）移除，繪製統一走 Session.Current.SpriteBatch。

            Scrollbars = new List<Scrollbar>();


            switch (frameScrollbarType)//根据不同的滚动条设置，设定包含何种类型的滚动条
            {
                case FrameScrollbarType.Horizontal:
                    HasHorizontalScrollbar = true;
                    break;
                case FrameScrollbarType.Vertical:
                    HasVerticalScrollbar = true;
                    break;
                case FrameScrollbarType.Both:
                    HasVerticalScrollbar = true;
                    HasHorizontalScrollbar = true;
                    break;
                case FrameScrollbarType.Auto://自动模式的滚动条在Draw方法内设置（实时更新）
                case FrameScrollbarType.None:
                    HasHorizontalScrollbar = false;
                    HasVerticalScrollbar = false;
                    break;
            }


            //根据设置生成相应的滚动条
            if (HasHorizontalScrollbar)
                Scrollbars.Add(new Scrollbar(this, ScrollbarType.Horizontal));
            if (HasVerticalScrollbar)
                Scrollbars.Add(new Scrollbar(this));

        }

        public void Draw()
        {
            if (ContentContorls.Count < 1)//框架包含控件
                return;

            lock (batchlock)//锁定并绘制控件
            {
                // B0：離屏畫布（RenderTarget2D）不可用（Season 引擎離屏能力待 B1+），降級為直通繪製：
                // 內容直接寫入共享 SpriteBatch，屏幕位置按「Position + 畫布坐標 − VisualFrame.XY」平移（見 CanvasToScreen）。
                var batch = Session.Current.SpriteBatch;

                if (BackgroundPic == null)
                {
                    //原實現為畫布 Clear(BackgroundColor) 填充底色；降級為可視框內直接畫純色塊（僅非全透明時）。
                    if (BackgroundColor.A > 0)
                        batch.Draw(SolidTextures.White,
                            new Rectangle((int)Position.X, (int)Position.Y, VisualFrame.Width, VisualFrame.Height),
                            BackgroundColor);
                }

                // B1+：溢出裁剪已由引擎裁剪能力恢復（SeasonXNA.SpriteBatch.PushClip → Draw2D 逐命令 Clip）：
                // 內容（含隨滾動移動的背景）與原離屏畫布一致裁剪到可視框（Position 起、VisualFrame 寬高）內，
                // 滾出可視框的行不再顯示（命中判定已在 CheckBox.IsInCanvasTexture 內限制於可視框）。
                batch.PushClip(new Rectangle((int)Position.X, (int)Position.Y, VisualFrame.Width, VisualFrame.Height));
                try
                {
                    if (BackgroundPic != null && !FixedBackground)//非固定背景原本畫在畫布上（隨滾動移動）；直通後與內容一同平移
                        batch.Draw(BackgroundPic, CanvasToScreen(Vector2.Zero), null, Color.White * BackgroundAlpha, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0);

                    ContentContorls.ForEach(cc => cc.DrawToCanvas(batch));//绘制每个控件
                }
                finally
                {
                    batch.PopClip();
                }

                if (BackgroundPic != null && FixedBackground) //绘制背景图片到固定位置
                    batch.Draw(BackgroundPic, Position, null, Color.White * BackgroundAlpha, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0);

                // B1+：原「畫布貼圖以 Position 為左上、源矩形 VisualFrame、整體乘 color*Aplha」的屏幕貼圖已移除；
                // Frame 整層透明度（color/Aplha/Depth）待引擎離屏能力落地後恢復。
            }

            //处理Auto类型的滚动条
            if (frameScrollbarType == FrameScrollbarType.Auto)
            {

                if (!HasHorizontalScrollbar && CanvasWidth > VisualFrame.Width)//如果画布宽度大于可视框架宽度且还没有水平滚动条则生成水平滚动条
                {
                    HasHorizontalScrollbar = true;
                    Scrollbars.Add(new Scrollbar(this, ScrollbarType.Horizontal));
                }
                else if (HasHorizontalScrollbar && CanvasWidth <= VisualFrame.Width)//如果画布宽度小于可视框架宽度且还有水平滚动条则删除水平滚动条
                {
                    HasHorizontalScrollbar = false;
                    Scrollbars.Remove(Scrollbars.Where(sb => sb.scrollbarType == ScrollbarType.Horizontal).FirstOrDefault());
                }

                if (!HasVerticalScrollbar && CanvasHeight > VisualFrame.Height)//如果画布高度大于可视框架高度且还没有垂直滚动条则生成垂直滚动条
                {
                    HasVerticalScrollbar = true;
                    Scrollbars.Add(new Scrollbar(this));
                }
                else if (HasVerticalScrollbar && CanvasHeight <= VisualFrame.Height)//如果画布高度小于可视框架高度且有垂直滚动条则删除垂直滚动条
                {
                    HasVerticalScrollbar = false;
                    Scrollbars.Remove(Scrollbars.Where(sb => sb.scrollbarType == ScrollbarType.Vertical).FirstOrDefault());
                }
            }

            Scrollbars.ForEach(sb => sb.Draw());

            if (HasHorizontalScrollbar && HasVerticalScrollbar) //如果两种滚动条都要，则生成两种滚动条交界处的空白块
            {
                Scrollbar horizontalScrollbar = Scrollbars.Where(sb => sb.scrollbarType == ScrollbarType.Horizontal).FirstOrDefault();
                Scrollbar verticalScrollbar = Scrollbars.Where(sb => sb.scrollbarType == ScrollbarType.Vertical).FirstOrDefault();
                // B0：原程序化 Texture2D（new+SetData 純色塊）改為「共享純白貼圖 + tint」拉伸繪製。
                Session.Current.SpriteBatch.Draw(SolidTextures.White,
                    new Rectangle((int)verticalScrollbar.BarPos.X, (int)horizontalScrollbar.BarPos.Y,
                        verticalScrollbar.ButtonWidth, horizontalScrollbar.ButtonHeight),
                    BlankBlockColor);
            }
        }

        /// <summary>
        /// 添加控件后计算新的画布大小
        /// </summary>
        /// <param name="contentContorl">添加的控件</param>
        private void CalculateCanvasSize(IFrameContent contentContorl)
        {
            contentContorl.CalculateControlSize();//计算添加控件的大小

            //如果新添加的控件大于现在画布的范围则生成适合新控件大小的画布
            if (contentContorl.OffsetPos.X + contentContorl.Width > CanvasWidth - CanvasRightPadding || contentContorl.OffsetPos.Y + contentContorl.Height > CanvasHeight - CanvasBottomPadding)
            {
                if (contentContorl.OffsetPos.X + contentContorl.Width > CanvasWidth - CanvasRightPadding)
                    CanvasWidth = contentContorl.OffsetPos.X + contentContorl.Width + CanvasRightPadding;//添加画布边界填充距离

                if (contentContorl.OffsetPos.Y + contentContorl.Height > CanvasHeight - CanvasBottomPadding)
                    CanvasHeight = contentContorl.OffsetPos.Y + contentContorl.Height + CanvasBottomPadding;//添加画布边界填充距离

                CanvasWidth = Math.Max(CanvasWidth, 10);
                CanvasHeight = Math.Max(CanvasHeight, 10);

                // B0：離屏畫布移除後不再重建渲染目標，畫布尺寸僅記錄在 CanvasWidth/CanvasHeight（供滾動與 CanvasToScreen 使用）。
            }

        }

        /// <summary>
        /// 用于删减控件后重新计算并生成新画布
        /// </summary>
        public void ReCalcuateCanvasSize()
        {
            CanvasWidth = CanvasHeight = 0;
            ContentContorls.ForEach(cc =>
            {
                cc.CalculateControlSize();
                CanvasWidth = cc.OffsetPos.X + cc.Width > CanvasWidth ? cc.OffsetPos.X + cc.Width : CanvasWidth;
                CanvasHeight = cc.OffsetPos.Y + cc.Height > CanvasHeight ? cc.OffsetPos.Y + cc.Height : CanvasHeight;
            });

            //添加画布边界填充
            CanvasWidth += CanvasRightPadding;
            CanvasHeight += CanvasBottomPadding;

            // B0：離屏畫布移除後不再重建渲染目標（同上，尺寸僅記錄）。
        }

        /// <summary>
        /// 向框架中添加控件
        /// </summary>
        /// <param name="contentContorl"></param>
        public void AddContentContorl(IFrameContent contentContorl)
        {
            ContentContorls.Add(contentContorl);//向画布内控件列表中添加新控件
            CalculateCanvasSize(contentContorl);
        }

        /// <summary>
        /// 清除画布所有内容
        /// </summary>
        public void Clear()
        {
            ContentContorls.Clear();
            CanvasWidth = CanvasHeight = 0;
            // B0：離屏畫布移除後已無自有可釋放資源（原 renderTarget2D/Canvas 的 Dispose 隨欄位移除）。
        }

        /// <summary>
        /// B0：畫布坐標 → 屏幕坐標。原離屏畫布以 Position 為左上、以 VisualFrame 為源矩形貼圖，
        /// 因此屏幕坐標 = Position + 畫布坐標 − VisualFrame.XY；直通繪製後由畫布內控件沿用同一映射。
        /// </summary>
        public Vector2 CanvasToScreen(Vector2 canvasPos)
            => new(Position.X + canvasPos.X - VisualFrame.X, Position.Y + canvasPos.Y - VisualFrame.Y);

        public void Update()
        {

            Scrollbars.ForEach(sb => sb.Update());//处理滚动条Update方法

            //根据滚动条的位置重新计算可视框架在画布上的坐标，以用于显示相应范围的画布内容
            // B0：離屏畫布貼圖移除後，改用邏輯畫布尺寸（CanvasWidth/CanvasHeight）計算；
            // 內容平移由 CanvasToScreen 施加（滾動生效；溢出裁剪見 Draw 內的 PushClip）。
            if (CanvasWidth > 0 && CanvasHeight > 0)
            {
                if (HasHorizontalScrollbar)
                    VisualFrame.X = (int)(Scrollbars.Where(sb => sb.scrollbarType == ScrollbarType.Horizontal).FirstOrDefault().Value * (CanvasWidth - VisualFrame.Width));
                if (HasVerticalScrollbar)
                    VisualFrame.Y = (int)(Scrollbars.Where(sb => sb.scrollbarType == ScrollbarType.Vertical).FirstOrDefault().Value * (CanvasHeight - VisualFrame.Height));
            }

            ContentContorls.ForEach(cc => cc.UpdateCanvas());
        }

    }


}
