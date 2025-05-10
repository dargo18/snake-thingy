using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Java.Lang;
using System;
using System.Collections.Generic;
using System.Linq;

namespace snake
{
    public class SnakeView : View
    {
        private const int SnakeSize = 20;
        private const int GameWidth = 700;
        private const int GameHeight = 700;
        private const int TimerDuration = 20;  // Timer in seconds
        private List<Point> snake;
        private Point food;
        private Point direction;
        private bool gameOver;
        private Random rand;
        private Handler handler;
        private Runnable gameLoopRunnable;
        private Runnable timerLoopRunnable;
        private float startX, startY;
        private int score;
        private int topScore;
        private int timeRemaining;  // Time remaining in seconds
        private DatabaseService _dbService;
        private string username;
        private GestureDetector gestureDetector;  // Gesture detector for swipe handling

        public SnakeView(Context context, string username) : base(context)
        {
            this.username = username;
            _dbService = new DatabaseService();
            snake = new List<Point>
            {
                new Point(10, 10),
                new Point(10, 9),
                new Point(10, 8)
            };
            direction = new Point(0, 1);  // Start moving down
            rand = new Random();
            gameOver = false;
            score = 0;
            timeRemaining = TimerDuration;  // Set initial time

            handler = new Handler(Looper.MainLooper);
            gameLoopRunnable = new Runnable(GameLoop);
            timerLoopRunnable = new Runnable(TimerLoop);

            LoadTopScore();
            SpawnFood();
            StartGameLoop();
            StartTimerLoop();

            // Initialize GestureDetector
            gestureDetector = new GestureDetector(context, new SwipeGestureListener(this));
        }

        public SnakeView(Context context) : base(context)
        {
        }

        private async void LoadTopScore()
        {
            var user = await _dbService.GetUser(username);
            if (user != null)
            {
                topScore = user.Score;
            }
        }

        private async void SaveTopScore()
        {
            if (score > topScore)
            {
                topScore = score;
                await _dbService.UpdateUserScore(username, topScore);
            }
        }

        private void GameLoop()
        {
            if (gameOver)
            {
                SaveTopScore();
                return;
            }

            UpdateSnakePosition();
            PostInvalidate();
            handler.PostDelayed(gameLoopRunnable, 100);  // Snake moves every 100ms (fixed speed)
        }

        private void StartGameLoop()
        {
            handler.Post(gameLoopRunnable);
        }

        private void TimerLoop()
        {
            if (gameOver)
                return;

            if (timeRemaining > 0)
            {
                timeRemaining--;
            }
            else
            {
                gameOver = true;
            }
            PostInvalidate();
            handler.PostDelayed(timerLoopRunnable, 1000);  // Update the timer every 1 second
        }

        private void StartTimerLoop()
        {
            handler.Post(timerLoopRunnable);
        }

        private void UpdateSnakePosition()
        {
            var head = snake.First();
            var newHead = new Point(head.X + direction.X, head.Y + direction.Y);

            // Check if the new head position is out of bounds or if it collides with the snake's body (tail)
            if (newHead.X < 0 || newHead.X >= GameWidth / SnakeSize || newHead.Y < 0 || newHead.Y >= GameHeight / SnakeSize || snake.Contains(newHead))
            {
                gameOver = true;
                return;
            }

            // Add the new head to the front of the snake
            snake.Insert(0, newHead);

            // Check if the snake eats the food
            if (newHead.Equals(food))
            {
                // Add 10 seconds to the timer when the snake eats the food
                timeRemaining += 10;

                // Make sure the time doesn't exceed 30 seconds
                if (timeRemaining > 20)
                {
                    timeRemaining = 20;
                }

                score++;
                if (score > topScore)
                {
                    topScore = score;
                    _ = _dbService.UpdateUserScore(username, topScore);
                }
                SpawnFood();
            }
            else
            {
                // Remove the last segment of the snake (tail) to maintain its size
                snake.RemoveAt(snake.Count - 1);
            }
        }



        private void SpawnFood()
        {
            Point newFood;
            bool foodOnSnake;
            int maxAttempts = 100;
            int attempts = 0;

            do
            {
                newFood = new Point(rand.Next(0, GameWidth / SnakeSize), rand.Next(0, GameHeight / SnakeSize));
                foodOnSnake = snake.Contains(newFood);
                attempts++;
            }
            while (foodOnSnake && attempts < maxAttempts);

            food = attempts < maxAttempts ? newFood : new Point(0, 0);
        }

        // Handle swipe gestures to change direction
        public void OnSwipe(Direction direction)
        {
            if (gameOver) return;

            // Prevent the snake from turning 180 degrees
            if (this.direction.X == 0 && direction == Direction.Left || this.direction.X == 0 && direction == Direction.Right)
                this.direction = new Point(direction == Direction.Left ? -1 : 1, 0);
            else if (this.direction.Y == 0 && direction == Direction.Up || this.direction.Y == 0 && direction == Direction.Down)
                this.direction = new Point(0, direction == Direction.Up ? -1 : 1);
        }

        // Override the onTouchEvent to detect the gesture
        public override bool OnTouchEvent(MotionEvent e)
        {
            base.OnTouchEvent(e);
            gestureDetector.OnTouchEvent(e);
            return true;
        }

        protected override void OnDraw(Canvas canvas)
        {
            base.OnDraw(canvas);

            int centerX = (Width - GameWidth) / 2;
            int centerY = (Height - GameHeight) / 2;

            // Draw background
            Paint backgroundPaint = new Paint { Color = Color.LightGray };
            canvas.DrawRect(centerX, centerY, centerX + GameWidth, centerY + GameHeight, backgroundPaint);

            // Draw score and top score
            Paint scorePaint = new Paint { Color = Color.Black, TextSize = 50, TextAlign = Paint.Align.Center };
            canvas.DrawText($"Score: {score}  Top Score: {topScore}", Width / 2, 100, scorePaint);

            // Draw timer
            Paint timerPaint = new Paint { Color = Color.Black, TextSize = 50, TextAlign = Paint.Align.Center };
            canvas.DrawText($"Time: {timeRemaining}s", Width / 2, 150, timerPaint);

            // Draw snake
            Paint snakePaint = new Paint { Color = Color.Green };
            foreach (var segment in snake)
            {
                canvas.DrawRect(centerX + segment.X * SnakeSize, centerY + segment.Y * SnakeSize,
                                centerX + (segment.X + 1) * SnakeSize, centerY + (segment.Y + 1) * SnakeSize, snakePaint);
            }

            // Draw food
            Paint foodPaint = new Paint { Color = Color.Red };
            canvas.DrawRect(centerX + food.X * SnakeSize, centerY + food.Y * SnakeSize,
                            centerX + (food.X + 1) * SnakeSize, centerY + (food.Y + 1) * SnakeSize, foodPaint);

            // Draw Game Over text
            if (gameOver)
            {
                Paint gameOverPaint = new Paint { Color = Color.White, TextSize = 100, TextAlign = Paint.Align.Center };
                canvas.DrawText("Game Over", Width / 2, Height / 2, gameOverPaint);

            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }

    // Define swipe directions for better clarity
    public enum Direction
    {
        Up,
        Down,
        Left,
        Right
    }

    // GestureListener to handle swipe gestures
    public class SwipeGestureListener : GestureDetector.SimpleOnGestureListener
    {
        private SnakeView snakeView;

        public SwipeGestureListener(SnakeView snakeView)
        {
            this.snakeView = snakeView;
        }

        // Handle swipe gestures to detect the direction
        public override bool OnFling(MotionEvent e1, MotionEvent e2, float velocityX, float velocityY)
        {
            if (e1 == null || e2 == null)
                return false;

            float deltaX = e2.GetX() - e1.GetX();
            float deltaY = e2.GetY() - e1.GetY();

            // Determine the swipe direction based on deltaX and deltaY
            if (System.Math.Abs(deltaX) > System.Math.Abs(deltaY))
            {
                // Horizontal swipe
                if (deltaX > 0)
                    snakeView.OnSwipe(Direction.Right);  // Right swipe
                else
                    snakeView.OnSwipe(Direction.Left);  // Left swipe
            }
            else
            {
                // Vertical swipe
                if (deltaY > 0)
                    snakeView.OnSwipe(Direction.Down);  // Down swipe
                else
                    snakeView.OnSwipe(Direction.Up);  // Up swipe
            }

            return true;
        }

    }
}
