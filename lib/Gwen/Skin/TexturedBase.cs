using Gwen.Skin.Texturing;
using System;
using System.Drawing;
using System.IO;
using Single = Gwen.Skin.Texturing.Single;

namespace Gwen.Skin
{
    #region UI element textures

    public struct SkinTextures
    {
        #region Fields

        public _CategoryList CategoryList;
        public _CheckBox CheckBox;
        public _Input Input;
        public _Menu Menu;
        public _Panel Panel;
        public _ProgressBar ProgressBar;
        public _RadioButton RadioButton;
        public _Scroller Scroller;
        public Bordered Selection;
        public Bordered Shadow;
        public Bordered StatusBar;
        public _Tab Tab;
        public _TextBox TextBox;
        public Bordered Tooltip;

        public _Tree Tree;

        public _Window Window;

        #endregion Fields

        #region Structs

        public struct _CategoryList
        {
            #region Fields

            public Bordered Header;
            public Bordered Inner;
            public Bordered Outer;

            #endregion Fields
        }

        public struct _CheckBox
        {
            #region Fields

            public _Active Active;

            public _Disabled Disabled;

            #endregion Fields

            #region Structs

            public struct _Active
            {
                #region Fields

                public Single Checked;
                public Single Normal;

                #endregion Fields
            }

            public struct _Disabled
            {
                #region Fields

                public Single Checked;
                public Single Normal;

                #endregion Fields
            }

            #endregion Structs
        }

        public struct _Input
        {
            #region Fields

            public _Button Button;

            public _ComboBox ComboBox;

            public _ListBox ListBox;

            public _Slider Slider;

            public _UpDown UpDown;

            #endregion Fields

            #region Structs

            public struct _Button
            {
                #region Fields

                public Bordered Disabled;
                public Bordered Hovered;
                public Bordered Normal;
                public Bordered Pressed;

                #endregion Fields
            }

            public struct _ComboBox
            {
                #region Fields

                public _Button Button;
                public Bordered Disabled;
                public Bordered Down;
                public Bordered Hover;
                public Bordered Normal;

                #endregion Fields

                #region Structs

                public struct _Button
                {
                    #region Fields

                    public Single Disabled;
                    public Single Down;
                    public Single Hover;
                    public Single Normal;

                    #endregion Fields
                }

                #endregion Structs
            }

            public struct _ListBox
            {
                #region Fields

                public Bordered Background;
                public Bordered EvenLine;
                public Bordered EvenLineSelected;
                public Bordered Hovered;
                public Bordered OddLine;
                public Bordered OddLineSelected;

                #endregion Fields
            }

            public struct _Slider
            {
                #region Fields

                public _H H;

                public _V V;

                #endregion Fields

                #region Structs

                public struct _H
                {
                    #region Fields

                    public Single Disabled;
                    public Single Down;
                    public Single Hover;
                    public Single Normal;

                    #endregion Fields
                }

                public struct _V
                {
                    #region Fields

                    public Single Disabled;
                    public Single Down;
                    public Single Hover;
                    public Single Normal;

                    #endregion Fields
                }

                #endregion Structs
            }

            public struct _UpDown
            {
                #region Fields

                public _Down Down;

                public _Up Up;

                #endregion Fields

                #region Structs

                public struct _Down
                {
                    #region Fields

                    public Single Disabled;
                    public Single Down;
                    public Single Hover;
                    public Single Normal;

                    #endregion Fields
                }

                public struct _Up
                {
                    #region Fields

                    public Single Disabled;
                    public Single Down;
                    public Single Hover;
                    public Single Normal;

                    #endregion Fields
                }

                #endregion Structs
            }

            #endregion Structs
        }

        public struct _Menu
        {
            #region Fields

            public Bordered Background;
            public Bordered BackgroundWithMargin;
            public Single Check;
            public Bordered Hover;
            public Single RightArrow;
            public Bordered Strip;

            #endregion Fields
        }

        public struct _Panel
        {
            #region Fields

            public Bordered Bright;
            public Bordered Dark;
            public Bordered Highlight;
            public Bordered Normal;

            #endregion Fields
        }

        public struct _ProgressBar
        {
            #region Fields

            public Bordered Back;
            public Bordered Front;

            #endregion Fields
        }

        public struct _RadioButton
        {
            #region Fields

            public _Active Active;

            public _Disabled Disabled;

            #endregion Fields

            #region Structs

            public struct _Active
            {
                #region Fields

                public Single Checked;
                public Single Normal;

                #endregion Fields
            }

            public struct _Disabled
            {
                #region Fields

                public Single Checked;
                public Single Normal;

                #endregion Fields
            }

            #endregion Structs
        }

        public struct _Scroller
        {
            #region Fields

            public _Button Button;
            public Bordered ButtonH_Disabled;
            public Bordered ButtonH_Down;
            public Bordered ButtonH_Hover;
            public Bordered ButtonH_Normal;
            public Bordered ButtonV_Disabled;
            public Bordered ButtonV_Down;
            public Bordered ButtonV_Hover;
            public Bordered ButtonV_Normal;
            public Bordered TrackH;
            public Bordered TrackV;

            #endregion Fields

            #region Structs

            public struct _Button
            {
                #region Fields

                public Bordered[] Disabled;
                public Bordered[] Down;
                public Bordered[] Hover;
                public Bordered[] Normal;

                #endregion Fields
            }

            #endregion Structs
        }

        public struct _Tab
        {
            #region Fields

            public _Bottom Bottom;

            public Bordered Control;

            public Bordered HeaderBar;

            public _Left Left;

            public _Right Right;

            public _Top Top;

            #endregion Fields

            #region Structs

            public struct _Bottom
            {
                #region Fields

                public Bordered Active;
                public Bordered Inactive;

                #endregion Fields
            }

            public struct _Left
            {
                #region Fields

                public Bordered Active;
                public Bordered Inactive;

                #endregion Fields
            }

            public struct _Right
            {
                #region Fields

                public Bordered Active;
                public Bordered Inactive;

                #endregion Fields
            }

            public struct _Top
            {
                #region Fields

                public Bordered Active;
                public Bordered Inactive;

                #endregion Fields
            }

            #endregion Structs
        }

        public struct _TextBox
        {
            #region Fields

            public Bordered Disabled;
            public Bordered Focus;
            public Bordered Normal;

            #endregion Fields
        }

        public struct _Tree
        {
            #region Fields

            public Bordered Background;
            public Single Minus;
            public Single Plus;

            #endregion Fields
        }

        public struct _Window
        {
            #region Fields

            public Single Close;
            public Single Close_Disabled;
            public Single Close_Down;
            public Single Close_Hover;
            public Bordered Inactive;
            public Bordered Normal;

            #endregion Fields
        }

        #endregion Structs
    }

    #endregion UI element textures

    /// <summary>
    /// Base textured skin.
    /// </summary>
    public class TexturedBase : Skin.SkinBase
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TexturedBase"/> class.
        /// </summary>
        /// <param name="renderer">Renderer to use.</param>
        /// <param name="textureName">Name of the skin texture map.</param>
        public TexturedBase(Renderer.RendererBase renderer, string textureName)
            : base(renderer)
        {
            m_Texture = new Texture(Renderer);
            m_Texture.Load(textureName);

            InitializeColors();
            InitializeTextures();
        }

        public TexturedBase(Renderer.RendererBase renderer, Stream textureData)
            : base(renderer)
        {
            m_Texture = new Texture(Renderer);
            m_Texture.LoadStream(textureData);

            InitializeColors();
            InitializeTextures();
        }
        private string _colorxml = null;
        public TexturedBase(Renderer.RendererBase renderer, Texture texture, string colorxml = null)
            : base(renderer)
        {
            m_Texture = texture;
            _colorxml = colorxml;
            InitializeColors();
            InitializeTextures();
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public override void Dispose()
        {
            m_Texture.Dispose();
            base.Dispose();
        }

        #endregion Methods

        #region Fields

        protected SkinTextures Textures;

        private readonly Texture m_Texture;

        #endregion Fields

        #region Initialization

        private void InitializeColors()
        {
            if (_colorxml != null)
            {
                System.Xml.XmlDocument read = new System.Xml.XmlDocument();

                read.LoadXml(_colorxml);
                var culture = System.Globalization.CultureInfo.InvariantCulture;
                Colors.Window.TitleActive = Color.FromArgb(int.Parse(read.DocumentElement["Window.TitleActive"].InnerText, System.Globalization.NumberStyles.HexNumber, culture));
                Colors.Window.TitleInactive = Color.FromArgb(int.Parse(read.DocumentElement["Window.TitleInactive"].InnerText, System.Globalization.NumberStyles.HexNumber, culture));
                Colors.Button.Normal = Color.FromArgb(int.Parse(read.DocumentElement["Button.Normal"].InnerText, System.Globalization.NumberStyles.HexNumber, culture));
                Colors.Button.Hover = Color.FromArgb(int.Parse(read.DocumentElement["Button.Hover"].InnerText, System.Globalization.NumberStyles.HexNumber, culture));
                Colors.Button.Down = Color.FromArgb(int.Parse(read.DocumentElement["Button.Down"].InnerText, System.Globalization.NumberStyles.HexNumber, culture));
                Colors.Button.Disabled = Color.FromArgb(int.Parse(read.DocumentElement["Button.Disabled"].InnerText, System.Globalization.NumberStyles.HexNumber, culture));

                Colors.Tab.Active.Normal = Color.FromArgb(int.Parse(read.DocumentElement["Tab.Active.Normal"].InnerText, System.Globalization.NumberStyles.HexNumber, culture));
                Colors.Tab.Active.Hover = Color.FromArgb(int.Parse(read.DocumentElement["Tab.Active.Hover"].InnerText, System.Globalization.NumberStyles.HexNumber, culture));
                Colors.Tab.Active.Down = Color.FromArgb(int.Parse(read.DocumentElement["Tab.Active.Down"].InnerText, System.Globalization.NumberStyles.HexNumber, culture));
                Colors.Tab.Active.Disabled = Color.FromArgb(int.Parse(read.DocumentElement["Tab.Active.Disabled"].InnerText, System.Globalization.NumberStyles.HexNumber, culture));
                Colors.Tab.Inactive.Normal = Color.FromArgb(int.Parse(read.DocumentElement["Tab.Inactive.Normal"].InnerText, System.Globalization.NumberStyles.HexNumber, culture));
                Colors.Tab.Inactive.Hover = Color.FromArgb(int.Parse(read.DocumentElement["Tab.Inactive.Hover"].InnerText, System.Globalization.NumberStyles.HexNumber, culture));
                Colors.Tab.Inactive.Down = Color.FromArgb(int.Parse(read.DocumentElement["Tab.Inactive.Down"].InnerText, System.Globalization.NumberStyles.HexNumber, culture));
                Colors.Tab.Inactive.Disabled = Color.FromArgb(int.Parse(read.DocumentElement["Tab.Inactive.Disabled"].InnerText, System.Globalization.NumberStyles.HexNumber, culture));

                Colors.Label.Default = Color.FromArgb(int.Parse(read.DocumentElement["Label.Default"].InnerText, System.Globalization.NumberStyles.HexNumber, culture));
                Colors.Label.Bright = Color.FromArgb(int.Parse(read.DocumentElement["Label.Bright"].InnerText, System.Globalization.NumberStyles.HexNumber, culture));
                Colors.Label.Dark = Color.FromArgb(int.Parse(read.DocumentElement["Label.Dark"].InnerText, System.Globalization.NumberStyles.HexNumber, culture));
                Colors.Label.Highlight = Color.FromArgb(int.Parse(read.DocumentElement["Label.Highlight"].InnerText, System.Globalization.NumberStyles.HexNumber, culture));

                Colors.Tree.Lines = Color.FromArgb(int.Parse(read.DocumentElement["Tree.Lines"].InnerText, System.Globalization.NumberStyles.HexNumber, culture));
                Colors.Tree.Normal = Color.FromArgb(int.Parse(read.DocumentElement["Tree.Normal"].InnerText, System.Globalization.NumberStyles.HexNumber, culture));
                Colors.Tree.Hover = Color.FromArgb(int.Parse(read.DocumentElement["Tree.Hover"].InnerText, System.Globalization.NumberStyles.HexNumber, culture));
                Colors.Tree.Selected = Color.FromArgb(int.Parse(read.DocumentElement["Tree.Selected"].InnerText, System.Globalization.NumberStyles.HexNumber, culture));

                Colors.Properties.Line_Normal = Color.FromArgb(int.Parse(read.DocumentElement["Properties.Line_Normal"].InnerText, System.Globalization.NumberStyles.HexNumber, culture));
                Colors.Properties.Line_Selected = Color.FromArgb(int.Parse(read.DocumentElement["Properties.Line_Selected"].InnerText, System.Globalization.NumberStyles.HexNumber, culture));
                Colors.Properties.Line_Hover = Color.FromArgb(int.Parse(read.DocumentElement["Properties.Line_Hover"].InnerText, System.Globalization.NumberStyles.HexNumber, culture));
                Colors.Properties.Title = Color.FromArgb(int.Parse(read.DocumentElement["Properties.Title"].InnerText, System.Globalization.NumberStyles.HexNumber, culture));
                Colors.Properties.Column_Normal = Color.FromArgb(int.Parse(read.DocumentElement["Properties.Column_Normal"].InnerText, System.Globalization.NumberStyles.HexNumber, culture));
                Colors.Properties.Column_Selected = Color.FromArgb(int.Parse(read.DocumentElement["Properties.Column_Selected"].InnerText, System.Globalization.NumberStyles.HexNumber, culture));
                Colors.Properties.Column_Hover = Color.FromArgb(int.Parse(read.DocumentElement["Properties.Column_Hover"].InnerText, System.Globalization.NumberStyles.HexNumber, culture));
                Colors.Properties.Border = Color.FromArgb(int.Parse(read.DocumentElement["Properties.Border"].InnerText, System.Globalization.NumberStyles.HexNumber, culture));
                Colors.Properties.Label_Normal = Color.FromArgb(int.Parse(read.DocumentElement["Properties.Label_Normal"].InnerText, System.Globalization.NumberStyles.HexNumber, culture));
                Colors.Properties.Label_Selected = Color.FromArgb(int.Parse(read.DocumentElement["Properties.Label_Selected"].InnerText, System.Globalization.NumberStyles.HexNumber, culture));
                Colors.Properties.Label_Hover = Color.FromArgb(int.Parse(read.DocumentElement["Properties.Label_Hover"].InnerText, System.Globalization.NumberStyles.HexNumber, culture));

                Colors.ModalBackground = Color.FromArgb(int.Parse(read.DocumentElement["ModalBackground"].InnerText, System.Globalization.NumberStyles.HexNumber, culture));

                Colors.TooltipText = Color.FromArgb(int.Parse(read.DocumentElement["TooltipText"].InnerText, System.Globalization.NumberStyles.HexNumber, culture));

                Colors.Category.Header = Color.FromArgb(int.Parse(read.DocumentElement["Category.Header"].InnerText, System.Globalization.NumberStyles.HexNumber, culture));
                Colors.Category.Header_Closed = Color.FromArgb(int.Parse(read.DocumentElement["Category.Header_Closed"].InnerText, System.Globalization.NumberStyles.HexNumber, culture));
                Colors.Category.Line.Text = Color.FromArgb(int.Parse(read.DocumentElement["Category.Line.Text"].InnerText, System.Globalization.NumberStyles.HexNumber, culture));
                Colors.Category.Line.Text_Hover = Color.FromArgb(int.Parse(read.DocumentElement["Category.Line.Text_Hover"].InnerText, System.Globalization.NumberStyles.HexNumber, culture));
                Colors.Category.Line.Text_Selected = Color.FromArgb(int.Parse(read.DocumentElement["Category.Line.Text_Selected"].InnerText, System.Globalization.NumberStyles.HexNumber, culture));
                Colors.Category.Line.Button = Color.FromArgb(int.Parse(read.DocumentElement["Category.Line.Button"].InnerText, System.Globalization.NumberStyles.HexNumber, culture));
                Colors.Category.Line.Button_Hover = Color.FromArgb(int.Parse(read.DocumentElement["Category.Line.Button_Hover"].InnerText, System.Globalization.NumberStyles.HexNumber, culture));
                Colors.Category.Line.Button_Selected = Color.FromArgb(int.Parse(read.DocumentElement["Category.Line.Button_Selected"].InnerText, System.Globalization.NumberStyles.HexNumber, culture));
                Colors.Category.LineAlt.Text = Color.FromArgb(int.Parse(read.DocumentElement["Category.LineAlt.Text"].InnerText, System.Globalization.NumberStyles.HexNumber, culture));
                Colors.Category.LineAlt.Text_Hover = Color.FromArgb(int.Parse(read.DocumentElement["Category.LineAlt.Text_Hover"].InnerText, System.Globalization.NumberStyles.HexNumber, culture));
                Colors.Category.LineAlt.Text_Selected = Color.FromArgb(int.Parse(read.DocumentElement["Category.LineAlt.Text_Selected"].InnerText, System.Globalization.NumberStyles.HexNumber, culture));
                Colors.Category.LineAlt.Button = Color.FromArgb(int.Parse(read.DocumentElement["Category.LineAlt.Button"].InnerText, System.Globalization.NumberStyles.HexNumber, culture));
                Colors.Category.LineAlt.Button_Hover = Color.FromArgb(int.Parse(read.DocumentElement["Category.LineAlt.Button_Hover"].InnerText, System.Globalization.NumberStyles.HexNumber, culture));
                Colors.Category.LineAlt.Button_Selected = Color.FromArgb(int.Parse(read.DocumentElement["Category.LineAlt.Button_Selected"].InnerText, System.Globalization.NumberStyles.HexNumber, culture));
            }
            else
            {
                Colors.Window.TitleActive = Renderer.PixelColor(m_Texture, 4 + 8 * 0, 508, Color.Red);
                Colors.Window.TitleInactive = Renderer.PixelColor(m_Texture, 4 + 8 * 1, 508, Color.Yellow);
                Colors.Button.Normal = Renderer.PixelColor(m_Texture, 4 + 8 * 2, 508, Color.Yellow);
                Colors.Button.Hover = Renderer.PixelColor(m_Texture, 4 + 8 * 3, 508, Color.Yellow);
                Colors.Button.Down = Renderer.PixelColor(m_Texture, 4 + 8 * 2, 500, Color.Yellow);
                Colors.Button.Disabled = Renderer.PixelColor(m_Texture, 4 + 8 * 3, 500, Color.Yellow);

                Colors.Tab.Active.Normal = Renderer.PixelColor(m_Texture, 4 + 8 * 4, 508, Color.Yellow);
                Colors.Tab.Active.Hover = Renderer.PixelColor(m_Texture, 4 + 8 * 5, 508, Color.Yellow);
                Colors.Tab.Active.Down = Renderer.PixelColor(m_Texture, 4 + 8 * 4, 500, Color.Yellow);
                Colors.Tab.Active.Disabled = Renderer.PixelColor(m_Texture, 4 + 8 * 5, 500, Color.Yellow);
                Colors.Tab.Inactive.Normal = Renderer.PixelColor(m_Texture, 4 + 8 * 6, 508, Color.Yellow);
                Colors.Tab.Inactive.Hover = Renderer.PixelColor(m_Texture, 4 + 8 * 7, 508, Color.Yellow);
                Colors.Tab.Inactive.Down = Renderer.PixelColor(m_Texture, 4 + 8 * 6, 500, Color.Yellow);
                Colors.Tab.Inactive.Disabled = Renderer.PixelColor(m_Texture, 4 + 8 * 7, 500, Color.Yellow);

                Colors.Label.Default = Renderer.PixelColor(m_Texture, 4 + 8 * 8, 508, Color.Yellow);
                Colors.Label.Bright = Renderer.PixelColor(m_Texture, 4 + 8 * 9, 508, Color.Yellow);
                Colors.Label.Dark = Renderer.PixelColor(m_Texture, 4 + 8 * 8, 500, Color.Yellow);
                Colors.Label.Highlight = Renderer.PixelColor(m_Texture, 4 + 8 * 9, 500, Color.Yellow);

                Colors.Tree.Lines = Renderer.PixelColor(m_Texture, 4 + 8 * 10, 508, Color.Yellow);
                Colors.Tree.Normal = Renderer.PixelColor(m_Texture, 4 + 8 * 11, 508, Color.Yellow);
                Colors.Tree.Hover = Renderer.PixelColor(m_Texture, 4 + 8 * 10, 500, Color.Yellow);
                Colors.Tree.Selected = Renderer.PixelColor(m_Texture, 4 + 8 * 11, 500, Color.Yellow);

                Colors.Properties.Line_Normal = Renderer.PixelColor(m_Texture, 4 + 8 * 12, 508, Color.Yellow);
                Colors.Properties.Line_Selected = Renderer.PixelColor(m_Texture, 4 + 8 * 13, 508, Color.Yellow);
                Colors.Properties.Line_Hover = Renderer.PixelColor(m_Texture, 4 + 8 * 12, 500, Color.Yellow);
                Colors.Properties.Title = Renderer.PixelColor(m_Texture, 4 + 8 * 13, 500, Color.Yellow);
                Colors.Properties.Column_Normal = Renderer.PixelColor(m_Texture, 4 + 8 * 14, 508, Color.Yellow);
                Colors.Properties.Column_Selected = Renderer.PixelColor(m_Texture, 4 + 8 * 15, 508, Color.Yellow);
                Colors.Properties.Column_Hover = Renderer.PixelColor(m_Texture, 4 + 8 * 14, 500, Color.Yellow);
                Colors.Properties.Border = Renderer.PixelColor(m_Texture, 4 + 8 * 15, 500, Color.Yellow);
                Colors.Properties.Label_Normal = Renderer.PixelColor(m_Texture, 4 + 8 * 16, 508, Color.Yellow);
                Colors.Properties.Label_Selected = Renderer.PixelColor(m_Texture, 4 + 8 * 17, 508, Color.Yellow);
                Colors.Properties.Label_Hover = Renderer.PixelColor(m_Texture, 4 + 8 * 16, 500, Color.Yellow);

                Colors.ModalBackground = Renderer.PixelColor(m_Texture, 4 + 8 * 18, 508, Color.Yellow);

                Colors.TooltipText = Renderer.PixelColor(m_Texture, 4 + 8 * 19, 508, Color.Yellow);

                Colors.Category.Header = Renderer.PixelColor(m_Texture, 4 + 8 * 18, 500, Color.Yellow);
                Colors.Category.Header_Closed = Renderer.PixelColor(m_Texture, 4 + 8 * 19, 500, Color.Yellow);
                Colors.Category.Line.Text = Renderer.PixelColor(m_Texture, 4 + 8 * 20, 508, Color.Yellow);
                Colors.Category.Line.Text_Hover = Renderer.PixelColor(m_Texture, 4 + 8 * 21, 508, Color.Yellow);
                Colors.Category.Line.Text_Selected = Renderer.PixelColor(m_Texture, 4 + 8 * 20, 500, Color.Yellow);
                Colors.Category.Line.Button = Renderer.PixelColor(m_Texture, 4 + 8 * 21, 500, Color.Yellow);
                Colors.Category.Line.Button_Hover = Renderer.PixelColor(m_Texture, 4 + 8 * 22, 508, Color.Yellow);
                Colors.Category.Line.Button_Selected = Renderer.PixelColor(m_Texture, 4 + 8 * 23, 508, Color.Yellow);
                Colors.Category.LineAlt.Text = Renderer.PixelColor(m_Texture, 4 + 8 * 22, 500, Color.Yellow);
                Colors.Category.LineAlt.Text_Hover = Renderer.PixelColor(m_Texture, 4 + 8 * 23, 500, Color.Yellow);
                Colors.Category.LineAlt.Text_Selected = Renderer.PixelColor(m_Texture, 4 + 8 * 24, 508, Color.Yellow);
                Colors.Category.LineAlt.Button = Renderer.PixelColor(m_Texture, 4 + 8 * 25, 508, Color.Yellow);
                Colors.Category.LineAlt.Button_Hover = Renderer.PixelColor(m_Texture, 4 + 8 * 24, 500, Color.Yellow);
                Colors.Category.LineAlt.Button_Selected = Renderer.PixelColor(m_Texture, 4 + 8 * 25, 500, Color.Yellow);
            }
        }
        private void ColorsToXML()
        {
            var settings = new System.Xml.XmlWriterSettings();
            settings.OmitXmlDeclaration = true;
            settings.Indent = true;
            settings.NewLineOnAttributes = true;
            using (System.Xml.XmlWriter wr = System.Xml.XmlWriter.Create("DefaultColors.xml", settings))
            {
                wr.WriteStartDocument();
                wr.WriteStartElement("Colors");
                wr.WriteElementString("Window.TitleActive", Colors.Window.TitleActive.ToArgb().ToString("X8"));
                wr.WriteElementString("Window.TitleInactive", Colors.Window.TitleInactive.ToArgb().ToString("X8"));
                wr.WriteElementString("Button.Normal", Colors.Button.Normal.ToArgb().ToString("X8"));
                wr.WriteElementString("Button.Hover", Colors.Button.Hover.ToArgb().ToString("X8"));
                wr.WriteElementString("Button.Down", Colors.Button.Down.ToArgb().ToString("X8"));
                wr.WriteElementString("Button.Disabled", Colors.Button.Disabled.ToArgb().ToString("X8"));
                wr.WriteElementString("Tab.Active.Normal", Colors.Tab.Active.Normal.ToArgb().ToString("X8"));
                wr.WriteElementString("Tab.Active.Hover", Colors.Tab.Active.Hover.ToArgb().ToString("X8"));
                wr.WriteElementString("Tab.Active.Down", Colors.Tab.Active.Down.ToArgb().ToString("X8"));
                wr.WriteElementString("Tab.Active.Disabled", Colors.Tab.Active.Disabled.ToArgb().ToString("X8"));
                wr.WriteElementString("Tab.Inactive.Normal", Colors.Tab.Inactive.Normal.ToArgb().ToString("X8"));
                wr.WriteElementString("Tab.Inactive.Hover", Colors.Tab.Inactive.Hover.ToArgb().ToString("X8"));
                wr.WriteElementString("Tab.Inactive.Down", Colors.Tab.Inactive.Down.ToArgb().ToString("X8"));
                wr.WriteElementString("Tab.Inactive.Disabled", Colors.Tab.Inactive.Disabled.ToArgb().ToString("X8"));
                wr.WriteElementString("Label.Default", Colors.Label.Default.ToArgb().ToString("X8"));
                wr.WriteElementString("Label.Bright", Colors.Label.Bright.ToArgb().ToString("X8"));
                wr.WriteElementString("Label.Dark", Colors.Label.Dark.ToArgb().ToString("X8"));
                wr.WriteElementString("Label.Highlight", Colors.Label.Highlight.ToArgb().ToString("X8"));
                wr.WriteElementString("Tree.Lines", Colors.Tree.Lines.ToArgb().ToString("X8"));
                wr.WriteElementString("Tree.Normal", Colors.Tree.Normal.ToArgb().ToString("X8"));
                wr.WriteElementString("Tree.Hover", Colors.Tree.Hover.ToArgb().ToString("X8"));
                wr.WriteElementString("Tree.Selected", Colors.Tree.Selected.ToArgb().ToString("X8"));
                wr.WriteElementString("Properties.Line_Normal", Colors.Properties.Line_Normal.ToArgb().ToString("X8"));
                wr.WriteElementString("Properties.Line_Selected", Colors.Properties.Line_Selected.ToArgb().ToString("X8"));
                wr.WriteElementString("Properties.Line_Hover", Colors.Properties.Line_Hover.ToArgb().ToString("X8"));
                wr.WriteElementString("Properties.Title", Colors.Properties.Title.ToArgb().ToString("X8"));
                wr.WriteElementString("Properties.Column_Normal", Colors.Properties.Column_Normal.ToArgb().ToString("X8"));
                wr.WriteElementString("Properties.Column_Selected", Colors.Properties.Column_Selected.ToArgb().ToString("X8"));
                wr.WriteElementString("Properties.Column_Hover", Colors.Properties.Column_Hover.ToArgb().ToString("X8"));
                wr.WriteElementString("Properties.Border", Colors.Properties.Border.ToArgb().ToString("X8"));
                wr.WriteElementString("Properties.Label_Normal", Colors.Properties.Label_Normal.ToArgb().ToString("X8"));
                wr.WriteElementString("Properties.Label_Selected", Colors.Properties.Label_Selected.ToArgb().ToString("X8"));
                wr.WriteElementString("Properties.Label_Hover", Colors.Properties.Label_Hover.ToArgb().ToString("X8"));
                wr.WriteElementString("ModalBackground", Colors.ModalBackground.ToArgb().ToString("X8"));
                wr.WriteElementString("TooltipText", Colors.TooltipText.ToArgb().ToString("X8"));
                wr.WriteElementString("Category.Header", Colors.Category.Header.ToArgb().ToString("X8"));
                wr.WriteElementString("Category.Header_Closed", Colors.Category.Header_Closed.ToArgb().ToString("X8"));
                wr.WriteElementString("Category.Line.Text", Colors.Category.Line.Text.ToArgb().ToString("X8"));
                wr.WriteElementString("Category.Line.Text_Hover", Colors.Category.Line.Text_Hover.ToArgb().ToString("X8"));
                wr.WriteElementString("Category.Line.Text_Selected", Colors.Category.Line.Text_Selected.ToArgb().ToString("X8"));
                wr.WriteElementString("Category.Line.Button", Colors.Category.Line.Button.ToArgb().ToString("X8"));
                wr.WriteElementString("Category.Line.Button_Hover", Colors.Category.Line.Button_Hover.ToArgb().ToString("X8"));
                wr.WriteElementString("Category.Line.Button_Selected", Colors.Category.Line.Button_Selected.ToArgb().ToString("X8"));
                wr.WriteElementString("Category.LineAlt.Text", Colors.Category.LineAlt.Text.ToArgb().ToString("X8"));
                wr.WriteElementString("Category.LineAlt.Text_Hover", Colors.Category.LineAlt.Text_Hover.ToArgb().ToString("X8"));
                wr.WriteElementString("Category.LineAlt.Text_Selected", Colors.Category.LineAlt.Text_Selected.ToArgb().ToString("X8"));
                wr.WriteElementString("Category.LineAlt.Button", Colors.Category.LineAlt.Button.ToArgb().ToString("X8"));
                wr.WriteElementString("Category.LineAlt.Button_Hover", Colors.Category.LineAlt.Button_Hover.ToArgb().ToString("X8"));
                wr.WriteElementString("Category.LineAlt.Button_Selected", Colors.Category.LineAlt.Button_Selected.ToArgb().ToString("X8"));
                wr.WriteEndElement();
                wr.WriteEndDocument();
                wr.Flush();
            }
        }

        private void InitializeTextures()
        {
            Textures.Shadow = new Bordered(m_Texture, 448, 0, 31, 31, Margin.Eight);
            Textures.Tooltip = new Bordered(m_Texture, 128, 320, 127, 31, Margin.Eight);
            Textures.StatusBar = new Bordered(m_Texture, 128, 288, 127, 31, Margin.Eight);
            Textures.Selection = new Bordered(m_Texture, 384, 32, 31, 31, Margin.Four);

            Textures.Panel.Normal = new Bordered(m_Texture, 256, 0, 63, 63, new Margin(16, 16, 16, 16));
            Textures.Panel.Bright = new Bordered(m_Texture, 256 + 64, 0, 63, 63, new Margin(16, 16, 16, 16));
            Textures.Panel.Dark = new Bordered(m_Texture, 256, 64, 63, 63, new Margin(16, 16, 16, 16));
            Textures.Panel.Highlight = new Bordered(m_Texture, 256 + 64, 64, 63, 63, new Margin(16, 16, 16, 16));

            Textures.Window.Normal = new Bordered(m_Texture, 0, 0, 127, 127, new Margin(8, 32, 8, 8));
            Textures.Window.Inactive = new Bordered(m_Texture, 128, 0, 127, 127, new Margin(8, 32, 8, 8));

            Textures.CheckBox.Active.Checked = new Single(m_Texture, 448, 32, 15, 15);
            Textures.CheckBox.Active.Normal = new Single(m_Texture, 464, 32, 15, 15);
            Textures.CheckBox.Disabled.Normal = new Single(m_Texture, 448, 48, 15, 15);
            Textures.CheckBox.Disabled.Normal = new Single(m_Texture, 464, 48, 15, 15);

            Textures.RadioButton.Active.Checked = new Single(m_Texture, 448, 64, 15, 15);
            Textures.RadioButton.Active.Normal = new Single(m_Texture, 464, 64, 15, 15);
            Textures.RadioButton.Disabled.Normal = new Single(m_Texture, 448, 80, 15, 15);
            Textures.RadioButton.Disabled.Normal = new Single(m_Texture, 464, 80, 15, 15);

            Textures.TextBox.Normal = new Bordered(m_Texture, 0, 150, 127, 21, Margin.Four);
            Textures.TextBox.Focus = new Bordered(m_Texture, 0, 172, 127, 21, Margin.Four);
            Textures.TextBox.Disabled = new Bordered(m_Texture, 0, 193, 127, 21, Margin.Four);

            Textures.Menu.Strip = new Bordered(m_Texture, 0, 128, 127, 21, Margin.One);
            Textures.Menu.BackgroundWithMargin = new Bordered(m_Texture, 128, 128, 127, 63, new Margin(24, 8, 8, 8));
            Textures.Menu.Background = new Bordered(m_Texture, 128, 192, 127, 63, Margin.Eight);
            Textures.Menu.Hover = new Bordered(m_Texture, 128, 256, 127, 31, Margin.Eight);
            Textures.Menu.RightArrow = new Single(m_Texture, 464, 112, 15, 15);
            Textures.Menu.Check = new Single(m_Texture, 448, 112, 15, 15);

            Textures.Tab.Control = new Bordered(m_Texture, 0, 256, 127, 127, Margin.Eight);
            Textures.Tab.Bottom.Active = new Bordered(m_Texture, 0, 416, 63, 31, Margin.Eight);
            Textures.Tab.Bottom.Inactive = new Bordered(m_Texture, 0 + 128, 416, 63, 31, Margin.Eight);
            Textures.Tab.Top.Active = new Bordered(m_Texture, 0, 384, 63, 31, Margin.Eight);
            Textures.Tab.Top.Inactive = new Bordered(m_Texture, 0 + 128, 384, 63, 31, Margin.Eight);
            Textures.Tab.Left.Active = new Bordered(m_Texture, 64, 384, 31, 63, Margin.Eight);
            Textures.Tab.Left.Inactive = new Bordered(m_Texture, 64 + 128, 384, 31, 63, Margin.Eight);
            Textures.Tab.Right.Active = new Bordered(m_Texture, 96, 384, 31, 63, Margin.Eight);
            Textures.Tab.Right.Inactive = new Bordered(m_Texture, 96 + 128, 384, 31, 63, Margin.Eight);
            Textures.Tab.HeaderBar = new Bordered(m_Texture, 128, 352, 127, 31, Margin.Four);

            Textures.Window.Close = new Single(m_Texture, 0, 224, 24, 24);
            Textures.Window.Close_Hover = new Single(m_Texture, 32, 224, 24, 24);
            Textures.Window.Close_Down = new Single(m_Texture, 64, 224, 24, 24);
            Textures.Window.Close_Disabled = new Single(m_Texture, 96, 224, 24, 24);

            Textures.Scroller.TrackV = new Bordered(m_Texture, 384, 208, 15, 127, Margin.Four);
            Textures.Scroller.ButtonV_Normal = new Bordered(m_Texture, 384 + 16, 208, 15, 127, Margin.Four);
            Textures.Scroller.ButtonV_Hover = new Bordered(m_Texture, 384 + 32, 208, 15, 127, Margin.Four);
            Textures.Scroller.ButtonV_Down = new Bordered(m_Texture, 384 + 48, 208, 15, 127, Margin.Four);
            Textures.Scroller.ButtonV_Disabled = new Bordered(m_Texture, 384 + 64, 208, 15, 127, Margin.Four);
            Textures.Scroller.TrackH = new Bordered(m_Texture, 384, 128, 127, 15, Margin.Four);
            Textures.Scroller.ButtonH_Normal = new Bordered(m_Texture, 384, 128 + 16, 127, 15, Margin.Four);
            Textures.Scroller.ButtonH_Hover = new Bordered(m_Texture, 384, 128 + 32, 127, 15, Margin.Four);
            Textures.Scroller.ButtonH_Down = new Bordered(m_Texture, 384, 128 + 48, 127, 15, Margin.Four);
            Textures.Scroller.ButtonH_Disabled = new Bordered(m_Texture, 384, 128 + 64, 127, 15, Margin.Four);

            Textures.Scroller.Button.Normal = new Bordered[4];
            Textures.Scroller.Button.Disabled = new Bordered[4];
            Textures.Scroller.Button.Hover = new Bordered[4];
            Textures.Scroller.Button.Down = new Bordered[4];

            Textures.Tree.Background = new Bordered(m_Texture, 256, 128, 127, 127, new Margin(16, 16, 16, 16));
            Textures.Tree.Plus = new Single(m_Texture, 448, 96, 15, 15);
            Textures.Tree.Minus = new Single(m_Texture, 464, 96, 15, 15);

            Textures.Input.Button.Normal = new Bordered(m_Texture, 480, 0, 31, 31, Margin.Eight);
            Textures.Input.Button.Hovered = new Bordered(m_Texture, 480, 32, 31, 31, Margin.Eight);
            Textures.Input.Button.Disabled = new Bordered(m_Texture, 480, 64, 31, 31, Margin.Eight);
            Textures.Input.Button.Pressed = new Bordered(m_Texture, 480, 96, 31, 31, Margin.Eight);

            for (int i = 0; i < 4; i++)
            {
                Textures.Scroller.Button.Normal[i] = new Bordered(m_Texture, 464 + 0, 208 + i * 16, 15, 15, Margin.Two);
                Textures.Scroller.Button.Hover[i] = new Bordered(m_Texture, 480, 208 + i * 16, 15, 15, Margin.Two);
                Textures.Scroller.Button.Down[i] = new Bordered(m_Texture, 464, 272 + i * 16, 15, 15, Margin.Two);
                Textures.Scroller.Button.Disabled[i] = new Bordered(m_Texture, 480 + 48, 272 + i * 16, 15, 15, Margin.Two);
            }

            Textures.Input.ListBox.Background = new Bordered(m_Texture, 256, 256, 63, 127, Margin.Eight);
            Textures.Input.ListBox.Hovered = new Bordered(m_Texture, 320, 320, 31, 31, Margin.Eight);
            Textures.Input.ListBox.EvenLine = new Bordered(m_Texture, 352, 256, 31, 31, Margin.Eight);
            Textures.Input.ListBox.OddLine = new Bordered(m_Texture, 352, 288, 31, 31, Margin.Eight);
            Textures.Input.ListBox.EvenLineSelected = new Bordered(m_Texture, 320, 270, 31, 31, Margin.Eight);
            Textures.Input.ListBox.OddLineSelected = new Bordered(m_Texture, 320, 288, 31, 31, Margin.Eight);

            Textures.Input.ComboBox.Normal = new Bordered(m_Texture, 384, 336, 127, 31, new Margin(8, 8, 32, 8));
            Textures.Input.ComboBox.Hover = new Bordered(m_Texture, 384, 336 + 32, 127, 31, new Margin(8, 8, 32, 8));
            Textures.Input.ComboBox.Down = new Bordered(m_Texture, 384, 336 + 64, 127, 31, new Margin(8, 8, 32, 8));
            Textures.Input.ComboBox.Disabled = new Bordered(m_Texture, 384, 336 + 96, 127, 31, new Margin(8, 8, 32, 8));

            Textures.Input.ComboBox.Button.Normal = new Single(m_Texture, 496, 272, 15, 15);
            Textures.Input.ComboBox.Button.Hover = new Single(m_Texture, 496, 272 + 16, 15, 15);
            Textures.Input.ComboBox.Button.Down = new Single(m_Texture, 496, 272 + 32, 15, 15);
            Textures.Input.ComboBox.Button.Disabled = new Single(m_Texture, 496, 272 + 48, 15, 15);

            Textures.Input.UpDown.Up.Normal = new Single(m_Texture, 384, 112, 7, 7);
            Textures.Input.UpDown.Up.Hover = new Single(m_Texture, 384 + 8, 112, 7, 7);
            Textures.Input.UpDown.Up.Down = new Single(m_Texture, 384 + 16, 112, 7, 7);
            Textures.Input.UpDown.Up.Disabled = new Single(m_Texture, 384 + 24, 112, 7, 7);
            Textures.Input.UpDown.Down.Normal = new Single(m_Texture, 384, 120, 7, 7);
            Textures.Input.UpDown.Down.Hover = new Single(m_Texture, 384 + 8, 120, 7, 7);
            Textures.Input.UpDown.Down.Down = new Single(m_Texture, 384 + 16, 120, 7, 7);
            Textures.Input.UpDown.Down.Disabled = new Single(m_Texture, 384 + 24, 120, 7, 7);

            Textures.ProgressBar.Back = new Bordered(m_Texture, 384, 0, 31, 31, Margin.Eight);
            Textures.ProgressBar.Front = new Bordered(m_Texture, 384 + 32, 0, 31, 31, Margin.Eight);

            Textures.Input.Slider.H.Normal = new Single(m_Texture, 416, 32, 15, 15);
            Textures.Input.Slider.H.Hover = new Single(m_Texture, 416, 32 + 16, 15, 15);
            Textures.Input.Slider.H.Down = new Single(m_Texture, 416, 32 + 32, 15, 15);
            Textures.Input.Slider.H.Disabled = new Single(m_Texture, 416, 32 + 48, 15, 15);

            Textures.Input.Slider.V.Normal = new Single(m_Texture, 416 + 16, 32, 15, 15);
            Textures.Input.Slider.V.Hover = new Single(m_Texture, 416 + 16, 32 + 16, 15, 15);
            Textures.Input.Slider.V.Down = new Single(m_Texture, 416 + 16, 32 + 32, 15, 15);
            Textures.Input.Slider.V.Disabled = new Single(m_Texture, 416 + 16, 32 + 48, 15, 15);

            Textures.CategoryList.Outer = new Bordered(m_Texture, 256, 384, 63, 63, Margin.Eight);
            Textures.CategoryList.Inner = new Bordered(m_Texture, 256 + 64, 384, 63, 63, new Margin(8, 21, 8, 8));
            Textures.CategoryList.Header = new Bordered(m_Texture, 320, 352, 63, 31, Margin.Eight);
        }

        #endregion Initialization

        #region UI elements

        public override void DrawButton(Controls.ControlBase control, bool depressed, bool hovered, bool disabled)
        {
            if (disabled)
            {
                Textures.Input.Button.Disabled.Draw(Renderer, control.RenderBounds);
                return;
            }
            if (depressed)
            {
                Textures.Input.Button.Pressed.Draw(Renderer, control.RenderBounds);
                return;
            }
            if (hovered)
            {
                Textures.Input.Button.Hovered.Draw(Renderer, control.RenderBounds);
                return;
            }
            Textures.Input.Button.Normal.Draw(Renderer, control.RenderBounds);
        }

        public override void DrawCategoryHolder(Controls.ControlBase control)
        {
            Textures.CategoryList.Outer.Draw(Renderer, control.RenderBounds);
        }

        public override void DrawCategoryInner(Controls.ControlBase control, bool collapsed)
        {
            if (collapsed)
                Textures.CategoryList.Header.Draw(Renderer, control.RenderBounds);
            else
                Textures.CategoryList.Inner.Draw(Renderer, control.RenderBounds);
        }

        public override void DrawCheckBox(Controls.ControlBase control, bool selected, bool depressed)
        {
            if (selected)
            {
                if (control.IsDisabled)
                    Textures.CheckBox.Disabled.Checked.Draw(Renderer, control.RenderBounds);
                else
                    Textures.CheckBox.Active.Checked.Draw(Renderer, control.RenderBounds);
            }
            else
            {
                if (control.IsDisabled)
                    Textures.CheckBox.Disabled.Normal.Draw(Renderer, control.RenderBounds);
                else
                    Textures.CheckBox.Active.Normal.Draw(Renderer, control.RenderBounds);
            }
        }

        public override void DrawColorDisplay(Controls.ControlBase control, Color color)
        {
            Rectangle rect = control.RenderBounds;

            if (color.A != 255)
            {
                Renderer.DrawColor = Color.FromArgb(255, 255, 255, 255);
                Renderer.DrawFilledRect(rect);

                Renderer.DrawColor = Color.FromArgb(128, 128, 128, 128);

                Renderer.DrawFilledRect(Util.FloatRect(0, 0, rect.Width * 0.5f, rect.Height * 0.5f));
                Renderer.DrawFilledRect(Util.FloatRect(rect.Width * 0.5f, rect.Height * 0.5f, rect.Width * 0.5f, rect.Height * 0.5f));
            }

            Renderer.DrawColor = color;
            Renderer.DrawFilledRect(rect);

            Renderer.DrawColor = Color.Black;
            Renderer.DrawLinedRect(rect);
        }

        public override void DrawComboBox(Controls.ControlBase control, bool down, bool open)
        {
            if (control.IsDisabled)
            {
                Textures.Input.ComboBox.Disabled.Draw(Renderer, control.RenderBounds);
                return;
            }

            if (down || open)
            {
                Textures.Input.ComboBox.Down.Draw(Renderer, control.RenderBounds);
                return;
            }

            if (control.IsHovered)
            {
                Textures.Input.ComboBox.Down.Draw(Renderer, control.RenderBounds);
                return;
            }

            Textures.Input.ComboBox.Normal.Draw(Renderer, control.RenderBounds);
        }

        public override void DrawComboBoxArrow(Controls.ControlBase control, bool hovered, bool down, bool open, bool disabled)
        {
            if (disabled)
            {
                Textures.Input.ComboBox.Button.Disabled.Draw(Renderer, control.RenderBounds);
                return;
            }

            if (down || open)
            {
                Textures.Input.ComboBox.Button.Down.Draw(Renderer, control.RenderBounds);
                return;
            }

            if (hovered)
            {
                Textures.Input.ComboBox.Button.Hover.Draw(Renderer, control.RenderBounds);
                return;
            }

            Textures.Input.ComboBox.Button.Normal.Draw(Renderer, control.RenderBounds);
        }

        public override void DrawGroupBox(Controls.ControlBase control, int textStart, int textHeight, int textWidth)
        {
            Rectangle rect = control.RenderBounds;

            rect.Y += (int)(textHeight * 0.5f);
            rect.Height -= (int)(textHeight * 0.5f);

            Color m_colDarker = Color.FromArgb(50, 0, 50, 60);
            Color m_colLighter = Color.FromArgb(150, 255, 255, 255);

            Renderer.DrawColor = m_colLighter;

            Renderer.DrawFilledRect(new Rectangle(rect.X + 1, rect.Y + 1, textStart - 3, 1));
            Renderer.DrawFilledRect(new Rectangle(rect.X + 1 + textStart + textWidth, rect.Y + 1, rect.Width - textStart + textWidth - 2, 1));
            Renderer.DrawFilledRect(new Rectangle(rect.X + 1, (rect.Y + rect.Height) - 1, rect.X + rect.Width - 2, 1));

            Renderer.DrawFilledRect(new Rectangle(rect.X + 1, rect.Y + 1, 1, rect.Height));
            Renderer.DrawFilledRect(new Rectangle((rect.X + rect.Width) - 2, rect.Y + 1, 1, rect.Height - 1));

            Renderer.DrawColor = m_colDarker;

            Renderer.DrawFilledRect(new Rectangle(rect.X + 1, rect.Y, textStart - 3, 1));
            Renderer.DrawFilledRect(new Rectangle(rect.X + 1 + textStart + textWidth, rect.Y, rect.Width - textStart - textWidth - 2, 1));
            Renderer.DrawFilledRect(new Rectangle(rect.X + 1, (rect.Y + rect.Height) - 1, rect.X + rect.Width - 2, 1));

            Renderer.DrawFilledRect(new Rectangle(rect.X, rect.Y + 1, 1, rect.Height - 1));
            Renderer.DrawFilledRect(new Rectangle((rect.X + rect.Width) - 1, rect.Y + 1, 1, rect.Height - 1));
        }

        public override void DrawHighlight(Controls.ControlBase control)
        {
            Rectangle rect = control.RenderBounds;
            Renderer.DrawColor = Color.FromArgb(255, 255, 100, 255);
            Renderer.DrawFilledRect(rect);
        }

        public override void DrawKeyboardHighlight(Controls.ControlBase control, Rectangle r, int offset)
        {
            Rectangle rect = r;

            rect.X += offset;
            rect.Y += offset;
            rect.Width -= offset * 2;
            rect.Height -= offset * 2;

            //draw the top and bottom
            bool skip = true;
            for (int i = 0; i < rect.Width * 0.5; i++)
            {
                m_Renderer.DrawColor = Color.Black;
                if (!skip)
                {
                    Renderer.DrawPixel(rect.X + (i * 2), rect.Y);
                    Renderer.DrawPixel(rect.X + (i * 2), rect.Y + rect.Height - 1);
                }
                else
                    skip = false;
            }

            for (int i = 0; i < rect.Height * 0.5; i++)
            {
                Renderer.DrawColor = Color.Black;
                Renderer.DrawPixel(rect.X, rect.Y + i * 2);
                Renderer.DrawPixel(rect.X + rect.Width - 1, rect.Y + i * 2);
            }
        }

        public override void DrawListBox(Controls.ControlBase control)
        {
            Textures.Input.ListBox.Background.Draw(Renderer, control.RenderBounds);
        }

        public override void DrawListBoxLine(Controls.ControlBase control, bool selected, bool even)
        {
            if (selected)
            {
                if (even)
                {
                    Textures.Input.ListBox.EvenLineSelected.Draw(Renderer, control.RenderBounds);
                    return;
                }
                Textures.Input.ListBox.OddLineSelected.Draw(Renderer, control.RenderBounds);
                return;
            }

            if (control.IsHovered)
            {
                Textures.Input.ListBox.Hovered.Draw(Renderer, control.RenderBounds);
                return;
            }

            if (even)
            {
                Textures.Input.ListBox.EvenLine.Draw(Renderer, control.RenderBounds);
                return;
            }

            Textures.Input.ListBox.OddLine.Draw(Renderer, control.RenderBounds);
        }

        public override void DrawMenu(Controls.ControlBase control, bool paddingDisabled)
        {
            if (!paddingDisabled)
            {
                Textures.Menu.BackgroundWithMargin.Draw(Renderer, control.RenderBounds);
                return;
            }

            Textures.Menu.Background.Draw(Renderer, control.RenderBounds);
        }

        public override void DrawMenuDivider(Controls.ControlBase control)
        {
            Rectangle rect = control.RenderBounds;
            Renderer.DrawColor = Color.FromArgb(100, 0, 0, 0);
            Renderer.DrawFilledRect(rect);
        }

        public override void DrawMenuItem(Controls.ControlBase control, bool submenuOpen, bool isChecked)
        {
            if (submenuOpen || control.IsHovered)
                Textures.Menu.Hover.Draw(Renderer, control.RenderBounds);

            if (isChecked)
                Textures.Menu.Check.Draw(Renderer, new Rectangle(control.RenderBounds.X + 4, control.RenderBounds.Y + 3, 15, 15));
        }

        public override void DrawMenuRightArrow(Controls.ControlBase control)
        {
            Textures.Menu.RightArrow.Draw(Renderer, control.RenderBounds);
        }

        public override void DrawMenuStrip(Controls.ControlBase control)
        {
            Textures.Menu.Strip.Draw(Renderer, control.RenderBounds);
        }

        public override void DrawModalControl(Controls.ControlBase control)
        {
            if (!control.ShouldDrawBackground)
                return;
            Rectangle rect = control.RenderBounds;
            Renderer.DrawColor = Colors.ModalBackground;
            Renderer.DrawFilledRect(rect);
        }

        public override void DrawNumericUpDownButton(Controls.ControlBase control, bool depressed, bool up)
        {
            if (up)
            {
                if (control.IsDisabled)
                {
                    Textures.Input.UpDown.Up.Disabled.DrawCenter(Renderer, control.RenderBounds);
                    return;
                }

                if (depressed)
                {
                    Textures.Input.UpDown.Up.Down.DrawCenter(Renderer, control.RenderBounds);
                    return;
                }

                if (control.IsHovered)
                {
                    Textures.Input.UpDown.Up.Hover.DrawCenter(Renderer, control.RenderBounds);
                    return;
                }

                Textures.Input.UpDown.Up.Normal.DrawCenter(Renderer, control.RenderBounds);
                return;
            }

            if (control.IsDisabled)
            {
                Textures.Input.UpDown.Down.Disabled.DrawCenter(Renderer, control.RenderBounds);
                return;
            }

            if (depressed)
            {
                Textures.Input.UpDown.Down.Down.DrawCenter(Renderer, control.RenderBounds);
                return;
            }

            if (control.IsHovered)
            {
                Textures.Input.UpDown.Down.Hover.DrawCenter(Renderer, control.RenderBounds);
                return;
            }

            Textures.Input.UpDown.Down.Normal.DrawCenter(Renderer, control.RenderBounds);
        }

        public override void DrawProgressBar(Controls.ControlBase control, bool horizontal, float progress)
        {
            Rectangle rect = control.RenderBounds;

            if (horizontal)
            {
                Textures.ProgressBar.Back.Draw(Renderer, rect);
                rect.Width = (int)(rect.Width * progress);
                Textures.ProgressBar.Front.Draw(Renderer, rect);
            }
            else
            {
                Textures.ProgressBar.Back.Draw(Renderer, rect);
                rect.Y = (int)(rect.Y + rect.Height * (1 - progress));
                rect.Height = (int)(rect.Height * progress);
                Textures.ProgressBar.Front.Draw(Renderer, rect);
            }
        }

        public override void DrawRadioButton(Controls.ControlBase control, bool selected, bool depressed)
        {
            if (selected)
            {
                if (control.IsDisabled)
                    Textures.RadioButton.Disabled.Checked.Draw(Renderer, control.RenderBounds);
                else
                    Textures.RadioButton.Active.Checked.Draw(Renderer, control.RenderBounds);
            }
            else
            {
                if (control.IsDisabled)
                    Textures.RadioButton.Disabled.Normal.Draw(Renderer, control.RenderBounds);
                else
                    Textures.RadioButton.Active.Normal.Draw(Renderer, control.RenderBounds);
            }
        }

        public override void DrawScrollBar(Controls.ControlBase control, bool horizontal, bool depressed)
        {
            if (horizontal)
                Textures.Scroller.TrackH.Draw(Renderer, control.RenderBounds);
            else
                Textures.Scroller.TrackV.Draw(Renderer, control.RenderBounds);
        }

        public override void DrawScrollBarBar(Controls.ControlBase control, bool depressed, bool hovered, bool horizontal)
        {
            if (!horizontal)
            {
                if (control.IsDisabled)
                {
                    Textures.Scroller.ButtonV_Disabled.Draw(Renderer, control.RenderBounds);
                    return;
                }

                if (depressed)
                {
                    Textures.Scroller.ButtonV_Down.Draw(Renderer, control.RenderBounds);
                    return;
                }

                if (hovered)
                {
                    Textures.Scroller.ButtonV_Hover.Draw(Renderer, control.RenderBounds);
                    return;
                }

                Textures.Scroller.ButtonV_Normal.Draw(Renderer, control.RenderBounds);
                return;
            }

            if (control.IsDisabled)
            {
                Textures.Scroller.ButtonH_Disabled.Draw(Renderer, control.RenderBounds);
                return;
            }

            if (depressed)
            {
                Textures.Scroller.ButtonH_Down.Draw(Renderer, control.RenderBounds);
                return;
            }

            if (hovered)
            {
                Textures.Scroller.ButtonH_Hover.Draw(Renderer, control.RenderBounds);
                return;
            }

            Textures.Scroller.ButtonH_Normal.Draw(Renderer, control.RenderBounds);
        }

        public override void DrawScrollButton(Controls.ControlBase control, Pos direction, bool depressed, bool hovered, bool disabled)
        {
            int i = 0;
            if (direction == Pos.Top) i = 1;
            if (direction == Pos.Right) i = 2;
            if (direction == Pos.Bottom) i = 3;

            if (disabled)
            {
                Textures.Scroller.Button.Disabled[i].Draw(Renderer, control.RenderBounds);
                return;
            }

            if (depressed)
            {
                Textures.Scroller.Button.Down[i].Draw(Renderer, control.RenderBounds);
                return;
            }

            if (hovered)
            {
                Textures.Scroller.Button.Hover[i].Draw(Renderer, control.RenderBounds);
                return;
            }

            Textures.Scroller.Button.Normal[i].Draw(Renderer, control.RenderBounds);
        }

        public override void DrawShadow(Controls.ControlBase control)
        {
            Rectangle r = control.RenderBounds;
            r.X -= 4;
            r.Y -= 4;
            r.Width += 10;
            r.Height += 10;
            Textures.Shadow.Draw(Renderer, r);
        }

        public override void DrawSlider(Controls.ControlBase control, bool horizontal, int numNotches, int barSize)
        {
            Rectangle rect = control.RenderBounds;
            Renderer.DrawColor = Color.FromArgb(100, 0, 0, 0);

            if (horizontal)
            {
                rect.X += (int)(barSize * 0.5);
                rect.Width -= barSize;
                rect.Y += (int)(rect.Height * 0.5 - 1);
                rect.Height = 1;
                DrawSliderNotchesH(rect, numNotches, barSize * 0.5f);
                Renderer.DrawFilledRect(rect);
                return;
            }

            rect.Y += (int)(barSize * 0.5);
            rect.Height -= barSize;
            rect.X += (int)(rect.Width * 0.5 - 1);
            rect.Width = 1;
            DrawSliderNotchesV(rect, numNotches, barSize * 0.4f);
            Renderer.DrawFilledRect(rect);
        }

        public override void DrawSliderButton(Controls.ControlBase control, bool depressed, bool horizontal)
        {
            if (!horizontal)
            {
                if (control.IsDisabled)
                {
                    Textures.Input.Slider.V.Disabled.DrawCenter(Renderer, control.RenderBounds);
                    return;
                }

                if (depressed)
                {
                    Textures.Input.Slider.V.Down.DrawCenter(Renderer, control.RenderBounds);
                    return;
                }

                if (control.IsHovered)
                {
                    Textures.Input.Slider.V.Hover.DrawCenter(Renderer, control.RenderBounds);
                    return;
                }

                Textures.Input.Slider.V.Normal.DrawCenter(Renderer, control.RenderBounds);
                return;
            }

            if (control.IsDisabled)
            {
                Textures.Input.Slider.H.Disabled.DrawCenter(Renderer, control.RenderBounds);
                return;
            }

            if (depressed)
            {
                Textures.Input.Slider.H.Down.DrawCenter(Renderer, control.RenderBounds);
                return;
            }

            if (control.IsHovered)
            {
                Textures.Input.Slider.H.Hover.DrawCenter(Renderer, control.RenderBounds);
                return;
            }

            Textures.Input.Slider.H.Normal.DrawCenter(Renderer, control.RenderBounds);
        }

        public void DrawSliderNotchesH(Rectangle rect, int numNotches, float dist)
        {
            if (numNotches == 0) return;

            float iSpacing = rect.Width / (float)numNotches;
            for (int i = 0; i < numNotches + 1; i++)
                Renderer.DrawFilledRect(Util.FloatRect(rect.X + iSpacing * i, rect.Y + dist - 2, 1, 5));
        }

        public void DrawSliderNotchesV(Rectangle rect, int numNotches, float dist)
        {
            if (numNotches == 0) return;

            float iSpacing = rect.Height / (float)numNotches;
            for (int i = 0; i < numNotches + 1; i++)
                Renderer.DrawFilledRect(Util.FloatRect(rect.X + dist - 2, rect.Y + iSpacing * i, 5, 1));
        }

        public override void DrawStatusBar(Controls.ControlBase control)
        {
            Textures.StatusBar.Draw(Renderer, control.RenderBounds);
        }

        public override void DrawTabButton(Controls.ControlBase control, bool active, Pos dir)
        {
            if (active)
            {
                DrawActiveTabButton(control, dir);
                return;
            }

            if (dir == Pos.Top)
            {
                Textures.Tab.Top.Inactive.Draw(Renderer, control.RenderBounds);
                return;
            }
            if (dir == Pos.Left)
            {
                Textures.Tab.Left.Inactive.Draw(Renderer, control.RenderBounds);
                return;
            }
            if (dir == Pos.Bottom)
            {
                Textures.Tab.Bottom.Inactive.Draw(Renderer, control.RenderBounds);
                return;
            }
            if (dir == Pos.Right)
            {
                Textures.Tab.Right.Inactive.Draw(Renderer, control.RenderBounds);
                return;
            }
        }

        public override void DrawTabControl(Controls.ControlBase control)
        {
            Textures.Tab.Control.Draw(Renderer, control.RenderBounds);
        }

        public override void DrawTabTitleBar(Controls.ControlBase control)
        {
            Textures.Tab.HeaderBar.Draw(Renderer, control.RenderBounds);
        }

        public override void DrawTextBox(Controls.ControlBase control)
        {
            if (control.IsDisabled)
            {
                Textures.TextBox.Disabled.Draw(Renderer, control.RenderBounds);
                return;
            }

            if (control.HasFocus)
                Textures.TextBox.Focus.Draw(Renderer, control.RenderBounds);
            else
                Textures.TextBox.Normal.Draw(Renderer, control.RenderBounds);
        }

        public override void DrawToolTip(Controls.ControlBase control)
        {
            Textures.Tooltip.Draw(Renderer, control.RenderBounds);
        }

        public override void DrawTreeButton(Controls.ControlBase control, bool open)
        {
            Rectangle rect = control.RenderBounds;

            if (open)
                Textures.Tree.Minus.Draw(Renderer, rect);
            else
                Textures.Tree.Plus.Draw(Renderer, rect);
        }

        public override void DrawTreeControl(Controls.ControlBase control)
        {
            Textures.Tree.Background.Draw(Renderer, control.RenderBounds);
        }

        public override void DrawTreeNode(Controls.ControlBase ctrl, bool open, bool selected, int labelHeight, int labelWidth, int halfWay, int lastBranch, bool isRoot)
        {
            if (selected)
            {
                Textures.Selection.Draw(Renderer, new Rectangle(17, 0, labelWidth + 2, labelHeight - 1));
            }

            base.DrawTreeNode(ctrl, open, selected, labelHeight, labelWidth, halfWay, lastBranch, isRoot);
        }

        public override void DrawWindow(Controls.ControlBase control, int topHeight, bool inFocus)
        {
            if (inFocus)
                Textures.Window.Normal.Draw(Renderer, control.RenderBounds);
            else
                Textures.Window.Inactive.Draw(Renderer, control.RenderBounds);
        }

        public override void DrawWindowCloseButton(Controls.ControlBase control, bool depressed, bool hovered, bool disabled)
        {
            if (disabled)
            {
                Textures.Window.Close_Disabled.Draw(Renderer, control.RenderBounds);
                return;
            }

            if (depressed)
            {
                Textures.Window.Close_Down.Draw(Renderer, control.RenderBounds);
                return;
            }

            if (hovered)
            {
                Textures.Window.Close_Hover.Draw(Renderer, control.RenderBounds);
                return;
            }

            Textures.Window.Close.Draw(Renderer, control.RenderBounds);
        }

        private void DrawActiveTabButton(Controls.ControlBase control, Pos dir)
        {
            if (dir == Pos.Top)
            {
                Textures.Tab.Top.Active.Draw(Renderer, control.RenderBounds.Add(new Rectangle(0, 0, 0, 8)));
                return;
            }
            if (dir == Pos.Left)
            {
                Textures.Tab.Left.Active.Draw(Renderer, control.RenderBounds.Add(new Rectangle(0, 0, 8, 0)));
                return;
            }
            if (dir == Pos.Bottom)
            {
                Textures.Tab.Bottom.Active.Draw(Renderer, control.RenderBounds.Add(new Rectangle(0, -8, 0, 8)));
                return;
            }
            if (dir == Pos.Right)
            {
                Textures.Tab.Right.Active.Draw(Renderer, control.RenderBounds.Add(new Rectangle(-8, 0, 8, 0)));
                return;
            }
        }

        #endregion UI elements
    }
}