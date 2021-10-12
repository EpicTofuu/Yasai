using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using Yasai.Debug;
using Yasai.Extensions;
using Yasai.Graphics.Groups;
using Yasai.Graphics.YasaiSDL;
using Yasai.Input.Keyboard;
using Yasai.Input.Mouse;
using Yasai.Resources;
using Yasai.Structures;
using static SDL2.SDL;
using static SDL2.SDL_ttf;

namespace Yasai
{
    /// <summary>
    /// The bare essentials of a Yasai application
    /// </summary>
    public class GameBase : IGroup, IDisposable
    {
        public Window Window { get; private set; }
        public Renderer Renderer { get; private set; }

        private bool _quit;
        
        public bool Loaded => Window != null && Renderer != null && Content != null;
        
        public bool Visible { get; set; } = true;
        public bool Enabled
        {
            get => throw new Exception("use Game.Quit() to quit the game");
            set => throw new Exception("use Game.Quit() to quit the game");
        }
        
        protected Group Children;
        
        protected ContentCache Content;

        protected FrameRateCounter FrameRateCounter;
        
        #region constructors
        public GameBase(string title, int w, int h, int refreshRate, string[] args = null)
        { 
            // SDL initialisation
            if (SDL_Init(SDL_INIT_EVERYTHING) != 0) 
                Console.WriteLine($"error on startup: {SDL_GetError()}");
            TTF_Init();
            
            
            // everything else
            Window = new Window(title, w, h, refreshRate);
            Renderer = new Renderer(Window);           
             
            Children = new Group();
            FrameRateCounter = new FrameRateCounter();

            //DependencyHandler.Store(ref Window);
            //DependencyHandler.Store(ref Renderer);
        }
        #endregion
        
        public void Run()
        {
            Load(Content);
            Children.Load(Content);
            
            LoadComplete();
            Children.LoadComplete(); 
            
            while (!_quit)
            {
                while (SDL_PollEvent(out var e) != 0)
                    OnEvent(e);
        
                Update();
        
                FrameRateCounter.StartCount = SDL_GetPerformanceCounter();
                Renderer.Clear();
                Draw(Renderer.GetPtr());
                Renderer.Present();
                Renderer.SetDrawColor(0,0,0,255);
                FrameRateCounter.EndCount = SDL_GetPerformanceCounter();
            }
        }
        
        private HashSet<KeyCode> pressed = new();
        
        protected virtual void OnEvent(SDL_Event ev)
        {
            switch (ev.type) 
            {
                // program exit
                case (SDL_EventType.SDL_QUIT):
                    _quit = true;
                    break;
                
                #region input systems
                case (SDL_EventType.SDL_KEYUP):
                    pressed.Remove(ev.key.keysym.sym.ToYasaiKeyCode());
                    KeyUp(new KeyArgs(pressed));
                    break;
                
                case (SDL_EventType.SDL_KEYDOWN):
                    pressed.Add(ev.key.keysym.sym.ToYasaiKeyCode());
                    KeyDown(new KeyArgs(pressed));
                    break;
                
                case (SDL_EventType.SDL_MOUSEBUTTONUP):
                    MouseUp(new MouseArgs((MouseButton) ev.button.button, new Vector2(ev.button.x, ev.button.y)));
                    break;
                
                case (SDL_EventType.SDL_MOUSEBUTTONDOWN):
                    MouseDown(new MouseArgs((MouseButton) ev.button.button, new Vector2(ev.button.x, ev.button.y)));
                    break;
                
                case (SDL_EventType.SDL_MOUSEWHEEL):
                    // TODO: mousewheel
                    break;
                
                case (SDL_EventType.SDL_MOUSEMOTION):
                    MouseMotion(new MouseArgs(new Vector2(ev.button.x, ev.button.y)));
                    break;
                
                #endregion
            }
        }
        
        public virtual void LoadComplete ()
        { }
        
        public virtual void Load(ContentCache cache)
        { }
        
        public virtual void Update() => Children.Update();
        
        // it's probably more efficient to pass around pointers than actual objects
        // can change this later though if need be
        public virtual void Draw(IntPtr ren)
        {
            if (Visible)
            {
                Children.Draw(ren);
            }
            else
            {
                #if DEBUG
                Console.WriteLine("Game is invisible. The use of Game.Visible is highly unadvised in the first place!");
                #endif 
            }
        }
        
        /// <summary>
        /// Quit the current game
        /// </summary>
        public void Quit()
        {
            _quit = false;
            Dispose();
        }
        
        public void Dispose()
        {
            Content.Dispose();
            SDL_Quit();
        }
        
        #region input
        public bool IgnoreHierarchy => true;
        public virtual void MouseDown(MouseArgs args) => Children.MouseDown(args);
        public virtual void MouseUp(MouseArgs args) => Children.MouseUp(args);
        public virtual void MouseMotion(MouseArgs args) => Children.MouseMotion(args);
        public virtual void KeyUp(KeyArgs key) => Children.KeyUp(key);
        public virtual void KeyDown(KeyArgs key) => Children.KeyDown(key);
        #endregion

        public DependencyCache DependencyCache { get; }
        public void LinkDependencies(Linkable<DependencyCache> parent)
        { }

        /*
        private static void SdlDllWorkaround()
        {
            if (Environment.OSVersion.Platform != PlatformID.Unix) return;
            
            var dllNames = new Dictionary<string, string>()
            {
                {"libSDL2.so",       "libSDL2-2.0.so.0"      },
                {"libSDL2_ttf.so",   "libSDL2_ttf-2.0.so.0"  },
                {"libSDL2_image.so", "libSDL2_image-2.0.so.0"}
            };
            
            string envTriplet = Environment.Is64BitProcess
                ? "x86_64-linux-gnu"
                : "i386";
            
            foreach(var checkFile in dllNames)
            {
                string checkLink = checkFile.Key;
                string checkPath1 = Path.Combine($"/usr/lib/{envTriplet}/", checkLink);
                string checkPath2 = Path.Combine("/usr/lib/", checkLink);
                if (File.Exists(checkLink) || File.Exists(checkPath1) ) continue;
                
                string targetLink = checkFile.Value;
                string targetPath = Path.Combine(checkPath1, targetLink);
                Console.WriteLine($"{checkLink} not found, creating symlink targetting {targetPath}");
                
                var symlinkInf = new ProcessStartInfo("ln", $"-s {targetPath} {checkLink}");
                symlinkInf.RedirectStandardOutput = true;
                symlinkInf.UseShellExecute = false;
                symlinkInf.CreateNoWindow = true;
                
                Process.Start(symlinkInf);
            }
        }
        */
    }
}