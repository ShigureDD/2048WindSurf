using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace _2048
{
    public partial class MainForm : Form
    {
        // Helper method to create a rounded rectangle path
        private static GraphicsPath RoundedRect(RectangleF bounds, float radius)
        {
            float diameter = radius * 2;
            RectangleF arc = new RectangleF(bounds.Location, new SizeF(diameter, diameter));
            GraphicsPath path = new GraphicsPath();

            // Top left arc  
            path.AddArc(arc, 180, 90);

            // Top right arc  
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);

            // Bottom right arc  
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);

            // Bottom left arc 
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }
        private Game2048 game;
        private const int TilePadding = 6; // Reduced space between tiles
        private const int TileInset = 0;    // No inset - tiles fill cells completely
        private const int BoardPadding = 24; // Padding around the game board
        private const int BoardSize = 4;    // 4x4 grid
        private const int MinTileSize = 80; // Minimum size for each tile

        // Animation state
        private List<Game2048.TileMove> animMoves = null;
        private float animProgress = 1f; // 1 = no animation, 0 = start
        private System.Windows.Forms.Timer animTimer;
        private const int AnimationDuration = 100; // ms - matching CSS transition-duration
        private const int AnimationFps = 60; // FPS for smooth animation
        private const double AnimationInterval = 1000.0 / AnimationFps; // ~16.67ms per frame
        private const int AnimationStartDelay = 5; // ms delay before animation starts
        private float EaseIn(float t) {
            return t * t; // Quadratic ease-in like CSS
        }
        private float EaseInOutCubic(float t) {
            return t < 0.5f ? 4 * t * t * t : (float)(1 - Math.Pow(-2 * t + 2, 3) / 2); // Cubic ease-in-out like CSS
        }
        private const float MergeScaleAmount = 1.2f; // More subtle merge effect
        private const float MergeFadeAmount = 0.8f; // Less fade during merge
        private const float SlideOvershoot = 0.05f; // Subtle slide overshoot
        private float[,] slideOffsets; // For tracking individual tile slide positions
        private DateTime animStartTime;
        private Game2048.Direction? animDirection = null;
        private int[,] animBoardBefore = null;

        public MainForm()
        {
            InitializeComponent();
            
            // Set form properties
            this.Text = "2048 Game";
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.TopMost = true; // Ensure window is on top
            this.ShowInTaskbar = true; // Ensure it shows in taskbar
            
            // Initialize slide offsets
            slideOffsets = new float[BoardSize, BoardSize];
            
            // Calculate window size based on game board
            int tileSize = 100; // Base tile size
            int windowWidth = BoardSize * tileSize + (BoardSize + 1) * TilePadding + 2 * BoardPadding;
            int windowHeight = 200 + BoardSize * tileSize + (BoardSize + 1) * TilePadding + 2 * BoardPadding;
            this.ClientSize = new Size(windowWidth, windowHeight);
            
            // Make sure window is visible
            this.Visible = true;
            this.BringToFront();
            this.Focus();
            
            // Enable double buffering and optimize painting
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | 
                         ControlStyles.AllPaintingInWmPaint | 
                         ControlStyles.UserPaint, true);
            this.UpdateStyles();
            
            // Force window to be visible
            this.Show();
            this.Activate();
            
            this.KeyPreview = true; // Allow form to receive key events
            this.KeyDown += MainForm_KeyDown;
            this.Paint += MainForm_Paint;
            this.Resize += MainForm_Resize;

            game = new Game2048();
            this.BackColor = Color.FromArgb(250, 248, 239);

            animTimer = new System.Windows.Forms.Timer();
            animTimer.Interval = (int)AnimationInterval;
            animTimer.Tick += (s, e) => this.Invalidate();
            animTimer.Tick += (s, e) =>
            {
                if (animMoves == null) 
                { 
                    animTimer.Stop(); 
                    this.Invalidate(); // Force final redraw when animation completes
                    return; 
                }
                double elapsedMs = (DateTime.Now - animStartTime).TotalMilliseconds;
                
                // Apply start delay (like CSS transition-delay)
                if (elapsedMs < AnimationStartDelay) {
                    animProgress = 0f;
                    this.Invalidate();
                    return;
                }
                
                // Calculate progress with easing
                elapsedMs -= AnimationStartDelay;
                animProgress = Math.Min(1f, (float)(elapsedMs / (AnimationDuration - AnimationStartDelay)));
                
                // Apply ease-in easing for both X and Y
                animProgress = EaseIn(animProgress);
                
                this.Invalidate();
                if (animProgress >= 1f)
                {
                    // Save the current board state
                    var moves = animMoves;
                    var direction = animDirection;
                    
                    // Reset animation state first
                    animMoves = null;
                    animDirection = null;
                    animBoardBefore = null;
                    
                    // Add new tile if there were any moves
                    if (moves.Count > 0)
                    {
                        game.AddRandomTile();
                    }
                    
                    // Stop the timer and redraw
                    animTimer.Stop();
                    this.Invalidate();
                    
                    // Check win/loss conditions after adding the new tile
                    if (game.HasWon())
                        MessageBox.Show("Congratulations! You've won!");
                    else if (game.HasLost())
                    {
                        var result = MessageBox.Show("Game Over! Try again?", "Game Over", MessageBoxButtons.OK);
                        if (result == DialogResult.OK)
                        {
                            game = new Game2048();
                            this.Invalidate();
                        }
                    }
                }
            };
        }

        private void MainForm_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(this.BackColor);

            // Calculate responsive tile size and padding
            // Calculate tile size and positions to fit within window
            int maxBoardWidth = this.ClientSize.Width - 2 * BoardPadding;
            int maxBoardHeight = this.ClientSize.Height - 200; // Leave space for score and padding
            
            // Calculate maximum possible tile size that fits in both dimensions
            int tileSize = Math.Min(
                (maxBoardWidth - (BoardSize + 1) * TilePadding) / BoardSize,
                (maxBoardHeight - (BoardSize + 1) * TilePadding) / BoardSize
            );
            
            // Ensure tiles don't get too small on small windows
            tileSize = Math.Max(MinTileSize, tileSize);
            
            // Calculate board dimensions and position
            int boardTotalWidth = BoardSize * tileSize + (BoardSize + 1) * TilePadding;
            int boardTotalHeight = BoardSize * tileSize + (BoardSize + 1) * TilePadding;
            int boardX = (this.ClientSize.Width - boardTotalWidth) / 2;
            int startY = 100; // Space at the top for the score

            // Draw score and animation duration
            using (var font = new Font("Arial", 24, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.FromArgb(119, 110, 101)))
            {
                var scoreText = $"Score: {game.GetScore()}";
                var scoreSize = TextRenderer.MeasureText(scoreText, font);
                g.DrawString(scoreText, font, brush, 
                    (this.ClientSize.Width - scoreSize.Width) / 2, 10);
            }
            using (var font = new Font("Arial", 14, FontStyle.Italic))
            using (var brush = new SolidBrush(Color.FromArgb(150, 110, 101)))
            {
                string animText = $"Animation Duration: {AnimationDuration} ms";
                var animSize = TextRenderer.MeasureText(animText, font);
                g.DrawString(animText, font, brush, 
                    (this.ClientSize.Width - animSize.Width) / 2, 44);
            }

            // Draw board background with rounded corners
            using (var boardBrush = new SolidBrush(Color.FromArgb(187, 173, 160)))
            using (var path = RoundedRect(new RectangleF(boardX, startY, boardTotalWidth, boardTotalHeight), 12))
            {
                g.FillPath(boardBrush, path);
            }

            // Draw cell backgrounds with rounded corners
            using (var cellBrush = new SolidBrush(Color.FromArgb(205, 193, 180)))
            {
                for (int row = 0; row < BoardSize; row++)
                {
                    for (int col = 0; col < BoardSize; col++)
                    {
                        int x = boardX + col * (tileSize + TilePadding) + TilePadding;
                        int y = startY + row * (tileSize + TilePadding) + TilePadding;
                        using (var path = RoundedRect(new RectangleF(x, y, tileSize, tileSize), 6))
                        {
                            g.FillPath(cellBrush, path);
                        }
                    }
                }
            }

            if (animMoves == null || animProgress >= 1f)
            {
                // Draw tiles normally
                for (int row = 0; row < 4; row++)
                {
                    for (int col = 0; col < 4; col++)
                    {
                        int value = game.GetBoard()[row, col];
                        DrawTile(g, value, row, col, tileSize, startY, boardX);
                    }
                }
            }
            else
            {
                // Draw animating tiles
                bool[,] drawn = new bool[4, 4];
                // Draw moving tiles
                foreach (var move in animMoves)
                {
                    // Calculate position with easing
                    // Calculate position with bounds checking
                    // Use easing for position interpolation (fluid movement)
                    float px = Math.Max(0, Math.Min(BoardSize - 1, 
                        move.FromCol + (move.ToCol - move.FromCol) * animProgress));
                    float py = Math.Max(0, Math.Min(BoardSize - 1, 
                        move.FromRow + (move.ToRow - move.FromRow) * animProgress));
                    // Bounce effect will be calculated later
                    // Calculate smooth position without clamping to grid
                    float cellWidth = tileSize + TilePadding;
                    float cellHeight = tileSize + TilePadding;
                    
                    // Calculate exact position based on progress
                    int startX = boardX + (int)(move.FromCol * cellWidth) + TilePadding;
                    int startYPos = startY + (int)(move.FromRow * cellHeight) + TilePadding;
                    int endX = boardX + (int)(move.ToCol * cellWidth) + TilePadding;
                    int endY = startY + (int)(move.ToRow * cellHeight) + TilePadding;
                    
                    // Smooth ease in/out with overshoot
                    float t = Math.Min(1.0f, animProgress);
                    float easedT = EaseInOutCubic(t);
                    
                    // Calculate position with sliding effect
                    float slideProgress = Math.Min(1.0f, animProgress * 1.5f);
                    float slideEase = 1f - (float)Math.Pow(1f - slideProgress, 2f);
                    
                    // Apply sliding from direction
                    int x = (int)(startX + (endX - startX) * slideEase);
                    int y = (int)(startYPos + (endY - startYPos) * slideEase);
                    
                    // Store current slide offset for this tile
                    slideOffsets[move.ToRow, move.ToCol] = slideEase;
                    // Only apply merge animation to tiles that are actually merging
                    if (move.Merged)
                    {
                        // For merging tiles, show both movement and merge animation
                        if (animProgress > 0.5f)
                        {
                            // Calculate merge progress (0-1) for the second half of animation
                            float mergeProgress = (animProgress - 0.5f) * 2f;
                            float scale = 1.0f + (MergeScaleAmount - 1.0f) * (1.0f - (float)Math.Pow(1.0f - mergeProgress, 2));
                            float opacity = 1.0f - (1.0f - MergeFadeAmount) * (1.0f - (float)Math.Pow(1.0f - mergeProgress, 2));
                            
                            // Save graphics state
                            var state = g.Save();
                            
                            // Apply scale transform from center
                            g.TranslateTransform(x + tileSize / 2, y + tileSize / 2);
                            g.ScaleTransform(scale, scale);
                            g.TranslateTransform(-(x + tileSize / 2), -(y + tileSize / 2));
                            
                            // Draw the tile with fade effect
                            var colorMatrix = new System.Drawing.Imaging.ColorMatrix();
                            using (var attributes = new System.Drawing.Imaging.ImageAttributes())
                            {
                                colorMatrix.Matrix33 = opacity; // Set opacity
                                attributes.SetColorMatrix(colorMatrix);
                                // Draw merged tile with fade and scale
                                DrawTile(g, move.Value, -1, -1, tileSize, startY, boardX, x, y);
                            }
                            
                            // Restore graphics state
                            g.Restore(state);
                        }
                        else
                        {
                            // First half of merge - just show movement
                            DrawTile(g, move.Value, -1, -1, tileSize - 2 * TileInset, startY, boardX, x, y);
                        }
                    }
                    else
                    {
                        // For non-merging tiles, use smooth sliding
                        float slideT = slideOffsets[move.ToRow, move.ToCol];
                        int tileX = boardX + (int)(move.ToCol * (tileSize + TilePadding)) + TilePadding;
                        int tileY = startY + (int)(move.ToRow * (tileSize + TilePadding)) + TilePadding;
                        
                        // Draw the tile with sliding effect
                        DrawTile(g, move.Value, -1, -1, tileSize, startY, boardX, tileX, tileY);
                        
                        // Reset slide offset after animation
                        if (animProgress >= 1f) {
                            slideOffsets[move.ToRow, move.ToCol] = 0f;
                        }
                    }
                    drawn[move.ToRow, move.ToCol] = true;
                }
                // Draw static tiles from before move
                for (int row = 0; row < 4; row++)
                {
                    for (int col = 0; col < 4; col++)
                    {
                        if (drawn[row, col]) continue;
                        int value = animBoardBefore[row, col];
                        if (value > 0)
                            DrawTile(g, value, row, col, tileSize, startY, boardX);
                    }
                }
            }
        }

        private void DrawTile(Graphics g, int value, int row, int col, int size, int startY, int boardX, int overrideX = -1, int overrideY = -1)
        {
            if (value <= 0) return;
            
            int x, y;
            int drawSize = size;  // Use full size of the cell
            
            if (overrideX >= 0 && overrideY >= 0)
            {
                x = overrideX;  // No inset for overridden positions
                y = overrideY;
            }
            else
            {
                x = boardX + col * (size + TilePadding) + TilePadding;
                y = startY + row * (size + TilePadding) + TilePadding;
            }
            
            using (var brush = new SolidBrush(GetTileColor(value)))
            using (var path = RoundedRect(new RectangleF(x, y, drawSize, drawSize), 6))
            {
                g.FillPath(brush, path);
            }
            
            using (var font = new Font("Arial", value < 100 ? 32 : 24, FontStyle.Bold))
            {
                string text = value.ToString();
                var textSize = g.MeasureString(text, font);
                g.DrawString(text, font, value < 8 ? Brushes.Black : Brushes.White,
                    x + (drawSize - textSize.Width) / 2,
                    y + (drawSize - textSize.Height) / 2);
            }
        }

        private void MainForm_KeyDown(object? sender, KeyEventArgs e)
        {
            if (animMoves != null && animProgress < 1f) return; // ignore input during animation

            Game2048.Direction? direction = null;

            switch (e.KeyCode)
            {
                case Keys.Up:
                case Keys.W:
                    direction = Game2048.Direction.Up;
                    break;
                case Keys.Down:
                case Keys.S:
                    direction = Game2048.Direction.Down;
                    break;
                case Keys.Left:
                case Keys.A:
                    direction = Game2048.Direction.Left;
                    break;
                case Keys.Right:
                case Keys.D:
                    direction = Game2048.Direction.Right;
                    break;
            }

            if (direction.HasValue)
            {
                var moves = game.MoveWithTracking(direction.Value);
                if (moves.Count > 0)
                {
                    animMoves = moves;
                    animDirection = direction;
                    animProgress = 0f;
                    animBoardBefore = (int[,])game.GetBoard().Clone(); // snapshot before move for static tiles
                    animStartTime = DateTime.Now;
                    animTimer.Start();
                }
            }
        }

        private void MainForm_Resize(object? sender, EventArgs e)
        {
            // Force repaint when window is resized
            this.Invalidate();
        }

        private Color GetTileColor(int value)
        {
            switch (value)
            {
                case 0: return Color.FromArgb(205, 193, 180);
                case 2: return Color.FromArgb(238, 228, 218);
                case 4: return Color.FromArgb(237, 224, 200);
                case 8: return Color.FromArgb(242, 177, 121);
                case 16: return Color.FromArgb(245, 149, 99);
                case 32: return Color.FromArgb(246, 124, 95);
                case 64: return Color.FromArgb(246, 94, 59);
                case 128: return Color.FromArgb(237, 207, 114);
                case 256: return Color.FromArgb(237, 204, 97);
                case 512: return Color.FromArgb(237, 200, 80);
                case 1024: return Color.FromArgb(237, 197, 63);
                case 2048: return Color.FromArgb(237, 194, 46);
                default: return Color.FromArgb(60, 58, 50);
            }
        }
    }
}
