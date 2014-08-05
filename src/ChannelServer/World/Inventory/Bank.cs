// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see licence.txt in the main folder

using System;
using System.Collections.Generic;
using System.Linq;
using Aura.Channel.Network.Sending;
using Aura.Channel.World.Entities;
using Aura.Data.Database;
using Aura.Shared.Mabi.Const;
using Aura.Shared.Util;
using Aura.Channel.Database;

namespace Aura.Channel.World
{
	/// <summary>
	/// Inventory for players
	/// </summary>
	/// <remarks>
	/// TODO: I'm dirty and unsafe, clean me up.
	/// </remarks>
	public class Bank
	{
		private const int DefaultWidth = 12;
		private const int DefaultHeight = 8;
		private const int MaxWidth = 32;
		private const int MaxHeight = 32;

		private Creature _creature;
		private Dictionary<Pocket, InventoryPocket> _pockets;
		public DateTime LastOpened { get; set; }
		public long Id { get; set; }
		public byte Assistant { get; set; }
		public int Gold { get; set; }
		public int Height { get; set; }
		public int Width { get; set; }
		public bool Locked { get; set; }
		public string Location { get; protected set; }
		public int Exists { get; protected set;  }

		public Bank(Creature creature, string location)
		{
			_creature = creature;
			_pockets = new Dictionary<Pocket, InventoryPocket>();
			LastOpened = DateTime.Now;
			Assistant = 0;
			Gold = 0;
			Height = DefaultHeight;
			Width = DefaultWidth;
			Location = location;

			// Bank reference needs to be stored in client so it can
			// be close packet is received.
			creature.Client.OpenBank = this;

			// Is this relevant for bank?
			//this.Add(new InventoryPocketStack(Pocket.Temporary));
			//this.Add(new InventoryPocketStack(Pocket.Quests));
			//this.Add(new InventoryPocketSingle(Pocket.Cursor));
			
			Exists = ChannelDb.Instance.LoadBank(creature);
		}

		/// <summary>
		/// Deposits a given amount of gold from the bank.
		/// </summary>
		/// <param name="amount"></param>
		/// <returns></returns>
		public bool Deposit(int amount)
		{
			if (_creature.Inventory.Gold < amount)
				return false;

			var success = _creature.Inventory.RemoveGold(amount);
			if (success)
			{
				Gold += amount;
				
				// 10% depositing fee for non-GMs
				//if (_creature.Client.Account.Authority < 50)
				//Gold -= amount / 10;

				Send.BankGoldSet(_creature, Gold);
			}

			return success;
		}

		/// <summary>
		/// Withdraws a given amount of gold from the bank.
		/// </summary>
		/// <param name="amount"></param>
		/// <returns></returns>
		public bool Withdraw(int amount)
		{
			if (Gold < amount)
				return false;

			_creature.Inventory.AddGold(amount);
			Gold -= amount;
			Send.BankGoldSet(_creature, Gold);

			return true;
		}

		/// <summary>
		/// Converts bank location to a displayable string.
		/// </summary>
		/// <param name="location"></param>
		public string LocDisplayName(string location)
		{
			switch (location)
			{
				case "TirChonaillBank":
					return "Tir Chonaill Bank";

				case "DunbartonBank":
					return "Dunbarton Bank";

				case "IriaConnousBank":
					return "Connous Filia Bank";

				default:
					return "";
			}
		}
	}
}
