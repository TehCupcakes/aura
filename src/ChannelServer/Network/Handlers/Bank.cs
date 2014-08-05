// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aura.Shared.Network;
using Aura.Channel.Network.Sending;
using Aura.Shared.Util;
using Aura.Shared.Mabi.Const;
using Aura.Channel.World.Entities.Creatures;
using Aura.Channel.Database;
using Aura.Channel.World;

namespace Aura.Channel.Network.Handlers
{
	public partial class ChannelServerHandlers : PacketHandlerManager<ChannelClient>
	{
		/// <summary>
		/// Loads an assistant character's bank.
		/// </summary>
		/// <param name="client"></param>
		/// <param name="packet"></param>
		[PacketHandler(Op.BankAssistantLoad)]
		public void BankAssistantLoad(ChannelClient client, Packet packet)
		{
			var assistantId = packet.GetByte();

			var creature = client.GetCreature(packet.Id);
			if (creature == null)
				return;

			// Assistant's not implemented... Should look something like this
			// var bank = new Bank(creature.GetAssistant(assistantId), creature.Client.OpenBank.Location)

			// Temporary; prevents freeze on checking assistant box
			var bank = new Bank(creature, creature.Client.OpenBank.Location);

			Send.OpenBank(creature, bank, assistantId);
		}

		/// <summary>
		/// Withdraw gold in the bank.
		/// </summary>
		/// <param name="client"></param>
		/// <param name="packet"></param>
		/// <example>
		/// 0001 [........00000064] Int    : 100
		/// </example>
		[PacketHandler(Op.BankDeposit)]
		public void BankDeposit(ChannelClient client, Packet packet)
		{
			var amount = packet.GetInt();

			var creature = client.GetCreature(packet.Id);
			if (creature == null)
				return;

			var success = creature.Client.OpenBank.Deposit(amount);

			Send.BankDepositR(creature, success);
		}

		/// <summary>
		/// Withdraw gold from the bank.
		/// </summary>
		/// <param name="client"></param>
		/// <param name="packet"></param>
		/// <example>
		/// 0001 [..............00] Byte   : 0
		/// 0002 [........00000064] Int    : 100
		/// </example>
		[PacketHandler(Op.BankWithdraw)]
		public void BankWithdraw(ChannelClient client, Packet packet)
		{
			var val = packet.GetByte(); // Always 0?
			var amount = packet.GetInt();

			var creature = client.GetCreature(packet.Id);
			if (creature == null)
				return;

			var success = creature.Client.OpenBank.Withdraw(amount);

			Send.BankWithdrawR(creature, success);
		}

		/// <summary>
		/// Sent when closing the bank dialog.
		/// </summary>
		/// <example>
		/// 0001 [..............00] Byte    : 0
		/// </example>
		[PacketHandler(Op.CloseBank)]
		public void CloseBank(ChannelClient client, Packet packet)
		{
			var val = packet.GetByte(); // Always 0?

			var creature = client.GetCreature(packet.Id);
			if (creature == null)
				return;

			var success = ChannelDb.Instance.SaveBank(creature);
			creature.Client.OpenBank = null;

			Send.CloseBankR(creature, success);
		}

		/// <summary>
		/// Attempt to change bank lock password.
		/// </summary>
		/// <param name="client"></param>
		/// <param name="packet"></param>
		[PacketHandler(Op.LockBank)]
		public void LockBank(ChannelClient client, Packet packet)
		{
			var oldLock = packet.GetString();
			var newLock = packet.GetString();

			var creature = client.GetCreature(packet.Id);
			if (creature == null)
				return;

			byte result = ChannelDb.Instance.ChangeBankLock(creature, oldLock, newLock);

			Send.LockBankR(creature, result);
		}

		private static int failedAttempts = 0;

		/// <summary>
		/// Checks if pass matched bank's lock.
		/// </summary>
		/// <param name="client"></param>
		/// <param name="packet"></param>
		[PacketHandler(Op.BankLockCheck)]
		public void BankLockCheck(ChannelClient client, Packet packet)
		{
			var creature = client.GetCreature(packet.Id);
			if (creature == null)
				return;

			var success = false;
			// You get three tries before dialog closes
			success = ChannelDb.Instance.CheckBankLock(creature, packet.GetString());
			if (success)
			{
				Send.OpenBank(creature, client.OpenBank, client.OpenBank.Assistant);
				failedAttempts = 0;
			}
			
			if (failedAttempts < 2)
			{
				Send.BankLockCheckR(creature, success, false);
				failedAttempts++;
			}
			else
			{
				Send.BankLockCheckR(creature, success, true);
				failedAttempts = 0;
			}
		}
	}
}
