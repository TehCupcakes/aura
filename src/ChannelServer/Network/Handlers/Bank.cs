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

namespace Aura.Channel.Network.Handlers
{
	public partial class ChannelServerHandlers : PacketHandlerManager<ChannelClient>
	{
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
	}
}
