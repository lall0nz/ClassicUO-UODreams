using ClassicUO.Assets;
using FontStyle = ClassicUO.Game.FontStyle;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using static ClassicUO.Game.UI.XmlGumpHandler;

namespace ClassicUO.Game.UI
{
    internal class XmlGumpHandler
    {
        // Default spawn position used when a XML gump has no position saved in the
        // current profile and the gump file itself doesn't specify one.
        public const int DefaultGumpX = 200;
        public const int DefaultGumpY = 200;

        public static string XmlGumpPath => Path.Combine(CUOEnviroment.ExecutablePath, "Data", "XmlGumps");

        public static XmlGump CreateGumpFromFile(World world, string filePath)
        {
            XmlGump gump = new XmlGump(world);
            gump.CanCloseWithRightClick = true;
            gump.AcceptMouseInput = true;
            gump.CanMove = true;
            gump.GumpName = Path.GetFileNameWithoutExtension(filePath);

            if (File.Exists(filePath))
            {
                gump.FilePath = filePath;

                XmlDocument xmlDoc = new XmlDocument();

                try
                {
                    xmlDoc.LoadXml(File.ReadAllText(filePath));
                }
                catch (Exception e)
                {
                    GameActions.Print(world, e.Message);
                }

                if (xmlDoc.DocumentElement != null)
                {
                    XmlElement root = xmlDoc.DocumentElement;

                    foreach (XmlAttribute attr in root.Attributes)
                    {
                        switch (attr.Name.ToLower())
                        {
                            case "x":
                                int.TryParse(attr.Value, out gump.X);
                                break;
                            case "y":
                                int.TryParse(attr.Value, out gump.Y);
                                break;
                            case "locked":
                                if (bool.TryParse(attr.Value, out bool locked))
                                {
                                    gump.IsLocked = locked;
                                }
                                break;
                            case "saveposition":
                                if (bool.TryParse(attr.Value, out bool savePos))
                                {
                                    gump.SavePosition = savePos;
                                }
                                break;
                        }
                    }

                    ProcessChildNodes(gump, root);
                }

                gump.ForceSizeUpdate();
            }

            // A position saved in the current profile always wins (last spot the user moved it
            // to, kept across logout/login). Otherwise every XML gump spawns at the fixed
            // default below, regardless of any x/y authored in the gump file itself.
            if (
                ProfileManager.CurrentProfile != null
                && ProfileManager.CurrentProfile.XmlGumpPositions.TryGetValue(gump.GumpName, out string savedPos)
                && TryParsePosition(savedPos, out int savedX, out int savedY)
            )
            {
                gump.X = savedX;
                gump.Y = savedY;
            }
            else
            {
                gump.X = DefaultGumpX;
                gump.Y = DefaultGumpY;
            }

            return gump;
        }

        private static bool TryParsePosition(string value, out int x, out int y)
        {
            x = 0;
            y = 0;

            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            string[] parts = value.Split(',');

            return parts.Length == 2 && int.TryParse(parts[0], out x) && int.TryParse(parts[1], out y);
        }

        public static void TryAutoOpenByName(World world, string name)
        {
            string fullFile = Path.Combine(XmlGumpPath, name + ".xml");

            if (File.Exists(fullFile))
            {
                UIManager.Add(CreateGumpFromFile(world, fullFile));
            }
        }

        public static string[] GetAllXmlGumps()
        {
            List<string> fileList = new List<string>();

            try
            {
                if (Directory.Exists(XmlGumpPath))
                {
                    string[] allFiles = Directory.GetFiles(XmlGumpPath, "*.xml");

                    foreach (string file in allFiles)
                    {
                        fileList.Add(Path.GetFileNameWithoutExtension(file));
                    }
                }
                else
                {
                    Directory.CreateDirectory(XmlGumpPath);
                }
            }
            catch { }

            return fileList.ToArray();
        }

        private static void ProcessChildNodes(XmlGump gump, XmlNode node)
        {
            foreach (XmlNode child in node.ChildNodes)
            {
                switch (child.NodeType)
                {
                    case XmlNodeType.Element:
                        if (child.Name.Equals("text", StringComparison.OrdinalIgnoreCase))
                        {
                            HandleTextTag(gump, child);
                            break;
                        }
                        if (child.Name.Equals("colorbox", StringComparison.OrdinalIgnoreCase))
                        {
                            HandleColorBox(gump, child);
                            break;
                        }
                        if (child.Name.Equals("image", StringComparison.OrdinalIgnoreCase))
                        {
                            HandleImage(gump, child);
                            break;
                        }
                        if (child.Name.Equals("image_progress_bar", StringComparison.OrdinalIgnoreCase))
                        {
                            HandleImageProgressBar(gump, child);
                            break;
                        }
                        if (child.Name.Equals("color_progress_bar", StringComparison.OrdinalIgnoreCase))
                        {
                            HandleColorProgressBar(gump, child);
                            break;
                        }
                        if (child.Name.Equals("control", StringComparison.OrdinalIgnoreCase))
                        {
                            HandleControl(gump, child);
                            break;
                        }
                        if (child.Name.Equals("simple_border", StringComparison.OrdinalIgnoreCase))
                        {
                            HandleBorder(gump, child);
                            break;
                        }
                        if (child.Name.Equals("macro_button_color", StringComparison.OrdinalIgnoreCase))
                        {
                            HandleMacroButton(gump, child);
                            break;
                        }
                        if (child.Name.Equals("macro_button_graphic", StringComparison.OrdinalIgnoreCase))
                        {
                            HandleMacroButtonGraphic(gump, child);
                            break;
                        }
                        if (child.Name.Equals("hp_bar_color", StringComparison.OrdinalIgnoreCase))
                        {
                            HandleColorHPBar(gump, child);
                            break;
                        }
                        if (child.Name.Equals("hp_bar_image", StringComparison.OrdinalIgnoreCase))
                        {
                            HandleImageHPBar(gump, child);
                            break;
                        }
                        break;
                }
            }
        }

        private static void HandleImageHPBar(XmlGump gump, XmlNode node)
        {
            XmlHealthBar hpBar = new XmlHealthBar(gump.World, gump.World.Player.Serial);
            ApplyBasicAttributes(hpBar, node);
            hpBar.SetImageType();

            foreach (XmlAttribute attr in node.Attributes)
            {
                switch (attr.Name.ToLower())
                {
                    case "hue_normal":
                        if (ushort.TryParse(attr.Value, out ushort h))
                        {
                            hpBar.NormalHue = h;
                        }
                        break;
                    case "hue_background":
                        if (ushort.TryParse(attr.Value, out ushort hh))
                        {
                            hpBar.BackgroundHue = hh;
                        }
                        break;
                    case "hue_poisoned":
                        if (ushort.TryParse(attr.Value, out ushort hp))
                        {
                            hpBar.PoisonedHue = hp;
                        }
                        break;
                    case "direction":
                        if (attr.Value == "right")
                        {
                            hpBar.BarDirection = XmlHealthBar.Direction.LeftToRight;
                        }
                        else if (attr.Value == "up")
                        {
                            hpBar.BarDirection = XmlHealthBar.Direction.BottomToTop;
                        }
                        break;
                    case "image_background":
                        if (ushort.TryParse(attr.Value, out ushort bg))
                        {
                            hpBar.BackgroundImage = bg;
                        }
                        break;
                    case "image_foreground":
                        if (ushort.TryParse(attr.Value, out ushort fg))
                        {
                            hpBar.ForegroundImage = fg;
                        }
                        break;
                    case "image_foreground_poisoned":
                        if (ushort.TryParse(attr.Value, out ushort fgp))
                        {
                            hpBar.ForegroundImagePoisoned = fgp;
                        }
                        break;
                }
            }

            gump.Add(hpBar);
        }

        private static void HandleColorHPBar(XmlGump gump, XmlNode node)
        {
            XmlHealthBar hpBar = new XmlHealthBar(gump.World, gump.World.Player.Serial);
            ApplyBasicAttributes(hpBar, node);
            hpBar.SetColoredType();

            foreach (XmlAttribute attr in node.Attributes)
            {
                switch (attr.Name.ToLower())
                {
                    case "hue_normal":
                        if (ushort.TryParse(attr.Value, out ushort h))
                        {
                            hpBar.NormalHue = h;
                        }
                        break;
                    case "hue_background":
                        if (ushort.TryParse(attr.Value, out ushort hh))
                        {
                            hpBar.BackgroundHue = hh;
                        }
                        break;
                    case "hue_poisoned":
                        if (ushort.TryParse(attr.Value, out ushort hp))
                        {
                            hpBar.PoisonedHue = hp;
                        }
                        break;
                    case "direction":
                        if (attr.Value == "right")
                        {
                            hpBar.BarDirection = XmlHealthBar.Direction.LeftToRight;
                        }
                        else if (attr.Value == "up")
                        {
                            hpBar.BarDirection = XmlHealthBar.Direction.BottomToTop;
                        }
                        break;
                }
            }

            gump.Add(hpBar);
        }

        private static void HandleMacroButtonGraphic(XmlGump gump, XmlNode node)
        {
            HitBox hb = new HitBox(0, 0, 0, 0, null, 0);
            ApplyBasicAttributes(hb, node);

            GumpPic bg = new GumpPic(0, 0, 0, 0);
            hb.Add(bg);

            ushort hue = 0, graphic = 0;

            foreach (XmlAttribute attr in node.Attributes)
            {
                switch (attr.Name.ToLower())
                {
                    case "id":
                        if (ushort.TryParse(attr.Value, out ushort gra))
                        {
                            bg.Graphic = graphic = gra;

                            if (hb.Width < 1)
                            {
                                hb.ForceSizeUpdate();
                            }
                        }
                        break;
                    case "id_hover":
                        if (ushort.TryParse(attr.Value, out ushort gra_hover))
                        {
                            hb.MouseEnter += (s, e) => { bg.Graphic = gra_hover; };
                            hb.MouseExit += (s, e) => { bg.Graphic = graphic; };
                        }
                        break;
                    case "alpha":
                        if (float.TryParse(attr.Value, out float a))
                        {
                            bg.Alpha = a;
                        }
                        break;
                    case "hue":
                        if (ushort.TryParse(attr.Value, out ushort h))
                        {
                            bg.Hue = hue = h;
                        }
                        break;
                    case "hue_hover":
                        if (ushort.TryParse(attr.Value, out ushort hh))
                        {
                            hb.MouseEnter += (s, e) => { bg.Hue = hh; };
                            hb.MouseExit += (s, e) => { bg.Hue = hue; };
                        }
                        break;
                    case "macro":
                        MacroManager manager = gump.World.Macros;
                        Macro m = manager.FindMacro(attr.Value);

                        if (m != null)
                        {
                            hb.MouseUp += (s, e) =>
                            {
                                if (e.Button == MouseButtonType.Left)
                                {
                                    manager.SetMacroToExecute(m.Items as MacroObject);
                                }
                            };
                        }
                        break;
                }
            }

            gump.Add(hb);
        }

        private static void HandleMacroButton(XmlGump gump, XmlNode node)
        {
            HitBox hb = new HitBox(0, 0, 0, 0, null, 0);
            ApplyBasicAttributes(hb, node);

            AlphaBlendControl bg = new AlphaBlendControl() { Width = hb.Width, Height = hb.Height };
            hb.Add(bg);

            ushort hue = 0;

            foreach (XmlAttribute attr in node.Attributes)
            {
                switch (attr.Name.ToLower())
                {
                    case "alpha":
                        if (float.TryParse(attr.Value, out float a))
                        {
                            bg.Alpha = a;
                        }
                        break;
                    case "hue":
                        if (ushort.TryParse(attr.Value, out ushort h))
                        {
                            bg.Hue = hue = h;
                        }
                        break;
                    case "hue_hover":
                        if (ushort.TryParse(attr.Value, out ushort hh))
                        {
                            hb.MouseEnter += (s, e) => { bg.Hue = hh; };
                            hb.MouseExit += (s, e) => { bg.Hue = hue; };
                        }
                        break;
                    case "macro":
                        MacroManager manager = gump.World.Macros;
                        Macro m = manager.FindMacro(attr.Value);

                        if (m != null)
                        {
                            hb.MouseUp += (s, e) =>
                            {
                                if (e.Button == MouseButtonType.Left)
                                {
                                    manager.SetMacroToExecute(m.Items as MacroObject);
                                }
                            };
                        }
                        break;
                }
            }

            gump.Add(hb);
        }

        private static void HandleBorder(XmlGump gump, XmlNode node)
        {
            ushort hue = 0;
            int width = 0, height = 0;

            foreach (XmlAttribute attr in node.Attributes)
            {
                switch (attr.Name.ToLower())
                {
                    case "hue":
                        ushort.TryParse(attr.Value, out hue);
                        break;
                    case "width":
                        int.TryParse(attr.Value, out width);
                        break;
                    case "height":
                        int.TryParse(attr.Value, out height);
                        break;
                }
            }

            gump.Add(ApplyBasicAttributes(new SimpleBorder() { Hue = hue, Width = width, Height = height }, node));
        }

        private static void HandleControl(XmlGump gump, XmlNode node)
        {
            XmlGump newControl = new XmlGump(gump.World);
            ApplyBasicAttributes(newControl, node);

            ProcessChildNodes(newControl, node);

            if (newControl.Width < 1)
            {
                newControl.ForceSizeUpdate();
            }

            gump.Add(newControl);
        }

        private static void HandleColorProgressBar(XmlGump gump, XmlNode node)
        {
            ushort bg_hue = 0, fg_hue = 0;
            int value = 0, maxval = 0;
            bool needsUpdates = false;
            string originalValue = string.Empty, originalMaxVal = string.Empty;
            bool vertical = false;

            foreach (XmlAttribute attr in node.Attributes)
            {
                switch (attr.Name.ToLower())
                {
                    case "background_hue":
                        ushort.TryParse(attr.Value, out bg_hue);
                        break;
                    case "foreground_hue":
                        ushort.TryParse(attr.Value, out fg_hue);
                        break;
                    case "value":
                        originalValue = attr.Value;

                        if (!int.TryParse(attr.Value, out value))
                        {
                            int.TryParse(FormatText(gump.World, attr.Value), out value);
                        }
                        break;
                    case "max_value":
                        originalMaxVal = attr.Value;

                        if (!int.TryParse(attr.Value, out maxval))
                        {
                            int.TryParse(FormatText(gump.World, attr.Value), out maxval);
                        }
                        break;
                    case "updates":
                        bool.TryParse(attr.Value, out needsUpdates);
                        break;
                    case "direction":
                        if (attr.Value == "up")
                        {
                            vertical = true;
                        }
                        else if (attr.Value == "right")
                        {
                            vertical = false;
                        }
                        break;
                }
            }

            Control c;
            gump.Add(c = ApplyBasicAttributes(new ColorBox(0, 0, bg_hue) { AcceptMouseInput = false }, node));
            int maxWidth = c.Width;
            int maxHeight = c.Height;
            gump.Add(c = ApplyBasicAttributes(new ColorBox(0, 0, fg_hue) { AcceptMouseInput = false }, node));
            c.Width = (int)(GetPercentage(value, maxval) * c.Width);

            if (needsUpdates)
            {
                if (vertical)
                {
                    gump.VerticalProgressBarUpdates.Add(new XmlProgressBarInfo(c, maxHeight, originalValue, originalMaxVal));
                }
                else
                {
                    gump.ProgressBarUpdates.Add(new XmlProgressBarInfo(c, maxWidth, originalValue, originalMaxVal));
                }
            }
        }

        private static void HandleImageProgressBar(XmlGump gump, XmlNode node)
        {
            ushort bg_graphic = 0, fg_graphic = 0, bg_hue = 0, fg_hue = 0;
            int value = 0, maxval = 0;
            bool needsUpdates = false;
            string originalValue = string.Empty, originalMaxVal = string.Empty;
            bool vertical = false;

            foreach (XmlAttribute attr in node.Attributes)
            {
                switch (attr.Name.ToLower())
                {
                    case "image_background":
                        ushort.TryParse(attr.Value, out bg_graphic);
                        break;
                    case "image_foreground":
                        ushort.TryParse(attr.Value, out fg_graphic);
                        break;
                    case "background_hue":
                        ushort.TryParse(attr.Value, out bg_hue);
                        break;
                    case "foreground_hue":
                        ushort.TryParse(attr.Value, out fg_hue);
                        break;
                    case "value":
                        originalValue = attr.Value;

                        if (!int.TryParse(attr.Value, out value))
                        {
                            int.TryParse(FormatText(gump.World, attr.Value), out value);
                        }
                        break;
                    case "max_value":
                        originalMaxVal = attr.Value;

                        if (!int.TryParse(attr.Value, out maxval))
                        {
                            int.TryParse(FormatText(gump.World, attr.Value), out maxval);
                        }
                        break;
                    case "updates":
                        bool.TryParse(attr.Value, out needsUpdates);
                        break;
                    case "direction":
                        if (attr.Value == "up")
                        {
                            vertical = true;
                        }
                        else if (attr.Value == "right")
                        {
                            vertical = false;
                        }
                        break;
                }
            }

            Control c;
            gump.Add(c = ApplyBasicAttributes(new GumpPic(0, 0, bg_graphic, bg_hue), node));
            int maxWidth = c.Width;
            int maxHeight = c.Height;

            if (vertical)
            {
                gump.Add(c = ApplyBasicAttributes(new GumpPicInPic(0, 0, fg_graphic, 0, 0, (ushort)maxWidth, (ushort)maxHeight) { Hue = fg_hue }, node));
            }
            else
            {
                gump.Add(c = ApplyBasicAttributes(new GumpPicTiled(fg_graphic) { Hue = fg_hue }, node));
                c.Width = (int)(GetPercentage(value, maxval) * c.Width);
            }

            if (needsUpdates)
            {
                if (vertical)
                {
                    gump.VerticalProgressBarUpdates.Add(new XmlProgressBarInfo(c, maxHeight, originalValue, originalMaxVal));
                }
                else
                {
                    gump.ProgressBarUpdates.Add(new XmlProgressBarInfo(c, maxWidth, originalValue, originalMaxVal));
                }
            }
        }

        private static void HandleImage(XmlGump gump, XmlNode node)
        {
            ushort graphic = 0, hue = 0;
            Rectangle picinpic = Rectangle.Empty;
            bool isPicInPic = false;

            foreach (XmlAttribute attr in node.Attributes)
            {
                switch (attr.Name.ToLower())
                {
                    case "id":
                        ushort.TryParse(attr.Value, out graphic);
                        break;
                    case "hue":
                        ushort.TryParse(attr.Value, out hue);
                        break;
                    case "rect":
                        string[] parts = attr.Value.Split(';');

                        if (parts.Length == 4)
                        {
                            if (int.TryParse(parts[0], out int x) && int.TryParse(parts[1], out int y) && int.TryParse(parts[2], out int w) && int.TryParse(parts[3], out int h))
                            {
                                picinpic = new Rectangle(x, y, w, h);
                                isPicInPic = true;
                            }
                        }
                        break;
                }
            }

            if (isPicInPic)
            {
                gump.Add(ApplyBasicAttributes(new GumpPicInPic(0, 0, graphic, (ushort)picinpic.X, (ushort)picinpic.Y, (ushort)picinpic.Width, (ushort)picinpic.Height), node));
            }
            else
            {
                gump.Add(ApplyBasicAttributes(new GumpPic(0, 0, graphic, hue), node));
            }
        }

        private static void HandleColorBox(XmlGump gump, XmlNode colorNode)
        {
            ushort hue = 0;
            float alpha = 1;

            foreach (XmlAttribute attr in colorNode.Attributes)
            {
                switch (attr.Name.ToLower())
                {
                    case "hue":
                        ushort.TryParse(attr.Value, out hue);
                        break;
                    case "alpha":
                        float.TryParse(attr.Value, out alpha);
                        break;
                }
            }

            gump.Add(ApplyBasicAttributes(new ColorBox(0, 0, hue) { Alpha = alpha, AcceptMouseInput = true, CanCloseWithRightClick = true, CanMove = true }, colorNode));
        }

        private static void HandleTextTag(XmlGump gump, XmlNode textNode)
        {
            string fontAttr = null;
            int x = 0, y = 0, width = 0, hue = 997, size = 0;
            bool needsUpdates = false;
            TEXT_ALIGN_TYPE align = TEXT_ALIGN_TYPE.TS_LEFT;

            foreach (XmlAttribute attr in textNode.Attributes)
            {
                switch (attr.Name.ToLower())
                {
                    case "x":
                        int.TryParse(attr.Value, out x);
                        break;
                    case "y":
                        int.TryParse(attr.Value, out y);
                        break;
                    case "font":
                        fontAttr = attr.Value;
                        break;
                    case "size":
                        int.TryParse(attr.Value, out size);
                        break;
                    case "width":
                        int.TryParse(attr.Value, out width);
                        break;
                    case "hue":
                        int.TryParse(attr.Value, out hue);
                        break;
                    case "updates":
                        bool.TryParse(attr.Value, out needsUpdates);
                        break;
                    case "align":
                        switch (attr.Value.ToLower())
                        {
                            case "left":
                                align = TEXT_ALIGN_TYPE.TS_LEFT;
                                break;
                            case "center":
                                align = TEXT_ALIGN_TYPE.TS_CENTER;
                                break;
                            case "right":
                                align = TEXT_ALIGN_TYPE.TS_RIGHT;
                                break;
                        }
                        break;
                }
            }

            ParseXmlTextFont(fontAttr, size, out byte font, out bool isUnicode);

            Label label;
            gump.Add(label = new Label(FormatText(gump.World, textNode.InnerText), isUnicode, (ushort)hue, width > 0 ? width : 0, font, FontStyle.None, align) { X = x, Y = y, AcceptMouseInput = false });

            if (needsUpdates)
            {
                gump.LabelUpdates.Add(new Tuple<Label, Tuple<string, int>>(label, new Tuple<string, int>(textNode.InnerText, width)));
            }
        }

        /// <summary>
        /// Maps TazUO/UODreams XML font names (e.g. uo-unicode-1) and optional size to UO bitmap font indices.
        /// </summary>
        private static void ParseXmlTextFont(string fontAttr, int size, out byte font, out bool isUnicode)
        {
            isUnicode = true;
            font = 1;

            if (!string.IsNullOrWhiteSpace(fontAttr))
            {
                string f = fontAttr.Trim().ToLowerInvariant();

                if (f.StartsWith("uo-unicode-", StringComparison.Ordinal))
                {
                    isUnicode = true;

                    if (byte.TryParse(f.AsSpan("uo-unicode-".Length), out byte idx))
                    {
                        font = idx;
                    }

                    if (size > 0)
                    {
                        font = Math.Max(font, MapSizeToUnicodeFont(size));
                    }

                    return;
                }

                if (f.StartsWith("uo-regular-", StringComparison.Ordinal) || f.StartsWith("uo-sans-", StringComparison.Ordinal))
                {
                    isUnicode = false;
                    int prefixLen = f.StartsWith("uo-regular-", StringComparison.Ordinal) ? "uo-regular-".Length : "uo-sans-".Length;

                    if (byte.TryParse(f.AsSpan(prefixLen), out byte idx))
                    {
                        font = idx > 0 ? (byte)(idx - 1) : (byte)0;
                    }

                    return;
                }

                if (byte.TryParse(f, out byte numeric))
                {
                    font = numeric;
                    return;
                }
            }

            if (size > 0)
            {
                font = MapSizeToUnicodeFont(size);
            }
        }

        private static byte MapSizeToUnicodeFont(int size)
        {
            if (size >= 22)
            {
                return 2;
            }

            if (size >= 18)
            {
                return 1;
            }

            return 0;
        }

        private static Control ApplyBasicAttributes(Control c, XmlNode node)
        {
            foreach (XmlAttribute attr in node.Attributes)
            {
                switch (attr.Name.ToLower())
                {
                    case "x":
                        int.TryParse(attr.Value, out c.X);
                        break;
                    case "y":
                        int.TryParse(attr.Value, out c.Y);
                        break;
                    case "acceptmouseinput":
                        if (bool.TryParse(attr.Value, out bool b))
                        {
                            c.AcceptMouseInput = b;
                        }
                        break;
                    case "canmove":
                        if (bool.TryParse(attr.Value, out bool cm))
                        {
                            c.CanMove = cm;
                        }
                        break;
                    case "width":
                        int.TryParse(attr.Value, out c.Width);
                        break;
                    case "height":
                        int.TryParse(attr.Value, out c.Height);
                        break;
                }
            }

            return c;
        }

        public static float GetPercentage(double value, double max)
        {
            return (float)(value / max);
        }

        public static string FormatText(World world, string text)
        {
            if (world?.Player == null)
            {
                return text;
            }

            PlayerMobile player = world.Player;

            text = text.Replace("{charname}", player.Name);
            text = text.Replace("{hp}", player.Hits.ToString());
            text = text.Replace("{maxhp}", player.HitsMax.ToString());
            text = text.Replace("{mana}", player.Mana.ToString());
            text = text.Replace("{maxmana}", player.ManaMax.ToString());
            text = text.Replace("{stam}", player.Stamina.ToString());
            text = text.Replace("{maxstam}", player.StaminaMax.ToString());
            text = text.Replace("{weight}", player.Weight.ToString());
            text = text.Replace("{maxweight}", player.WeightMax.ToString());
            text = text.Replace("{str}", player.Strength.ToString());
            text = text.Replace("{dex}", player.Dexterity.ToString());
            text = text.Replace("{int}", player.Intelligence.ToString());
            text = text.Replace("{damagemin}", player.DamageMin.ToString());
            text = text.Replace("{damagemax}", player.DamageMax.ToString());
            text = text.Replace("{hci}", player.HitChanceIncrease.ToString());
            text = text.Replace("{di}", player.DamageIncrease.ToString());
            text = text.Replace("{ssi}", player.SwingSpeedIncrease.ToString());
            text = text.Replace("{defchance}", player.DefenseChanceIncrease.ToString());
            text = text.Replace("{defchancemax}", player.MaxDefenseChanceIncrease.ToString());
            text = text.Replace("{sdi}", player.SpellDamageIncrease.ToString());
            text = text.Replace("{fc}", player.FasterCasting.ToString());
            text = text.Replace("{fcr}", player.FasterCastRecovery.ToString());
            text = text.Replace("{lmc}", player.LowerManaCost.ToString());
            text = text.Replace("{lrc}", player.LowerReagentCost.ToString());
            text = text.Replace("{phyres}", player.PhysicalResistance.ToString());
            text = text.Replace("{phyresmax}", player.MaxPhysicResistence.ToString());
            text = text.Replace("{fireres}", player.FireResistance.ToString());
            text = text.Replace("{fireresmax}", player.MaxFireResistence.ToString());
            text = text.Replace("{coldres}", player.ColdResistance.ToString());
            text = text.Replace("{coldresmax}", player.MaxColdResistence.ToString());
            text = text.Replace("{poisonres}", player.PoisonResistance.ToString());
            text = text.Replace("{poisonresmax}", player.MaxPoisonResistence.ToString());
            text = text.Replace("{energyres}", player.EnergyResistance.ToString());
            text = text.Replace("{energyresmax}", player.MaxEnergyResistence.ToString());
            text = text.Replace("{maxstats}", player.StatsCap.ToString());
            text = text.Replace("{luck}", player.Luck.ToString());
            text = text.Replace("{gold}", player.Gold.ToString());
            text = text.Replace("{pets}", player.Followers.ToString());
            text = text.Replace("{petsmax}", player.FollowersMax.ToString());

            return text;
        }

        internal class XmlProgressBarInfo
        {
            public XmlProgressBarInfo(Control control, int maxSize, string value, string maxValue)
            {
                Control = control;
                MaxSize = maxSize;
                Value = value;
                MaxValue = maxValue;
            }

            public Control Control { get; }
            public int MaxSize { get; }
            public string Value { get; }
            public string MaxValue { get; }
        }
    }

    internal class XmlGump : Gump
    {
        public static uint UpdateFrequency { get; set; } = 250;

        public List<Tuple<Label, Tuple<string, int>>> LabelUpdates { get; set; } = new List<Tuple<Label, Tuple<string, int>>>();
        public List<XmlProgressBarInfo> ProgressBarUpdates { get; set; } = new List<XmlProgressBarInfo>();
        public List<XmlProgressBarInfo> VerticalProgressBarUpdates { get; set; } = new List<XmlProgressBarInfo>();
        public bool SavePosition { get; set; }
        public string FilePath { get; set; }

        /// <summary>
        /// File name (without extension) used as the key to save/restore this gump's position
        /// in the current profile (ProfileManager.CurrentProfile.XmlGumpPositions).
        /// </summary>
        public string GumpName { get; set; }

        private uint nextUpdate;
        private int _savingFile;
        private uint saveFileAfter = uint.MaxValue;
        private bool _isLocked;

        public bool IsLocked
        {
            get => _isLocked;
            set
            {
                _isLocked = value;
                CanMove = !value;

                if (value)
                {
                    CanCloseWithRightClick = false;
                }
            }
        }

        public XmlGump(World world) : base(world, 0, 0)
        {
        }

        public override void Update()
        {
            base.Update();

            if (Time.Ticks >= nextUpdate)
            {
                foreach (var t in LabelUpdates)
                {
                    if (t.Item1 != null && !t.Item1.IsDisposed)
                    {
                        string newString = XmlGumpHandler.FormatText(World, t.Item2.Item1);

                        if (t.Item1.Text != newString)
                        {
                            t.Item1.Text = newString;
                        }
                    }
                }

                foreach (var p in ProgressBarUpdates)
                {
                    if (p.Control != null && !p.Control.IsDisposed)
                    {
                        if (int.TryParse(XmlGumpHandler.FormatText(World, p.Value), out int val))
                        {
                            if (int.TryParse(XmlGumpHandler.FormatText(World, p.MaxValue), out int max))
                            {
                                p.Control.Width = (int)(XmlGumpHandler.GetPercentage(val, max) * p.MaxSize);
                            }
                        }
                    }
                }

                foreach (var p in VerticalProgressBarUpdates)
                {
                    if (p.Control != null && !p.Control.IsDisposed)
                    {
                        if (int.TryParse(XmlGumpHandler.FormatText(World, p.Value), out int val))
                        {
                            if (int.TryParse(XmlGumpHandler.FormatText(World, p.MaxValue), out int max))
                            {
                                int newHeight = (int)(XmlGumpHandler.GetPercentage(val, max) * p.MaxSize);

                                if (p.Control is GumpPicInPic picnpic)
                                {
                                    picnpic.PicInPicBounds = new Rectangle(0, p.MaxSize - newHeight, picnpic.Width, p.MaxSize - (p.MaxSize - newHeight));
                                    picnpic.DrawOffset = new Vector2(0, p.MaxSize - newHeight);
                                }
                                else
                                {
                                    p.Control.Height = newHeight;
                                    p.Control.Y = p.MaxSize - newHeight;
                                }
                            }
                        }
                    }
                }

                nextUpdate = Time.Ticks + UpdateFrequency;
            }

            if (Time.Ticks > saveFileAfter)
            {
                saveFileAfter = uint.MaxValue;
                Task.Run(SaveFile);
            }
        }

        protected override void OnMove(int x, int y)
        {
            base.OnMove(x, y);

            SaveProfilePosition();

            if (SavePosition)
            {
                saveFileAfter = Time.Ticks + 2000;
            }
        }

        /// <summary>
        /// Persists the current position into the active profile (in-memory) so it survives
        /// logout/login. Written to disk whenever the profile itself gets saved (e.g. on logout).
        /// </summary>
        private void SaveProfilePosition()
        {
            if (!string.IsNullOrEmpty(GumpName) && ProfileManager.CurrentProfile != null)
            {
                ProfileManager.CurrentProfile.XmlGumpPositions[GumpName] = $"{X},{Y}";
            }
        }

        private void SaveFile()
        {
            if (Interlocked.CompareExchange(ref _savingFile, 1, 0) != 0)
            {
                return;
            }

            int snapshotX = X;
            int snapshotY = Y;

            if (!string.IsNullOrEmpty(FilePath) && File.Exists(FilePath))
            {
                XmlDocument xmlDoc = new XmlDocument();

                try
                {
                    xmlDoc.LoadXml(File.ReadAllText(FilePath));

                    if (xmlDoc.DocumentElement != null)
                    {
                        XmlElement root = xmlDoc.DocumentElement;
                        root.SetAttribute("x", snapshotX.ToString());
                        root.SetAttribute("y", snapshotY.ToString());

                        xmlDoc.Save(FilePath);
                    }
                }
                catch (Exception e)
                {
                    GameActions.Print(World, e.Message);
                }
            }

            Interlocked.Exchange(ref _savingFile, 0);
        }

        public override void Dispose()
        {
            if (SavePosition && (saveFileAfter != uint.MaxValue || Interlocked.CompareExchange(ref _savingFile, 0, 0) == 1))
            {
                int attempts = 0;

                while (Interlocked.CompareExchange(ref _savingFile, 0, 0) == 1 && attempts++ < 50)
                {
                    Thread.Sleep(10);
                }

                if (Interlocked.CompareExchange(ref _savingFile, 0, 0) == 0)
                {
                    SaveFile();
                }
            }

            base.Dispose();
        }
    }

    internal class XmlHealthBar : Control
    {
        private readonly World _world;
        private ColorBox color_background, color_foreground;
        private GumpPic image_background;
        private GumpPicInPic image_foreground;
        private Mobile mobile;

        private ushort backgroundHue, backgroundImage, foregroundImage, foregroundImagePoisoned;

        public ushort NormalHue = 97;
        public ushort PoisonedHue = 62;

        public ushort BackgroundHue
        {
            get => backgroundHue;
            set
            {
                backgroundHue = value;

                if (color_background != null)
                {
                    color_background.Hue = value;
                }

                if (image_background != null)
                {
                    image_background.Hue = value;
                }
            }
        }

        public Direction BarDirection { get; set; } = Direction.LeftToRight;

        public ushort BackgroundImage
        {
            get => backgroundImage;
            set
            {
                backgroundImage = value;

                if (image_background != null)
                {
                    image_background.Graphic = value;
                    Width = image_background.Width;
                    Height = image_background.Height;
                }
            }
        }

        public ushort ForegroundImage
        {
            get => foregroundImage;
            set
            {
                foregroundImage = value;

                if (image_foreground != null)
                {
                    image_foreground.Graphic = value;
                }
            }
        }

        public ushort ForegroundImagePoisoned
        {
            get => foregroundImagePoisoned == 0 ? foregroundImage : foregroundImagePoisoned;
            set => foregroundImagePoisoned = value;
        }

        public XmlHealthBar(World world, uint serial)
        {
            _world = world;
            LocalSerial = serial;

            if (SerialHelper.IsMobile(serial))
            {
                mobile = _world.Get(serial) as Mobile;
            }

            AcceptMouseInput = false;
        }

        public void SetImageType()
        {
            color_background = null;
            color_foreground = null;

            image_background = new GumpPic(0, 0, 0, BackgroundHue);
            image_foreground = new GumpPicInPic(0, 0, 0, 0, 0, 0, 0);
        }

        public void SetColoredType()
        {
            image_background = null;
            image_foreground = null;

            color_background = new ColorBox(Width, Height, BackgroundHue);
            color_foreground = new ColorBox(Width, Height, NormalHue);
        }

        public override void Update()
        {
            base.Update();

            if (mobile != null)
            {
                if (mobile.IsPoisoned)
                {
                    if (color_foreground != null)
                    {
                        color_foreground.Hue = PoisonedHue;
                    }

                    if (image_foreground != null)
                    {
                        image_foreground.Hue = PoisonedHue;

                        if (foregroundImagePoisoned != 0)
                        {
                            image_foreground.Graphic = foregroundImagePoisoned;
                        }
                    }
                }
                else
                {
                    if (color_foreground != null)
                    {
                        color_foreground.Hue = NormalHue;
                    }

                    if (image_foreground != null)
                    {
                        image_foreground.Hue = NormalHue;

                        if (image_foreground.Graphic != foregroundImage)
                        {
                            image_foreground.Graphic = foregroundImage;
                        }
                    }
                }

                UpdateForegroundSizeForPercent();
            }
        }

        private void UpdateForegroundSizeForPercent()
        {
            switch (BarDirection)
            {
                case Direction.LeftToRight:
                    if (color_foreground != null)
                    {
                        color_foreground.Width = (int)(Width * healthPercent());
                    }

                    if (image_foreground != null)
                    {
                        int widthPerc = (int)(Width * healthPercent());
                        image_foreground.PicInPicBounds = new Rectangle(0, 0, widthPerc, Height);
                        image_foreground.Width = widthPerc;
                    }
                    break;
                case Direction.BottomToTop:
                    if (color_foreground != null)
                    {
                        color_foreground.Height = (int)(Height * healthPercent());
                        color_foreground.Y = Height - color_foreground.Height;
                    }

                    if (image_foreground != null)
                    {
                        int heightPerc = (int)(Height * healthPercent());
                        image_foreground.PicInPicBounds = new Rectangle(0, Height - heightPerc, Width, Height - (Height - heightPerc));
                        image_foreground.Y = Height - heightPerc;
                    }
                    break;
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            base.Draw(batcher, x, y);

            color_background?.Draw(batcher, x, y);
            color_foreground?.Draw(batcher, x, y + (color_foreground?.Y ?? 0));
            image_background?.Draw(batcher, x, y);
            image_foreground?.Draw(batcher, x, y + (image_foreground?.Y ?? 0));

            return true;
        }

        private float healthPercent()
        {
            if (mobile == null || mobile.HitsMax == 0)
            {
                return 0f;
            }

            return (float)((double)mobile.Hits / (double)mobile.HitsMax);
        }

        public enum Direction
        {
            LeftToRight,
            BottomToTop
        }
    }
}
