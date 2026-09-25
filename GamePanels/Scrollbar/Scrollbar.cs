using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Platforms;
using GameManager;
using Zhsan.GameManager;

namespace GamePanels.Scrollbar
{
    /// <summary>
    /// 滚动条类型
    /// </summary>
    public enum ScrollbarType
    {
        Horizontal,
        Vertical,
    }
    public class Scrollbar
    {
        /// <summary>
        /// 包含滚动条的框架
        /// </summary>
        public Frame baseFrame;
        /// <summary>
        /// 滚动条按钮的像素值
        /// </summary>
        public float DisValue
        {
            get
            {
                return _disValue;
            }
            set
            {
                //设置滚动条像素值的边界
                if (value < 0)
                    _disValue = 0;
                else if (value > (scrollbarType == ScrollbarType.Horizontal ? BarWidth - ButtonWidth : BarHeight - ButtonHeight))
                    _disValue = scrollbarType == ScrollbarType.Horizontal ? BarWidth - ButtonWidth : BarHeight - ButtonHeight;
                else
                    _disValue = value;

            }
        }
        /// <summary>
        /// 滚动条的值
        /// </summary>
        public float Value
        {
            get
            {
                _value = DisValue / (scrollbarType == ScrollbarType.Horizontal ? BarWidth - ButtonWidth : BarHeight - ButtonHeight);
                if (_value < 0)
                    _value = 0;
                if (_value > 1)
                    _value = 1;
                return _value;
            }

            set
            {
                if (value < 0)
                    _value = 0;
                else if (value > 1)
                    _value = 1;
                else
                    _value = value;
            }
        }
        /// <summary>
        /// 滚动条的值
        /// </summary>
        private float _value;
        /// <summary>
        /// 滚动条的边界值
        /// </summary>
        private float _disValue;
        /// <summary>
        /// 滚动条按钮的矩形
        /// </summary>
        public Rectangle ScrollButton;
        /// <summary>
        /// 滚动条按钮的颜色
        /// </summary>
        public Color ButtonColor = Color.DeepSkyBlue;
        /// <summary>
        /// 滚动条空白的颜色
        /// </summary>
        public Color BarColor = Color.DimGray;
        /// <summary>
        /// 滚动条类型
        /// </summary>
        public ScrollbarType scrollbarType;
        /// <summary>
        /// 滚动条按钮的材质（B0：共享純白貼圖，顔色由 ButtonColor tint）
        /// </summary>
        public  Texture2D ButtonTexture;
        /// <summary>
        /// 滚动条的材质（B0：共享純白貼圖，顔色由 BarColor tint）
        /// </summary>
        public Texture2D BarTexture;
        /// <summary>
        /// 滚动条按钮的宽高（B0：原由程序化純色材質的尺寸承載，改用邏輯尺寸字段）
        /// </summary>
        public int ButtonWidth, ButtonHeight;
        /// <summary>
        /// 滚动条的宽高（B0：同上）
        /// </summary>
        public int BarWidth, BarHeight;
        /// <summary>
        /// 是否可见
        /// </summary>
        public bool Visible = true;
        /// <summary>
        /// 是否启用
        /// </summary>
        public bool Enable = true;
        /// <summary>
        /// 鼠标是否经过滚动条
        /// </summary>
        protected bool MouseOver = false;
        /// <summary>
        /// 鼠标是否经过滚动条按钮
        /// </summary>
        protected bool MouseOverButton = false;
        /// <summary>
        /// 滚动条和按钮的位置
        /// </summary>
        public Vector2 BarPos, ButtonPos;
        //protected DateTime? prePressTime;
        /// <summary>
        /// 鼠标滚轮滚动的距离
        /// </summary>
        public int RollingDistance;
        /// <summary>
        /// 上一次是否按下鼠标左键
        /// </summary>
        protected bool preDown = false;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="frame">包含滚动条的框架</param>
        /// <param name="scrollButton">包含按钮宽高设置的矩形</param>
        /// <param name="buttonColor">按钮颜色</param>
        /// <param name="barColor">空白颜色</param>
        /// <param name="type">滚动条的类型</param>
        /// <param name="rollingDistance">鼠标滚轮的距离</param>
        public Scrollbar(Frame frame, Rectangle scrollButton, Color buttonColor, Color barColor, ScrollbarType type = ScrollbarType.Vertical, int rollingDistance = 20)
        {
            baseFrame = frame;

            scrollbarType = type;
            ScrollButton = scrollButton;
            ButtonColor = buttonColor;
            BarColor = barColor;
            BarPos = new Vector2();
            ButtonPos = new Vector2();
            RollingDistance = rollingDistance;

            //if (type == ScrollbarType.Vertical)
            //    BarColor = Color.Black;

            // B0：SeasonXNA 紋理不可程序化生成（無 SetData），原「按尺寸生成純色材質」改為
            // 「共享純白貼圖 + 繪製時 tint」：材質統一引用 SolidTextures.White，
            // 實際寬高由 ButtonWidth/ButtonHeight/BarWidth/BarHeight 邏輯尺寸承載。
            ButtonTexture = SolidTextures.White;
            ButtonWidth = ScrollButton.Width;
            ButtonHeight = ScrollButton.Height;
            switch (scrollbarType)//根据不同类型重新设置框架可是范围和生成滚动条的材质
            {
                case ScrollbarType.Horizontal:
                    //baseFrame.VisualFrame.Height -= ScrollButton.Height;
                    BarTexture = SolidTextures.White;
                    BarWidth = baseFrame.VisualFrame.Width;
                    BarHeight = ScrollButton.Height;
                    BarPos.X = baseFrame.Position.X;//将滚动条位置设置放构造函数内可以使滚动条与框架的位置分离
                    BarPos.Y = baseFrame.Position.Y + baseFrame.VisualFrame.Height;
                    break;
                case ScrollbarType.Vertical:
                    //baseFrame.VisualFrame.Width -= ScrollButton.Width;
                    BarTexture = SolidTextures.White;
                    BarWidth = ScrollButton.Width;
                    BarHeight = baseFrame.VisualFrame.Height;
                    BarPos.X = baseFrame.Position.X + baseFrame.VisualFrame.Width;
                    BarPos.Y = baseFrame.Position.Y;
                    break;

            }

            DisValue = 0f;//此处必须放在BarTexture生成之后
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="frame">包含滚动条的框架</param>
        /// <param name="type">滚动条的类型</param>
        public Scrollbar(Frame frame, ScrollbarType type = ScrollbarType.Vertical) : this(frame, type == ScrollbarType.Vertical ? new Rectangle(0, 0, 13, 28) : new Rectangle(0, 0, 25, 13), Color.DeepSkyBlue, Color.DimGray, type)
        {
        }

        // B0：CreateScollbarTexture（new Texture2D + SetData 生成純色材質）已移除——
        // SeasonXNA 無程序化紋理寫入，純色改由「共享純白貼圖 + tint」在 Draw/IsInTexture 中以邏輯尺寸繪製/判定。

        public void Draw()
        {

            switch (scrollbarType)//根据滚动条的类型设置滚动条和按钮的位置坐标
            {
                case ScrollbarType.Horizontal:
                    //BarPos.X = baseFrame.Position.X;//将滚动条位置设置放在Draw方法内可以保障框架位置变动后，滚动条仍能显示在正确位置
                    //BarPos.Y = baseFrame.Position.Y + baseFrame.VisualFrame.Height;
                    ButtonPos = BarPos + new Vector2(DisValue, 0);
                    break;
                case ScrollbarType.Vertical:
                    //BarPos.X = baseFrame.Position.X + baseFrame.VisualFrame.Width;
                    //BarPos.Y = baseFrame.Position.Y;
                    ButtonPos = BarPos + new Vector2(0, DisValue);
                    break;
            }

            // B0：共享純白貼圖 + tint 拉伸（原為程序化純色材質整圖繪製）。
            Session.Current.SpriteBatch.Draw(BarTexture, new Rectangle((int)BarPos.X, (int)BarPos.Y, BarWidth, BarHeight), BarColor);//绘制滚动条
            Session.Current.SpriteBatch.Draw(ButtonTexture, new Rectangle((int)ButtonPos.X, (int)ButtonPos.Y, ButtonWidth, ButtonHeight), ButtonColor);//绘制滚动条按钮
        }
        
        /// <summary>
        /// 判断鼠标是否经过滚动条
        /// </summary>
        /// <param name="poX">鼠标横坐标</param>
        /// <param name="poY">鼠标纵坐标</param>
        /// <param name="basePos">偏移量</param>
        /// <returns></returns>
        public bool IsInTexture(float poX, float poY, Vector2? basePos)
        {
            if (Visible && Enable)
            {
                //PreMouseOver = MouseOver;
                if (basePos == null)
                {
                    MouseOver = BarPos.X <= poX && poX <= BarPos.X + BarWidth
                        && BarPos.Y <= poY && poY <= BarPos.Y + BarHeight;
                }
                else
                {
                    MouseOver = this.BarPos.X + ((Vector2)basePos).X <= poX && poX <= this.BarPos.X + ((Vector2)basePos).X + BarWidth
                        && this.BarPos.Y + ((Vector2)basePos).Y <= poY && poY <= this.BarPos.Y + ((Vector2)basePos).Y + BarHeight;
                }
            }
            else
            {
                MouseOver = false;
            }

            if (MouseOver)//判断鼠标是否经过滚动条按钮
            {
                if (basePos == null)
                {
                    MouseOverButton = ButtonPos.X <= poX && poX <= ButtonPos.X + ButtonWidth
                        && ButtonPos.Y <= poY && poY <= ButtonPos.Y + ButtonHeight;
                }
                else
                {
                    MouseOverButton = ButtonPos.X + ((Vector2)basePos).X <= poX && poX <= ButtonPos.X + ((Vector2)basePos).X + ButtonWidth
                        && ButtonPos.Y + ((Vector2)basePos).Y <= poY && poY <= ButtonPos.Y + ((Vector2)basePos).Y + ButtonHeight;
                }
            }
            return MouseOver;
        }

        public void Update()
        {
            Update(null);
        }
        public void Update(Vector2? basePos)
        {
            Update(InputManager.PoX, InputManager.PoY, basePos);
        }
        public void Update(int poX, int poY, Vector2? basePos)
        {
            Update(poX, poY, basePos, InputManager.IsPressed, InputManager.IsDown, false);
        }
        //SoundPlayer player = new SoundPlayer("Content/Textures/Resources/Start/Select");
        public void Update(int poX, int poY, Vector2? basePos, bool press, bool down, bool sound)
        {
            MouseOver = Enable && IsInTexture(Convert.ToSingle(poX), Convert.ToSingle(poY), basePos) && (poX != 0 || poY != 0);//设置鼠标是否经过滚动条的布尔值变量

            if (scrollbarType == ScrollbarType.Vertical && InputManager.PinchMove != 0 && IsInFrame(poX, poY))//如果鼠标在框架内滚动滚轮
            {
                DisValue -= InputManager.PinchMove * RollingDistance;//更改相应的按钮纵坐标像素值
                InputManager.PinchMove = 0;
                return;
            }

            //如果鼠标释放（按下的按钮）
            if (InputManager.IsReleased)//IsReleased一定要放在preDown判定之前，否则永远不会访问到
            {
                preDown = false;
                return;
            }
            if (preDown)//如果鼠标一直按下，即使偏离按钮一样可以起到按下滚动条按钮的功能（拖动按钮）
            {
                PressBarButton();
                return;
            }

            if (MouseOver)//如果鼠标经过滚动条
                if (MouseOverButton && down && InputManager.IsMoved)//判断鼠标释放拖动滚动条按钮
                    PressBarButton();
                else if (!MouseOverButton && press)//如果鼠标单击滚动条空白处
                    //单击滚动条空白处
                    PressBarBlank(poX, poY);
        }

        /// <summary>
        /// 判断鼠标是否在框架内
        /// </summary>
        /// <param name="poX">鼠标横坐标</param>
        /// <param name="poY">鼠标纵坐标</param>
        /// <returns>反正是否在框架内的布尔值</returns>
        protected bool IsInFrame(int poX, int poY)
        {
            bool inFrame = false;

            if (Visible && Enable)
            {
                //框架范围包括框架可视范围加上滚动条的范围
                Scrollbar scrollbar;
                scrollbar = baseFrame.Scrollbars.Where(sb => sb.scrollbarType == ScrollbarType.Horizontal).FirstOrDefault();
                int horizontalButtonHeight = scrollbar == null ? 0 : scrollbar.ButtonHeight;

                scrollbar = baseFrame.Scrollbars.Where(sb => sb.scrollbarType == ScrollbarType.Vertical).FirstOrDefault();
                int verticalButtonWidth = scrollbar == null ? 0 : scrollbar.ButtonWidth;

                inFrame = baseFrame.Position.X <= poX && poX <= baseFrame.Position.X + baseFrame.VisualFrame.Width + verticalButtonWidth
                        && baseFrame.Position.Y <= poY && poY <= baseFrame.Position.Y + baseFrame.VisualFrame.Height + horizontalButtonHeight;
            }
            return inFrame;
        }

        /// <summary>
        /// 按下滚动条按钮的动作
        /// </summary>
        protected void PressBarButton()
        {
            //InputManager.ClickTime++;
            //DateTime now = DateTime.Now;
            //if (prePressTime != null)
            //{
            //    TimeSpan ts = now - (DateTime)prePressTime;
            //    if (ts.TotalSeconds < 0.3f || !Platform.IsActive)
            //    {
            //        return;
            //    }
            //}
            //prePressTime = now;

            //根据不同的滚动条类型设置相应的滚动条按钮像素值
            if (scrollbarType == ScrollbarType.Horizontal)
                DisValue += InputManager.PoXMove;
            //else

            if (scrollbarType == ScrollbarType.Vertical)
                DisValue += InputManager.PoYMove;
            preDown = true;
            
        }

        /// <summary>
        /// 点击滚动条空白处
        /// </summary>
        /// <param name="poX">鼠标横坐标</param>
        /// <param name="poY">鼠标纵坐标</param>
        protected void PressBarBlank(int poX, int poY)
        {
            //根据不同的滚动条类型，设置相应的横纵移动像素值
            if (scrollbarType == ScrollbarType.Horizontal)
            {
                if (poX < ButtonPos.X)
                    DisValue -= BarWidth * 0.15f;
                if (poX > ButtonPos.X + ButtonWidth)
                    DisValue += BarWidth * 0.15f;
            }

            if (scrollbarType == ScrollbarType.Vertical)
            {
                if (poY < ButtonPos.Y)
                    DisValue -= BarHeight * 0.15f;
                if (poY > ButtonPos.Y + ButtonHeight)
                    DisValue += BarHeight * 0.15f;
                //DisValue += ((float)baseFrame.VisualFrame.Height / baseFrame.CanvasHeight) * BarTexture.Height;
            }
        }
    }
}
