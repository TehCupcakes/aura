﻿// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using System;
using System.Net.Sockets;
using System.Threading;
using Aura.Channel.Network;
using Aura.Channel.Network.Handlers;
using Aura.Channel.Network.Sending;
using Aura.Channel.Util;
using Aura.Shared.Network;
using Aura.Shared.Util;
using Aura.Channel.World;

namespace Aura.Channel
{
	public class ChannelServer : ServerMain
	{
		public static readonly ChannelServer Instance = new ChannelServer();

		/// <summary>
		/// Milliseconds between connection tries.
		/// </summary>
		private const int LoginTryTime = 10 * 1000;

		private const int UpdateTime = 60 * 1000;

		private bool _running = false;

		private Timer _statusUpdateTimer;

		/// <summary>
		/// Instance of the actual server component.
		/// </summary>
		public DefaultServer<ChannelClient> Server { get; protected set; }

		/// <summary>
		/// List of servers and channels.
		/// </summary>
		public ServerInfoManager ServerList { get; private set; }

		/// <summary>
		/// Configuration
		/// </summary>
		public ChannelConf Conf { get; private set; }

		/// <summary>
		/// Client connecting to the login server.
		/// </summary>
		public ChannelClient LoginServer { get; private set; }

		private ChannelServer()
		{
			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

			this.Server = new DefaultServer<ChannelClient>();
			this.Server.Handlers = new ChannelServerHandlers();
			this.Server.Handlers.AutoLoad();
			this.Server.ClientDisconnected += this.OnClientDisconnected;

			this.ServerList = new ServerInfoManager();
		}

		/// <summary>
		/// Loads all necessary components and starts the server.
		/// </summary>
		public void Run()
		{
			if (_running)
				throw new Exception("Server is already running.");

			CliUtil.WriteHeader("Channel Server", ConsoleColor.Magenta);
			CliUtil.LoadingTitle();

			this.NavigateToRoot();

			// Conf
			this.LoadConf(this.Conf = new ChannelConf());

			// Database
			this.InitDatabase(this.Conf);

			// Data
			this.LoadData(DataLoad.ChannelServer, false);

			// Localization
			this.LoadLocalization(this.Conf);

			// World
			this.InitializeWorld();

			// Start
			this.Server.Start(this.Conf.Channel.ChannelPort);

			// Inter
			this.ConnectToLogin(true);
			this.StartStatusUpdateTimer();

			CliUtil.RunningTitle();
			_running = true;

			// Commands
			var commands = new ChannelConsoleCommands();
			commands.Wait();
		}

		/// <summary>
		/// Tries to connect to login server, keeps trying every 10 seconds
		/// till there is a success. Blocking.
		/// </summary>
		public void ConnectToLogin(bool firstTime)
		{
			if (this.LoginServer != null && this.LoginServer.State == ClientState.LoggedIn)
				throw new Exception("Channel already connected to login server.");

			Log.WriteLine();

			if (firstTime)
				Log.Info("Trying to connect to login server at {0}:{1}...", ChannelServer.Instance.Conf.Channel.LoginHost, ChannelServer.Instance.Conf.Channel.LoginPort);
			else
			{
				Log.Info("Trying to re-connect to login server in {0} seconds.", LoginTryTime / 1000);
				Thread.Sleep(LoginTryTime);
			}

			var success = false;
			while (!success)
			{
				try
				{
					if (this.LoginServer != null && this.LoginServer.State != ClientState.Dead)
						this.LoginServer.Kill();

					this.LoginServer = new ChannelClient();
					this.LoginServer.Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
					this.LoginServer.Socket.Connect(ChannelServer.Instance.Conf.Channel.LoginHost, ChannelServer.Instance.Conf.Channel.LoginPort);

					var buffer = new byte[255];

					// Recv Seed, send back empty packet to get done with the challenge.
					this.LoginServer.Socket.Receive(buffer);
					this.LoginServer.Crypto = new MabiCrypto(BitConverter.ToUInt32(buffer, 0));
					this.LoginServer.Send(Packet.Empty());

					// Challenge end
					this.LoginServer.Socket.Receive(buffer);

					// Inject login server intoto normal data receiving.
					this.Server.AddReceivingClient(this.LoginServer);

					// Identify
					this.LoginServer.State = ClientState.LoggingIn;

					success = true;

					Send.Internal_ServerIdentify();
				}
				catch (Exception ex)
				{
					Log.Error("Unable to connect to login server. ({0})", ex.Message);
					Log.Info("Trying again in {0} seconds.", LoginTryTime / 1000);
					Thread.Sleep(LoginTryTime);
				}
			}

			Log.Info("Connection to login server at '{0}' established.", this.LoginServer.Address);
			Log.WriteLine();
		}

		private void OnClientDisconnected(ChannelClient client)
		{
			if (client == this.LoginServer)
				this.ConnectToLogin(false);
		}

		/// <summary>
		/// Handler for unhandled exceptions.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			try
			{
				Log.Error("Oh no! Ferghus escaped his memory block and infected the rest of the server!");
				Log.Error("Aura has encountered an unexpected and unrecoverable error. We're going to try to save as much as we can.");
			}
			catch { }
			try
			{
				this.Server.Stop();
			}
			catch { }
			try
			{
				// save the world
			}
			catch { }
			try
			{
				Log.Exception((Exception)e.ExceptionObject);
				Log.Status("Closing server.");
			}
			catch { }

			CliUtil.Exit(1, false);
		}

		private void StartStatusUpdateTimer()
		{
			if (_statusUpdateTimer != null)
				return;

			_statusUpdateTimer = new Timer((_) =>
			{
				if (this.LoginServer == null || this.LoginServer.State != ClientState.LoggedIn)
					return;

				Send.Internal_ChannelStatus();
			}
			, null, UpdateTime, UpdateTime);
		}

		private void InitializeWorld()
		{
			Log.Info("Initilizing world...");

			WorldManager.Instance.Initialize();
			// Weather

			Log.Info("  done loading {0} regions.", WorldManager.Instance.Count);
		}
	}
}