﻿#region --- License ---
/* Copyright (c) 2006, 2007 Stefanos Apostolopoulos
 * See license.txt for license info
 */
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using OpenTK.Platform;
using OpenTK.Input;
using System.Threading;
using OpenTK.OpenGL;

namespace OpenTK
{
    /// <summary>
    /// The GameWindow class contains cross-platform methods to create and render on an OpenGL window, handle input and load resources.
    /// </summary>
    /// <remarks>
    /// GameWindow contains several events you can hook or override to add your custom logic:
    /// <list>
    /// <item>OnLoad: Occurs after creating the OpenGL context, but before entering the main loop. Override to load resources.</item>
    /// <item>OnUnload: Occurs after exiting the main loop, but before deleting the OpenGL context. Override to unload resources.</item>
    /// <item>OnResize: Occurs whenever GameWindow is resized. You should update the OpenGL Viewport and Projection Matrix here.</item>
    /// <item>OnUpdateFrame: Occurs at the specified logic update rate. Override to add your game logic.</item>
    /// <item>OnRenderFrame: Occurs at the specified frame render rate. Override to add your rendering code.</item>
    /// </list>
    /// Call the Run() method to start the application's main loop. Run(double, double) takes two parameters that
    /// specify the logic update rate, and the render update rate.
    /// </remarks>
    public class GameWindow : INativeGLWindow
    {
        #region --- Fields ---

        INativeGLWindow glWindow;
        DisplayMode mode;

        ResizeEventArgs resizeEventArgs = new ResizeEventArgs();

        bool isExiting;
        bool disposed;

        double update_period, render_period;
        double target_update_period, target_render_period, target_render_period_doubled;
        // TODO: Implement these:
        double update_time, render_time, event_time;

        int width, height;

        VSyncMode vsync;

        #endregion

        #region --- Internal Properties ---

        bool MustResize
        {
            get { return glWindow.Width != this.Width || glWindow.Height != this.Height; }
        }

        #endregion

        #region --- Contructors ---

        /// <summary>
        /// Constructs a new GameWindow. Call CreateWindow() to open a render window.
        /// </summary>
        public GameWindow()
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT:
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                case PlatformID.WinCE:
                    glWindow = new OpenTK.Platform.Windows.WinGLNative();
                    break;
            
                case PlatformID.Unix:
                case (PlatformID)128:
                    glWindow = new OpenTK.Platform.X11.X11GLNative();
                    break;
                
                default:
                    throw new PlatformNotSupportedException("Your platform is not supported currently. Please, refer to http://opentk.sourceforge.net for more information.");
            }

            //glWindow.Resize += new ResizeEvent(glWindow_Resize);
            glWindow.Destroy += new DestroyEvent(glWindow_Destroy);
        }

        /// <summary>
        /// Constructs a new GameWindow, and opens a render window with the specified DisplayMode.
        /// </summary>
        /// <param name="mode">The DisplayMode of the GameWindow.</param>
        public GameWindow(DisplayMode mode)
            : this()
        {
            CreateWindow(mode);
        }

        /// <summary>
        /// Constructs a new GameWindow with the specified title, and opens a render window with the specified DisplayMode.
        /// </summary>
        /// <param name="mode">The DisplayMode of the GameWindow.</param>
        /// <param name="title">The Title of the GameWindow.</param>
        public GameWindow(DisplayMode mode, string title)
            : this()
        {
            CreateWindow(mode, title);
        }

        void glWindow_Destroy(object sender, EventArgs e)
        {
            Debug.Print("GameWindow destruction imminent.");
            this.isExiting = true;
            this.OnDestroy(EventArgs.Empty);
            glWindow.Destroy -= glWindow_Destroy;
            //this.Dispose();
        }

        void glWindow_Resize(object sender, ResizeEventArgs e)
        {
            this.OnResizeInternal(e);
        }

        #endregion

        #region --- Functions ---

        #region public void CreateWindow(DisplayMode mode, string title)

        /// <summary>
        /// Creates a render window for the calling GameWindow, with the specified DisplayMode and Title.
        /// </summary>
        /// <param name="mode">The DisplayMode of the render window.</param>
        /// <param name="title">The Title of the render window.</param>
        /// <remarks>
        /// It is an error to call this function when a render window already exists.
        /// <para>Call DestroyWindow to close the render window.</para>
        /// </remarks>
        /// <exception cref="ApplicationException">Occurs when a render window already exists.</exception>
        public void CreateWindow(DisplayMode mode, string title)
        {
            if (!Exists)
            {
                try
                {
                    glWindow.CreateWindow(mode);
                    this.Title = title;
                }
                catch (ApplicationException expt)
                {
                    Debug.Print(expt.ToString());
                    throw;
                }
            }
            else
            {
                throw new ApplicationException("A render window already exists for this GameWindow.");
            }
        }

        #endregion

        #endregion

        #region --- INativeGLWindow Members ---

        #region public void Exit()

        /// <summary>
        /// Gracefully exits the current GameWindow.
        /// Override if you want to provide yor own exit sequence.
        /// If you override this method, place a call to base.Exit(), to ensure
        /// proper OpenTK shutdown.
        /// </summary>
        public virtual void Exit()
        {
            isExiting = true;
            //glWindow.Exit();
            //this.Dispose();
        }

        #endregion

        #region public bool IsIdle

        /// <summary>
        /// Gets a value indicating whether the current GameWindow is idle.
        /// If true, the OnUpdateFrame and OnRenderFrame functions should be called.
        /// </summary>
        public bool IsIdle
        {
            get { return glWindow.IsIdle; }
        }

        #endregion

        #region public bool Fullscreen

        /// <summary>
        /// TODO: This property is not implemented.
        /// Gets or sets a value indicating whether the GameWindow is in fullscrren mode.
        /// </summary>
        public bool Fullscreen
        {
            get { return glWindow.Fullscreen; }
            set { glWindow.Fullscreen = value; }
        }

        #endregion

        #region public IGLContext Context

        /// <summary>
        /// Returns the opengl IGLontext associated with the current GameWindow.
        /// Forces window creation.
        /// </summary>
        public IGLContext Context
        {
            get
            {
				if (!this.Exists && !this.IsExiting)
				{
				    Debug.WriteLine("WARNING: OpenGL Context accessed before creating a render window. This may indicate a programming error. Force-creating a render window.");
				    mode = new DisplayMode(640, 480);
				    this.CreateWindow(mode);
				}
				return glWindow.Context;
			}
        }

        #endregion

        #region public bool Exists

        /// <summary>
        /// Gets a value indicating whether a render window exists.
        /// </summary>
        public bool Exists
        {
            get { return glWindow == null ? false : glWindow.Exists; }
        }

        #endregion

        #region public string Text

        /// <summary>
        /// Gets or sets the GameWindow title.
        /// </summary>
        public string Title
        {
            get
            {
                return glWindow.Title;
            }
            set
            {
                glWindow.Title = value;
            }
        }

        #endregion

        #region public bool Visible

        /// <summary>
        /// TODO: This property is not implemented
        /// Gets or sets a value indicating whether the GameWindow is visible.
        /// </summary>
        public bool Visible
        {
            get
            {
                throw new NotImplementedException();
                return glWindow.Visible;
            }
            set
            {
                throw new NotImplementedException();
                glWindow.Visible = value;
            }
        }

        #endregion

        #region public IWindowInfo WindowInfo

        public IWindowInfo WindowInfo
        {
            get { return glWindow.WindowInfo; }
        }

        #endregion

        #region public IInputDriver InputDriver

        /// <summary>
        /// Gets an interface to the InputDriver used to obtain Keyboard, Mouse and Joystick input.
        /// </summary>
        public IInputDriver InputDriver
        {
            get
            {
                return glWindow.InputDriver;
            }
        }

        #endregion

        #region public void CreateWindow(DisplayMode mode)

        /// <summary>
        /// Creates a render window for the calling GameWindow.
        /// </summary>
        /// <param name="mode">The DisplayMode of the render window.</param>
        /// <remarks>
        /// It is an error to call this function when a render window already exists.
        /// <para>Call DestroyWindow to close the render window.</para>
        /// </remarks>
        /// <exception cref="ApplicationException">Occurs when a render window already exists.</exception>
        public void CreateWindow(DisplayMode mode)
        {
            if (!Exists)
            {
                try
                {
                    glWindow.CreateWindow(mode);
                }
                catch (ApplicationException expt)
                {
                    Debug.Print(expt.ToString());
                    throw;
                }
            }
            else
            {
                throw new ApplicationException("A render window already exists for this GameWindow.");
            }
        }

        #endregion

        #region OnCreate

        [Obsolete("The Create event is obsolete and will be removed on later versions. Use the Load event instead.")]
        public event CreateEvent Create;

        /// <summary>
        /// Raises the Create event. Override in derived classes to initialize resources.
        /// </summary>
        /// <param name="e"></param>
        [Obsolete("The OnCreate method is obsolete and will be removed on later versions. Use the OnLoad method instead.")]
        public virtual void OnCreate(EventArgs e)
        {
            Debug.WriteLine("Firing GameWindow.Create event");
            if (this.Create != null)
            {
                this.Create(this, e);
            }
        }

        #endregion

        #region public void DestroyWindow()

        /// <summary>
        /// Destroys the GameWindow. The Destroy event is raised before destruction commences
        /// (while the opengl context still exists), to allow resource cleanup.
        /// </summary>
        public void DestroyWindow()
        {
            if (Exists)
            {
                glWindow.DestroyWindow();
            }
            else
            {
                throw new ApplicationException("Tried to destroy inexistent window.");
            }
        }

        #endregion

        #region OnDestroy

        /// <summary>
        /// Raises the Destroy event. Override in derived classes, to modify the shutdown
        /// sequence (e.g. to release resources before shutdown).
        /// </summary>
        /// <param name="e"></param>
        public virtual void OnDestroy(EventArgs e)
        {
            Debug.WriteLine("Firing GameWindow.Destroy event");
            if (this.Destroy != null)
            {
                this.Destroy(this, e);
            }
        }

        public event DestroyEvent Destroy;

        #endregion

        #endregion

        #region --- GameWindow Methods ---

        #region void Run()

        /// <summary>
        /// Enters the game loop of GameWindow, updating and rendering at the maximum possible frequency.
        /// </summary>
        /// <see cref="public virtual void Run(float update_frequency, float render_frequency)"/>
        public void Run()
        {
            Run(0.0, 0.0);
        }

        /// <summary>
        /// Runs the default game loop on GameWindow at the specified update frequency, maintaining the
        /// maximum possible render frequency.
        /// </summary>
        /// <see cref="public virtual void Run(double updateFrequency, double renderFrequency)"/>
        public void Run(double updateFrequency)
        {
            Run(updateFrequency, 0.0);
        }

#if false
        /// <summary>
        /// Runs the default game loop on GameWindow at the specified update and render frequency.
        /// </summary>
        /// <param name="updateFrequency">If greater than zero, indicates how many times UpdateFrame will be called per second. If less than or equal to zero, UpdateFrame is raised at maximum possible frequency.</param>
        /// <param name="renderFrequency">If greater than zero, indicates how many times RenderFrame will be called per second. If less than or equal to zero, RenderFrame is raised at maximum possible frequency.</param>
        /// <remarks>
        /// <para>
        /// A default game loop consists of three parts: Event processing, frame updating and a frame rendering.
        /// This function will try to maintain the requested updateFrequency at all costs, dropping the renderFrequency if
        /// there is not enough CPU time.
        /// </para>
        /// <para>
        /// It is recommended that you specify a target for update- and renderFrequency.
        /// Doing so, will yield unused CPU time to other processes, dropping power consumption
        /// and maximizing batter life. If either frequency is left unspecified, the GameWindow
        /// will consume all available CPU time (only useful for benchmarks and stress tests).
        /// </para>
        /// <para>
        /// Override this function if you want to change the behaviour of the
        /// default game loop. If you override this function, you must place
        /// a call to the ProcessEvents function, to ensure window will respond
        /// to Operating System events.
        /// </para>
        /// </remarks>
        public virtual void Run(double updateFrequency, double renderFrequency)
        {
            // Setup timer
            Stopwatch watch = new Stopwatch();
            UpdateFrameEventArgs updateArgs = new UpdateFrameEventArgs();
            RenderFrameEventArgs renderArgs = new RenderFrameEventArgs();

            // Setup update and render rates. If updateFrequency or renderFrequency <= 0.0, use full throttle for that frequency.
            double next_update = 0.0, next_render = 0.0;
            double start_time;

            double update_watch = 0.0, render_watch = 0.0;
            int num_updates = 1;
            double t0 = 0.0, t1 = 0.0, t2 = 0.0, t3 = 0.0;

            if (updateFrequency > 0.0)
            {
                next_update = updateTimeTarget = 1.0 / updateFrequency;
            }
            if (renderFrequency > 0.0)
            {
                next_render = renderTimeTarget = 1.0 / renderFrequency;
            }
            renderTargetDoubled = renderTimeTarget * 2.0;

            this.OnLoad(EventArgs.Empty);

            // Enter main loop:
            // (1) Update total frame time (capped at 0.1 sec)
            // (2) Process events and update event_time
            // (3) Raise UpdateFrame event(s) and update update_time.
            //     If there is enough CPU time, update and render events will be 1 on 1.
            //     If there is not enough time, render events will be dropped in order to match the requested updateFrequency.
            //     If the requested updateFrequency can't be matched, processing will slow down.
            // (4) Raise RenderFrame event and update render_time.
            // (5) If there is any CPU time left, and we are not running full-throttle, Sleep() to lower CPU usage.
            Debug.Print("Entering main loop.");
            while (this.Exists && !IsExiting)
            {
                watch.Reset();
                watch.Start();

                //frameTime = watch.Elapsed.TotalSeconds;
                /*
                // Adaptive VSync control:
                bool disable_vsync = VSync == VSyncMode.Adaptive && Context.VSync && renderTime > renderTargetDoubled;
                bool enable_vsync = VSync == VSyncMode.Adaptive && !Context.VSync && renderTime <= renderTargetDoubled;
                if (disable_vsync)
                {
                    //Debug.Print("Disabled vsync");
                    Title = "Off";
                    Context.VSync = false;
                }
                else if (enable_vsync)
                {
                    //Debug.Print("Enabled vsync");
                    Title = "On";
                    Context.VSync = true;
                }
                */
                t0 = watch.Elapsed.TotalSeconds;
                // Process events and update eventTime
                eventTime = t2 + t3 + t0;      // t2 and t3 come from the previous run through the loop.
                this.ProcessEvents();

                if (!IsExiting)
                {
                    // --- UpdateFrame ---
                    // Raise the necessary amount of UpdateFrame events to keep
                    // the UpdateFrame rate constant. If the user didn't set an
                    // UpdateFrame rate, raise only one event.

                    t1 = watch.Elapsed.TotalSeconds - t0;

                    start_time = t3 + t0 + t1;     // t3 come from the previous run through the loop.
                    update_watch += start_time;
                    if (num_updates > 0)
                    {
                        updateTime = update_watch / (double)num_updates;
                        num_updates = 0;
                        update_watch = 0.0;
                    }

                    next_update -= start_time;
                    updateArgs.Time = update_watch;
                    if (next_update <= 0.0)
                    {
                        //updateArgs.Time += watch.Elapsed.TotalSeconds;
                        double prev_update = watch.Elapsed.TotalSeconds;
                        this.OnUpdateFrameInternal(updateArgs);
                        updateArgs.Time = watch.Elapsed.TotalSeconds - prev_update;

                        ++num_updates;

                        // Schedule next update
                        //if (updateTimeTarget != 0.0)
                        {
                            next_update += updateTimeTarget;
                            next_update -= (watch.Elapsed.TotalSeconds - start_time);
                        }
                        //else
                        //    break;  // User didn't request a fixed UpdateFrame rate.
                    }
                    // --------------------
                    t2 = watch.Elapsed.TotalSeconds - t1;
                    // --- Render Frame ---
                    // Raise RenderFrame event and update render_time.

                    start_time = t0 + t1 + t2;
                    render_watch += start_time;
                    next_render -= start_time;
                    if (next_render <= 0.0)
                    {
                        // Update framerate counters
                        renderTime = renderArgs.Time = render_watch;
                        render_watch = 0.0;

                        this.OnRenderFrameInternal(renderArgs);

                        next_render += renderTimeTarget;
                        next_render -= (watch.Elapsed.TotalSeconds - start_time);
                    }

                    // --------------------

                    // If there is any CPU time left, and we are not running full-throttle, Sleep() to lower CPU usage.
                    /*
                    if (renderTime < renderTimeTarget && updateTime < updateTimeTarget)
                    {
                        int sleep_time = (int)System.Math.Truncate(1000.0 * System.Math.Min(renderTimeTarget - renderTime - eventTime,
                            updateTimeTarget - updateTime - eventTime));
                        if (sleep_time < 0)
                            sleep_time = 0;
                        Thread.Sleep(sleep_time);

                    }
                    */
                    /*
                    loop_time_clock = watch.Elapsed.TotalSeconds;
                    if (loop_time_clock > 0.05)
                        loop_time_clock = 0.05;
                    render_time_clock += loop_time_clock;
                    update_time_clock += loop_time_clock;
                    */
                    //if (loop_time_clock > 0.1)
                    //    loop_time_clock = 0.1;

                    t3 = watch.Elapsed.TotalSeconds - t2;
                }
            }

            OnUnloadInternal(EventArgs.Empty);

            if (this.Exists)
            {
                glWindow.DestroyWindow();
                while (this.Exists)
                {
                    this.ProcessEvents();
                }
            }
        }
#endif
        public void Run(double updates_per_second, double frames_per_second)
        {
            if (updates_per_second < 0.0 || updates_per_second > 200.0)
                throw new ArgumentOutOfRangeException("updates_per_second", updates_per_second, "Parameter should be inside the range [0.0, 200.0]");
            if (frames_per_second < 0.0 || frames_per_second > 200.0)
                throw new ArgumentOutOfRangeException("frames_per_second", frames_per_second, "Parameter should be inside the range [0.0, 200.0]");

            TargetUpdateFrequency = updates_per_second;
            TargetRenderFrequency = frames_per_second;

            Stopwatch update_watch = new Stopwatch(), render_watch = new Stopwatch();
            double time, next_render = 0.0, next_update = 0.0, update_time_counter = 0.0;
            int num_updates = 0;
            UpdateFrameEventArgs update_args = new UpdateFrameEventArgs();
            RenderFrameEventArgs render_args = new RenderFrameEventArgs();

            GC.Collect(2);
            GC.WaitForPendingFinalizers();
            GC.Collect(2);

            OnLoadInternal(EventArgs.Empty);

            while (!isExiting)
            {
                // Events
                ProcessEvents();

                if (isExiting)
                    break;

                // Updates
                time = update_watch.Elapsed.TotalSeconds;
                if (time > 0.1)
                    time = 0.1;
                while (next_update - time <= 0.0)
                {
                    next_update = next_update - time + TargetUpdatePeriod;

                    update_time_counter += time;
                    ++num_updates;

                    update_watch.Reset();
                    update_watch.Start();

                    update_args.Time = time;
                    OnUpdateFrameInternal(update_args);

                    if (TargetUpdateFrequency == 0.0)
                        break;

                    time = update_watch.Elapsed.TotalSeconds;
                    next_update -= time;
                    update_time_counter += time;
                }
                if (num_updates > 0)
                {
                    update_period = update_time_counter / (double)num_updates;
                    num_updates = 0;
                    update_time_counter = 0.0;
                }

                // Frame
                if (isExiting)
                    break;

                time = render_watch.Elapsed.TotalSeconds;
                if (time > 0.1)
                    time = 0.1;
                if (next_render - time <= 0.0)
                {
                    next_render = next_render - time + TargetRenderPeriod;
                    render_watch.Reset();
                    render_watch.Start();

                    render_period = render_args.Time = time;
                    render_args.ScaleFactor = RenderPeriod / UpdatePeriod;
                    OnRenderFrameInternal(render_args);
                }
            }

            OnUnloadInternal(EventArgs.Empty);

            if (this.Exists)
            {
                glWindow.DestroyWindow();
                while (this.Exists)
                {
                    this.ProcessEvents();
                }
            }
        }

        #endregion

        #region public void ProcessEvents()

        /// <summary>
        /// Processes operating system events until the GameWindow becomes idle.
        /// </summary>
        /// <remarks>
        /// When overriding the default GameWindow game loop (provided by the Run() function)
        /// you should call ProcessEvents() to ensure that your GameWindow responds to
        /// operating system events.
        /// <para>
        /// Once ProcessEvents() returns, it is time to call update and render the next frame.
        /// </para>
        /// </remarks>
        public void ProcessEvents()
        {
            //if (!isExiting)
            //    InputDriver.Poll();
            glWindow.ProcessEvents();
        }

        #endregion

        #region OnRenderFrame(RenderFrameEventArgs e)

        /// <summary>
        /// Raises the RenderFrame event, and calls the public function.
        /// </summary>
        /// <param name="e"></param>
        private void OnRenderFrameInternal(RenderFrameEventArgs e)
        {
            if (!this.Exists && !this.IsExiting)
            {
                Debug.Print("WARNING: RenderFrame event raised, without a valid render window. This may indicate a programming error. Creating render window.");
                mode = new DisplayMode(640, 480);
                this.CreateWindow(mode);
            }
            if (RenderFrame != null)
                RenderFrame(this, e);

            // Call the user's override.
            OnRenderFrame(e);
        }

        /// <summary>
        /// Override in derived classes to render a frame.
        /// </summary>
        /// <param name="e">Contains information necessary for frame rendering.</param>
        /// <remarks>
        /// The base implementation (base.OnRenderFrame) is empty, there is no need to call it.
        /// </remarks>
        public virtual void OnRenderFrame(RenderFrameEventArgs e)
        {
        }

        /// <summary>
        /// Occurs when it is time to render the next frame.
        /// </summary>
        public event RenderFrameEvent RenderFrame;

        #endregion

        #region OnUpdateFrame(UpdateFrameEventArgs e)

        private void OnUpdateFrameInternal(UpdateFrameEventArgs e)
        {
            if (!this.Exists && !this.IsExiting)
            {
                Debug.Print("WARNING: UpdateFrame event raised without a valid render window. This may indicate a programming error. Creating render window.");
                mode = new DisplayMode(640, 480);
                this.CreateWindow(mode);
            }

            if (MustResize)
            {
                resizeEventArgs.Width = glWindow.Width;
                resizeEventArgs.Height = glWindow.Height;
                OnResizeInternal(resizeEventArgs);
            }

            if (UpdateFrame != null)
            {
                UpdateFrame(this, e);
            }

            OnUpdateFrame(e);
        }

        /// <summary>
        /// Override in derived classes to update a frame.
        /// </summary>
        /// <param name="e">Contains information necessary for frame updating.</param>
        /// <remarks>
        /// The base implementation (base.OnUpdateFrame) is empty, there is no need to call it.
        /// </remarks>
        public virtual void OnUpdateFrame(UpdateFrameEventArgs e)
        {
        }

        /// <summary>
        /// Occurs when it is time to update the next frame.
        /// </summary>
        public event UpdateFrameEvent UpdateFrame;

        #endregion

        #region OnLoad(EventArgs e)

        /// <summary>
        /// Occurs after establishing an OpenGL context, but before entering the main loop.
        /// </summary>
        public event LoadEvent Load;

        /// <summary>
        /// Raises the Load event, and calls the user's OnLoad override.
        /// </summary>
        /// <param name="e"></param>
        private void OnLoadInternal(EventArgs e)
        {
            Debug.WriteLine(String.Format("OpenGL driver information: {0}, {1}, {2}",
                GL.GetString(GL.Enums.StringName.RENDERER),
                GL.GetString(GL.Enums.StringName.VENDOR),
                GL.GetString(GL.Enums.StringName.VERSION)));

            if (this.Load != null)
            {
                this.Load(this, e);
            }

            OnLoad(e);
        }

        /// <summary>
        /// Occurs after establishing an OpenGL context, but before entering the main loop.
        /// Override to load resources that should be maintained for the lifetime of the application.
        /// </summary>
        /// <param name="e">Not used.</param>
        public virtual void OnLoad(EventArgs e)
        {
        }

        #endregion

        #region OnUnload(EventArgs e)

        /// <summary>
        /// Occurs after after calling GameWindow.Exit, but before destroying the OpenGL context.
        /// </summary>
        public event UnloadEvent Unload;

        /// <summary>
        /// Raises the Unload event, and calls the user's OnUnload override.
        /// </summary>
        /// <param name="e"></param>
        private void OnUnloadInternal(EventArgs e)
        {
            if (this.Unload != null)
            {
                this.Unload(this, e);
            }

            OnUnload(e);
        }

        /// <summary>
        /// Occurs after after calling GameWindow.Exit, but before destroying the OpenGL context.
        /// Override to unload application resources.
        /// </summary>
        /// <param name="e">Not used.</param>
        public virtual void OnUnload(EventArgs e)
        {
        }

        #endregion

        #region public bool IsExiting

        /// <summary>
        /// Gets a value indicating whether the shutdown sequence has been initiated
        /// for this window, by calling GameWindow.Exit() or hitting the 'close' button.
        /// If this property is true, it is no longer safe to use any OpenTK.Input or
        /// OpenTK.OpenGL functions or properties.
        /// </summary>
        public bool IsExiting
        {
            get { return isExiting; }
        }

        #endregion

        #region public Keyboard Keyboard

        /// <summary>
        /// Gets the primary Keyboard device, or null if no Keyboard exists.
        /// </summary>
        public KeyboardDevice Keyboard
        {
            get
            {
                if (InputDriver.Keyboard.Count > 0)
                    return InputDriver.Keyboard[0];
                else
                    return null;
            }
        }

        #endregion

        #region public Mouse Mouse

        /// <summary>
        /// Gets the primary Mouse device, or null if no Mouse exists.
        /// </summary>
        public MouseDevice Mouse
        {
            get
            {
                if (InputDriver.Mouse.Count > 0)
                    return InputDriver.Mouse[0];
                else
                    return null;
            }
        }

        #endregion

        #region public VSyncMode VSync

        /// <summary>
        /// Gets or sets the VSyncMode.
        /// </summary>
        public VSyncMode VSync
        {
            get
            {
                return vsync;
            }
            set
            {
                if (value == VSyncMode.Off)
                    Context.VSync = false;
                else if (value == VSyncMode.On)
                    Context.VSync = true;

                vsync = value;
            }
        }

        #endregion

        #region public void SwapBuffers()

        /// <summary>
        /// Swaps the front and back buffer, presenting the rendered scene to the user.
        /// Only useful in double- or triple-buffered formats.
        /// </summary>
        /// <remarks>Calling this function is equivalent to calling Context.SwapBuffers()</remarks>
        public void SwapBuffers()
        {
            Context.SwapBuffers();
        }

        #endregion

        #endregion

        #region --- GameWindow Timing ---


        #region public double TargetRenderPeriod

        /// <summary>
        /// Gets or sets the target render period in seconds.
        /// </summary>
        /// <para>A value of 0.0 indicates that RenderFrame events are generated at the maximum possible frequency (i.e. only limited by the hardware's capabilities).</para>
        /// <para>Values lower than 0.005 seconds (200Hz) are clamped to 0.0. Values higher than 1.0 seconds (1Hz) are clamped to 1.0.</para>
        /// </remarks>
        public double TargetRenderPeriod
        {
            get
            {
                return target_render_period;
            }
            set
            {
                if (value <= 0.005)
                {
                    target_render_period = target_render_period_doubled = 0.0;
                }
                else if (value <= 1.0)
                {
                    target_render_period = value;
                    target_render_period_doubled = 2.0 * target_render_period;
                }
                else Debug.Print("Target render period clamped to 1.0 seconds.");
            }
        }

        #endregion

        #region public double TargetRenderFrequency

        /// <summary>
        /// Gets or sets the target render frequency in Herz.
        /// </summary>
        /// <remarks>
        /// <para>A value of 0.0 indicates that RenderFrame events are generated at the maximum possible frequency (i.e. only limited by the hardware's capabilities).</para>
        /// <para>Values lower than 1.0Hz are clamped to 1.0Hz. Values higher than 200.0Hz are clamped to 200.0Hz.</para>
        /// </remarks>
        public double TargetRenderFrequency
        {
            get
            {
                if (TargetRenderPeriod == 0.0)
                    return 0.0;
                return 1.0 / TargetRenderPeriod;
            }
            set
            {
                if (value <= 1.0)
                {
                    TargetRenderPeriod = 0.0;
                }
                else if (value <= 200.0)
                {
                    TargetRenderPeriod = 1.0 / value;
                }
                else Debug.Print("Target render frequency clamped to 200.0Hz.");
            }
        }

        #endregion

        #region public double TargetUpdatePeriod

        /// <summary>
        /// Gets or sets the target update period in seconds.
        /// </summary>
        /// <remarks>
        /// <para>A value of 0.0 indicates that UpdateFrame events are generated at the maximum possible frequency (i.e. only limited by the hardware's capabilities).</para>
        /// <para>Values lower than 0.005 seconds (200Hz) are clamped to 0.0. Values higher than 1.0 seconds (1Hz) are clamped to 1.0.</para>
        /// </remarks>
        public double TargetUpdatePeriod
        {
            get
            {
                return target_update_period;
            }
            set
            {
                if (value <= 0.005)
                {
                    target_update_period = 0.0;
                }
                else if (value <= 1.0)
                {
                    target_update_period = value;
                }
                else Debug.Print("Target update period clamped to 1.0 seconds.");
            }
        }

        #endregion

        #region public double TargetUpdateFrequency

        /// <summary>
        /// Gets or sets the target update frequency in Herz.
        /// </summary>
        /// <remarks>
        /// <para>A value of 0.0 indicates that UpdateFrame events are generated at the maximum possible frequency (i.e. only limited by the hardware's capabilities).</para>
        /// <para>Values lower than 1.0Hz are clamped to 1.0Hz. Values higher than 200.0Hz are clamped to 200.0Hz.</para>
        /// </remarks>
        public double TargetUpdateFrequency
        {
            get
            {
                if (TargetUpdatePeriod == 0.0)
                    return 0.0;
                return 1.0 / TargetUpdatePeriod;
            }
            set
            {
                if (value <= 1.0)
                {
                    TargetUpdatePeriod = 0.0;
                }
                else if (value <= 200.0)
                {
                    TargetUpdatePeriod = 1.0 / value;
                }
                else Debug.Print("Target update frequency clamped to 200.0Hz.");
            }
        }

        #endregion

        #region public double RenderFrequency

        /// <summary>
        /// Gets the actual frequency of RenderFrame events in Herz (i.e. FPS or Frames Per Second).
        /// </summary>
        public double RenderFrequency
        {
            get
            {
                if (render_period == 0.0)
                    return 1.0;
                return 1.0 / render_period;
            }
        }

        #endregion

        #region public double RenderPeriod

        /// <summary>
        /// Gets the period of RenderFrame events in seconds.
        /// </summary>
        public double RenderPeriod
        {
            get
            {
                return render_period;
            }
        }

        #endregion

        #region public double UpdateFrequency

        /// <summary>
        /// Gets the frequency of UpdateFrame events in Herz.
        /// </summary>
        public double UpdateFrequency
        {
            get
            {
                if (update_period == 0.0)
                    return 1.0;
                return 1.0 / update_period;
            }
        }

        #endregion

        #region public double UpdatePeriod

        /// <summary>
        /// Gets the period of UpdateFrame events in seconds.
        /// </summary>
        public double UpdatePeriod
        {
            get
            {
                return update_period;
            }
        }

        #endregion

        #endregion

        #region --- IResizable Members ---

        #region public int Width, Height

        /// <summary>
        /// Gets or sets the Width of the GameWindow's rendering area, in pixels.
        /// </summary>
        public int Width
        {
            get { return width; }
            set
            {
                if (value == this.Width)
                {
                    return;
                }
                else if (value > 0)
                {
                    glWindow.Width = value;
                }
                else
                {
                    throw new ArgumentOutOfRangeException(
                        "Width",
                        value,
                        "Width must be greater than 0"
                    );
                }
            }
        }

        /// <summary>
        /// Gets or sets the Height of the GameWindow's rendering area, in pixels.
        /// </summary>
        public int Height
        {
            get { return height; }
            set
            {
                if (value == this.Height)
                {
                    return;
                }
                else if (value > 0)
                {
                    glWindow.Height = value;
                }
                else
                {
                    throw new ArgumentOutOfRangeException(
                        "Height",
                        value,
                        "Height must be greater than 0"
                    );
                }
            }
        }

        #endregion

        #region public event ResizeEvent Resize;

        /// <summary>
        /// Occurs when the GameWindow is resized. Derived classes should override the OnResize method for better performance.
        /// </summary>
        public event ResizeEvent Resize;

        /// <summary>
        /// Raises the Resize event.
        /// </summary>
        /// <param name="e">Contains information about the Resize event.</param>
        private void OnResizeInternal(ResizeEventArgs e)
        {
            Debug.Print("Firing GameWindow.Resize event: {0}.", e.ToString());

            this.width = e.Width;
            this.height = e.Height;
            
            if (this.Resize != null)
                this.Resize(this, e);

            OnResize(e);
        }

        /// <summary>
        /// Override in derived classes to respond to the Resize events.
        /// </summary>
        /// <param name="e">Contains information about the Resize event.</param>
        protected virtual void OnResize(ResizeEventArgs e)
        {
        }

        #endregion
        /*
        /// <summary>
        /// Gets the Top coordinate of the GameWindow's rendering area, in pixel coordinates relative to the GameWindow's top left point.
        /// </summary>
        public int Top
        {
            get { return glWindow.Top; }
        }

        /// <summary>
        /// /// Gets the Bottom coordinate of the GameWindow's rendering area, in pixel coordinates relative to the GameWindow's top left point.
        /// </summary>
        public int Bottom
        {
            get { return glWindow.Bottom; }
        }

        /// <summary>
        /// Gets the Left coordinate of the GameWindow's rendering area, in pixel coordinates relative to the GameWindow's top left point.
        /// </summary>
        public int Left
        {
            get { return glWindow.Left; }
        }

        /// <summary>
        /// Gets the Right coordinate of the GameWindow's rendering area, in pixel coordinates relative to the GameWindow's top left point.
        /// </summary>
        public int Right
        {
            get { return glWindow.Right; }
        }
        */
        #endregion

        #region --- IDisposable Members ---

        /// <summary>
        /// Not used yet.
        /// </summary>
        private void DisposeInternal()
        {
            Dispose();                  // User overridable Dispose method.

        }

        /// <summary>
        /// Disposes of the GameWindow, releasing all resources consumed by it.
        /// </summary>
        public virtual void Dispose()
        {
            Dispose(true);              // Real Dispose method.
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool manual)
        {
            if (!disposed)
            {
                // Is this safe? Maybe 'Debug' has been disposed, too...
                //Debug.Print("{0} disposing GameWindow.", manual ? "Manually" : "Automatically");

                if (manual)
                {
                    if (glWindow != null)
                    {
                        glWindow.Dispose();
                        glWindow = null;
                    }
                }
                disposed = true;
            }
        }

        ~GameWindow()
        {
            this.Dispose(false);
        }

        #endregion
    }

    #region public enum VSyncMode

    /// <summary>
    /// Indicates the available VSync modes.
    /// </summary>
    public enum VSyncMode
    {
        /// <summary>
        /// Vsync disabled.
        /// </summary>
        Off = 0,
        /// <summary>
        /// VSync enabled.
        /// </summary>
        On,
        /// <summary>
        /// VSync enabled, but automatically disabled if framerate falls below a specified limit.
        /// </summary>
        Adaptive
    }

    #endregion

    #region --- GameWindow Events ---

    public delegate void UpdateFrameEvent(GameWindow sender, UpdateFrameEventArgs e);
    public delegate void RenderFrameEvent(GameWindow sender, RenderFrameEventArgs e);
    public delegate void LoadEvent(GameWindow sender, EventArgs e);
    public delegate void UnloadEvent(GameWindow sender, EventArgs e);

    public class UpdateFrameEventArgs : EventArgs
    {
        private double time;

        /// <summary>
        /// Gets the Time elapsed between frame updates, in seconds.
        /// </summary>
        public double Time
        {
            get { return time; }
            internal set { time = value; }
        }
    }

    public class RenderFrameEventArgs : EventArgs
    {
        private double time;
        private double scale_factor;

        /// <summary>
        /// Gets the Time elapsed between frame updates, in seconds.
        /// </summary>
        public double Time
        {
            get { return time; }
            internal set { time = value; }
        }

        public double ScaleFactor
        {
            get
            {
                return scale_factor;
            }
            internal set
            {
                if (value != 0.0 && !Double.IsNaN(value))
                    scale_factor = value;
                else
                    scale_factor = 1.0;
            }
        }
    }

    #endregion
}
