﻿//   
// Copyright (c) Jesse Freeman. All rights reserved.  
//  
// Licensed under the Microsoft Public License (MS-PL) License. 
// See LICENSE file in the project root for full license information. 
// 
// Contributors
// --------------------------------------------------------
// This is the official list of Pixel Vision 8 contributors:
//  
// Jesse Freeman - @JesseFreeman
// Christer Kaitila - @McFunkypants
// Pedro Medeiros - @saint11
// Shawn Rakowski - @shwany

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PixelVisionSDK.Utils;

namespace PixelVisionSDK.Chips
{
    
    public enum DrawMode
    {
        TilemapCache = -1,
        Background,
        SpriteBelow,
        Tile,
        Sprite,
        UI,
        SpriteAbove
    }

    public enum InputState
    {

        Down,
        Released

    }

    /// <summary>
    ///     The GameChip represents the foundation of a game class
    ///     with all the logic it needs to work correctly in the PixelVisionEngine.
    ///     The AbstractChip class manages configuring the game when created via the
    ///     chip life-cycle. The engine manages the game's state, the game's own life-cycle and
    ///     serialization/deserialization of the game's data.
    /// </summary>
    public class GameChip : AbstractChip, IUpdate, IDraw, IGameChip
    {
        protected Canvas cachedTileMap;
        public Dictionary<string, string> textFiles = new Dictionary<string, string>();
        
        protected double frameCount;
        protected double dt;
        protected double updateRate = 4.0;  // 4 updates per sec

//        private int i;
        
        public int fps
        {
            get; private set;
        }

        #region Debug
        
        /// <summary>
        ///     This returns the averaged FPS that is calculated by the Game Creator on each frame. This is not always
        ///     accurate and should only be used as the best guest estimate into the real framerate of your game.
        /// </summary>
        /// <returns>Returns an int with the current FPS out of a maximum of 60.</returns>
        public int ReadFPS()
        {
            return fps;
        }
        
        /// <summary>
        ///     Returns the total number of sprites on the display in the current frame. Call this at the end of the
        ///     Draw() method to get an accurate count of sprite draw calls.
        /// </summary>
        /// <returns>Returns an int for the total number of sprite draw calls at the current point in time.</returns>
        public int ReadTotalSprites()
        {
            return currentSprites;
        }
        
        #endregion
        
        /// <summary>
        ///     This allows you to add your Lua scripts at runtime to a game from a string. This could be useful for
        ///     dynamically generating code such as level data or other custom Lua objects in memory. Simply give the
        ///     script a name and pass in a string with valid Lua code. If a script with the same name exists, this will
        ///     override it. Make sure to call LoadScript() after to parse it.
        /// </summary>
        /// <param name="name">Name of the script. This should contain the .lua extension.</param>
        /// <param name="file">The string text representing the Lua script data.</param>
        public void AddTextFile(string name, string file)
        {
            if (textFiles.ContainsKey(name))
            {
                textFiles[name] = file;
            }
            else
            {
                textFiles.Add(name, file);
            }
        }
        
        protected readonly Dictionary<string, int> tmpTileData = new Dictionary<string, int>
        {
            {"spriteID", -1},
            {"colorOffset", -1},
            {"flag", -1}
        };

        protected string _name = "Untitle_Game";
        protected int _saveSlots;
        public Dictionary<string, string> savedData = new Dictionary<string, string>();

        private int[] tmpSpriteData = new int[0];
        
        
        public int currentSprites { get; private set; }
        

        #region GameChip Properties

        /// <summary>
        ///     Flag for the maximum size the game should be.
        /// </summary>
        public int maxSize = 256;

        public bool lockSpecs = false;

        public string ext = ".pv8";
        public string version = "0.0.0";
        
        /// <summary>
        ///     Used to limit the amount of data the game can save.
        /// </summary>
        public int saveSlots
        {
            get { return _saveSlots; }
            set
            {
                value = value.Clamp(8, 96);
                _saveSlots = value;

                // resize dictionary?
                for (var i = savedData.Count - 1; i >= 0; i--)
                {
                    var item = savedData.ElementAt(i);
                    if (i > value)
                        savedData.Remove(item.Key);
                }
            }
        }

        /// <summary>
        ///     Name of the game.
        /// </summary>
        public string name
        {
            get { return _name ?? GetType().Name; }
            set { _name = value; }
        }
        
        /// <summary>
        ///     The description for the game.
        /// </summary>
        public string description { get; set; }

        #endregion


        #region Chip References

        protected ColorChip colorChip;
        protected ControllerChip controllerChip;
        protected DisplayChip displayChip;
        protected SoundChip soundChip;
        protected SpriteChip spriteChip;
        protected TilemapChip tilemapChip;
        protected FontChip fontChip;
        protected MusicChip musicChip;
        
        private readonly int[] singlePixel = {0};

        #endregion

        #region Lifecycle

        
        public override void Configure()
        {
            // Set the engine's game to this instance
            engine.gameChip = this;
        }

        /// <summary>
        ///     Update() is called once per frame at the beginning of the game loop. This is where you should put all
        ///     non-visual game logic such as character position calculations, detecting input and performing updates to
        ///     your animation system. The time delta is provided on each frame so you can calculate the difference in
        ///     milliseconds since the last render took place.
        /// </summary>
        /// <param name="timeDelta">A float value representing the time in milliseconds since the last Draw() call was completed.</param>
        
        public virtual void Update(float timeDelta)
        {
            // Calculate framerate
            frameCount++;
            dt += timeDelta;
            if (dt > 1.0 / updateRate)
            {
                fps = (int)(frameCount / dt);
                frameCount = 0;
                dt -= 1.0 / updateRate;
            }
            
            // Reset the current sprite count
            currentSprites = 0;
        }

        /// <summary>
        ///     Draw() is called once per frame after the Update() has completed. This is where all visual updates to
        ///     your game should take place such as clearing the display, drawing sprites, and pushing raw pixel data
        ///     into the display.
        /// </summary>
        public virtual void Draw()
        {
            // Overwrite this method and add your own draw logic.
        }

        /// <summary>
        ///     Reset() is called when a game is restarted. This is usually called instead of reloading the entire game.
        ///     It allows you to perform additional configuration that would not be able to happen if the Init() method
        ///     is not called. This is mostly ignored in the Runner and is mainly used in the Game Creator.
        /// </summary>
        public override void Reset()
        {
            
            currentSprites = 0;
            
            _scrollX = 0;
            _scrollY = 0;
            
            // Get references to each of the chips
            colorChip = engine.colorChip; 
            controllerChip = engine.controllerChip;
            displayChip = engine.displayChip;
            soundChip = engine.soundChip;
            spriteChip = engine.spriteChip;
            tilemapChip = engine.tilemapChip;
            fontChip = engine.fontChip;
            musicChip = engine.musicChip;
            
            // Create a new canvas for the tilemap cache
            if(cachedTileMap == null)
                cachedTileMap = new Canvas(0, 0, this);
            
            // Build tilemap cache
            RebuildCache(cachedTileMap);
            
            // Resize the tmpSpriteData so it mateches the sprite's width and height
            Array.Resize(ref tmpSpriteData, spriteChip.width * spriteChip.height);
            
            base.Reset();
        }
        
        public override void Deactivate()
        {
            base.Deactivate();
            engine.gameChip = null;
        }
        
        /// <summary>
        ///     Shutdown() is called when quitting a game or shutting down the Runner/Game Creator. This hook allows you
        ///     to perform any last minute changes to the game's data such as saving or removing any temp files that
        ///     will not be needed.
        /// </summary>
        public override void Shutdown()
        {
            // Put save logic here
        }
        
        #endregion

        #region Color

        /// <summary>
        ///     The background color is used to fill the screen when clearing the display. You can use
        ///     this method to read or update the background color at any point during the GameChip's
        ///     draw phase. When calling BackgroundColor(), without an argument, it returns the current
        ///     background color int. You can pass in an optional int to update the background color by
        ///     calling BackgroundColor(0) where 0 is any valid ID in the ColorChip. Passing in a value
        ///     such as -1, or one that is out of range, defaults the background color to magenta (#ff00ff)
        ///     which is the engine's default transparent color.
        /// </summary>
        /// <param name="id">
        ///     This argument is optional. Supply an int to update the existing background color value.
        /// </param>
        /// <returns>
        ///     This method returns the current background color ID. If no color exists, it returns -1
        ///     which is magenta (#FF00FF).
        /// </returns>
        public virtual int BackgroundColor(int? id = null)
        {
            if (id.HasValue)
                colorChip.backgroundColor = id.Value;

            return colorChip.backgroundColor;
        }

        /// <summary>
        ///     The Color() method allows you to read and update color values in the ColorChip. This
        ///     method has two modes which require a color ID to work. By calling the method with just
        ///     an ID, like Color(0), it returns a hex string for the given color at the supplied color
        ///     ID. By passing in a new hex string, like Color(0, "#FFFF00"), you can change the color
        ///     with the given ID. While you can use this method to modify color values directly, you
        ///     should avoid doing this at run time since the DisplayChip must parse and cache the new
        ///     hex value. If you just want to change a color to an existing value, use the ReplaceColor()
        ///     method.
        /// </summary>
        /// <param name="id">
        ///     The ID of the color you want to access.
        /// </param>
        /// <param name="value">
        ///     This argument is optional. It accepts a hex as a string and updates the supplied color ID's value.
        /// </param>
        /// <returns>
        ///     This method returns a hex string for the supplied color ID. If the color has not been set
        ///     or is out of range, it returns magenta (#FF00FF) which is the default transparent system color.
        /// </returns>
        public string Color(int id, string value = null)
        {
            if (value == null)
                return colorChip.ReadColorAt(id);

            colorChip.UpdateColorAt(id, value);

            return value;
        }

        /// <summary>
        ///     The TotalColors() method simply returns the total number of colors in the ColorChip. By default,
        ///     it returns only colors that have been set to value other than magenta (#FF00FF) which is the
        ///     default transparent value used by the engine. By calling TotalColors(false), it returns the total
        ///     available color slots in the ColorChip.
        /// </summary>
        /// <param name="ignoreEmpty">
        ///     This is an optional value that defaults to true. When set to true, the ColorChip returns the total
        ///     number of colors not set to magenta (#FF00FF). Set this value to false if you want to get all of
        ///     the available color slots in the ColorChip regardless if they are empty or not.
        /// </param>
        /// <returns>
        ///     This method returns the total number of colors in the color chip based on the ignoreEmpty argument's
        ///     value.
        /// </returns>
        public int TotalColors(bool ignoreEmpty = false)
        {
            return ignoreEmpty ? colorChip.supportedColors : colorChip.total;
        }

        /// <summary>
        ///     Pixel Vision 8 sprites have limits around how many colors they can display at once which is called
        ///     the Colors Per Sprite or CPS. The ColorsPerSprite() method returns this value from the SpriteChip.
        ///     While this is read-only at run-time, it has other important uses. If you set up your ColorChip in
        ///     palettes, grouping sets of colors together based on the SpriteChip's CPS value, you can use this to
        ///     shift a sprite's color offset up or down by a fixed amount when drawing it to the display. Since this
        ///     value does not change when a game is running, it is best to get a reference to it when the game starts
        ///     up and store it in a local variable.
        /// </summary>
        /// <returns>
        ///     This method returns the Color Per Sprite limit value as an int.
        /// </returns>
        public int ColorsPerSprite()
        {
            // This can not be changed at run time so it will never need to be invalidated
            return spriteChip.colorsPerSprite; //colorsPerSpriteCached;//;
        }

        /// <summary>
        ///     The ReplaceColor() method allows you to quickly change a color to an existing color without triggering
        ///     the DisplayChip to parse and cache a new hex value. Consider this an alternative to the Color() method.
        ///     It is useful for simulating palette swapping animation on sprites pointed to a fixed group of color IDs.
        ///     Simply cal the ReplaceColor() method and supply a target color ID position, then the new color ID it
        ///     should point to. Since you are only changing the color's ID pointer, there is little to no performance
        ///     penalty during the GameChip's draw phase.
        /// </summary>
        /// <param name="index">The ID of the color you want to change.</param>
        /// <param name="id">The ID of the color you want to replace it with.</param>
        public void ReplaceColor(int index, int id)
        {
            colorChip.UpdateColorAt(index, colorChip.ReadColorAt(id));
        }

        #endregion

        #region Display

        private int w;
        private int h;
        
        /// <summary>
        ///     Clearing the display removed all of the existing pixel data, replacing it with the default background
        ///     color. The Clear() method allows you specify what region of the display to clear. By simply calling
        ///     Clear(), with no arguments, it automatically clears the entire display. You can manually define an area
        ///     of the screen to clear by supplying option x, y, width and height arguments. When clearing a specific
        ///     area of the display, anything outside of the defined boundaries remains on the next draw phase. This is
        ///     useful for drawing a HUD but clearing the display below for a scrolling map and sprites. Clear can only
        ///     be used once during the draw phase.
        /// </summary>
        /// <param name="x">
        ///     This is an optional value that defaults to 0 and defines where the clear's X position should begin.
        ///     When X is 0, clear starts on the far left-hand side of the display. Values less than 0 or greater than
        ///     the width of the display are ignored.
        /// </param>
        /// <param name="y">
        ///     This is an optional value that defaults to 0 and defines where the clear's Y position should begin. When Y
        ///     is 0, clear starts at the top of the display. Values less than 0 or greater than the height of the display
        ///     are ignored.
        /// </param>
        /// <param name="width">
        ///     This is an optional value that defaults to the width of the display and defines how many horizontal pixels
        ///     to clear. When the width is 0, clear starts at the x position and ends at the far right-hand side of the
        ///     display. Values less than 0 or greater than the width are adjusted to stay within the boundaries of the
        ///     screen's visible pixels.
        /// </param>
        /// <param name="height">
        ///     This is an optional value that defaults to the height of the display and defines how many vertical pixels
        ///     to clear. When the height is 0, clear starts at the Y position and ends at the bottom of the display.
        ///     Values less than 0 or greater than the height are adjusted to stay within the boundaries of the screen's
        ///     visible pixels.
        /// </param>
        public void Clear(int x = 0, int y = 0, int? width = null, int? height = null)
        {
            
//            displayChip.Clear();
            
            w = width.HasValue ? width.Value : displayChip.width - x;
            h = height.HasValue ? height.Value : displayChip.height - y;

            DrawRect(x, y, w, h, colorChip.backgroundColor);
            
        }

        protected Vector display = new Vector();
        private int offsetX;
        private int offsetY;
        
        /// <summary>
        ///     The display's size defines the visible area where pixel data exists on the screen. Calculating this is
        ///     important for knowing how to position sprites on the screen. The Display() method allows you to get
        ///     the resolution of the display at run time. By default, this will return the visble screen area based on
        ///     the overscan value set on the display chip. To calculate the exact overscan in pixels, you must subtract
        ///     the full size from the visible size. Simply supply false as an argument to get the full display dimensions.
        /// </summary>
        public Vector Display(bool visible = true)
        {
            offsetX = visible ? displayChip.overscanXPixels : 0;
            offsetY = visible ? displayChip.overscanYPixels : 0;

            display.x = displayChip.width - offsetX;
            display.y = displayChip.height - offsetY;

            return display;
        }
        
        public Rect VisibleBounds()
        {
            return displayChip.visibleBounds;
        }

        /// <summary>
        ///     This method allows you to draw raw pixel data directly to the display. Depending on which draw mode you
        ///     use, the pixel data could be rendered as a sprite or drawn directly onto the tilemap cache. Sprites drawn
        ///     with this method still count against the total number the display can render but you can draw irregularly
        ///     shaped sprites by defining a custom width and height. For drawnig into the tilemap cache directly, you can
        ///     use this to change the way the tilemap looks at run-time without having to modify a sprite's pixel data.
        ///     It is important to note that when you change a tile's sprite ID or color offset, the tilemap redraws it
        ///     back to the cache overwriting any pixel data that was previously there.
        /// </summary>
        /// <param name="pixelData">
        ///     The pixelData argument accepts an int array representing references to color IDs. The pixelData array length
        ///     needs to be the same size as the supplied width and height, or it will throw an error.
        /// </param>
        /// <param name="x">
        ///     The x position where to display the new pixel data. The display's horizontal 0 position is on the far left-hand
        ///     side.
        ///     When using DrawMode.TilemapCache, the pixel data is drawn into the tilemap's cache instead of directly on
        ///     the display when using DrawMode.Sprite.
        /// </param>
        /// <param name="y">
        ///     The Y position where to display the new pixel data. The display's vertical 0 position is on the top. When using
        ///     DrawMode.TilemapCache, the pixel data is drawn into the tilemap's cache instead of directly on the display
        ///     when using DrawMode.Sprite.
        /// </param>
        /// <param name="width">
        ///     The width of the pixel data to use when rendering to the display.
        /// </param>
        /// <param name="height">
        ///     The height of the pixel data to use when rendering to the display.
        /// </param>
        /// <param name="drawMode">
        ///     This argument accepts the DrawMode enum. You can use Sprite, SpriteBelow, and TilemapCache to change where the
        ///     pixel data is drawn to. By default, this value is DrawMode.Sprite.
        /// </param>
        /// <param name="flipH">
        ///     This is an optional argument which accepts a bool. The default value is set to false but passing in true flips
        ///     the pixel data horizontally.
        /// </param>
        /// <param name="flipV">
        ///     This is an optional argument which accepts a bool. The default value is set to false but passing in true flips
        ///     the pixel data vertically.
        /// </param>
        /// <param name="colorOffset">
        ///     This optional argument accepts an int that offsets all the color IDs in the pixel data array. This value is added
        ///     to each int, in the pixel data array, allowing you to simulate palette shifting.
        /// </param>
        public void DrawPixels(int[] pixelData, int x, int y, int width, int height, DrawMode drawMode = DrawMode.Sprite, bool flipH = false, bool flipV = false, int colorOffset = 0)
        {
            if (flipH || flipV)
                SpriteChipUtil.FlipSpriteData(ref pixelData, width, height, flipH, flipV);

            switch (drawMode)
            {
                case DrawMode.TilemapCache:
                    
                    UpdateCachedTilemap(pixelData, x, y, width, height, colorOffset);

                    break;
                    
                default:
                    
                    // Need to flip the y position to draw correctly
//                    y = displayChip.height - height - y;
                    
                    displayChip.NewDrawCall(pixelData, x, y, width, height, (int)drawMode, colorOffset);

                    break;
            }
        }

        /// <summary>
        ///     This method allows you to draw a single pixel to the Tilemap Cache. It's an expensive operation which leverages 
        ///     DrawPixels(). This should only be used in special occasions when batching pixel data draw request aren't possible.
        /// </summary>
        /// <param name="x">
        ///     The x position where to display the new pixel data. The display's horizontal 0 position is on the far left-hand
        ///     side.
        ///     When using DrawMode.TilemapCache, the pixel data is drawn into the tilemap's cache instead of directly on
        ///     the display when using DrawMode.Sprite.
        /// </param>
        /// <param name="y">
        ///     The Y position where to display the new pixel data. The display's vertical 0 position is on the top. When using
        ///     DrawMode.TilemapCache, the pixel data is drawn into the tilemap's cache instead of directly on the display
        ///     when using DrawMode.Sprite.
        /// </param>
        /// <param name="colorRef">
        ///     The color ID to use when drawing the pixel.
        /// </param>
        public void DrawPixel(int x, int y, int colorRef)
        {

            singlePixel[0] = colorRef;
            
            // Route the single pixel call to the DrawPixels call
            DrawPixels(singlePixel, x, y, 1, 1, DrawMode.TilemapCache);
            
        }

        /// <summary>
        ///     Sprites represent individual collections of pixel data at a fixed size. By default, Pixel Vision 8 sprites are
        ///     8 x 8 pixels and have a set limit of visible colors. You can use the DrawSprite() method to render any sprite
        ///     stored in the Sprite Chip. The display also has a limitation on how many sprites can be on the screen at one time.
        ///     Each time you call DrawSprite(), the sprite counts against the total amount the display can render. If you attempt
        ///     to
        ///     draw more sprites than the display can handle, the call is ignored. One thing to keep in mind when drawing sprites
        ///     is that their x and y position wraps if they reach the right or bottom border of the screen. You need to change
        ///     the overscan border to hide sprites offscreen.
        /// </summary>
        /// <param name="id">
        ///     The unique ID of the sprite to use in the SpriteChip.
        /// </param>
        /// <param name="x">
        ///     An int value representing the X position to place sprite on the display. If set to 0, it renders on the far
        ///     left-hand side of the screen.
        /// </param>
        /// <param name="y">
        ///     An int value representing the Y position to place sprite on the display. If set to 0, it renders on the top
        ///     of the screen.
        /// </param>
        /// <param name="flipH">
        ///     This is an optional argument which accepts a bool. The default value is set to false but passing in true flips
        ///     the pixel data horizontally.
        /// </param>
        /// <param name="flipV">
        ///     This is an optional argument which accepts a bool. The default value is set to false but passing in true flips
        ///     the pixel data vertically.
        /// </param>
        /// <param name="aboveBG">
        ///     An optional bool that defines if the sprite is above or below the tilemap. Sprites are set to render above the
        ///     tilemap by default. When rendering below the tilemap, the sprite is visible in the transparent area of the tile
        ///     above the background color.
        /// </param>
        /// <param name="colorOffset">
        ///     This optional argument accepts an int that offsets all the color IDs in the pixel data array. This value is added
        ///     to each int, in the pixel data array, allowing you to simulate palette shifting.
        /// </param>
        public virtual void DrawSprite(int id, int x, int y, bool flipH = false, bool flipV = false, DrawMode drawMode = DrawMode.Sprite, int colorOffset = 0)
        {
            // Only apply the max sprite count to sprite draw modes

            if (drawMode == DrawMode.Tile)
            {
                Tile(x, y, id, colorOffset);
            }
            else if (drawMode == DrawMode.TilemapCache || drawMode == DrawMode.UI)
            {
                spriteChip.ReadSpriteAt(id, tmpSpriteData);
                
                DrawPixels(tmpSpriteData, x, y, spriteChip.width, spriteChip.height, drawMode, flipH, flipV, colorOffset);
            }
            else
            {
                if (spriteChip.maxSpriteCount > 0 && currentSprites >= spriteChip.maxSpriteCount)
                    return;
            
                //TODO flipping H, V and colorOffset should all be passed into reading a sprite
                spriteChip.ReadSpriteAt(id, tmpSpriteData);

                DrawPixels(tmpSpriteData, x, y, spriteChip.width, spriteChip.height, drawMode, flipH, flipV, colorOffset);
                
                currentSprites ++;
            }
            
            
            
            
            
            
//            var spriteMode = (drawMode == DrawMode.Sprite || drawMode == DrawMode.SpriteAbove || drawMode == DrawMode.SpriteBelow);
//            
//            if (spriteChip.maxSpriteCount > 0 && currentSprites >= spriteChip.maxSpriteCount && spriteMode)
//                return;
//            
//            // TODO this should be condensed into a single call?
//            
//            //TODO flipping H, V and colorOffset should all be passed into reading a sprite
//            spriteChip.ReadSpriteAt(id, tmpSpriteData);
//
//            // Mode 0 is sprite above bg and mode 1 is sprite below bg.
//            //var mode = aboveBG ? DrawMode.Sprite : DrawMode.SpriteBelow;
//            DrawPixels(tmpSpriteData, x, y, spriteChip.width, spriteChip.height, drawMode, flipH, flipV, colorOffset);
//            
//            if(spriteMode)
//                currentSprites ++;
        }

        protected int[] tmpIDs = new int[0];
        private int total;
        
        private int height;
        private int startX;
        private int startY;
        private int id;
        private bool render;
        /// <summary>
        ///     The DrawSprites method makes it easier to combine and draw groups of sprites to the display in a grid. This is
        ///     useful when trying to render 4 sprites together as a larger 16x16 pixel graphic. While there is no limit on the
        ///     size of the sprite group which can be rendered, it is important to note that each sprite in the array still counts
        ///     as an individual sprite. Sprites passed into the DrawSprites() method are visible if the display can render it.
        ///     Under the hood, this method uses DrawSprite but solely manages positioning the sprites out in a grid. Another
        ///     unique feature of his helper method is that it automatically hides sprites that go offscreen. When used with
        ///     overscan border, it greatly simplifies drawing larger sprites to the display.
        /// </summary>
        /// <param name="ids">
        ///     An array of sprite IDs to display on the screen.
        /// </param>
        /// <param name="x">
        ///     An int value representing the X position to place sprite on the display. If set to 0, it renders on the far
        ///     left-hand side of the screen.
        /// </param>
        /// <param name="y">
        ///     An int value representing the Y position to place sprite on the display. If set to 0, it renders on the top
        ///     of the screen.
        /// </param>
        /// <param name="width">
        ///     The width, in sprites, of the grid. A value of 2 renders 2 sprites wide. The DrawSprites method continues to
        ///     run through all of the sprites in the ID array until reaching the end. Sprite groups do not have to be perfect
        ///     squares since the width value is only used to wrap sprites to the next row.
        /// </param>
        /// <param name="flipH">
        ///     This is an optional argument which accepts a bool. The default value is set to false but passing in true flips
        ///     the pixel data horizontally.
        /// </param>
        /// <param name="flipV">
        ///     This is an optional argument which accepts a bool. The default value is set to false but passing in true flips
        ///     the pixel data vertically.
        /// </param>
        /// <param name="drawMode"></param>
        /// <param name="colorOffset">
        ///     This optional argument accepts an int that offsets all the color IDs in the pixel data array. This value is added
        ///     to each int, in the pixel data array, allowing you to simulate palette shifting.
        /// </param>
        /// <param name="onScreen">
        ///     This flag defines if the sprites should not render when they are off the screen. Use this in conjunction with
        ///     overscan border control what happens to sprites at the edge of the display. If this value is false, the sprites
        ///     wrap around the screen when they reach the edges of the screen.
        /// </param>
        /// <param name="useScrollPos">This will automatically offset the sprite's x and y position based on the scroll value.</param>
        public void DrawSprites(int[] ids, int x, int y, int width, bool flipH = false, bool flipV = false, DrawMode drawMode = DrawMode.Sprite, int colorOffset = 0, bool onScreen = true, bool useScrollPos = true, Rect bounds = null)
        {
            
            total = ids.Length;
            
            // TODO added this so C# code isn't corrupted, need to check performance impact
            if(tmpIDs.Length != total)
                Array.Resize(ref tmpIDs, total);

            Array.Copy(ids, tmpIDs, total);
            
            height = MathUtil.CeilToInt(total / width);

            startX = x - (useScrollPos ? _scrollX : 0);
            startY = y - (useScrollPos ? _scrollY : 0);

            var paddingW = spriteChip.width;
            var paddingH = spriteChip.height;

            if (drawMode == DrawMode.Tile)
            {
                paddingW = 1;
                paddingH = 1;
            }
//            startY = displayChip.height - height - startY;
            
            if (flipH || flipV)
                SpriteChipUtil.FlipSpriteData(ref tmpIDs, width, height, flipH, flipV);

            // Store the sprite id from the ids array
//            int id;
//            bool render;

            // TODO need to offset the bounds based on the scroll position before testing against it

            for (var i = 0; i < total; i++)
            {
                // Set the sprite id
                id = tmpIDs[i];

                // TODO should also test that the sprite is not greater than the total sprites (from a cached value)
                // Test to see if the sprite is within range
                if (id > -1)
                {
                    x = (MathUtil.FloorToInt(i % width) * paddingW) + startX;
                    y = (MathUtil.FloorToInt(i / width) * paddingH) + startY;
//
//                    var render = true;
                    
                    // Check to see if we need to test the bounds
                    if (onScreen)
                    {
                        if (bounds == null)
                            bounds = displayChip.visibleBounds;

                        // This can set the render flag to true or false based on it's location
                        //TODO need to take into account the current bounds of the screen
                        render = x >= bounds.x && x <= bounds.width && y >= bounds.y && y <= bounds.height;
                    }
                    else
                    {
                        // If we are not testing to see if the sprite is onscreen it will always render and wrap based on its position
                        render = true;
                    }

                    // If the sprite should be rendered, call DrawSprite()
                    if (render)
                        DrawSprite(id, x, y, flipH, flipV, drawMode, colorOffset);
                }
            }
        }

        
        /// <summary>
        ///     DrawSpriteBlock() is similar to DrawSprites except you define the first sprite (upper left corner) and the width x height 
        ///     (in sprites) to sample from sprite ram. This will create a larger sprite by using neighbor sprites.
        /// </summary>
        /// <param name="id">The top left sprite to start with. </param>
        /// <param name="x">
        ///     An int value representing the X position to place sprite on the display. If set to 0, it renders on the far
        ///     left-hand side of the screen.
        /// </param>
        /// <param name="y">
        ///     An int value representing the Y position to place sprite on the display. If set to 0, it renders on the top
        ///     of the screen.
        /// </param>
        /// <param name="width">
        ///     The width, in sprites, of the grid. A value of 2 renders 2 sprites wide. The DrawSprites method continues to
        ///     run through all of the sprites in the ID array until reaching the end. Sprite groups do not have to be perfect
        ///     squares since the width value is only used to wrap sprites to the next row.
        /// </param>
        /// <param name="flipH">
        ///     This is an optional argument which accepts a bool. The default value is set to false but passing in true flips
        ///     the pixel data horizontally.
        /// </param>
        /// <param name="flipV">
        ///     This is an optional argument which accepts a bool. The default value is set to false but passing in true flips
        ///     the pixel data vertically.
        /// </param>
        /// <param name="drawMode"></param>
        /// <param name="colorOffset">
        ///     This optional argument accepts an int that offsets all the color IDs in the pixel data array. This value is added
        ///     to each int, in the pixel data array, allowing you to simulate palette shifting.
        /// </param>
        /// <param name="onScreen">
        ///     This flag defines if the sprites should not render when they are off the screen. Use this in conjunction with
        ///     overscan border control what happens to sprites at the edge of the display. If this value is false, the sprites
        ///     wrap around the screen when they reach the edges of the screen.
        /// </param>
        /// <param name="useScrollPos">This will automatically offset the sprite's x and y position based on the scroll value.</param>
        public void DrawSpriteBlock(int id, int x, int y, int width = 1, int height = 1, bool flipH = false, bool flipV = false, DrawMode drawMode = DrawMode.Sprite, int colorOffset = 0, bool onScreen = true, bool useScrollPos = true, Rect bounds = null)
        {

            total = width * height;
            
            var sprites = new int[total];

            var sW = MathUtil.CeilToInt((float)spriteChip.textureWidth/spriteChip.width);
            
            var startC = id % sW;
            var tmpC = id % sW;
            var tmpR = (int)Math.Floor(id / (double)sW);

            var tmpCols = tmpC + width;

            for (var i = 0; i < total; i++)
            {

                sprites[i] = tmpC + tmpR * sW;

                tmpC += 1;

                if (tmpC >= tmpCols)
                {
                    tmpC = startC;
                    tmpR += 1;
                }

            }
            
            DrawSprites(sprites, x, y, width, flipH, flipV, drawMode, colorOffset, onScreen, useScrollPos, bounds);

        }
        
        /// <summary>
        ///     The DrawTile method makes it easier to update the visuals of a tile on any of the map layers. By default, 
        ///     this will modify a single tile's sprite id and color offset. You can also define the DrawMode to target a 
        ///     specific layer. By default, DrawMode.Tile is used, but this method also accepts DrawMode.TilemapCache and 
        ///     DrawMode.UI to target the UI layer above the tilemap. It's important to note that this method can only draw 
        ///     a tile at a specific column and row. If you need pixel perfect drawing on the TilemapCache or UI layer, use 
        ///     the DrawPixels method. Finally, drawing a tile into the tilemap itself will force that tile to be copied to 
        ///     the Tilemap Cache on the next render pass just like calling the Tile() method.
        /// </summary>
        /// <param name="id">Sprite ID to use for the tile.</param>
        /// <param name="c">The column in the layer.</param>
        /// <param name="r">The row in the layer.</param>
        /// <param name="drawMode">This accepts DrawMode.Tile, DrawMode.TilemapCache and DrawMode.UI.</param>
        /// <param name="colorOffset">This is the color offset to use for the tile.</param>
        public void DrawTile(int id, int c, int r, DrawMode drawMode = DrawMode.Tile, int colorOffset = 0)
        {

            if (drawMode == DrawMode.Tile)
            {
                Tile(c, r, id, colorOffset);
            }
            else if (drawMode == DrawMode.TilemapCache)
            {
                c *= spriteChip.width;
                r *= spriteChip.height;
                
                //TODO flipping H, V and colorOffset should all be passed into reading a sprite
                spriteChip.ReadSpriteAt(id, tmpSpriteData);
                
                // Mode 0 is sprite above bg and mode 1 is sprite below bg.
                //var mode = aboveBG ? DrawMode.Sprite : DrawMode.SpriteBelow;
                DrawPixels(tmpSpriteData, c, r, spriteChip.width, spriteChip.height, drawMode, false, false, colorOffset);
            }
            
        }
        
        /// <summary>
        ///     The DrawTiles method makes it easier to update the visuals of multiple tiles at once by leveraging the 
        ///     DrawTile method. Simply pass in an Array of sprite IDs, the column, row and width (in tiles) to 
        ///     make bulk changes to a tilemap layer. You can also define the DrawMode to target a specific layer. By default, 
        ///     DrawMode.Tile is used, but this method also accepts DrawMode.TilemapCache and DrawMode.UI to target the UI 
        ///     layer above the tilemap.
        /// </summary>
        /// <param name="ids">An Array of Sprite IDs.</param>
        /// <param name="c">The column in the layer.</param>
        /// <param name="r">The row in the layer.</param>
        /// <param name="width">The number of horizontal tiles in the group.</param>
        /// <param name="drawMode">This accepts DrawMode.Tile, DrawMode.TilemapCache and DrawMode.UI.</param>
        /// <param name="colorOffset">This is the color offset to use for the tile.</param>
        public void DrawTiles(int[] ids, int c, int r, int width, DrawMode drawMode = DrawMode.Tile, int colorOffset = 0)
        {

            if (drawMode == DrawMode.Tile)
            {
                UpdateTiles(c, r, width, ids, colorOffset);
            }
            else if (drawMode == DrawMode.TilemapCache)
            {

                total = ids.Length;

                // Store the sprite id from the ids array
                int id;

                for (var i = 0; i < total; i++)
                {
                    // Set the sprite id
                    id = ids[i];

                    // TODO should also test that the sprite is not greater than the total sprites (from a cached value)
                    // Test to see if the sprite is within range
                    if (id > -1)
                    {
                        var c1 = (MathUtil.FloorToInt(i % width));
                        var r2 = (MathUtil.FloorToInt(i / width));

                        DrawTile(id, c1 + c, r2 + r, drawMode, colorOffset);
                    }
                }
            }
        }

        private Vector spriteSize;
        private int charWidth;
        private int nextX;
        private int nextY;
        private int[] spriteIDs;
//        private int j;
        private int[] pixelData;

        /// <summary>
        ///     The DrawText() method allows you to render text to the display. By supplying a custom DrawMode, you can render
        ///     characters as individual sprites (DrawMode.Sprite), tiles (DrawMode.Tile) or drawn directly into the tilemap
        ///     cache (DrawMode.TilemapCache). When drawing text as sprites, you have more flexibility over position, but each
        ///     character counts against the displays' maximum sprite count. When rendering text to the tilemap, more characters
        ///     are shown and also increase performance when rendering large amounts of text. You can also define the color offset,
        ///     letter spacing which only works for sprite and tilemap cache rendering, and a width in characters if you want the
        ///     text to wrap.
        /// </summary>
        /// <param name="text">
        ///     A text string to display on the screen.
        /// </param>
        /// <param name="x">
        ///     An int value representing the X position to start the text on the display. If set to 0, it renders on the far
        ///     left-hand side of the screen.
        /// </param>
        /// <param name="y">
        ///     An int value representing the Y position to place sprite on the display. If set to 0, it renders on the top
        ///     of the screen.
        /// </param>
        /// <param name="drawMode">
        ///     This argument accepts the DrawMode enum. You can use Sprite, SpriteBelow, and TilemapCache to change where the
        ///     pixel data is drawn to. By default, this value is DrawMode.Sprite.
        /// </param>
        /// <param name="font">
        ///     The name of the font to use. You do not need to add the font's file extension. If the file is called
        ///     The name of the font to use. You do not need to add the font's file extension. If the file is called
        ///     default.font.png,
        ///     you can simply refer to it as "default" when supplying an argument value.
        /// </param>
        /// <param name="colorOffset">
        ///     This optional argument accepts an int that offsets all the color IDs in the pixel data array. This value is added
        ///     to each color ID in the font's pixel data, allowing you to simulate palette shifting.
        /// </param>
        /// <param name="spacing">
        ///     This optional argument sets the number of pixels between each character when rendering text. This value is ignored
        ///     when rendering text as tiles. This value can be positive or negative depending on your needs. By default, it is 0.
        /// </param>
        /// <returns></returns>
        public void DrawText(string text, int x, int y, DrawMode drawMode = DrawMode.Sprite, string font = "Default", int colorOffset = 0, int spacing = 0)
        {
            // TODO this should use DrawSprites() API
            spriteSize = SpriteSize();
            charWidth = spriteSize.x;

            nextX = x;
            nextY = y;

            spriteIDs = ConvertTextToSprites(text, font);
            total = spriteIDs.Length;

            var offset = charWidth + spacing;

            if (drawMode == DrawMode.Tile)
                offset = 1;
            
            for (var j = 0; j < total; j++)
            {
                DrawSprite(spriteIDs[j], nextX, nextY, false, false, drawMode, colorOffset);
                nextX += offset;
            }
        }

        
        
        private int[] tmpTilemapCache = new int[0];
        private int oX;
        private int oY;
        private int width;
        private int sY;
        
        /// <summary>
        ///     By default, the tilemap renders to the display by simply calling DrawTilemap(). This automatically fills the entire
        ///     display with the visible portion of the tilemap. To have more granular control over how to render the tilemap, you
        ///     can supply an optional X and Y position to change where it draws on the screen. You can also modify the width
        ///     (columns) and height (rows) that are displayed too. This is useful if you want to show a HUD or some other kind of
        ///     image on the screen that is not overridden by the tilemap. To scroll the tilemap, you need to call the
        ///     ScrollPosition() and supply a new scroll X and Y value.
        /// </summary>
        /// <param name="x">
        ///     An optional int value representing the X position to render the tilemap on the display. If set to 0, it
        ///     renders on the far left-hand side of the screen.
        /// </param>
        /// <param name="y">
        ///     An optional int value representing the Y position to render the tilemap on the display. If set to 0, it
        ///     renders on the top of the screen.
        /// </param>
        /// <param name="columns">
        ///     An optional int value representing how many horizontal tiles to include when drawing the map. By default, this is
        ///     0 which automatically uses the full visible width of the display, while taking into account the X position offset.
        /// </param>
        /// <param name="rows">
        ///     An optional int value representing how many vertical tiles to include when drawing the map. By default, this is 0
        ///     which automatically uses the full visible height of the display, while taking into account the Y position offset.
        /// </param>
        /// <param name="offsetX">
        ///     An optional int value to override the scroll X position. This is useful when you need to change the left x position 
        ///     from where to sample the tilemap data from.
        /// </param>
        /// <param name="offsetY">
        ///     An optional int value to override the scroll Y position. This is useful when you need to change the top y position 
        ///     from where to sample the tilemap data from.
        /// </param>
        /// <param name="DrawMode">
        ///     This accepts DrawMode Tile and TilemapCache.
        /// </param>
        public void DrawTilemap(int x = 0, int y = 0, int columns = 0, int rows = 0, int? offsetX = null, int? offsetY = null, DrawMode drawMode = DrawMode.Tile)
        {
            
            viewPort.x = offsetX ?? _scrollX;
            viewPort.y = offsetY ?? _scrollY;
            viewPort.width = columns == 0 ? displayChip.width : columns * spriteChip.width;
            viewPort.height = rows == 0 ? displayChip.height : rows * spriteChip.height;
            
            // Grab the correct cached pixel data
            GetCachedPixels(viewPort.x, viewPort.y, viewPort.width, viewPort.height, ref tmpTilemapCache);
    
            // Copy over the cached pixel data from the tilemap request
            DrawPixels(tmpTilemapCache, x, y, viewPort.width, viewPort.height, drawMode);

        }
        
        private Rect viewPort = new Rect();
        
        /// <summary>
        ///     This method allows you to draw a rectangle with a fill color. By default, this method is used to clear the screen but you can supply a color offset to change the color value and use it to fill a rectangle area with a specific color instead.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="color"></param>
        /// <param name="drawMode"></param>
        public void DrawRect(int x, int y, int width, int height, int color = -1, DrawMode drawMode = DrawMode.Background)
        {
            // TODO is there a faster way to do this?
            DrawPixels(new int[width * height], x, y, width, height, drawMode, false, false, color);
        }
        
        /// <summary>
        ///     You can use RedrawDisplay to make clearing and drawing the tilemap easier. This is a helper method automatically
        ///     calls both Clear() and DrawTilemap() for you.
        /// </summary>
        public void RedrawDisplay()
        {
            Clear();
            DrawTilemap();
        }
        
        protected int _scrollX;
        protected int _scrollY;
        
//        Vector pos = new Vector();
        
        /// <summary>
        ///     You can scroll the tilemap by calling the ScrollPosition() method and supplying a new scroll X and Y position.
        ///     By default, calling ScrollPosition() with no arguments returns a vector with the current scroll X and Y values.
        ///     If you supply an X and Y value, it updates the tilemap's scroll position the next time you call the
        ///     DrawTilemap() method.
        /// </summary>
        /// <param name="x">
        ///     An optional int value representing the scroll X position of the tilemap. If set to 0, it starts on the far
        ///     left-hand side of the tilemap.
        /// </param>
        /// <param name="y">
        ///     An optional int value representing the scroll Y position of the tilemap. If set to 0, it starts on the top of
        ///     the tilemap.
        /// </param>
        /// <returns>
        ///     By default, this method returns a vector with the current scroll X and Y position.
        /// </returns>
        public Vector ScrollPosition(int? x = null, int? y = null)
        {
            var pos = new Vector();

            if (x.HasValue)
            {
                pos.x = x.Value;
                _scrollX = pos.x;
            }
            else
            {
                pos.x = _scrollX;
            }

            if (y.HasValue)
            {
                pos.y = y.Value;
                _scrollY = pos.y;
            }
            else
            {
                pos.y = _scrollY;
            }

//            pos.y = realHeight - 1 - pos.y;
            return pos;
        }

        #endregion
        
        #region Pixel Data

        private int index;
        private int spriteID;
        private char character;
        
        protected  int charOffset = 32;
        
        public int[] ConvertTextToSprites(string text, string fontName = "default")
        {
            total = text.Length;

            spriteIDs = new int[total];

//            char character;

//            int spriteID, index;
            
            var fontMap = fontChip.ReadFont(fontName);
            
            // Test to make sure font exists
            if (fontMap == null)
                throw new Exception("Font '" + fontName + "' not found.");

            var totalCharacters = fontMap.Length;

            for (var i = 0; i < total; i++)
            {
                character = text[i];
                index = Convert.ToInt32(character) - charOffset;
                spriteID = -1;

                if (index < totalCharacters && index > -1)
                    spriteID = fontMap[index];

                spriteIDs[i] = spriteID;
            }

            return spriteIDs;
        }

        public int[] ConvertCharacterToPixelData(char character, string fontName)
        {

            var fontMap = fontChip.ReadFont(fontName);
            
            // Test to make sure font exists
            if (fontMap == null)
                throw new Exception("Font '" + fontName + "' not found.");

            var index = Convert.ToInt32(character) - charOffset;

            var totalCharacters = fontMap.Length;
            var spriteID = -1;

            if (index < totalCharacters && index > -1)
                spriteID = fontMap[index];

            if (spriteID > -1)
            {
                return Sprite(spriteID);
            }

            return null;
        }
                
        #endregion
        
        #region File IO

        /// <summary>
        ///     Allows you to save string data to the game file itself. This data persistent even after restarting a game.
        /// </summary>
        /// <param name="key">
        ///     A string to use as the key for the data.
        /// </param>
        /// <param name="value">
        ///     A string representing the data to be saved.
        /// </param>
        public void WriteSaveData(string key, string value)
        {
            if (savedData.Count > saveSlots)
                return;

            if (savedData.ContainsKey(key))
            {
                savedData[key] = value;
                return;
            }

            savedData.Add(key, value);
        }

        /// <summary>
        ///     Allows you to read saved data by supplying a key. If no matching key exists, "undefined" is returned.
        /// </summary>
        /// <param name="key">
        ///     The string key used to find the data.
        /// </param>
        /// <param name="defaultValue">
        ///     The optional string to use if data does not exist.
        /// </param>
        /// <returns>
        ///     Returns string data associated with the supplied key.
        /// </returns>
        public string ReadSaveData(string key, string defaultValue = "undefine")
        {
            if (!savedData.ContainsKey(key))
                WriteSaveData(key, defaultValue);

            return savedData[key];
        }

        #endregion

        #region Input

        /// <summary>
        ///     While the main form of input in Pixel Vision 8 comes from the controllers, you can test for keyboard
        ///     input by calling the Key() method. When called, this method returns the current state of a key. The
        ///     method accepts the Keys enum, or an int, for a specific key. In additon, you need to provide the input
        ///     state to check for. The InputState enum has two states, Down and Released. By default, Down is
        ///     automatically used which returns true when the key is being pressed in the current frame. When using
        ///     Released, the method returns true if the key is currently up but was down in the last frame.
        /// </summary>
        /// <param name="key">
        ///     This argument accepts the Keys enum or an int for the key's ID.
        /// </param>
        /// <param name="state">
        ///     Optional InputState enum. Returns down state by default. This argument accepts InputState.Down (0)
        ///     or InputState.Released (1).
        /// </param>
        /// <returns>
        ///     This method returns a bool based on the state of the button.
        /// </returns>
        public bool Key(Keys key, InputState state = InputState.Down)
        {
            return state == InputState.Released
                ? controllerChip.GetKeyUp((int) key)
                : controllerChip.GetKeyDown((int) key);
        }

        /// <summary>
        ///     Pixel Vision 8 supports mouse input. You can get the current state of the mouse's left (0) and
        ///     right (1) buttons by calling MouseButton(). In addition to supplying a button ID, you also need
        ///     to provide the InputState enum. The InputState enum contains options for testing the Down and
        ///     Released states of the supplied button ID. By default, Down is automatically used which returns
        ///     true when the key was pressed in the current frame. When using Released, the method returns true
        ///     if the key is currently up but was down in the last frame.
        /// </summary>
        /// <param name="button">
        ///     Accepts an int for the left (0) or right (1) mouse button.
        /// </param>
        /// <param name="state">
        ///     An optional InputState enum. Uses InputState.Down default.
        /// </param>
        /// <returns>
        ///     Returns a bool based on the state of the button.
        /// </returns>
        public bool MouseButton(int button, InputState state = InputState.Down)
        {
            return state == InputState.Released
                ? controllerChip.GetMouseButtonUp(button)
                : controllerChip.GetMouseButtonDown(button);
        }

        /// <summary>
        ///     The main form of input for Pixel Vision 8 is the controller's buttons. You can get the current
        ///     state of any button by calling the Button() method and supplying a button ID, an InputState enum,
        ///     and the controller ID. When called, the Button() method returns a bool for the requested button
        ///     and its state. The InputState enum contains options for testing the Down and Released states of
        ///     the supplied button ID. By default, Down is automatically used which returns true when the key
        ///     was pressed in the current frame. When using Released, the method returns true if the key is
        ///     currently up but was down in the last frame.
        /// </summary>
        /// <param name="button">
        ///     Accepts the Buttons enum or int for the button's ID.
        /// </param>
        /// <param name="state">
        ///     Optional InputState enum. Returns down state by default.
        /// </param>
        /// <param name="controllerID">
        ///     An optional InputState enum. Uses InputState.Down default.
        /// </param>
        /// <returns>
        ///     Returns a bool based on the state of the button.
        /// </returns>
        public bool Button(Buttons button, InputState state = InputState.Down, int controllerID = 0)
        {
            return state == InputState.Released
                ? controllerChip.ButtonReleased(button, controllerID)
                : controllerChip.ButtonDown(button, controllerID);
        }

        /// <summary>
        ///     The MousePosition() method returns a vector for the current cursor's X and Y position.
        ///     This value is read-only. The mouse's 0,0 position is in the upper left-hand corner of the
        ///     display
        /// </summary>
        /// <returns>
        ///     Returns a vector for the mouse's X and Y poisition.
        /// </returns>
        public Vector MousePosition()
        {
            return controllerChip.ReadMousePosition();
        }

        /// <summary>
        ///     The InputString() method returns the keyboard input entered this frame. This method is
        ///     useful for capturing keyboard text input.
        /// </summary>
        /// <returns>
        ///     A string of all the characters entered during the frame.
        /// </returns>
        public string InputString()
        {
            return controllerChip.ReadInputString();
        }

        #endregion

        #region Sound

        /// <summary>
        ///     This method plays back a sound on a specific channel. The SoundChip has a limit of
        ///     active channels so playing a sound effect while another was is playing on the same
        ///     channel will cancel it out and replace with the new sound.
        /// </summary>
        /// <param name="id">
        ///     The ID of the sound in the SoundCollection.
        /// </param>
        /// <param name="channel">
        ///     The channel the sound should play back on. Channel 0 is set by default.
        /// </param>
        public void PlaySound(int id, int channel = 0)
        {
            soundChip.PlaySound(id, channel);
        }
        
        /// <summary>
        ///     This method allows your read and write raw sound data on the SoundChip.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="data"></param>
        public string Sound(int id, string data = null)
        {

            if (data != null)
            {
                soundChip.UpdateSound(id, data);
            }
            
            return soundChip.ReadSound(id).ReadSettings();
        }
        
        /// <summary>
        ///     Use StopSound() to stop any sound playing on a specific channel.
        /// </summary>
        /// <param name="channel">The channel ID to stop a sound on.</param>
        public void StopSound(int channel = 0)
        {
            soundChip.StopSound(channel);
        }
        
        /// <summary>
        ///     This helper method allows you to automatically load a set of loops as a complete
        ///     song and plays them back. You can also define if the tracks should loop when they
        ///     are done playing.
        /// </summary>
        /// <param name="loopIDs">
        ///     An array of loop IDs to playback as a single song.
        /// </param>
        /// <param name="loop">
        ///     A bool that determines if the song should loop back to the first ID when it is
        ///     done playing.
        /// </param>
        public void PlaySong(int[] loopIDs, bool loop = true)
        {
            musicChip.PlaySongs(loopIDs, loop);
        }

        /// <summary>
        ///     Toggles the current playback state of the sequencer. If the song
        ///     is playing it will pause, if it is paused it will play.
        /// </summary>
        public void PauseSong()
        {
            musicChip.PauseSong();
        }

        /// <summary>
        ///     Stops the sequencer.
        /// </summary>
        public void StopSong()
        {
            musicChip.StopSong();
        }

        /// <summary>
        ///     Rewinds the sequencer to the beginning of the currently loaded song. You can define
        ///     the position in the loop and the loop where playback should begin. Calling this method
        ///     without any arguments will simply rewind the song to the beginning of the first loop.
        /// </summary>
        /// <param name="position">
        ///     Position in the loop to start playing at.
        /// </param>
        /// <param name="loopID">
        ///     The loop to rewind too.
        /// </param>
        public void RewindSong(int position = 0, int loopID = 0)
        {
            //TODO need to add in better support for rewinding a song across multiple loops
            musicChip.RewindSong();
        }

        #endregion

        #region Sprite

        /// <summary>
        ///     Returns the size of the sprite as a Vector where X and Y represent the width and height.
        /// </summary>
        /// <param name="width">
        ///     Optional argument to change the width of the sprite. Currently not enabled.
        /// </param>
        /// <param name="height">
        ///     Optional argument to change the height of the sprite. Currently not enabled.
        /// </param>
        /// <returns>
        ///     Returns a vector where the X and Y for the sprite's width and height.
        /// </returns>
        public Vector SpriteSize(int? width = 8, int? height = 8)
        {
            var size = new Vector(spriteChip.width, spriteChip.height);

            // TODO you can't resize sprites at runtime

            return size;
        }

        /// <summary>
        ///     This allows you to return the pixel data of a sprite or overwrite it with new data. Sprite
        ///     pixel data is an array of color reference ids. When calling the method with only an id
        ///     argument, you will get the sprite's pixel data. If you supply data, it will overwrite the
        ///     sprite. It is important to make sure that any new pixel data should be the same length of
        ///     the existing sprite's pixel data. This can be calculated by multiplying the sprite's width
        ///     and height. You can add the transparent area to a sprite's data by using -1.
        /// </summary>
        /// <param name="id">
        ///     The sprite to access.
        /// </param>
        /// <param name="data">
        ///     Optional data to write over the sprite's current pixel data.
        /// </param>
        /// <returns>
        ///     Returns an array of int data which points to color ids.
        /// </returns>
        public int[] Sprite(int id, int[] data = null)
        {
            if (data != null)
            {
                spriteChip.UpdateSpriteAt(id, data);
                tilemapChip.InvalidateTileID(id);

                return data;
            }

            spriteChip.ReadSpriteAt(id, tmpSpriteData);

            return tmpSpriteData;
        }
        
        /// <summary>
        ///     This allows you to get the pixel data of multiple sprites. This is a read only method but
        ///     can be used to copy a collection of sprites into memory and draw them to the display in a
        ///     single pass.
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        public int[] Sprites(int[] ids, int width)
        {
            var spriteSize = SpriteSize();
            var realWidth = width * spriteSize.x;
            var realHeight = ((int)Math.Ceiling((float)ids.Length/width)) * spriteSize.y;

            var textureData = new TextureData(realWidth, realHeight);

            var pixelData = new int[realWidth * realHeight];
            
            textureData.CopyPixels(ref pixelData);
            
            return pixelData;
            
        }
        
        /// <summary>
        ///     Returns the total number of sprites in the system. You can pass in an optional argument to
        ///     get a total number of sprites the Sprite Chip can store by passing in false for ignoreEmpty.
        ///     By default, only sprites with pixel data will be included in the total return.
        /// </summary>
        /// <param name="ignoreEmpty">
        ///     This is an optional value that defaults to true. When set to true, the SpriteChip returns
        ///     the total number of sprites that are not empty (where all the pixel data is set to -1).
        ///     Set this value to false if you want to get all of the available color slots in the ColorChip
        ///     regardless if they are empty or not.
        /// </param>
        /// <returns>
        ///     This method returns the total number of sprites in the color chip based on the ignoreEmpty
        ///     argument's value.
        /// </returns>
        public int TotalSprites(bool ignoreEmpty = true)
        {
            return ignoreEmpty ? spriteChip.totalSprites : spriteChip.spritesInRam;
        }

        /// <summary>
        ///     This method returns the maximum number of sprites the Display Chip can render in a single frame. Use this 
        ///     to better understand the limitations of the hardware your game is running on. This is a read only property
        ///     at runtime.
        /// </summary>
        /// <param name="total"></param>
        /// <returns>Returns an int representing the total number of sprites on the screen at once.</returns>
        public int MaxSpriteCount(int? total = null)
        {
            if (total.HasValue)
            {
                spriteChip.maxSpriteCount = total.Value;
            }
            
            return spriteChip.maxSpriteCount;
        }

        #endregion

        #region Tilemap

        /// <summary>
        ///     This allows you to quickly access just the flag value of a tile. This is useful when trying
        ///     to the caluclate collision on the tilemap. By default, you can call this method and return
        ///     the flag value. If you supply a new value, it will be overridden on the tile. Changing a
        ///     tile's flag value does not force the tile to be redrawn to the tilemap cache.
        /// </summary>
        /// <param name="column">
        ///     The X position of the tile in the tilemap. The 0 position is on the far left of the tilemap.
        /// </param>
        /// <param name="row">
        ///     The Y position of the tile in the tilemap. The 0 position is on the top of the tilemap.
        /// </param>
        /// <param name="value">
        ///     The new value for the flag. Setting the flag to -1 means no collision.
        /// </param>
        /// <returns></returns>
        public int Flag(int column, int row, int? value = null)
        {
            if (value.HasValue)
                tilemapChip.UpdateFlagAt(column, row, value.Value);

            return tilemapChip.ReadFlagAt(column, row);
        }

        /// <summary>
        ///     This allows you to get the current sprite id, color offset and flag values associated with
        ///     a given tile. You can optionally supply your own if you want to change the tile's values.
        ///     Changing a tile's sprite id or color offset will for the tilemap to redraw it to the cache
        ///     on the next frame. If you are drawing raw pixel data into the tilemap cache in the same
        ///     position, it will be overwritten with the new tile's pixel data.
        /// </summary>
        /// <param name="column">
        ///     The X position of the tile in the tilemap. The 0 position is on the far left of the tilemap.
        /// </param>
        /// <param name="row">
        ///     The Y position of the tile in the tilemap. The 0 position is on the top of the tilemap.
        /// </param>
        /// <param name="spriteID">
        ///     The sprite id to use for the tile.
        /// </param>
        /// <param name="colorOffset">
        ///     Shift the color IDs by this value.
        /// </param>
        /// <param name="flag">
        ///     An int value between -1 and 16 used for collision detection.
        /// </param>
        /// <returns>
        ///     Returns a dictionary containing the spriteID, colorOffset, and flag for an individual tile.
        /// </returns>
        //TODO this should return a custom class not a Dictionary
        public TileData Tile(int column, int row, int? spriteID = null, int? colorOffset = null, int? flag = null)
        {
            if (spriteID.HasValue)
                tilemapChip.UpdateSpriteAt(column, row, spriteID.Value);

            if (colorOffset.HasValue)
                tilemapChip.UpdateTileColorAt(column, row, colorOffset.Value);

            if (flag.HasValue)
                tilemapChip.UpdateFlagAt(column, row, flag.Value);
            
//            
//            
//            tmpTileData["spriteID"] = tilemapChip.ReadSpriteAt(column, row);
//            tmpTileData["colorOffset"] = tilemapChip.ReadTileColorAt(column, row);
//            tmpTileData["flag"] = tilemapChip.ReadFlagAt(column, row);

            return new TileData(tilemapChip.ReadSpriteAt(column, row), tilemapChip.ReadTileColorAt(column, row), tilemapChip.ReadFlagAt(column, row));
        }

        /// <summary>
        ///     This forces the map to redraw its cached pixel data. Use this to clear any pixel data added
        ///     after the map created the pixel data cache.
        /// </summary>
        public void RebuildTilemap()
        {

            cachedTileMap.Clear();
            
            tilemapChip.InvalidateAll();
        }

        /// <summary>
        ///     This will return a vector representing the size of the tilemap in columns (x) and rows (y).
        ///     To find the size in pixels, you will need to multiply the returned vectors x and y values by
        ///     the sprite size's x and y. This method also allows you to resize the tilemap by passing in an
        ///     optional new width and height. Resizing the tile map is destructive, so any changes will
        ///     automatically clear the tilemap's sprite ids, color offsets, and flag values.
        /// </summary>
        /// <param name="width">
        ///     An optional parameter for the width in tiles of the map.
        /// </param>
        /// <param name="height">
        ///     An option parameter for the height in tiles of the map.
        /// </param>
        /// <returns>
        ///     Returns a vector of the tile maps size in tiles where x and y are the columns and rows of the tilemap.
        /// </returns>
        public Vector TilemapSize(int? width = null, int? height = null)
        {
            var size = new Vector(tilemapChip.columns, tilemapChip.rows);

            var resize = false;

            if (width.HasValue)
            {
                size.x = width.Value;
                resize = true;
            }

            if (height.HasValue)
            {
                size.y = height.Value;
                resize = true;
            }

            if (resize)
                tilemapChip.Resize(size.x, size.y);

            return size;
        }

        /// <summary>
        ///     A helper method which allows you to update several tiles at once. Simply define the start column
        ///     and row position, the width of the area to update in tiles and supply a new int array of sprite
        ///     IDs. You can also modify the color offset and flag value of the tiles via the optional parameters.
        ///     This helper method uses calls the Tile() method to update each tile, so any changes to a tile
        ///     will be automatically redrawn to the tilemap's cache.
        /// </summary>
        /// <param name="column">
        ///     Start column of the first tile to update. The 0 column is on the far left of the tilemap.
        /// </param>
        /// <param name="row">
        ///     Start row of the first tile to update. The 0 row is on the top of the tilemap.
        /// </param>
        /// <param name="columns">
        ///     The width of the area in tiles to update.
        /// </param>
        /// <param name="ids">
        ///     An array of sprite IDs to use for each tile being updated.
        /// </param>
        /// <param name="colorOffset">
        ///     An optional color offset int value to be applied to each updated tile.
        /// </param>
        /// <param name="flag">
        ///     An optional flag int value to be applied to each updated tile.
        /// </param>
        public void UpdateTiles(int column, int row, int columns, int[] ids, int? colorOffset = null, int? flag = null)
        {
            var total = ids.Length;

            int id, newX, newY;

            //TODO need to get offset and flags working

            for (var i = 0; i < total; i++)
            {
                id = ids[i];
    
                newX = MathUtil.FloorToInt(i % columns) + column;
                newY = MathUtil.FloorToInt(i / (float) columns) + row;

                Tile(newX, newY, id, colorOffset, flag);
            }
        }
        
        #endregion


        #region Tilemap Cache
        
        protected int[] tmpPixelData = new int[8 * 8];
        
//        public int realWidth
//        {
//            get { return spriteChip.width * tilemapChip.columns; }
//        }
//
//        public int realHeight
//        {
//            get { return spriteChip.height * tilemapChip.rows; }
//        }
        
        
        
        /// <summary>
        ///     This method converts the tile map into pixel data that can be
        ///     rendered by the engine. It's an expensive operation and should only
        ///     be called when the game or level is loading up. This data can be
        ///     passed into the ScreenBufferChip to allow cached rendering of the
        ///     tile map as well as scrolling of the tile map if it is larger then
        ///     the screen's resolution.
        /// </summary>
        /// <param name="textureData">
        ///     A reference to a <see cref="TextureData" /> class to populate with
        ///     tile map pixel data.
        /// </param>
        /// <param name="clearColor">
        ///     The transparent color to use when a tile is set to -1. The default
        ///     value is -1 for transparent.
        /// </param>
        /// <ignore/>
        protected void RebuildCache(Canvas targetTextureData)
        {
            if (tilemapChip.invalid != true)
                return;

            var realWidth = spriteChip.width * tilemapChip.columns;
            var realHeight = spriteChip.height * tilemapChip.rows;
            
            if (realWidth != cachedTileMap.width || realHeight != cachedTileMap.height)
            {
                cachedTileMap.Resize(realWidth, realHeight);
            }

            var tileSize = SpriteSize();
            
            // Get a local reference to the layers we need
            var tmpSpriteIDs = tilemapChip.layers[(int) TilemapChip.Layer.Sprites];
            var tmpPaletteIDs = tilemapChip.layers[(int) TilemapChip.Layer.Colors];
            var invalideLayer = tilemapChip.layers[(int) TilemapChip.Layer.Invalid];

            // Create tmp variables for loop
            int x, y, spriteID;

            // Get a local reference to the total number of tiles
            var totalTiles = tilemapChip.total;

            var totalTilesUpdated = 0;

            // Loop through all of the tiles in the tilemap
            for (var i = 0; i < totalTiles; i++)
                if (invalideLayer[i] != 0)
                {
                    // Get the sprite id
                    spriteID = tmpSpriteIDs[i];

                    // Calculate the new position of the tile;
                    x = i % tilemapChip.columns * tileSize.x;
                    y = i / tilemapChip.columns * tileSize.y;

                    spriteChip.ReadSpriteAt(spriteID, tmpPixelData);

                    // Draw the pixel data into the cachedTilemap
                    targetTextureData.MergePixels(x, y, tileSize.x, tileSize.y, tmpPixelData, tmpPaletteIDs[i]);

                    totalTilesUpdated++;
                    
                }

            // Reset the invalidation state
            tilemapChip.ResetValidation();
        }

        public void GetCachedPixels(int x, int y, int blockWidth, int blockHeight, ref int[] pixelData)
        {
            if (tilemapChip.invalid)
            {
                RebuildCache(cachedTileMap);
            }
            
            cachedTileMap.CopyPixels(ref pixelData, x, y, blockWidth, blockHeight);
        }
        
        protected void UpdateCachedTilemap(int[] pixels, int x, int y, int blockWidth, int blockHeight,
            int colorOffset = 0)
        {
            
            // Check to see if the tilemap cache is invalide before drawing to it
            if (tilemapChip.invalid)
            {
                
                // Rebuild the tilemap cache first
                RebuildCache(cachedTileMap);
                
            }
                
            // Flip the y axis 
//            y = cachedTileMap.height - y - blockHeight;
            
            
            // Todo need to go through and draw to the tilemap cache but ignore transparent pixels
            
            
            // Set pixels on the tilemap cache
            cachedTileMap.MergePixels(x, y, blockWidth, blockHeight, pixels, colorOffset, true);
            
//            Invalidate();
        }

        public void ReadPixelData(int width, int height, ref int[] pixelData, int offsetX = 0, int offsetY = 0)
        {
            // Test if we need to rebuild the cached tilemap
            if (tilemapChip.invalid)
                RebuildCache(cachedTileMap);

            // Return the requested pixel data
            cachedTileMap.CopyPixels(ref pixelData, offsetX, offsetY, width, height);
        }

        #endregion
        
        #region Geometry
        
        /// <summary>
        ///     A Rect is a Pixel Vision 8 primitive used for defining the bounds of an object on the display. It
        ///     contains an x, y, width and height property. The Rect class also has some additional methods to aid with
        ///     collision detection such as Intersect(rect, rect), IntersectsWidth(rect) and Contains(x,y).
        /// </summary>
        /// <param name="x">The x position of the rect as an int.</param>
        /// <param name="y">The y position of the rect as an int.</param>
        /// <param name="w">The width value of the rect as an int.</param>
        /// <param name="h">The height value of the rect as an int.</param>
        /// <returns>Returns a new instance of a Rect to be used as a Lua object.</returns>
        public Rect NewRect(int x = 0, int y = 0, int w = 0, int h = 0)
        {
            return new Rect(x, y, w, h);
        }
        
        /// <summary>
        ///     A Vector is a Pixel Vision 8 primitive used for defining a position on the display as an x,y value.
        /// </summary>
        /// <param name="x">The x position of the Vector as an int.</param>
        /// <param name="y">The y position of the Vector as an int.</param>
        /// <returns>Returns a new instance of a Vector to be used as a Lua object.</returns>
        public Vector NewVector(int x = 0, int y = 0)
        {
            return new Vector(x, y);
        }

//        public TextureData NewTextureData(int width, int height, bool wrapMode = true)
//        {
//            return new TextureData(width, height, wrapMode);
//        }
        
        #endregion

        #region Graphics

        /// <summary>
        ///     Creates a new canvas instance.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public Canvas NewCanvas(int width, int height)
        {
            return new Canvas(width, height, this);
        }

        #endregion
        
        #region Math

        /// <summary>
        ///     Limits a value between a minimum and maximum.
        /// </summary>
        /// <param name="val">
        ///     The value to clamp.
        /// </param>
        /// <param name="min">
        ///     The minimum the value can be.
        /// </param>
        /// <param name="max">
        ///     The maximum the value can be.
        /// </param>
        /// <returns>
        ///     Returns an int within the min and max range.
        /// </returns>
        public int Clamp(int val, int min, int max)
        {
            return val.Clamp(min, max);
        }

        /// <summary>
        ///     Repeats a value based on the max. When the value is greater than the max, it starts
        ///     over at 0 plus the remaining value.
        /// </summary>
        /// <param name="val">
        ///     The value to repeat.
        /// </param>
        /// <param name="max">
        ///     The maximum the value can be.
        /// </param>
        /// <returns>
        ///     Returns an int that is never less than 0 or greater than the max.
        /// </returns>
        public int Repeat(int val, int max)
        {
            return (int) (val - Math.Floor(val / (float) max) * max);
        }

        /// <summary>
        ///     Converts an X and Y position into an index. This is useful for finding positions in 1D
        ///     arrays that represent 2D data.
        /// </summary>
        /// <param name="x">
        ///     The x position.
        /// </param>
        /// <param name="y">
        ///     The y position.
        /// </param>
        /// <param name="width">
        ///     The width of the data if it was represented as a 2D array.
        /// </param>
        /// <returns>
        ///     Returns an int value representing the X and Y position in a 1D array.
        /// </returns>
        public int CalculateIndex(int x, int y, int width)
        {
            int index;
            index = x + y * width;
            return index;
        }

        /// <summary>
        ///     Converts an index into an X and Y position to help when working with 1D arrays that
        ///     represent 2D data.
        /// </summary>
        /// <param name="index">
        ///     The position of the 1D array.
        /// </param>
        /// <param name="width">
        ///     The width of the data if it was a 2D array.
        /// </param>
        /// <returns>
        ///     Returns a vector representing the X and Y position of an index in a 1D array.
        /// </returns>
        public Vector CalculatePosition(int index, int width)
        {
            int x, y;

            x = index % width;
            y = index / width;

            return new Vector(x, y);
        }

        #endregion
        
        #region Utils

        private string newline = "\n";

        /// <summary>
        ///     This allows you to call the TextUtil's WordWrap helper to wrap a string of text to a specified character
        ///     width. Since the FontChip only knows how to render characters as sprites, this can be used to calculate
        ///     blocks of text then each line can be rendered with a DrawText() call.
        /// </summary>
        /// <param name="text">The string of text to wrap.</param>
        /// <param name="width">The width of characters to wrap each line of text.</param>
        /// <returns></returns>
        public string WordWrap(string text, int width)
        {
            int pos, next;
            StringBuilder sb = new StringBuilder();
        
            // Lucidity check
            if (width < 1)
                return text;
        
            // Parse each line of text
            for (pos = 0; pos < text.Length; pos = next)
            {
                // Find end of line
                int eol = text.IndexOf(newline, pos);
                if (eol == -1)
                    next = eol = text.Length;
                else
                    next = eol + newline.Length;
        
                // Copy this line of text, breaking into smaller lines as needed
                if (eol > pos)
                {
                    do
                    {
                        int len = eol - pos;
                        if (len > width)
                            len = BreakLine(text, pos, width);
                        sb.Append(text, pos, len);
                        sb.Append(newline);
        
                        // Trim whitespace following break
                        pos += len;
                        while (pos < eol && Char.IsWhiteSpace(text[pos]))
                            pos++;
                    } while (eol > pos);
                }
                else sb.Append(newline); // Empty line
            }
            return sb.ToString();
        }
        
        /// <summary>
        /// Locates position to break the given line so as to avoid
        /// breaking words.
        /// </summary>
        /// <param name="text">String that contains line of text</param>
        /// <param name="pos">Index where line of text starts</param>
        /// <param name="max">Maximum line length</param>
        /// <returns>The modified line length</returns>
        private int BreakLine(string text, int pos, int max)
        {
            // Find last whitespace in line
            var i = max;
            while (i >= 0 && !Char.IsWhiteSpace(text[pos + i]))
                i--;
        
            // If no whitespace found, break at maximum length
            if (i < 0)
                return max;
        
            // Find start of whitespace
            while (i >= 0 && Char.IsWhiteSpace(text[pos + i]))
                i--;
        
            // Return length of text before whitespace
            return i + 1;
        }
        
        /// <summary>
        ///     This calls the TextUtil's SplitLines() helper to convert text with line breaks (\n) into a collection of
        ///     lines. This can be used in conjunction with the WordWrap() helper to render large blocks of text line by
        ///     line with the DrawText() API.
        /// </summary>
        /// <param name="str">The string of text to split.</param>
        /// <returns>Returns an array of strings representing each line of text.</returns>
        public string[] SplitLines(string str)
        {
            string[] lines = str.Split(
                new[] { newline },
                StringSplitOptions.None
            );

            return lines;
        }
        
        //TODO need to write a commment for this
        public int CalcualteDistance(int x0, int y0, int x1, int y1)
        {
            var dx = x1 - x0; 
            var dy = y1 - y0;
            var distance = Math.Sqrt((dx * dx) + (dy * dy));
            return (int)distance;
        }
        
        public int[] BitArray(int value)
        {
            
            BitArray bits = new BitArray(BitConverter.GetBytes(value));

            var intArray = new int[bits.Length];
            
            bits.CopyTo(intArray, 0);

            return intArray;
        }
        
        #endregion
    }

}