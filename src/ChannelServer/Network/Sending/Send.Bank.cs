// Copyright (c) Aura development team - Licensed under GNU GPL
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
		public static void OpenBank(Creature creature, Bank bank, byte assistant = 0, byte msg = 1)
		{
			var packet = new Packet(Op.OpenBank, creature.EntityId);
			packet.PutByte(msg);
			if (msg == 51)	// Bank is locked; this packet will tell it to check pass
			{
				packet.PutByte(1);
			}

			if (msg == 1)	// Default when unlocked. Go ahead and open the bank
			{
				packet.PutByte(assistant);
				packet.PutLong(bank.LastOpened.Ticks);
				packet.PutByte(bank.Locked);
				packet.PutString(creature.Client.Account.Id);
				packet.PutString(bank.Location);
				packet.PutString(bank.LocDisplayName(bank.Location));
				packet.PutInt(bank.Gold);
				// This is wrong... Apparently there is a PlayerCreature Id on each account.
				// Your first PlayerCreature will be 1, second will be 2, etc... If the char
				// Does not exist (when checking assistant char box) it sends 0. That's why
				// I used bank.Exists.
				packet.PutInt(bank.Exists);
				// TODO: Replace bank.Exists; Instead, it should get banks for all
				// PlayerCreatures besides assistant chars and add these values for each
				if (bank.Exists == 1)
				{
					packet.PutString(creature.Name);
					packet.PutByte(bank.Assistant);
					packet.PutInt(bank.Width);
					packet.PutInt(bank.Height);
					packet.PutInt(0); // Number of item pockets in use
				}
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

		/// <summary>
		/// Response after attempting to change lock.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="result"></param>
		public static void LockBankR(Creature creature, byte result)
		{
			byte success = 51; // Send error message id if unsuccessful
			if (result == 1 || result == 0)
				success = 1;

			var packet = new Packet(Op.LockBankR, creature.EntityId);
			packet.PutByte(success);
			packet.PutByte(result);
			creature.Client.Send(packet);
		}

		/// <summary>
		/// Response after attempting to open a locked bank.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="result"></param>
		public static void BankLockCheckR(Creature creature, bool success, bool terminate = false)
		{
			var packet = new Packet(Op.BankLockCheckR, creature.EntityId);
			packet.PutByte(success);
			if (!success)
				packet.PutByte(terminate);

			creature.Client.Send(packet);
		}
	}
}