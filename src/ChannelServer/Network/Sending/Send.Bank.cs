﻿// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using System;
using Aura.Channel.World.Entities;
using Aura.Shared.Mabi.Const;
using Aura.Shared.Network;
using Aura.Shared.Util;
using Aura.Channel.World.Entities.Creatures;
using System.Globalization;
using Aura.Channel.World;

namespace Aura.Channel.Network.Sending
{
	public static partial class Send
	{
		/// <summary>
		/// Updates the amount of gold in the bank.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="amount"></param>
		public static void BankGoldSet(Creature creature, int amount)
		{
			var bank = creature.Client.OpenBank;
			var packet = new Packet(Op.BankGoldSet, creature.EntityId);
			packet.PutInt(bank.Gold);

			creature.Client.Send(packet);
		}

		/// <summary>
		/// Response afer depositing gold.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="success"></param>
		public static void BankDepositR(Creature creature, bool success)
		{
			var packet = new Packet(Op.BankDepositR, creature.EntityId);
			packet.PutByte(success);
			creature.Client.Send(packet);
		}

		/// <summary>
		/// Response afer withdrawing gold.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="success"></param>
		public static void BankWithdrawR(Creature creature, bool success)
		{
			var packet = new Packet(Op.BankWithdrawR, creature.EntityId);
			packet.PutByte(success);
			creature.Client.Send(packet);
		}

		/// <summary>
		/// Sends OpenBank to creature's client.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="item"></param>
		/// <param name="source"></param>
		public static void OpenBank(Creature creature, string location)
		{
			var bank = creature.Client.OpenBank;
			var packet = new Packet(Op.OpenBank, creature.EntityId);
			packet.PutByte(1); // ?
			packet.PutByte(bank.Assistant);
			packet.PutLong(bank.LastOpened);
			packet.PutByte(0); // ?
			packet.PutString(creature.Client.Account.Id);
			packet.PutString(location);
			packet.PutString(bank.LocDisplayName(location));
			packet.PutInt(bank.Gold);
			packet.PutInt(bank.Exists);
			if (bank.Exists == 1)
			{
				packet.PutString(creature.Name);
				packet.PutByte(bank.Assistant);
				packet.PutInt(12); // Default width of bank inventory (= 12 without Inventory Plus)
				packet.PutInt(bank.Height);
				packet.PutInt(bank.Width);
			}

			creature.Client.Send(packet);
		}

		/// <summary>
		/// Response after closing bank.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="success"></param>
		public static void CloseBankR(Creature creature, bool success)
		{
			var packet = new Packet(Op.CloseBankR, creature.EntityId);
			packet.PutByte(success);
			creature.Client.Send(packet);
		}
	}
}