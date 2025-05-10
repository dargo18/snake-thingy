using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using AndroidX.Core.App;
using Java.Util;
using System.Threading.Tasks;

namespace snake
{
    [Activity(Label = "SnakeGame", MainLauncher = true)]
    public class MainActivity : Activity
    {
        private SnakeView snakeView;
        private DatabaseService _dbService;
        private EditText _usernameEntry;
        private EditText _passwordEntry;
        private Button _loginButton;
        private Button _registerButton;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            // Initialize DatabaseService and UI components
            _dbService = new DatabaseService();

            _usernameEntry = FindViewById<EditText>(Resource.Id.usernameEntry);
            _passwordEntry = FindViewById<EditText>(Resource.Id.passwordEntry);
            _loginButton = FindViewById<Button>(Resource.Id.loginButton);
            _registerButton = FindViewById<Button>(Resource.Id.registerButton);

            _loginButton.Click += async (sender, e) => await OnLoginClicked();
            _registerButton.Click += async (sender, e) => await OnRegisterClicked();

            // Set up the daily reminder
        }

        private async Task OnLoginClicked()
        {
            var user = await _dbService.LoginUser(_usernameEntry.Text, _passwordEntry.Text);
            if (user != null)
            {
                Toast.MakeText(this, "Login Success! Welcome " + user.Username, ToastLength.Short).Show();

                // Set the SnakeView as the main view after login
                RunOnUiThread(() => SetContentView(new SnakeView(this, user.Username)));
            }
            else
            {
                Toast.MakeText(this, "Login Failed!", ToastLength.Short).Show();
            }
        }

        private async Task OnRegisterClicked()
        {
            StartActivity(typeof(RegisterActivity));
        }

        private void StartGame(string username)
        {
            snakeView = new SnakeView(this, username);
            SetContentView(snakeView);
        }

        // Function to set up the daily reminder at 8:00 AM
        private void ShowNotification(string title, string message, Context context)
{
    string channelId = "game_channel_id";

    var notificationManager = (NotificationManager)context.GetSystemService(Context.NotificationService);

    if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
    {
        var channel = new NotificationChannel(channelId, "Game Channel", NotificationImportance.Default)
        {
            Description = "Notifications from Snake Game"
        };
        notificationManager.CreateNotificationChannel(channel);
    }

    var builder = new NotificationCompat.Builder(context: context, channelId)
        .SetContentTitle(title)
        .SetContentText(message)
        .SetAutoCancel(true)
        .SetPriority((int)NotificationPriority.Default);

    notificationManager.Notify(1001, builder.Build());
}



    }
}
