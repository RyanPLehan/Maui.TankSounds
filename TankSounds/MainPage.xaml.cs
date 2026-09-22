using Plugin.Maui.Audio;

namespace TankSounds;

public partial class MainPage : ContentPage
{
	private const string StartupSound = "tank_start.wav";
	private const string ShutdownSound = "tank_stop.wav";
	private const string MoveSound = "tank_move.wav";
	private const string MachineGunSound = "machine_gun.wav";
	private const string MainGunSound = "main_gun.wav";

	private readonly IAudioManager _audioManager;
	private readonly Dictionary<string, IAudioPlayer> _players = [];
	private readonly Dictionary<string, Stream> _soundStreams = [];
	private bool _isLoadingSounds;
	private bool _soundsLoaded;
	private bool _tankStarted;

	public MainPage(IAudioManager audioManager)
	{
		InitializeComponent();
		_audioManager = audioManager;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		if (!_soundsLoaded && !_isLoadingSounds)
		{
			await LoadSoundsAsync();
		}
	}

	protected override void OnDisappearing()
	{
		StopAllPlayers();
		_tankStarted = false;
		UpdateControls();
		base.OnDisappearing();
	}

	private async Task LoadSoundsAsync()
	{
		_isLoadingSounds = true;

		try
		{
			await AddPlayerAsync(StartupSound);
			await AddPlayerAsync(ShutdownSound);
			await AddPlayerAsync(MoveSound);
			await AddPlayerAsync(MachineGunSound);
			await AddPlayerAsync(MainGunSound);

			_players[MoveSound].Loop = true;
			_players[MachineGunSound].Loop = true;
			_soundsLoaded = true;
			StatusLabel.Text = "Ready to start";
		}
		catch (Exception exception) when (
			exception is FileNotFoundException
			or IOException
			or UnauthorizedAccessException
			or InvalidOperationException
			or NotSupportedException)
		{
			StatusLabel.Text = "Sound system unavailable";
			await DisplayAlert("Unable to load sounds", exception.Message, "OK");
		}
		finally
		{
			_isLoadingSounds = false;
			UpdateControls();
		}
	}

	private async Task AddPlayerAsync(string fileName)
	{
		Stream stream = await FileSystem.OpenAppPackageFileAsync(fileName);
		_soundStreams.Add(fileName, stream);
		_players.Add(fileName, _audioManager.CreatePlayer(stream));
	}

	private void OnStartClicked(object sender, EventArgs e)
	{
		StopAllPlayers();
		_tankStarted = true;
		PlayFromBeginning(StartupSound);
		StatusLabel.Text = "Tank running";
		UpdateControls();
		SemanticScreenReader.Announce("Tank started. Sound controls enabled.");
	}

	private void OnStopClicked(object sender, EventArgs e)
	{
		StopAllPlayers();
		_tankStarted = false;
		PlayFromBeginning(ShutdownSound);
		StatusLabel.Text = "Tank stopped";
		UpdateControls();
		SemanticScreenReader.Announce("Tank stopped. Sound controls disabled.");
	}

	private void OnMoveClicked(object sender, EventArgs e)
	{
		if (_tankStarted)
		{
			PlayFromBeginning(MoveSound);
		}
	}

	private void OnStopMoveClicked(object sender, EventArgs e)
	{
		if (_tankStarted)
		{
			StopPlayer(MoveSound);
		}
	}

	private void OnMachineGunPressed(object sender, EventArgs e)
	{
		if (_tankStarted)
		{
			PlayFromBeginning(MachineGunSound);
			StatusLabel.Text = "Machine gun firing";
		}
	}

	private void OnMachineGunReleased(object sender, EventArgs e)
	{
		StopPlayer(MachineGunSound);

		if (_tankStarted)
		{
			StatusLabel.Text = "Tank running";
		}
	}

	private void OnMainGunClicked(object sender, EventArgs e)
	{
		if (_tankStarted)
		{
			PlayFromBeginning(MainGunSound);
		}
	}

	private void PlayFromBeginning(string sound)
	{
		IAudioPlayer player = _players[sound];
		player.Stop();
		player.Play();
	}

	private void StopPlayer(string sound)
	{
		if (_players.TryGetValue(sound, out IAudioPlayer? player))
		{
			player.Stop();
		}
	}

	private void StopAllPlayers()
	{
		foreach (IAudioPlayer player in _players.Values)
		{
			player.Stop();
		}
	}

	private void UpdateControls()
	{
		StartButton.IsEnabled = _soundsLoaded;
		StopButton.IsEnabled = _soundsLoaded && _tankStarted;
		MoveButton.IsEnabled = _soundsLoaded && _tankStarted;
		StopMoveButton.IsEnabled = _soundsLoaded && _tankStarted;
		MachineGunButton.IsEnabled = _soundsLoaded && _tankStarted;
		MainGunButton.IsEnabled = _soundsLoaded && _tankStarted;
		StatusLight.BackgroundColor = _tankStarted ? Color.FromArgb("#8CAA45") : Color.FromArgb("#8B3A2E");
	}
}
