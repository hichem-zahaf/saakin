using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Drawing;

namespace saakin
{
    /// <summary>
    /// Entry point and CLI parser for the saakin mouse-jitter command-line utility.
    /// </summary>
    internal class Program
    {
        // Default parameters
        private const int DefaultIntervalMs = 1000;
        private const int DefaultRangeMinPx = 10;
        private const int DefaultRangeMaxPx = 80;

        private static readonly string HelpText = @"Usage:
  saakin -start [-duration <milliseconds>] [-interval <milliseconds>] [-range <max|min-max>]
  saakin -clicker [-interval <ms>] [-delay <ms>] [-button <btn>]
  saakin -guard [-area <px>] [-clicks <n>] [-clickinterval <ms>] [-delay <ms>] [-button <btn>]
  saakin -help

Options:
  -start                 Starts the mouse jitter.
  -duration <ms>         Total run time in milliseconds. If omitted, saakin runs until stopped (Ctrl+C).
  -interval <ms>         Delay between two consecutive jitters. Default: 1000 ms.
  -range <max|min-max>   Jitter distance. Provide a single value for max distance, or ""min-max"" for a variable range.
                         Default: 10-80 px.
  -clicker               Starts the auto-clicker mode.
  -guard                 Starts the pixel guard mode.
  -delay <ms>            Delay for clicker or guard mode.
  -button <btn>          Mouse button for clicker or guard mode.
  -area <px>             Area size for pixel guard mode.
  -clicks <n>           Number of clicks for pixel guard mode.
  -clickinterval <ms>    Click interval for pixel guard mode.
  -help                  Display this help message.

Behaviour:
  • The utility moves the cursor randomly at the requested interval.
  • If the user moves the mouse manually, saakin pauses automatically and
    resumes only after the mouse has been idle for at least one interval.
  • Stop the tool at any moment with Ctrl+C."
        ;

        private static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                PrintAsciiArt();
                PauseIfLaunchedFromExplorer();
                return;
            }

            if (args[0].Equals("-help", StringComparison.OrdinalIgnoreCase))
            {
                PrintAsciiArt();
                Console.WriteLine(HelpText);
                return;
            }

            // CLI arguments
            bool jitterMode = false;
            bool clickerMode = false;
            bool guardMode = false;

            int intervalMs = DefaultIntervalMs;
            int rangeMinPx = DefaultRangeMinPx;
            int rangeMaxPx = DefaultRangeMaxPx;
            int? durationMs = null;

            int clickerDelayMs = 5000;
            string clickButtonStr = "left";

            int guardAreaPx = 50;
            int guardClicks = 2;
            int guardClickIntervalMs = 500;
            int guardDelayMs = 5000;

            try
            {
                for (int i = 0; i < args.Length; i++)
                {
                    string arg = args[i].ToLowerInvariant();
                    switch (arg)
                    {
                        case "-start":
                            jitterMode = true;
                            break;
                        case "-interval":
                            intervalMs = int.Parse(args[++i]);
                            break;
                        case "-duration":
                            durationMs = int.Parse(args[++i]);
                            break;
                        case "-range":
                            string rangeToken = args[++i];
                            if (rangeToken.Contains("-"))
                            {
                                var parts = rangeToken.Split('-');
                                if (parts.Length != 2) throw new ArgumentException("Invalid -range format. Use <max> or <min>-<max>.");
                                rangeMinPx = int.Parse(parts[0]);
                                rangeMaxPx = int.Parse(parts[1]);
                                if (rangeMinPx < 0 || rangeMaxPx < 0 || rangeMinPx > rangeMaxPx)
                                    throw new ArgumentException("Invalid range values. Ensure 0 <= min <= max.");
                            }
                            else
                            {
                                rangeMaxPx = int.Parse(rangeToken);
                                rangeMinPx = 0;
                                if (rangeMaxPx < 0) throw new ArgumentException("Range must be non-negative.");
                            }
                            break;
                        case "-clicker":
                            clickerMode = true;
                            break;
                        case "-guard":
                            guardMode = true;
                            break;
                        case "-delay":
                            int delayVal = int.Parse(args[++i]);
                            if (clickerMode) clickerDelayMs = delayVal;
                            else if (guardMode) guardDelayMs = delayVal;
                            else throw new ArgumentException("-delay is only valid after -clicker or -guard mode is specified.");
                            break;
                        case "-button":
                            clickButtonStr = args[++i].ToLowerInvariant();
                            break;
                        case "-area":
                            guardAreaPx = int.Parse(args[++i]);
                            break;
                        case "-clicks":
                            guardClicks = int.Parse(args[++i]);
                            break;
                        case "-clickinterval":
                            guardClickIntervalMs = int.Parse(args[++i]);
                            break;
                        default:
                            throw new ArgumentException($"Unknown argument: {args[i]}");
                    }
                }
            }
            catch (Exception ex)
            {
                PrintAsciiArt();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ResetColor();
                Console.WriteLine();
                Console.WriteLine(HelpText);
                PauseIfLaunchedFromExplorer();
                return;
            }

            if (!jitterMode && !clickerMode && !guardMode)
            {
                PrintAsciiArt();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("No action specified. Did you forget to add -start, -clicker, or -guard ?");
                Console.ResetColor();
                Console.WriteLine();
                Console.WriteLine(HelpText);
                PauseIfLaunchedFromExplorer();
                return;
            }

            // Ready – run jitter
            PrintAsciiArt();
            var cts = new CancellationTokenSource();
            Console.ResetColor();
            if (jitterMode)
            {
                Console.WriteLine($"Interval : {intervalMs} ms | Range : {rangeMinPx}-{rangeMaxPx}px | IdlePause : 10000 ms");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("[{0:HH:mm:ss}] Mode started.", DateTime.Now);
                Console.ResetColor();
            }
            else if (clickerMode)
            {
                Console.WriteLine($"Delay : {clickerDelayMs} ms | Interval : {intervalMs} ms | Button : {clickButtonStr}");
            }
            else if (guardMode)
            {
                Console.WriteLine($"Delay : {guardDelayMs} ms | Area : {guardAreaPx}px | Clicks : {guardClicks} @ {guardClickIntervalMs} ms | Button : {clickButtonStr}");
            }

            // Setup Ctrl+C
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true; // don't kill the process immediately
                cts.Cancel();
            };

            if (jitterMode)
            {
                var jitter = new MouseJitter(intervalMs, rangeMinPx, rangeMaxPx);
                var startTime = DateTime.UtcNow;

                while (!cts.IsCancellationRequested)
                {
                    if (durationMs.HasValue && (DateTime.UtcNow - startTime).TotalMilliseconds >= durationMs.Value)
                        break;

                    jitter.Tick();
                    Thread.Sleep(intervalMs);
                }
            }
            else if (clickerMode)
            {
                var clicker = new MouseAutoClicker(clickerDelayMs, intervalMs, ParseMouseButton(clickButtonStr));
                clicker.Run(cts.Token);
            }
            else if (guardMode)
            {
                var guard = new PixelGuard(guardDelayMs, guardAreaPx, guardClicks, guardClickIntervalMs, ParseMouseButton(clickButtonStr));
                guard.Run(cts.Token);
            }

            Console.WriteLine("\nsaakin stopped. Goodbye!");
        }

        /// <summary>
        /// Prints the banner using console colours.
        /// </summary>
        private static void PrintAsciiArt()
        {
            string[] banner =
            {
                @"                                                                                                   ",
                @"   d888888o.           .8.                   .8.          8 8888     ,88'  8 8888 b.             8 ",
                @" .`8888:' `88.        .888.                 .888.         8 8888    ,88'   8 8888 888o.          8 ",
                @" 8.`8888.   Y8       :88888.               :88888.        8 8888   ,88'    8 8888 Y88888o.       8 ",
                @" `8.`8888.          . `88888.             . `88888.       8 8888  ,88'     8 8888 .`Y888888o.    8 ",
                @"  `8.`8888.        .8. `88888.           .8. `88888.      8 8888 ,88'      8 8888 8o. `Y888888o. 8 ",
                @"   `8.`8888.      .8`8. `88888.         .8`8. `88888.     8 8888 88'       8 8888 8`Y8o. `Y88888o8 ",
                @"    `8.`8888.    .8' `8. `88888.       .8' `8. `88888.    8 888888<        8 8888 8   `Y8o. `Y8888 ",
                @"8b   `8.`8888.  .8'   `8. `88888.     .8'   `8. `88888.   8 8888 `Y8.      8 8888 8      `Y8o. `Y8 ",
                @"`8b.  ;8.`8888 .888888888. `88888.   .888888888. `88888.  8 8888   `Y8.    8 8888 8         `Y8o.` ",
                @" `Y8888P ,88P'.8'       `8. `88888. .8'       `8. `88888. 8 8888     `Y8.  8 8888 8            `Yo "
            };

            ConsoleColor[] colours =
            {
                ConsoleColor.DarkRed,
                ConsoleColor.DarkYellow,
                ConsoleColor.Yellow,
                ConsoleColor.Green,
                ConsoleColor.Cyan,
                ConsoleColor.Blue,
                ConsoleColor.DarkBlue,
                ConsoleColor.Magenta,
                ConsoleColor.DarkMagenta,
                ConsoleColor.Red,
                ConsoleColor.White
            };

            for (int i = 0; i < banner.Length; i++)
            {
                Console.ForegroundColor = colours[i % colours.Length];
                Console.WriteLine(banner[i]);
            }
            Console.ResetColor();
            Console.WriteLine();
        }

        /// <summary>
        /// When the program is launched by double-click (no parent console), the window closes immediately on exit.
        /// This helper waits for a key press so users can read the output.
        /// It detects Explorer launch by checking if STDIN is not redirected and no arguments were passed.
        /// </summary>
        private static void PauseIfLaunchedFromExplorer()
        {
            if (!Console.IsInputRedirected && Environment.UserInteractive)
            {
                Console.WriteLine("\nPress any key to close…");
                Console.ReadKey(true);
            }
        }

        private static MouseButton ParseMouseButton(string buttonStr)
        {
            switch (buttonStr)
            {
                case "left":
                    return MouseButton.Left;
                case "right":
                    return MouseButton.Right;
                case "middle":
                    return MouseButton.Middle;
                case "x1":
                case "mouse4":
                    return MouseButton.X1;
                case "x2":
                case "mouse5":
                    return MouseButton.X2;
                default:
                    throw new ArgumentException($"Unknown mouse button: {buttonStr}");
            }
        }
    }

    /// <summary>
    /// Handles cursor jitter logic, including automatic pause while the user moves the mouse.
    /// </summary>
    internal sealed class MouseJitter
    {
        private readonly Random _rnd = new Random();
        private readonly int _intervalMs;
        private const int PauseAfterUserMoveMs = 10000; // 10-second idle period before jitter resumes
        private readonly int _rangeMinPx;
        private readonly int _rangeMaxPx;

        private POINT _expectedPos;
        private bool _isFirstTick = true;
        private DateTime _lastUserMove = DateTime.UtcNow;
        private bool _isPaused = false;
        private bool _hasMoved = false;

        public MouseJitter(int intervalMs, int rangeMinPx, int rangeMaxPx)
        {
            _intervalMs = intervalMs;
            _rangeMinPx = Math.Max(1, rangeMinPx);
            _rangeMaxPx = Math.Max(1, rangeMaxPx);
            GetCursorPos(out _expectedPos);
        }

        /// <summary>
        /// Performs one jitter cycle if appropriate.
        /// </summary>
        public void Tick()
        {
            GetCursorPos(out POINT currentPos);

            // On first invocation, only record position; don't move.
            if (_isFirstTick)
            {
                _expectedPos = currentPos;
                _isFirstTick = false;
                return;
            }

            // Detect user movement:
            if (!PointsEqual(currentPos, _expectedPos))
            {
                _lastUserMove = DateTime.UtcNow;
                _expectedPos = currentPos; // follow the user's new position
                if (!_isPaused)
                {
                    _isPaused = true;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("[{0:HH:mm:ss}] User activity detected – pausing jitter for 10 s…", DateTime.Now);
                    Console.ResetColor();
                }
                return; // skip move while user active
            }

            // If still within pause duration, stay idle
            if ((DateTime.UtcNow - _lastUserMove).TotalMilliseconds < PauseAfterUserMoveMs)
                return;

            // If we reach here and was paused, resume notice
            if (_isPaused)
            {
                _isPaused = false;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[{0:HH:mm:ss}] Idle threshold reached – resuming jitter.", DateTime.Now);
                Console.ResetColor();
            }

            // Time to jitter
            int magnitudeX = _rnd.Next(_rangeMinPx, _rangeMaxPx + 1);
            int magnitudeY = _rnd.Next(_rangeMinPx, _rangeMaxPx + 1);
            int dx = (_rnd.Next(0, 2) == 0 ? -1 : 1) * magnitudeX;
            int dy = (_rnd.Next(0, 2) == 0 ? -1 : 1) * magnitudeY;

            int newX = currentPos.X + dx;
            int newY = currentPos.Y + dy;

            // Clamp to screen bounds
            newX = Math.Max(0, Math.Min(newX, GetSystemMetrics(SystemMetric.SM_CXSCREEN) - 1));
            newY = Math.Max(0, Math.Min(newY, GetSystemMetrics(SystemMetric.SM_CYSCREEN) - 1));

            SetCursorPos(newX, newY);
            if (!_hasMoved)
            {
                _hasMoved = true;
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("[{0:HH:mm:ss}] Jitter movement initiated.", DateTime.Now);
                Console.ResetColor();
            }
            _expectedPos = new POINT { X = newX, Y = newY };
        }

        #region Win32 Interop

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        private enum SystemMetric
        {
            SM_CXSCREEN = 0,
            SM_CYSCREEN = 1
        }

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(SystemMetric smIndex);

        #endregion

        private static bool PointsEqual(POINT a, POINT b) => a.X == b.X && a.Y == b.Y;
    }

    internal enum MouseButton
    {
        Left,
        Right,
        Middle,
        X1,
        X2
    }

    internal static class MouseActions
    {
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        private const uint MOUSEEVENTF_XDOWN = 0x0080;
        private const uint MOUSEEVENTF_XUP = 0x0100;
        private const uint XBUTTON1 = 0x0001;
        private const uint XBUTTON2 = 0x0002;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        public static void Click(MouseButton button)
        {
            switch (button)
            {
                case MouseButton.Left:
                    mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                    mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
                    break;
                case MouseButton.Right:
                    mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, UIntPtr.Zero);
                    mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, UIntPtr.Zero);
                    break;
                case MouseButton.Middle:
                    mouse_event(MOUSEEVENTF_MIDDLEDOWN, 0, 0, 0, UIntPtr.Zero);
                    mouse_event(MOUSEEVENTF_MIDDLEUP, 0, 0, 0, UIntPtr.Zero);
                    break;
                case MouseButton.X1:
                    mouse_event(MOUSEEVENTF_XDOWN, 0, 0, XBUTTON1, UIntPtr.Zero);
                    mouse_event(MOUSEEVENTF_XUP, 0, 0, XBUTTON1, UIntPtr.Zero);
                    break;
                case MouseButton.X2:
                    mouse_event(MOUSEEVENTF_XDOWN, 0, 0, XBUTTON2, UIntPtr.Zero);
                    mouse_event(MOUSEEVENTF_XUP, 0, 0, XBUTTON2, UIntPtr.Zero);
                    break;
            }
        }
    }

    /// <summary>
    /// Repeatedly clicks at locked position until cancellation or user movement.
    /// </summary>
    internal sealed class MouseAutoClicker
    {
        private readonly int _lockDelayMs;
        private readonly int _intervalMs;
        private readonly MouseButton _button;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X; public int Y; }

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT p);

        public MouseAutoClicker(int lockDelayMs, int intervalMs, MouseButton button)
        {
            _lockDelayMs = lockDelayMs;
            _intervalMs = intervalMs;
            _button = button;
        }

        public void Run(CancellationToken token)
        {
            Console.WriteLine("Waiting {0} ms to lock position…", _lockDelayMs);
            Thread.Sleep(_lockDelayMs);
            GetCursorPos(out POINT lockedPos);
            Console.WriteLine("Locked position at ({0},{1}). Starting auto-click…", lockedPos.X, lockedPos.Y);

            while (!token.IsCancellationRequested)
            {
                GetCursorPos(out POINT current);
                if (current.X != lockedPos.X || current.Y != lockedPos.Y)
                {
                    Console.WriteLine("Mouse moved by user. Auto-clicker stopping.");
                    break;
                }
                MouseActions.Click(_button);
                Thread.Sleep(_intervalMs);
            }
        }
    }

    /// <summary>
    /// Watches a region for pixel changes then triggers clicks.
    /// </summary>
    internal sealed class PixelGuard
    {
        private readonly int _lockDelayMs;
        private readonly int _areaPx;
        private readonly int _clicks;
        private readonly int _clickIntervalMs;
        private readonly MouseButton _button;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X; public int Y; }

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT p);

        public PixelGuard(int lockDelayMs, int areaPx, int clicks, int clickIntervalMs, MouseButton button)
        {
            _lockDelayMs = lockDelayMs;
            _areaPx = areaPx;
            _clicks = clicks;
            _clickIntervalMs = clickIntervalMs;
            _button = button;
        }

        public void Run(CancellationToken token)
        {
            Console.WriteLine("Waiting {0} ms to lock watch area…", _lockDelayMs);
            Thread.Sleep(_lockDelayMs);
            GetCursorPos(out POINT center);
            int half = _areaPx / 2;
            Rectangle rect = new Rectangle(center.X - half, center.Y - half, _areaPx, _areaPx);

            using (Bitmap baseline = CaptureArea(rect))
            {
                Console.WriteLine("Area locked. Monitoring for changes…");
                while (!token.IsCancellationRequested)
                {
                    Thread.Sleep(100);
                    using (Bitmap current = CaptureArea(rect))
                    {
                        if (HasDifference(baseline, current))
                        {
                            Console.WriteLine("Change detected! Performing clicks…");
                            for (int i = 0; i < _clicks; i++)
                            {
                                MouseActions.Click(_button);
                                Thread.Sleep(_clickIntervalMs);
                            }
                            return;
                        }
                    }
                    // stop if user moves mouse
                    GetCursorPos(out POINT pos);
                    if (pos.X != center.X || pos.Y != center.Y)
                    {
                        Console.WriteLine("Mouse moved by user. Pixel guard stopping.");
                        return;
                    }
                }
            }
        }

        private static Bitmap CaptureArea(Rectangle rect)
        {
            Bitmap bmp = new Bitmap(rect.Width, rect.Height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(rect.Location, Point.Empty, rect.Size);
            }
            return bmp;
        }

        private static bool HasDifference(Bitmap a, Bitmap b)
        {
            for (int x = 0; x < a.Width; x += 5)
            {
                for (int y = 0; y < a.Height; y += 5)
                {
                    if (a.GetPixel(x, y) != b.GetPixel(x, y))
                        return true;
                }
            }
            return false;
        }
    }
}
